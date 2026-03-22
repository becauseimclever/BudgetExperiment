// <copyright file="DebugLogEntry.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Api.Observability;

/// <summary>
/// A single captured log entry stored in the debug buffer (pre-sanitization).
/// </summary>
public sealed record DebugLogEntry
{
    /// <summary>
    /// Gets the UTC timestamp of the log entry.
    /// </summary>
    public required DateTime TimestampUtc
    {
        get; init;
    }

    /// <summary>
    /// Gets the log level (e.g., Information, Warning, Error).
    /// </summary>
    public required string Level
    {
        get; init;
    }

    /// <summary>
    /// Gets the message template (with placeholders).
    /// </summary>
    public required string MessageTemplate
    {
        get; init;
    }

    /// <summary>
    /// Gets the rendered message with values substituted.
    /// </summary>
    public required string RenderedMessage
    {
        get; init;
    }

    /// <summary>
    /// Gets the exception type name, if an exception was logged.
    /// </summary>
    public string? ExceptionType
    {
        get; init;
    }

    /// <summary>
    /// Gets the exception message, if an exception was logged.
    /// </summary>
    public string? ExceptionMessage
    {
        get; init;
    }

    /// <summary>
    /// Gets the exception stack trace, if an exception was logged.
    /// </summary>
    public string? ExceptionStackTrace
    {
        get; init;
    }

    /// <summary>
    /// Gets the trace ID from the current activity/request context.
    /// </summary>
    public string? TraceId
    {
        get; init;
    }

    /// <summary>
    /// Gets the span ID from the current activity/request context.
    /// </summary>
    public string? SpanId
    {
        get; init;
    }

    /// <summary>
    /// Gets the structured log properties (key-value pairs).
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Properties
    {
        get; init;
    }
}
