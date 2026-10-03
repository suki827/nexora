using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Nexora.Api;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Request failed for {Method} {Path}", context.Request.Method, context.Request.Path);
            var (status, title, detail) = exception switch
            {
                ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request", exception.Message),
                InvalidOperationException => (StatusCodes.Status409Conflict, "Operation conflict", exception.Message),
                DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                    (StatusCodes.Status409Conflict, "Resource conflict", "A resource with the same unique value already exists."),
                DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } } =>
                    (StatusCodes.Status409Conflict, "Related resource conflict", "A related resource prevents this operation."),
                _ => (StatusCodes.Status500InternalServerError, "Request failed", "The server could not complete the request.")
            };

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            });
        }
    }
}
