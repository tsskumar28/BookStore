using BookStore.Domain.Books;

namespace BookStore.Application.Abstractions;

public enum BookSortField
{
    Title,
    Author,
    Genre,
    Price,
    Stock,
    PublishedYear
}

public sealed record BookListCriteria(
    Genre? Genre,
    int Page,
    int PageSize,
    BookSortField SortBy,
    bool Descending);

public sealed record GenreCount(Genre Genre, int Count);

/// <summary>Port for the store's own persistence (SQLite is just the current adapter).</summary>
public interface IBookRepository
{
    Task<(IReadOnlyList<Book> Items, int TotalCount)> ListAsync(BookListCriteria criteria, CancellationToken ct);
    Task<Book?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsAsync(string? isbn, string openLibraryKey, CancellationToken ct);
    Task AddAsync(Book book, CancellationToken ct);
    Task UpdateAsync(Book book, CancellationToken ct);
    Task DeleteAsync(Book book, CancellationToken ct);

    /// <summary>Books per genre, via a hand-written SQL query (also powers the Header filter).</summary>
    Task<IReadOnlyList<GenreCount>> GetGenreCountsAsync(CancellationToken ct);
}
