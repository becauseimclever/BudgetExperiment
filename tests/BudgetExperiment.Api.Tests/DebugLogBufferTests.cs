// <copyright file="DebugLogBufferTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Api.Observability;

using Microsoft.Extensions.Time.Testing;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Unit tests for <see cref="DebugLogBuffer"/>.
/// </summary>
public sealed class DebugLogBufferTests
{
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 3, 16, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void IsEnabled_ReturnsTrue()
    {
        var buffer = new DebugLogBuffer(timeProvider: _timeProvider);

        Assert.True(buffer.IsEnabled);
    }

    [Fact]
    public void Add_And_GetByTraceId_ReturnMatchingEntries()
    {
        var buffer = new DebugLogBuffer(timeProvider: _timeProvider);
        var entry1 = CreateEntry("trace-1", "First");
        var entry2 = CreateEntry("trace-2", "Second");
        var entry3 = CreateEntry("trace-1", "Third");

        buffer.Add(entry1);
        buffer.Add(entry2);
        buffer.Add(entry3);

        var result = buffer.GetByTraceId("trace-1");

        Assert.Equal(2, result.Count);
        Assert.Equal("First", result[0].RenderedMessage);
        Assert.Equal("Third", result[1].RenderedMessage);
    }

    [Fact]
    public void GetByTraceId_NoMatch_ReturnsEmptyList()
    {
        var buffer = new DebugLogBuffer(timeProvider: _timeProvider);
        buffer.Add(CreateEntry("trace-1", "Entry"));

        var result = buffer.GetByTraceId("nonexistent");

        Assert.Empty(result);
    }

    [Fact]
    public void GetByTraceId_RespectsMaxEntries()
    {
        var buffer = new DebugLogBuffer(timeProvider: _timeProvider);

        for (int i = 0; i < 10; i++)
        {
            buffer.Add(CreateEntry("trace-1", $"Entry {i}"));
        }

        var result = buffer.GetByTraceId("trace-1", maxEntries: 3);

        Assert.Equal(3, result.Count);
        Assert.Equal("Entry 7", result[0].RenderedMessage);
        Assert.Equal("Entry 8", result[1].RenderedMessage);
        Assert.Equal("Entry 9", result[2].RenderedMessage);
    }

    [Fact]
    public void Add_EvictsOldestWhenOverCapacity()
    {
        var buffer = new DebugLogBuffer(maxSize: 3, timeProvider: _timeProvider);

        buffer.Add(CreateEntry("t1", "Entry 1"));
        buffer.Add(CreateEntry("t2", "Entry 2"));
        buffer.Add(CreateEntry("t3", "Entry 3"));
        buffer.Add(CreateEntry("t4", "Entry 4"));

        var result = buffer.GetByTraceId("t1");
        Assert.Empty(result);

        var all = buffer.GetRecent(TimeSpan.FromHours(1), maxEntries: 100);
        Assert.Equal(3, all.Count);
        Assert.Equal("Entry 2", all[0].RenderedMessage);
    }

    [Fact]
    public void Add_EvictsExpiredEntries()
    {
        var retention = TimeSpan.FromSeconds(60);
        var buffer = new DebugLogBuffer(retention: retention, timeProvider: _timeProvider);

        buffer.Add(CreateEntry("t1", "Old entry"));

        // Advance time past retention
        _timeProvider.Advance(TimeSpan.FromSeconds(61));

        buffer.Add(CreateEntry("t2", "New entry"));

        var result = buffer.GetByTraceId("t1");
        Assert.Empty(result);

        var recent = buffer.GetRecent(TimeSpan.FromSeconds(5));
        Assert.Single(recent);
        Assert.Equal("New entry", recent[0].RenderedMessage);
    }

    [Fact]
    public void GetRecent_ReturnsEntriesWithinWindow()
    {
        var buffer = new DebugLogBuffer(timeProvider: _timeProvider);

        buffer.Add(CreateEntry("t1", "Old"));

        _timeProvider.Advance(TimeSpan.FromSeconds(20));

        buffer.Add(CreateEntry("t2", "Recent"));

        var result = buffer.GetRecent(TimeSpan.FromSeconds(10));

        Assert.Single(result);
        Assert.Equal("Recent", result[0].RenderedMessage);
    }

    [Fact]
    public void GetRecent_RespectsMaxEntries()
    {
        var buffer = new DebugLogBuffer(timeProvider: _timeProvider);

        for (int i = 0; i < 10; i++)
        {
            buffer.Add(CreateEntry("t1", $"Entry {i}"));
        }

        var result = buffer.GetRecent(TimeSpan.FromHours(1), maxEntries: 3);

        Assert.Equal(3, result.Count);
        Assert.Equal("Entry 7", result[0].RenderedMessage);
    }

    [Fact]
    public void Constructor_ThrowsForNonPositiveMaxSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DebugLogBuffer(maxSize: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DebugLogBuffer(maxSize: -1));
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentAddAndRead()
    {
        var buffer = new DebugLogBuffer(maxSize: 500, timeProvider: _timeProvider);
        var tasks = new Task[10];

        for (int t = 0; t < 10; t++)
        {
            var taskIndex = t;
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    buffer.Add(CreateEntry($"trace-{taskIndex}", $"Entry {taskIndex}-{i}"));
                    buffer.GetByTraceId($"trace-{taskIndex}");
                    buffer.GetRecent(TimeSpan.FromSeconds(30));
                }
            });
        }

        await Task.WhenAll(tasks);

        // Buffer should not exceed max size
        var all = buffer.GetRecent(TimeSpan.FromHours(1), maxEntries: 1000);
        Assert.True(all.Count <= 500);
    }

    private DebugLogEntry CreateEntry(string traceId, string message)
    {
        return new DebugLogEntry
        {
            TimestampUtc = _timeProvider.GetUtcNow().UtcDateTime,
            Level = "Information",
            MessageTemplate = message,
            RenderedMessage = message,
            TraceId = traceId,
        };
    }
}
