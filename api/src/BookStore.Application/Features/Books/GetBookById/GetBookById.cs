using BookStore.Application.Abstractions;
using BookStore.Application.Abstractions.Messaging;
using BookStore.Application.Common;

namespace BookStore.Application.Features.Books.GetBookById;

public sealed record GetBookByIdQuery(Guid Id) : IRequest<BookDto>;

public sealed class GetBookByIdHandler(IBookRepository books) : IRequestHandler<GetBookByIdQuery, BookDto>
{
    public async Task<BookDto> Handle(GetBookByIdQuery q, CancellationToken ct)
    {
        var book = await books.GetByIdAsync(q.Id, ct)
            ?? throw new NotFoundException($"Book {q.Id} was not found.");
        return BookDto.From(book);
    }
}
