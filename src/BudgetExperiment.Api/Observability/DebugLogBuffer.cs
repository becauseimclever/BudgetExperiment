// <copyright file="DebugLogBuffer.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Collections.Concurrent;

namespace BudgetExperiment.Api.Observability;

/// <summary>
/// Thread-safe circular buffer that stores recent log entries for debug export.
/// Entries are evicted when the buffer exceeds capacity or when they expire past the retention TTL.
/// </summary>
public sealed class DebugLogBuffer : IDebugLogBuffer
{
    private readonly ConcurrentQueue<DebugLogEntry> _entries = new();
    private readonly int _maxSize;
    private readonly TimeSpan _retention;
    private readonly TimeProvider _timeProvider;
    private int _count;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugLogBuffer"/> class.
    /// </summary>
    /// <param name="maxSize">Maximum number of entries in the buffer.</param>
    /// <param name="retention">How long entries are retained before eviction.</param>
    /// <param name="timeProvider">Time provider for testability.</param>
    public DebugLogBuffer(int maxSize = 1000, TimeSpan? retention = null, TimeProvider? timeProvider = null)
    {
        _maxSize = maxSize > 0 ? maxSize : throw new ArgumentOutOfRangeException(nameof(maxSize), "Must be positive.");
        _retention = retention ?? TimeSpan.FromSeconds(300);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public bool IsEnabled => true;

    /// <inheritdoc/>
    public void Add(DebugLogEntry entry)
    {
        EvictExpired();

        _entries.Enqueue(entry);
        Interlocked.Increment(ref _count);

        // Evict oldest if over capacity
        while (Interlocked.CompareExchange(ref _count, 0, 0) > _maxSize && _entries.TryDequeue(out _))
        {
            Interlocked.Decrement(ref _count);
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<DebugLogEntry> GetByTraceId(string traceId, int maxEntries = 50)
    {
        EvictExpired();

        return _entries
            .Where(e => string.Equals(e.TraceId, traceId, StringComparison.Ordinal))
            .TakeLast(maxEntries)
            .ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<DebugLogEntry> GetRecent(TimeSpan window, int maxEntries = 50)
    {
        EvictExpired();

        var cutoff = _timeProvider.GetUtcNow().UtcDateTime - window;

        return _entries
            .Where(e => e.TimestampUtc >= cutoff)
            .TakeLast(maxEntries)
            .ToList();
    }

    private void EvictExpired()
    {
        var cutoff = _timeProvider.GetUtcNow().UtcDateTime - _retention;

        while (_entries.TryPeek(out var oldest) && oldest.TimestampUtc < cutoff)
        {
            if (_entries.TryDequeue(out _))
            {
                Interlocked.Decrement(ref _count);
            }
        }
    }
}
