using BookStore.Application.Abstractions;
using BookStore.Application.Common;
using BookStore.Domain.Books;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Persistence;

internal sealed class BookRepository(BookStoreDbContext db) : IBookRepository
{
    public async Task<(IReadOnlyList<Book> Items, int TotalCount)> ListAsync(
        BookListCriteria criteria, CancellationToken ct)
    {
        IQueryable<Book> query = db.Books.AsNoTracking();
        if (criteria.Genre is { } genre)
            query = query.Where(b => b.Genre == genre);

        var total = await query.CountAsync(ct);

        var items = await Sort(query, criteria)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    private static IQueryable<Book> Sort(IQueryable<Book> query, BookListCriteria c)
    {
        var desc = c.Descending;
        IOrderedQueryable<Book> ordered = c.SortBy switch
        {
            BookSortField.Author => Order(query, b => EF.Functions.Collate(b.Author, "NOCASE"), desc),
            BookSortField.Genre => Order(query, b => b.Genre, desc),
            BookSortField.Price => Order(query, b => b.Price, desc),
            BookSortField.Stock => Order(query, b => b.Stock, desc),
            BookSortField.PublishedYear => Order(query, b => b.PublishedYear, desc),
            _ => Order(query, b => EF.Functions.Collate(b.Title, "NOCASE"), desc)
        };

        // Tie-breaker keeps paging stable when the sort column has duplicates.
        return ordered.ThenBy(b => b.Id);
    }

    private static IOrderedQueryable<Book> Order<TKey>(
        IQueryable<Book> query, System.Linq.Expressions.Expression<Func<Book, TKey>> key, bool desc) =>
        desc ? query.OrderByDescending(key) : query.OrderBy(key);

    public Task<Book?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Books.FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<bool> ExistsAsync(string? isbn, string openLibraryKey, CancellationToken ct) =>
        db.Books.AnyAsync(b => b.OpenLibraryKey == openLibraryKey || (isbn != null && b.Isbn == isbn), ct);

    public async Task AddAsync(Book book, CancellationToken ct)
    {
        db.Books.Add(book);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.Sqlite.SqliteException { SqliteErrorCode: 19 })
        {
            // Lost a race against a concurrent insert of the same ISBN / key: the unique index is the source of truth.
            throw new ConflictException("This book is already in the store (same ISBN or Open Library key).");
        }
    }

    public Task UpdateAsync(Book book, CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task DeleteAsync(Book book, CancellationToken ct)
    {
        db.Books.Remove(book);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Hand-written SQL: books grouped by genre with a count per genre.</summary>
    public async Task<IReadOnlyList<GenreCount>> GetGenreCountsAsync(CancellationToken ct)
    {
        var rows = await db.Database
            .SqlQuery<GenreCountRow>($"SELECT Genre AS Genre, COUNT(*) AS Count FROM Books GROUP BY Genre ORDER BY Genre")
            .ToListAsync(ct);

        return rows
            .Where(r => Enum.TryParse<Genre>(r.Genre, out _))
            .Select(r => new GenreCount(Enum.Parse<Genre>(r.Genre), r.Count))
            .ToList();
    }

    private sealed class GenreCountRow
    {
        public string Genre { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
