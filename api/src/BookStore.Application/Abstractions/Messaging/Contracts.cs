namespace BookStore.Application.Abstractions.Messaging;

/// <summary>Marker for a request (command or query) that yields <typeparamref name="TResponse"/>.</summary>
public interface IRequest<out TResponse>;

public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

public interface IMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}

public sealed record ValidationError(string Field, string Message);

/// <summary>Optional per-request validator, run by the mediator before the handler.</summary>
public interface IValidator<in TRequest>
{
    IEnumerable<ValidationError> Validate(TRequest request);
}
