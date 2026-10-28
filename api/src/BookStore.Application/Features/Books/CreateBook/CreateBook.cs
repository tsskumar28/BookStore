using BookStore.Application.Abstractions;
using BookStore.Application.Abstractions.Messaging;
using BookStore.Application.Common;
using BookStore.Domain.Books;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Books.CreateBook;

/// <summary>Persists a catalog result the user selected, plus the store-owned fields.</summary>
public sealed record CreateBookCommand(
    string Title,
    string Author,
    string? Isbn,
    int? PublishedYear,
    string? CoverUrl,
    string OpenLibraryKey,
    string? Description,
    Genre Genre,
    decimal Price,
    int Stock = 0) : IRequest<BookDto>;

public sealed class CreateBookValidator : IValidator<CreateBookCommand>
{
    public IEnumerable<ValidationError> Validate(CreateBookCommand c)
    {
        if (string.IsNullOrWhiteSpace(c.Title) || c.Title.Length > 500)
            yield return new(nameof(c.Title), "Title is required and must be at most 500 characters.");
        if (string.IsNullOrWhiteSpace(c.Author) || c.Author.Length > 500)
            yield return new(nameof(c.Author), "Author is required and must be at most 500 characters.");
        if (string.IsNullOrWhiteSpace(c.OpenLibraryKey) || c.OpenLibraryKey.Length > 100)
            yield return new(nameof(c.OpenLibraryKey), "OpenLibraryKey is required (select a search result).");
        if (c.Isbn is { Length: > 20 })
            yield return new(nameof(c.Isbn), "Isbn must be at most 20 characters.");
        if (c.PublishedYear is { } y && (y < 1000 || y > DateTime.UtcNow.Year + 1))
            yield return new(nameof(c.PublishedYear), "PublishedYear is not a plausible year.");
        if (c.CoverUrl is not null &&
            !(Uri.TryCreate(c.CoverUrl, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"))
            yield return new(nameof(c.CoverUrl), "CoverUrl must be an absolute http(s) URL.");

        foreach (var e in StoreFieldRules.Check(c.Genre, c.Price, c.Stock, c.Description))
            yield return e;
    }
}

public sealed class CreateBookHandler(IBookRepository books, ILogger<CreateBookHandler> logger)
    : IRequestHandler<CreateBookCommand, BookDto>
{
    public async Task<BookDto> Handle(CreateBookCommand c, CancellationToken ct)
    {
        var isbn = string.IsNullOrWhiteSpace(c.Isbn) ? null : c.Isbn.Trim();
        var key = c.OpenLibraryKey.Trim();

        if (await books.ExistsAsync(isbn, key, ct))
            throw new ConflictException("This book is already in the store (same ISBN or Open Library key).");

        var book = Book.Create(
            c.Title.Trim(), c.Author.Trim(), isbn, c.PublishedYear, c.CoverUrl, key,
            string.IsNullOrWhiteSpace(c.Description) ? null : c.Description.Trim(),
            c.Genre, c.Price, c.Stock);

        await books.AddAsync(book, ct);
        logger.LogInformation("Added book {BookId} {OpenLibraryKey} in genre {Genre}", book.Id, key, book.Genre);
        return BookDto.From(book);
    }
}
