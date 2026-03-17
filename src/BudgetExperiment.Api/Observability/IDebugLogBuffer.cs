// <copyright file="IDebugLogBuffer.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Api.Observability;

/// <summary>
/// In-memory circular buffer that captures recent structured log entries
/// for debug export. Queryable by traceId and time window.
/// </summary>
public interface IDebugLogBuffer
{
    /// <summary>
    /// Gets a value indicating whether the debug log buffer is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Adds a log entry to the buffer.
    /// </summary>
    /// <param name="entry">The log entry to add.</param>
    void Add(DebugLogEntry entry);

    /// <summary>
    /// Gets log entries matching the given trace ID.
    /// </summary>
    /// <param name="traceId">The trace ID to filter by.</param>
    /// <param name="maxEntries">Maximum number of entries to return.</param>
    /// <returns>Matching log entries in chronological order.</returns>
    IReadOnlyList<DebugLogEntry> GetByTraceId(string traceId, int maxEntries = 50);

    /// <summary>
    /// Gets the most recent log entries within the specified time window.
    /// </summary>
    /// <param name="window">How far back to look.</param>
    /// <param name="maxEntries">Maximum number of entries to return.</param>
    /// <returns>Recent log entries in chronological order.</returns>
    IReadOnlyList<DebugLogEntry> GetRecent(TimeSpan window, int maxEntries = 50);
}
