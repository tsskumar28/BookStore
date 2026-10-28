using BookStore.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.Api.Infrastructure;

/// <summary>Maps application exceptions to RFC 7807 problem responses.</summary>
public sealed class ApiExceptionHandler(IProblemDetailsService problems, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var problem = ex switch
        {
            RequestValidationException v => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Extensions =
                {
                    ["errors"] = v.Errors
                        .GroupBy(e => e.Field)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray())
                }
            },
            NotFoundException => new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Not found", Detail = ex.Message },
            ConflictException => new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Conflict", Detail = ex.Message },
            CatalogUnavailableException => new ProblemDetails { Status = StatusCodes.Status502BadGateway, Title = "Catalog unavailable", Detail = ex.Message },
            _ => null
        };

        if (problem is null)
        {
            logger.LogError(ex, "Unhandled exception");
            problem = new ProblemDetails { Status = StatusCodes.Status500InternalServerError, Title = "Unexpected error" };
        }

        ctx.Response.StatusCode = problem.Status!.Value;
        return await problems.TryWriteAsync(new ProblemDetailsContext { HttpContext = ctx, ProblemDetails = problem, Exception = ex });
    }
}
