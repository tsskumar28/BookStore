using BookStore.Domain.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Infrastructure.Persistence;

internal sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> b)
    {
        b.ToTable("Books");
        b.HasKey(x => x.Id);

        b.Property(x => x.Title).IsRequired().HasMaxLength(500);
        b.Property(x => x.Author).IsRequired().HasMaxLength(500);
        b.Property(x => x.Isbn).HasMaxLength(20);
        b.Property(x => x.CoverUrl).HasMaxLength(500);
        b.Property(x => x.OpenLibraryKey).IsRequired().HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(4000);

        // Stored as readable text so the raw SQL genre query and the DB itself stay legible.
        b.Property(x => x.Genre).HasConversion<string>().HasMaxLength(30).IsRequired();

        // SQLite has no decimal type and EF refuses to ORDER BY a decimal column; REAL keeps sorting server-side.
        b.Property(x => x.Price).HasConversion(
            v => (double)v,
            v => Math.Round((decimal)v, 2));

        // Duplicate prevention is enforced by the database, not just by a check-then-insert.
        b.HasIndex(x => x.OpenLibraryKey).IsUnique();
        b.HasIndex(x => x.Isbn).IsUnique();
        b.HasIndex(x => x.Genre);
    }
}
