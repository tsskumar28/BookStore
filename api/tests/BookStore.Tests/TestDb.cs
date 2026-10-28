using BookStore.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Tests;

/// <summary>Real SQLite (in-memory) so unique indexes, collations and raw SQL behave like production.</summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public TestDb()
    {
        _connection.Open();
        Context = new BookStoreDbContext(new DbContextOptionsBuilder<BookStoreDbContext>().UseSqlite(_connection).Options);
        Context.Database.EnsureCreated();
        Repository = new BookRepository(Context);
    }

    public BookStoreDbContext Context { get; }
    internal BookRepository Repository { get; }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
