using BookStore.Application.Abstractions.Messaging;
using BookStore.Domain.Books;

namespace BookStore.Application.Features.Books;

/// <summary>Validation shared by the slices that accept store-owned fields (create and update).</summary>
internal static class StoreFieldRules
{
    public const decimal MaxPrice = 100_000m;
    public const int MaxDescriptionLength = 4_000;

    public static IEnumerable<ValidationError> Check(Genre genre, decimal price, int stock, string? description)
    {
        if (!Enum.IsDefined(genre))
            yield return new(nameof(genre), "Genre must be one of the supported genres.");
        if (price <= 0 || price > MaxPrice)
            yield return new(nameof(price), $"Price must be greater than 0 and at most {MaxPrice:0}.");
        if (decimal.Round(price, 2) != price)
            yield return new(nameof(price), "Price can have at most 2 decimal places.");
        if (stock < 0)
            yield return new(nameof(stock), "Stock cannot be negative.");
        if (description is { Length: > MaxDescriptionLength })
            yield return new(nameof(description), $"Description must be at most {MaxDescriptionLength} characters.");
    }
}
