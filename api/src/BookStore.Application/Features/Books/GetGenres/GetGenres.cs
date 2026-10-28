using BookStore.Application.Abstractions;
using BookStore.Application.Abstractions.Messaging;
using BookStore.Domain.Books;

namespace BookStore.Application.Features.Books.GetGenres;

public sealed record GenreSummaryDto(Genre Genre, int Count);

public sealed record GetGenresQuery : IRequest<IReadOnlyList<GenreSummaryDto>>;

/// <summary>Every supported genre with its book count (zero when empty), so the filter list is always complete.</summary>
public sealed class GetGenresHandler(IBookRepository books)
    : IRequestHandler<GetGenresQuery, IReadOnlyList<GenreSummaryDto>>
{
    public async Task<IReadOnlyList<GenreSummaryDto>> Handle(GetGenresQuery q, CancellationToken ct)
    {
        var counts = (await books.GetGenreCountsAsync(ct)).ToDictionary(c => c.Genre, c => c.Count);
        return Enum.GetValues<Genre>()
            .Select(g => new GenreSummaryDto(g, counts.GetValueOrDefault(g)))
            .ToList();
    }
}
