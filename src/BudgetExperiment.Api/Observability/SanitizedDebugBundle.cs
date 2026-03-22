// <copyright file="SanitizedDebugBundle.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Api.Observability;

/// <summary>
/// The final sanitized debug log export payload. All PII has been removed.
/// </summary>
public sealed record SanitizedDebugBundle
{
    /// <summary>
    /// Gets a notice explaining what the file contains and that PII has been redacted.
    /// </summary>
    public required string Notice
    {
        get; init;
    }

    /// <summary>
    /// Gets a summary of how many fields were redacted and in what categories.
    /// </summary>
    public required RedactionSummary RedactionSummary
    {
        get; init;
    }

    /// <summary>
    /// Gets the trace ID for cross-referencing with server logs.
    /// </summary>
    public required string TraceId
    {
        get; init;
    }

    /// <summary>
    /// Gets the UTC timestamp when this bundle was exported.
    /// </summary>
    public required DateTime ExportedAtUtc
    {
        get; init;
    }

    /// <summary>
    /// Gets the environment context (version, OS, runtime).
    /// </summary>
    public required EnvironmentContext Environment
    {
        get; init;
    }

    /// <summary>
    /// Gets the request context (method, route, status code), if available.
    /// </summary>
    public RequestContext? Request
    {
        get; init;
    }

    /// <summary>
    /// Gets the exception context, if an exception was logged.
    /// </summary>
    public ExceptionContext? Exception
    {
        get; init;
    }

    /// <summary>
    /// Gets the sanitized log entries.
    /// </summary>
    public required IReadOnlyList<SanitizedLogEntry> LogEntries
    {
        get; init;
    }
}
