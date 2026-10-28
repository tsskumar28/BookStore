using BookStore.Application.Abstractions;
using BookStore.Application.Abstractions.Messaging;
using BookStore.Application.Common;
using BookStore.Domain.Books;

namespace BookStore.Application.Features.Books.GetBooks;

public sealed record GetBooksQuery(
    Genre? Genre = null,
    int Page = 1,
    int PageSize = 10,
    BookSortField SortBy = BookSortField.Title,
    bool Descending = false) : IRequest<PagedResult<BookDto>>;

public sealed class GetBooksValidator : IValidator<GetBooksQuery>
{
    public const int MaxPageSize = 100;

    public IEnumerable<ValidationError> Validate(GetBooksQuery q)
    {
        if (q.Genre is { } g && !Enum.IsDefined(g))
            yield return new(nameof(q.Genre), "Unknown genre.");
        if (q.Page < 1)
            yield return new(nameof(q.Page), "Page must be 1 or greater.");
        if (q.PageSize is < 1 or > MaxPageSize)
            yield return new(nameof(q.PageSize), $"PageSize must be between 1 and {MaxPageSize}.");
        if (!Enum.IsDefined(q.SortBy))
            yield return new(nameof(q.SortBy), "Unknown sort field.");
    }
}

public sealed class GetBooksHandler(IBookRepository books) : IRequestHandler<GetBooksQuery, PagedResult<BookDto>>
{
    public async Task<PagedResult<BookDto>> Handle(GetBooksQuery q, CancellationToken ct)
    {
        var (items, total) = await books.ListAsync(
            new BookListCriteria(q.Genre, q.Page, q.PageSize, q.SortBy, q.Descending), ct);
        return new PagedResult<BookDto>(items.Select(BookDto.From).ToList(), q.Page, q.PageSize, total);
    }
}
