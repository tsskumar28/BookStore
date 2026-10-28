using BookStore.Application.Abstractions;
using BookStore.Application.Abstractions.Messaging;
using BookStore.Application.Common;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Books.DeleteBook;

public sealed record DeleteBookCommand(Guid Id) : IRequest<bool>;

public sealed class DeleteBookHandler(IBookRepository books, ILogger<DeleteBookHandler> logger)
    : IRequestHandler<DeleteBookCommand, bool>
{
    public async Task<bool> Handle(DeleteBookCommand c, CancellationToken ct)
    {
        var book = await books.GetByIdAsync(c.Id, ct)
            ?? throw new NotFoundException($"Book {c.Id} was not found.");

        await books.DeleteAsync(book, ct);
        logger.LogInformation("Deleted book {BookId}", c.Id);
        return true;
    }
}
