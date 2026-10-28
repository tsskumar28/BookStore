using BookStore.Domain.Books;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    /// <summary>Creates the SQLite schema if missing and seeds sample books into an empty store.</summary>
    public static async Task InitializeAsync(BookStoreDbContext db, CancellationToken ct = default)
    {
        await db.Database.EnsureCreatedAsync(ct);
        if (await db.Books.AnyAsync(ct)) return;

        db.Books.AddRange(
            Seed("Dune", "Frank Herbert", "9780441172719", 1965, "/works/OL893415W", Genre.SciFi, 12.99m, 14),
            Seed("The Hobbit", "J. R. R. Tolkien", "9780547928227", 1937, "/works/OL262758W", Genre.Fantasy, 10.50m, 22),
            Seed("To Kill a Mockingbird", "Harper Lee", "9780061120084", 1960, "/works/OL3140822W", Genre.Fiction, 9.99m, 30),
            Seed("Gone Girl", "Gillian Flynn", "9780307588371", 2012, "/works/OL16807705W", Genre.Mystery, 11.25m, 8),
            Seed("Educated", "Tara Westover", "9780399590504", 2018, "/works/OL20038566W", Genre.Biography, 14.00m, 11),
            Seed("Sapiens", "Yuval Noah Harari", "9780062316110", 2011, "/works/OL17930368W", Genre.History, 16.95m, 17),
            Seed("Charlotte's Web", "E. B. White", "9780064400558", 1952, "/works/OL1982827W", Genre.Children, 7.99m, 25),
            Seed("Neuromancer", "William Gibson", "9780441569595", 1984, "/works/OL27258W", Genre.SciFi, 9.50m, 6),
            Seed("Thinking, Fast and Slow", "Daniel Kahneman", "9780374533557", 2011, "/works/OL17771893W", Genre.NonFiction, 15.75m, 9),
            Seed("The Name of the Wind", "Patrick Rothfuss", "9780756404741", 2007, "/works/OL5735363W", Genre.Fantasy, 13.49m, 0),
            Seed("The Girl with the Dragon Tattoo", "Stieg Larsson", "9780307949486", 2005, "/works/OL16313124W", Genre.Mystery, 12.00m, 5),
            Seed("Brave New World", "Aldous Huxley", "9780060850524", 1932, "/works/OL26435W", Genre.Fiction, 8.99m, 13));

        await db.SaveChangesAsync(ct);
    }

    private static Book Seed(string title, string author, string isbn, int year, string key, Genre genre, decimal price, int stock) =>
        Book.Create(title, author, isbn, year, $"https://covers.openlibrary.org/b/isbn/{isbn}-M.jpg",
            key, description: null, genre, price, stock);
}
