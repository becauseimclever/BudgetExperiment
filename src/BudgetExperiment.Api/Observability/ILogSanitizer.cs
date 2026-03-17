// <copyright file="ILogSanitizer.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Api.Observability;

/// <summary>
/// Sanitizes debug log entries by stripping PII using an allowlist-based approach.
/// </summary>
public interface ILogSanitizer
{
    /// <summary>
    /// Sanitizes a collection of log entries, producing a debug bundle with all PII redacted.
    /// </summary>
    /// <param name="entries">The raw log entries to sanitize.</param>
    /// <param name="traceId">The trace ID for the bundle.</param>
    /// <param name="environment">Environment metadata to include.</param>
    /// <returns>A sanitized debug bundle ready for export.</returns>
    SanitizedDebugBundle Sanitize(
        IReadOnlyList<DebugLogEntry> entries,
        string traceId,
        EnvironmentContext environment);
}
