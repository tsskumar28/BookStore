using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BookStore.Application.Abstractions;
using BookStore.Application.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookStore.Infrastructure.OpenLibrary;

/// <summary>
/// Adapter for the Open Library search API. Everything external (URLs, field names, failure modes)
/// stays in here; callers only see <see cref="IBookCatalog"/> and <see cref="CatalogUnavailableException"/>.
/// </summary>
internal sealed class OpenLibraryClient(
    HttpClient http,
    IOptions<OpenLibraryOptions> options,
    ILogger<OpenLibraryClient> logger) : IBookCatalog
{
    private const string Fields = "key,title,author_name,first_publish_year,isbn,cover_i";
    private readonly OpenLibraryOptions _options = options.Value;

    public async Task<CatalogSearchResult> SearchAsync(string term, CancellationToken ct)
    {
        var url = $"search.json?q={Uri.EscapeDataString(term)}&fields={Fields}&limit={_options.ResultLimit}";

        try
        {
            logger.LogInformation("Searching Open Library for {SearchTerm}", term);
            var response = await http.GetFromJsonAsync<SearchResponse>(url, ct)
                ?? throw new CatalogUnavailableException("Open Library returned an empty response.");

            var items = (response.Docs ?? [])
                .Where(d => !string.IsNullOrWhiteSpace(d.Key) && !string.IsNullOrWhiteSpace(d.Title))
                .Select(Map)
                .ToList();

            logger.LogInformation("Open Library returned {ResultCount} of {TotalFound} results",
                items.Count, response.NumFound);
            return new CatalogSearchResult(items, response.NumFound);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Open Library search timed out after {TimeoutSeconds}s", _options.TimeoutSeconds);
            throw new CatalogUnavailableException("Open Library timed out. Please try again.");
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException or NotSupportedException)
        {
            logger.LogWarning(ex, "Open Library search failed");
            throw new CatalogUnavailableException("Open Library is currently unavailable.", ex);
        }
    }

    private CatalogBook Map(SearchDoc d)
    {
        var isbn = d.Isbn?.FirstOrDefault(i => !string.IsNullOrWhiteSpace(i));
        var cover = d.CoverId is { } id
            ? $"{CoversBase}b/id/{id}-M.jpg"
            : isbn is not null ? $"{CoversBase}b/isbn/{isbn}-M.jpg" : null;

        var authors = d.AuthorName is { Count: > 0 } ? string.Join(", ", d.AuthorName) : "Unknown author";
        return new CatalogBook(d.Key!, d.Title!, authors, d.FirstPublishYear, isbn, cover);
    }

    private string CoversBase => _options.CoversBaseUrl.TrimEnd('/') + "/";

    private sealed record SearchResponse(
        [property: JsonPropertyName("numFound")] int NumFound,
        [property: JsonPropertyName("docs")] List<SearchDoc>? Docs);

    private sealed record SearchDoc(
        [property: JsonPropertyName("key")] string? Key,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("author_name")] List<string>? AuthorName,
        [property: JsonPropertyName("first_publish_year")] int? FirstPublishYear,
        [property: JsonPropertyName("isbn")] List<string>? Isbn,
        [property: JsonPropertyName("cover_i")] long? CoverId);
}
