namespace BookStore.Domain.Books;

/// <summary>
/// A book owned by the store. Bibliographic fields are copied from Open Library once, at add-time;
/// Genre, Price and Stock are store-owned and are the only things that change afterwards.
/// </summary>
public sealed class Book
{
    private Book() { }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Author { get; private set; } = string.Empty;
    public string? Isbn { get; private set; }
    public int? PublishedYear { get; private set; }
    public string? CoverUrl { get; private set; }
    public string OpenLibraryKey { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Genre Genre { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }

    public static Book Create(
        string title,
        string author,
        string? isbn,
        int? publishedYear,
        string? coverUrl,
        string openLibraryKey,
        string? description,
        Genre genre,
        decimal price,
        int stock) => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Author = author,
            Isbn = isbn,
            PublishedYear = publishedYear,
            CoverUrl = coverUrl,
            OpenLibraryKey = openLibraryKey,
            Description = description,
            Genre = genre,
            Price = price,
            Stock = stock
        };

    /// <summary>Updates store-owned fields only; Open Library data is immutable after add.</summary>
    public void UpdateStoreDetails(Genre genre, decimal price, int stock, string? description)
    {
        Genre = genre;
        Price = price;
        Stock = stock;
        Description = description;
    }
}
