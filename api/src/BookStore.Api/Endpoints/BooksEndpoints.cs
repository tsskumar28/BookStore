using BookStore.Application.Abstractions;
using BookStore.Application.Abstractions.Messaging;
using BookStore.Application.Common;
using BookStore.Application.Features.Books.CreateBook;
using BookStore.Application.Features.Books.DeleteBook;
using BookStore.Application.Features.Books.GetBookById;
using BookStore.Application.Features.Books.GetBooks;
using BookStore.Application.Features.Books.GetGenres;
using BookStore.Application.Features.Books.UpdateBook;
using BookStore.Domain.Books;

namespace BookStore.Api.Endpoints;

public sealed record UpdateBookRequest(Genre Genre, decimal Price, int Stock, string? Description);

public static class BooksEndpoints
{
    public static void MapBooksEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/books").WithTags("Books");

        g.MapGet("/", (IMediator m, string? genre, int? page, int? pageSize, string? sortBy, bool? desc, CancellationToken ct) =>
            m.Send(new GetBooksQuery(
                ParseOptionalEnum<Genre>(genre, nameof(genre)),
                page ?? 1,
                pageSize ?? 10,
                ParseOptionalEnum<BookSortField>(sortBy, nameof(sortBy)) ?? BookSortField.Title,
                desc ?? false), ct));

        g.MapGet("/genres", (IMediator m, CancellationToken ct) => m.Send(new GetGenresQuery(), ct));

        g.MapGet("/{id:guid}", (IMediator m, Guid id, CancellationToken ct) => m.Send(new GetBookByIdQuery(id), ct));

        g.MapPost("/", async (IMediator m, CreateBookCommand cmd, CancellationToken ct) =>
        {
            var book = await m.Send(cmd, ct);
            return Results.Created($"/api/books/{book.Id}", book);
        });

        g.MapPut("/{id:guid}", (IMediator m, Guid id, UpdateBookRequest body, CancellationToken ct) =>
            m.Send(new UpdateBookCommand(id, body.Genre, body.Price, body.Stock, body.Description), ct));

        g.MapDelete("/{id:guid}", async (IMediator m, Guid id, CancellationToken ct) =>
        {
            await m.Send(new DeleteBookCommand(id), ct);
            return Results.NoContent();
        });
    }

    /// <summary>Case-insensitive enum parsing that reports bad values as a normal validation problem (400 + errors).</summary>
    private static T? ParseOptionalEnum<T>(string? value, string field) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (Enum.TryParse<T>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)) return parsed;
        throw new RequestValidationException([new ValidationError(field, $"'{value}' is not a valid {typeof(T).Name}.")]);
    }
}
