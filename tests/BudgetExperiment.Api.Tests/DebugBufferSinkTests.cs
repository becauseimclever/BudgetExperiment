// <copyright file="DebugBufferSinkTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Api.Observability;

using Serilog;
using Serilog.Events;
using Serilog.Parsing;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Unit tests for <see cref="DebugBufferSink"/>.
/// </summary>
public sealed class DebugBufferSinkTests
{
    [Fact]
    public void Emit_CapturesBasicLogEvent()
    {
        var buffer = new DebugLogBuffer();
        var sink = new DebugBufferSink(buffer);

        var logEvent = CreateLogEvent(LogEventLevel.Information, "Hello {Name}", ("Name", "World"));

        sink.Emit(logEvent);

        var entries = buffer.GetRecent(TimeSpan.FromMinutes(1));
        Assert.Single(entries);
        Assert.Equal("Information", entries[0].Level);
        Assert.Equal("Hello {Name}", entries[0].MessageTemplate);
        Assert.Contains("World", entries[0].RenderedMessage);
    }

    [Fact]
    public void Emit_CapturesExceptionDetails()
    {
        var buffer = new DebugLogBuffer();
        var sink = new DebugBufferSink(buffer);

        var exception = new InvalidOperationException("Test error");
        var logEvent = CreateLogEvent(LogEventLevel.Error, "Something failed", exception);

        sink.Emit(logEvent);

        var entries = buffer.GetRecent(TimeSpan.FromMinutes(1));
        Assert.Single(entries);
        Assert.Equal("System.InvalidOperationException", entries[0].ExceptionType);
        Assert.Equal("Test error", entries[0].ExceptionMessage);
        Assert.NotNull(entries[0].ExceptionStackTrace);
    }

    [Fact]
    public void Emit_CapturesStructuredProperties()
    {
        var buffer = new DebugLogBuffer();
        var sink = new DebugBufferSink(buffer);

        var logEvent = CreateLogEvent(
            LogEventLevel.Warning,
            "Processing {ItemCount} items for {UserId}",
            ("ItemCount", 42),
            ("UserId", "user-123"));

        sink.Emit(logEvent);

        var entries = buffer.GetRecent(TimeSpan.FromMinutes(1));
        Assert.Single(entries);
        Assert.NotNull(entries[0].Properties);
        Assert.Equal(42, entries[0].Properties!["ItemCount"]);
        Assert.Equal("user-123", entries[0].Properties!["UserId"]);
    }

    [Fact]
    public void Emit_WithNoException_SetsExceptionFieldsNull()
    {
        var buffer = new DebugLogBuffer();
        var sink = new DebugBufferSink(buffer);

        var logEvent = CreateLogEvent(LogEventLevel.Information, "No exception");

        sink.Emit(logEvent);

        var entries = buffer.GetRecent(TimeSpan.FromMinutes(1));
        Assert.Single(entries);
        Assert.Null(entries[0].ExceptionType);
        Assert.Null(entries[0].ExceptionMessage);
        Assert.Null(entries[0].ExceptionStackTrace);
    }

    [Fact]
    public void Emit_FallsBackToTraceIdProperty()
    {
        var buffer = new DebugLogBuffer();
        var sink = new DebugBufferSink(buffer);

        // Create a log event with TraceId as a property (no Activity.Current)
        var logEvent = CreateLogEvent(
            LogEventLevel.Information,
            "Request handled",
            ("TraceId", "abc123trace"));

        sink.Emit(logEvent);

        var entries = buffer.GetRecent(TimeSpan.FromMinutes(1));
        Assert.Single(entries);

        // Either Activity trace ID or property fallback should be present
        // In test context without Activity, it should use property fallback
        if (System.Diagnostics.Activity.Current == null)
        {
            Assert.Equal("abc123trace", entries[0].TraceId);
        }
    }

    [Fact]
    public void Emit_IntegrationWithSerilogLogger()
    {
        var buffer = new DebugLogBuffer();
        var sink = new DebugBufferSink(buffer);

        using var logger = new LoggerConfiguration()
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.Information("Test message from Serilog with {Value}", 42);

        var entries = buffer.GetRecent(TimeSpan.FromMinutes(1));
        Assert.Single(entries);
        Assert.Contains("42", entries[0].RenderedMessage);
    }

    private static LogEvent CreateLogEvent(
        LogEventLevel level,
        string messageTemplate,
        params (string Name, object Value)[] properties)
    {
        return CreateLogEvent(level, messageTemplate, null, properties);
    }

    private static LogEvent CreateLogEvent(
        LogEventLevel level,
        string messageTemplate,
        Exception? exception,
        params (string Name, object Value)[] properties)
    {
        var parser = new MessageTemplateParser();
        var template = parser.Parse(messageTemplate);

        var logProperties = properties
            .Select(p => new LogEventProperty(p.Name, new ScalarValue(p.Value)))
            .ToList();

        return new LogEvent(
            DateTimeOffset.UtcNow,
            level,
            exception,
            template,
            logProperties);
    }
}
