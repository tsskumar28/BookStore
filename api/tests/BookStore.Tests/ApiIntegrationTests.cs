using System.Net;
using System.Net.Http.Json;
using BookStore.Application.Abstractions;
using BookStore.Application.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BookStore.Tests;

public sealed class ApiIntegrationTests : IClassFixture<ApiIntegrationTests.Factory>
{
    public sealed class FakeCatalog : IBookCatalog
    {
        public bool Fail;
        public Task<CatalogSearchResult> SearchAsync(string term, CancellationToken ct) =>
            Fail
                ? throw new CatalogUnavailableException("down")
                : Task.FromResult(new CatalogSearchResult(
                    [new CatalogBook("/works/OLTEST1W", "Test Book", "Tester", 2001, "9781234567897", null)], 1));
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"bookstore-test-{Guid.NewGuid():N}.db");
        public FakeCatalog Catalog { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
            builder.ConfigureTestServices(s =>
            {
                s.RemoveAll<IBookCatalog>();
                s.AddSingleton<IBookCatalog>(Catalog);
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { File.Delete(_dbPath); } catch (IOException) { }
        }
    }

    private readonly Factory _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(Factory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Search_is_transient_then_select_and_persist_then_duplicate_is_409()
    {
        var before = await _client.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/books?pageSize=100");
        var countBefore = before.GetProperty("totalCount").GetInt32();

        var search = await _client.GetAsync("/api/catalog/search?q=test");
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);

        var afterSearch = await _client.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/books?pageSize=100");
        Assert.Equal(countBefore, afterSearch.GetProperty("totalCount").GetInt32());

        var body = new
        {
            title = "Test Book", author = "Tester", isbn = "9781234567897", publishedYear = 2001,
            openLibraryKey = "/works/OLTEST1W", genre = "Mystery", price = 9.99, stock = 2
        };
        var created = await _client.PostAsJsonAsync("/api/books", body);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var duplicate = await _client.PostAsJsonAsync("/api/books", body);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var filtered = await _client.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/books?genre=Mystery&pageSize=100");
        Assert.Contains(filtered.GetProperty("items").EnumerateArray(),
            b => b.GetProperty("title").GetString() == "Test Book");
    }

    [Fact]
    public async Task Invalid_create_returns_400_problem_details()
    {
        var response = await _client.PostAsJsonAsync("/api/books", new
        {
            title = "", author = "A", openLibraryKey = "/works/X", genre = "Fiction", price = -5, stock = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.True(problem.GetProperty("errors").TryGetProperty("Title", out _));
    }

    [Fact]
    public async Task Search_returns_502_when_catalog_is_down_and_400_without_term()
    {
        _factory.Catalog.Fail = true;
        try
        {
            Assert.Equal(HttpStatusCode.BadGateway, (await _client.GetAsync("/api/catalog/search?q=anything")).StatusCode);
        }
        finally { _factory.Catalog.Fail = false; }

        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/catalog/search")).StatusCode);
    }

    [Fact]
    public async Task Missing_book_returns_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/books/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Response_carries_correlation_id()
    {
        var response = await _client.GetAsync("/api/books/genres");
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
    }
}
