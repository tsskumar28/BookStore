using BookStore.Application.Abstractions.Messaging;
using BookStore.Application.Features.Catalog.SearchCatalog;

namespace BookStore.Api.Endpoints;

public static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/catalog/search", (IMediator m, string? q, CancellationToken ct) =>
            m.Send(new SearchCatalogQuery(q), ct))
            .WithTags("Catalog")
            .WithSummary("Search Open Library (proxied); results are transient and not persisted.");
    }
}
