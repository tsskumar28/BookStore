using BookStore.Application.Abstractions.Messaging;

namespace BookStore.Application.Common;

public sealed class RequestValidationException(IReadOnlyCollection<ValidationError> errors)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyCollection<ValidationError> Errors { get; } = errors;
}

public sealed class NotFoundException(string message) : Exception(message);

public sealed class ConflictException(string message) : Exception(message);

/// <summary>The external catalog (Open Library) timed out, failed, or returned garbage.</summary>
public sealed class CatalogUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
