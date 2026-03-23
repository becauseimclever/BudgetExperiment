using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;

using BudgetExperiment.Domain;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BudgetExperiment.Api.Middleware;

/// <summary>Converts exceptions to RFC 7807 problem details responses.</summary>
[ExcludeFromCodeCoverage]
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.</summary>
    /// <param name="next">Next request delegate.</param>
    /// <param name="logger">Logger.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Invoke middleware.</summary>
    /// <param name="context">HTTP context.</param>
    /// <returns>Task.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await this.WriteProblemAsync(context, ex).ConfigureAwait(false);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception ex)
    {
        // Don't process if response has already started (e.g., client disconnected)
        if (context.Response.HasStarted)
        {
            _logger.LogDebug("Response already started, cannot write problem details");
            return;
        }

        if (ex is OperationCanceledException or TaskCanceledException)
        {
            // Client disconnected — abort without writing a response body
            _logger.LogDebug("Request was cancelled");
            context.Abort();
            return;
        }

        int status;
        string title;
        if (ex is DbUpdateConcurrencyException)
        {
            status = StatusCodes.Status409Conflict;
            title = "Conflict";
        }
        else if (ex is DomainException de)
        {
            (status, title) = de.ExceptionType switch
            {
                DomainExceptionType.NotFound => ((int)HttpStatusCode.NotFound, "Not Found"),
                DomainExceptionType.InvalidState => (StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity"),
                _ => (StatusCodes.Status400BadRequest, "Domain Validation Error"),
            };
        }
        else
        {
            status = StatusCodes.Status500InternalServerError;
            title = "Internal Server Error";
            _logger.LogError(ex, "Unhandled exception");
        }

        var problem = new ProblemDetails
        {
            Type = "about:blank",
            Title = title,
            Status = status,
            Detail = ex.Message,
            Instance = context.Request.Path,
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem)).ConfigureAwait(false);
    }
}
