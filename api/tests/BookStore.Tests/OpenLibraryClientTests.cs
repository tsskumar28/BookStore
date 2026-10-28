using System.Net;
using System.Text;
using BookStore.Application.Common;
using BookStore.Infrastructure.OpenLibrary;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BookStore.Tests;

public class OpenLibraryClientTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            return send(request);
        }
    }

    private static OpenLibraryClient Client(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://openlibrary.org/") },
            Options.Create(new OpenLibraryOptions()), NullLogger<OpenLibraryClient>.Instance);

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task Normalizes_results_and_derives_cover_urls()
    {
        var handler = new StubHandler(_ => Task.FromResult(Json("""
            {"numFound": 3, "docs": [
              {"key":"/works/OL1W","title":"Dune","author_name":["Frank Herbert","Other"],"first_publish_year":1965,"isbn":["978","979"],"cover_i":42},
              {"key":"/works/OL2W","title":"No Cover Id","isbn":["123"]},
              {"title":"No key - dropped"}
            ]}
            """)));

        var result = await Client(handler).SearchAsync("dune & co", default);

        Assert.Equal(3, result.TotalFound);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Frank Herbert, Other", result.Items[0].Author);
        Assert.Equal("978", result.Items[0].Isbn);
        Assert.Equal("https://covers.openlibrary.org/b/id/42-M.jpg", result.Items[0].CoverUrl);
        Assert.Equal("https://covers.openlibrary.org/b/isbn/123-M.jpg", result.Items[1].CoverUrl);
        Assert.Equal("Unknown author", result.Items[1].Author);
        Assert.Contains("q=dune%20%26%20co", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task Upstream_error_becomes_catalog_unavailable()
    {
        var handler = new StubHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        await Assert.ThrowsAsync<CatalogUnavailableException>(() => Client(handler).SearchAsync("x1", default));
    }

    [Fact]
    public async Task Timeout_becomes_catalog_unavailable()
    {
        var handler = new StubHandler(_ => throw new TaskCanceledException("timeout"));
        await Assert.ThrowsAsync<CatalogUnavailableException>(() => Client(handler).SearchAsync("x1", default));
    }

    [Fact]
    public async Task Garbage_payload_becomes_catalog_unavailable()
    {
        var handler = new StubHandler(_ => Task.FromResult(Json("<html>nope</html>")));
        await Assert.ThrowsAsync<CatalogUnavailableException>(() => Client(handler).SearchAsync("x1", default));
    }
}
