using BookStore.Application.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace BookStore.Application;

public static class DependencyInjection
{
    private static readonly Type[] OpenGenerics = [typeof(IRequestHandler<,>), typeof(IValidator<>)];

    /// <summary>Registers the mediator and scans this assembly for handlers and validators.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IMediator, Mediator>();

        var concreteTypes = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false });

        foreach (var type in concreteTypes)
        foreach (var contract in type.GetInterfaces()
                     .Where(i => i.IsGenericType && OpenGenerics.Contains(i.GetGenericTypeDefinition())))
        {
            services.AddScoped(contract, type);
        }

        return services;
    }
}
