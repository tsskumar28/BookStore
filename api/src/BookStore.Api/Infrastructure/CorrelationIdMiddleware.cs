using Serilog.Context;

namespace BookStore.Api.Infrastructure;

/// <summary>Reads or creates X-Correlation-ID, echoes it back, and attaches it to every log line of the request.</summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var id = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(id) || id.Length > 64)
            id = Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderName] = id;
        using (LogContext.PushProperty("CorrelationId", id))
        {
            await next(context);
        }
    }
}
