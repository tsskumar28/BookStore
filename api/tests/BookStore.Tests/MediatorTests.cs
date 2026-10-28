using BookStore.Application.Abstractions.Messaging;
using BookStore.Application.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookStore.Tests;

public class MediatorTests
{
    private sealed record Ping(string Text) : IRequest<string>;

    private sealed class PingHandler : IRequestHandler<Ping, string>
    {
        public int Calls;
        public Task<string> Handle(Ping r, CancellationToken ct) { Calls++; return Task.FromResult(r.Text + "!"); }
    }

    private sealed class PingValidator : IValidator<Ping>
    {
        public IEnumerable<ValidationError> Validate(Ping r)
        {
            if (string.IsNullOrEmpty(r.Text)) yield return new("text", "required");
        }
    }

    private static (IMediator Mediator, PingHandler Handler) Build()
    {
        var handler = new PingHandler();
        var provider = new ServiceCollection()
            .AddSingleton<IRequestHandler<Ping, string>>(handler)
            .AddSingleton<IValidator<Ping>, PingValidator>()
            .BuildServiceProvider();
        return (new Mediator(provider, NullLogger<Mediator>.Instance), handler);
    }

    [Fact]
    public async Task Routes_request_to_its_handler()
    {
        var (mediator, _) = Build();
        Assert.Equal("hi!", await mediator.Send(new Ping("hi")));
    }

    [Fact]
    public async Task Invalid_request_never_reaches_handler()
    {
        var (mediator, handler) = Build();

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() => mediator.Send(new Ping("")));

        Assert.Equal("text", ex.Errors.Single().Field);
        Assert.Equal(0, handler.Calls);
    }
}
