using System.Collections.Concurrent;
using BookStore.Application.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Abstractions.Messaging;

/// <summary>
/// Minimal hand-rolled mediator (MediatR moved to a commercial license). One handler per request,
/// resolved from DI; validators for the request run first. Cross-cutting concerns live here, not in endpoints.
/// </summary>
public sealed class Mediator(IServiceProvider services, ILogger<Mediator> logger) : IMediator
{
    private static readonly ConcurrentDictionary<Type, object> Wrappers = new();

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = (RequestWrapper<TResponse>)Wrappers.GetOrAdd(request.GetType(), static requestType =>
            Activator.CreateInstance(typeof(RequestWrapper<,>).MakeGenericType(requestType, typeof(TResponse)))!);

        return wrapper.Handle(request, services, logger, cancellationToken);
    }

    private abstract class RequestWrapper<TResponse>
    {
        public abstract Task<TResponse> Handle(
            IRequest<TResponse> request, IServiceProvider services, ILogger logger, CancellationToken ct);
    }

    private sealed class RequestWrapper<TRequest, TResponse> : RequestWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public override async Task<TResponse> Handle(
            IRequest<TResponse> request, IServiceProvider services, ILogger logger, CancellationToken ct)
        {
            var typed = (TRequest)request;

            var errors = services.GetServices<IValidator<TRequest>>()
                .SelectMany(v => v.Validate(typed))
                .ToList();
            if (errors.Count > 0)
            {
                logger.LogInformation("Validation failed for {RequestType} with {ErrorCount} error(s)",
                    typeof(TRequest).Name, errors.Count);
                throw new RequestValidationException(errors);
            }

            var handler = services.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
            logger.LogDebug("Handling {RequestType}", typeof(TRequest).Name);
            return await handler.Handle(typed, ct);
        }
    }
}
