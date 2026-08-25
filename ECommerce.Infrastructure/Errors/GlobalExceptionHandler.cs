using Microsoft.AspNetCore.Diagnostics;

namespace ECommerce.Infrastructure.Errors;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateConcurrencyException concurrencyException)
            return false;

        logger.LogWarning(
            exception,
            "Optimistic concurrency conflict on {Method} {Path}. {Count} stale tracked entries.",
            httpContext.Request.Method,
            httpContext.Request.Path,
            concurrencyException.Entries.Count);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Concurrency conflict",
            Detail = "The record was modified by someone else. Reload it and try again.",
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["errors"] = new[]
        {
            new
            {
                Code = "Common.ConcurrencyConflict",
                Description = "The record was modified by someone else. Reload it and try again."
            }
        };

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
