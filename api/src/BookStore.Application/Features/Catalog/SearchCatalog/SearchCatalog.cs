using BookStore.Application.Abstractions;
using BookStore.Application.Abstractions.Messaging;

namespace BookStore.Application.Features.Catalog.SearchCatalog;

public sealed record SearchCatalogQuery(string? Term) : IRequest<CatalogSearchResult>;

public sealed class SearchCatalogValidator : IValidator<SearchCatalogQuery>
{
    public IEnumerable<ValidationError> Validate(SearchCatalogQuery q)
    {
        if (string.IsNullOrWhiteSpace(q.Term))
            yield return new("q", "A search term is required.");
        else if (q.Term.Trim().Length is < 2 or > 200)
            yield return new("q", "Search term must be between 2 and 200 characters.");
    }
}

/// <summary>Results are transient: nothing is written to the store here.</summary>
public sealed class SearchCatalogHandler(IBookCatalog catalog) : IRequestHandler<SearchCatalogQuery, CatalogSearchResult>
{
    public Task<CatalogSearchResult> Handle(SearchCatalogQuery q, CancellationToken ct) =>
        catalog.SearchAsync(q.Term!.Trim(), ct);
}
