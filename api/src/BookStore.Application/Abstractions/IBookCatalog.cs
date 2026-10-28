namespace BookStore.Application.Abstractions;

/// <summary>A transient search hit from the external catalog. Never persisted unless the user selects it.</summary>
public sealed record CatalogBook(
    string OpenLibraryKey,
    string Title,
    string Author,
    int? PublishedYear,
    string? Isbn,
    string? CoverUrl);

public sealed record CatalogSearchResult(IReadOnlyList<CatalogBook> Items, int TotalFound);

/// <summary>Port for book discovery. Implementations must throw <c>CatalogUnavailableException</c> on failure.</summary>
public interface IBookCatalog
{
    Task<CatalogSearchResult> SearchAsync(string term, CancellationToken ct);
}
