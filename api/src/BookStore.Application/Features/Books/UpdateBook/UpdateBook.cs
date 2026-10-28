using BookStore.Application.Abstractions;
using BookStore.Application.Abstractions.Messaging;
using BookStore.Application.Common;
using BookStore.Domain.Books;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Books.UpdateBook;

/// <summary>Edits store-owned fields only. Never calls Open Library.</summary>
public sealed record UpdateBookCommand(
    Guid Id,
    Genre Genre,
    decimal Price,
    int Stock,
    string? Description) : IRequest<BookDto>;

public sealed class UpdateBookValidator : IValidator<UpdateBookCommand>
{
    public IEnumerable<ValidationError> Validate(UpdateBookCommand c) =>
        StoreFieldRules.Check(c.Genre, c.Price, c.Stock, c.Description);
}

public sealed class UpdateBookHandler(IBookRepository books, ILogger<UpdateBookHandler> logger)
    : IRequestHandler<UpdateBookCommand, BookDto>
{
    public async Task<BookDto> Handle(UpdateBookCommand c, CancellationToken ct)
    {
        var book = await books.GetByIdAsync(c.Id, ct)
            ?? throw new NotFoundException($"Book {c.Id} was not found.");

        book.UpdateStoreDetails(c.Genre, c.Price, c.Stock,
            string.IsNullOrWhiteSpace(c.Description) ? null : c.Description.Trim());
        await books.UpdateAsync(book, ct);

        logger.LogInformation("Updated book {BookId}", book.Id);
        return BookDto.From(book);
    }
}
