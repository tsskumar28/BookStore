using BookStore.Domain.Books;

namespace BookStore.Application.Features.Books;

public sealed record BookDto(
    Guid Id,
    string Title,
    string Author,
    string? Isbn,
    int? PublishedYear,
    string? CoverUrl,
    string OpenLibraryKey,
    string? Description,
    Genre Genre,
    decimal Price,
    int Stock)
{
    public static BookDto From(Book b) => new(
        b.Id, b.Title, b.Author, b.Isbn, b.PublishedYear, b.CoverUrl,
        b.OpenLibraryKey, b.Description, b.Genre, b.Price, b.Stock);
}
