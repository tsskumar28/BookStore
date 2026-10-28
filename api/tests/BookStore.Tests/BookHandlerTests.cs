using BookStore.Application.Abstractions;
using BookStore.Application.Common;
using BookStore.Application.Features.Books.CreateBook;
using BookStore.Application.Features.Books.DeleteBook;
using BookStore.Application.Features.Books.GetBooks;
using BookStore.Application.Features.Books.GetGenres;
using BookStore.Application.Features.Books.UpdateBook;
using BookStore.Domain.Books;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookStore.Tests;

public class BookHandlerTests : IDisposable
{
    private readonly TestDb _db = new();

    public void Dispose() => _db.Dispose();

    private static CreateBookCommand NewBook(string key = "/works/OL1W", string? isbn = "111", Genre genre = Genre.Fiction,
        decimal price = 10m, string title = "Title") =>
        new(title, "Author", isbn, 2000, null, key, null, genre, price, 3);

    private Task<BookStore.Application.Features.Books.BookDto> Create(CreateBookCommand c) =>
        new CreateBookHandler(_db.Repository, NullLogger<CreateBookHandler>.Instance).Handle(c, default);

    [Fact]
    public async Task Create_persists_book_with_store_fields()
    {
        var dto = await Create(NewBook(genre: Genre.SciFi, price: 12.5m));

        var stored = await _db.Repository.GetByIdAsync(dto.Id, default);
        Assert.NotNull(stored);
        Assert.Equal(Genre.SciFi, stored.Genre);
        Assert.Equal(12.5m, stored.Price);
    }

    [Fact]
    public async Task Create_rejects_duplicate_isbn_or_key()
    {
        await Create(NewBook());

        await Assert.ThrowsAsync<ConflictException>(() => Create(NewBook(key: "/works/OTHER", isbn: "111")));
        await Assert.ThrowsAsync<ConflictException>(() => Create(NewBook(key: "/works/OL1W", isbn: "222")));
    }

    [Fact]
    public async Task Unique_index_backs_up_the_duplicate_check()
    {
        var book = Book.Create("T", "A", "999", null, null, "/works/X", null, Genre.Fiction, 5m, 1);
        await _db.Repository.AddAsync(book, default);

        var dup = Book.Create("T2", "A", "999", null, null, "/works/Y", null, Genre.Fiction, 5m, 1);
        await Assert.ThrowsAsync<ConflictException>(() => _db.Repository.AddAsync(dup, default));
    }

    [Fact]
    public void Create_validator_flags_bad_input()
    {
        var errors = new CreateBookValidator()
            .Validate(NewBook(price: -1m, title: " ") with { Stock = -2, Genre = (Genre)99 })
            .Select(e => e.Field.ToLowerInvariant()).ToList();

        Assert.Contains("title", errors);
        Assert.Contains("price", errors);
        Assert.Contains("stock", errors);
        Assert.Contains("genre", errors);
    }

    [Fact]
    public async Task Update_changes_store_fields_only_and_404s_when_missing()
    {
        var dto = await Create(NewBook());
        var handler = new UpdateBookHandler(_db.Repository, NullLogger<UpdateBookHandler>.Instance);

        var updated = await handler.Handle(new UpdateBookCommand(dto.Id, Genre.History, 20m, 9, "d"), default);

        Assert.Equal(Genre.History, updated.Genre);
        Assert.Equal(20m, updated.Price);
        Assert.Equal(dto.Title, updated.Title);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateBookCommand(Guid.NewGuid(), Genre.History, 1m, 1, null), default));
    }

    [Fact]
    public async Task Delete_removes_book()
    {
        var dto = await Create(NewBook());
        await new DeleteBookHandler(_db.Repository, NullLogger<DeleteBookHandler>.Instance)
            .Handle(new DeleteBookCommand(dto.Id), default);

        Assert.Null(await _db.Repository.GetByIdAsync(dto.Id, default));
    }

    [Fact]
    public async Task List_filters_sorts_and_pages_server_side()
    {
        await Create(NewBook("/w/1", "1", Genre.Fiction, 30m, "Bravo"));
        await Create(NewBook("/w/2", "2", Genre.Fiction, 10m, "alpha"));
        await Create(NewBook("/w/3", "3", Genre.Fantasy, 20m, "Charlie"));
        var handler = new GetBooksHandler(_db.Repository);

        var fiction = await handler.Handle(new GetBooksQuery(Genre.Fiction, SortBy: BookSortField.Price), default);
        Assert.Equal(2, fiction.TotalCount);
        Assert.Equal(["alpha", "Bravo"], fiction.Items.Select(b => b.Title));

        var byTitle = await handler.Handle(new GetBooksQuery(PageSize: 2), default);
        Assert.Equal(3, byTitle.TotalCount);
        Assert.Equal(["alpha", "Bravo"], byTitle.Items.Select(b => b.Title)); // case-insensitive order

        var page2 = await handler.Handle(new GetBooksQuery(Page: 2, PageSize: 2), default);
        Assert.Equal(["Charlie"], page2.Items.Select(b => b.Title));
    }

    [Fact]
    public async Task Genre_counts_come_from_raw_sql_and_include_empty_genres()
    {
        await Create(NewBook("/w/1", "1", Genre.Fiction));
        await Create(NewBook("/w/2", "2", Genre.Fiction));
        await Create(NewBook("/w/3", "3", Genre.History));

        var genres = await new GetGenresHandler(_db.Repository).Handle(new GetGenresQuery(), default);

        Assert.Equal(Enum.GetValues<Genre>().Length, genres.Count);
        Assert.Equal(2, genres.Single(g => g.Genre == Genre.Fiction).Count);
        Assert.Equal(1, genres.Single(g => g.Genre == Genre.History).Count);
        Assert.Equal(0, genres.Single(g => g.Genre == Genre.Mystery).Count);
    }

    [Fact]
    public void GetBooks_validator_rejects_bad_paging()
    {
        var errors = new GetBooksValidator().Validate(new GetBooksQuery(Page: 0, PageSize: 1000)).ToList();
        Assert.Equal(2, errors.Count);
    }
}
