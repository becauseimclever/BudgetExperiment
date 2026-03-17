// <copyright file="DebugBufferSink.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Diagnostics;

using Serilog.Core;
using Serilog.Events;

namespace BudgetExperiment.Api.Observability;

/// <summary>
/// A Serilog sink that writes log events into the <see cref="IDebugLogBuffer"/>
/// for later debug export.
/// </summary>
public sealed class DebugBufferSink : ILogEventSink
{
    private readonly IDebugLogBuffer _buffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugBufferSink"/> class.
    /// </summary>
    /// <param name="buffer">The debug log buffer to write entries to.</param>
    public DebugBufferSink(IDebugLogBuffer buffer)
    {
        _buffer = buffer;
    }

    /// <inheritdoc/>
    public void Emit(LogEvent logEvent)
    {
        var properties = new Dictionary<string, object?>();
        foreach (var kvp in logEvent.Properties)
        {
            properties[kvp.Key] = RenderPropertyValue(kvp.Value);
        }

        var activity = Activity.Current;

        var entry = new DebugLogEntry
        {
            TimestampUtc = logEvent.Timestamp.UtcDateTime,
            Level = logEvent.Level.ToString(),
            MessageTemplate = logEvent.MessageTemplate.Text,
            RenderedMessage = logEvent.RenderMessage(),
            ExceptionType = logEvent.Exception?.GetType().FullName,
            ExceptionMessage = logEvent.Exception?.Message,
            ExceptionStackTrace = logEvent.Exception?.ToString(),
            TraceId = activity?.TraceId.ToString()
                ?? (properties.TryGetValue("TraceId", out var tid) ? tid?.ToString() : null),
            SpanId = activity?.SpanId.ToString()
                ?? (properties.TryGetValue("SpanId", out var sid) ? sid?.ToString() : null),
            Properties = properties,
        };

        _buffer.Add(entry);
    }

    private static object? RenderPropertyValue(LogEventPropertyValue value)
    {
        return value switch
        {
            ScalarValue sv => sv.Value,
            SequenceValue seq => seq.Elements.Select(RenderPropertyValue).ToList(),
            StructureValue st => st.Properties.ToDictionary(p => p.Name, p => RenderPropertyValue(p.Value)),
            DictionaryValue dv => dv.Elements.ToDictionary(
                kvp => RenderPropertyValue(kvp.Key)?.ToString() ?? string.Empty,
                kvp => RenderPropertyValue(kvp.Value)),
            _ => value.ToString(),
        };
    }
}
