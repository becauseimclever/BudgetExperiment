// <copyright file="SanitizedLogEntry.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Api.Observability;

/// <summary>
/// A single sanitized log entry in the debug bundle.
/// </summary>
public sealed record SanitizedLogEntry
{
    /// <summary>
    /// Gets the UTC timestamp of the log entry.
    /// </summary>
    public required DateTime TimestampUtc { get; init; }

    /// <summary>
    /// Gets the log level.
    /// </summary>
    public required string Level { get; init; }

    /// <summary>
    /// Gets the message template.
    /// </summary>
    public required string MessageTemplate { get; init; }

    /// <summary>
    /// Gets the sanitized properties (only allowlisted keys).
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Properties { get; init; }

    /// <summary>
    /// Gets the exception context, if an exception was logged with this entry.
    /// </summary>
    public ExceptionContext? Exception { get; init; }
}
