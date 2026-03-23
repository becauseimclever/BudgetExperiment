// <copyright file="RecurrencePatternJsonConverter.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;

using BudgetExperiment.Domain;

namespace BudgetExperiment.Infrastructure.Persistence.Configurations;

/// <summary>
/// JSON converter for <see cref="RecurrencePatternValue"/> which has a private constructor.
/// </summary>
internal sealed class RecurrencePatternJsonConverter : JsonConverter<RecurrencePatternValue>
{
    /// <inheritdoc />
    public override RecurrencePatternValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var frequency = Enum.Parse<RecurrenceFrequency>(root.GetProperty("frequency").GetString()!, true);
        var interval = root.GetProperty("interval").GetInt32();
        int? dayOfMonth = root.TryGetProperty("dayOfMonth", out var domProp) && domProp.ValueKind != JsonValueKind.Null
            ? domProp.GetInt32()
            : null;
        DayOfWeek? dayOfWeek = root.TryGetProperty("dayOfWeek", out var dowProp) && dowProp.ValueKind != JsonValueKind.Null
            ? Enum.Parse<DayOfWeek>(dowProp.GetString()!, true)
            : null;
        int? monthOfYear = root.TryGetProperty("monthOfYear", out var moyProp) && moyProp.ValueKind != JsonValueKind.Null
            ? moyProp.GetInt32()
            : null;

        // Use the appropriate factory method based on frequency
        return frequency switch
        {
            RecurrenceFrequency.Daily => RecurrencePatternValue.CreateDaily(interval),
            RecurrenceFrequency.Weekly when dayOfWeek.HasValue => RecurrencePatternValue.CreateWeekly(interval, dayOfWeek.Value),
            RecurrenceFrequency.BiWeekly when dayOfWeek.HasValue => RecurrencePatternValue.CreateBiWeekly(dayOfWeek.Value),
            RecurrenceFrequency.Monthly when dayOfMonth.HasValue => RecurrencePatternValue.CreateMonthly(interval, dayOfMonth.Value),
            RecurrenceFrequency.Quarterly when dayOfMonth.HasValue => RecurrencePatternValue.CreateQuarterly(dayOfMonth.Value),
            RecurrenceFrequency.Yearly when dayOfMonth.HasValue && monthOfYear.HasValue => RecurrencePatternValue.CreateYearly(monthOfYear.Value, dayOfMonth.Value),
            _ => RecurrencePatternValue.CreateDaily(1), // Default fallback
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, RecurrencePatternValue value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("frequency", value.Frequency.ToString());
        writer.WriteNumber("interval", value.Interval);

        if (value.DayOfMonth.HasValue)
        {
            writer.WriteNumber("dayOfMonth", value.DayOfMonth.Value);
        }
        else
        {
            writer.WriteNull("dayOfMonth");
        }

        if (value.DayOfWeek.HasValue)
        {
            writer.WriteString("dayOfWeek", value.DayOfWeek.Value.ToString());
        }
        else
        {
            writer.WriteNull("dayOfWeek");
        }

        if (value.MonthOfYear.HasValue)
        {
            writer.WriteNumber("monthOfYear", value.MonthOfYear.Value);
        }
        else
        {
            writer.WriteNull("monthOfYear");
        }

        writer.WriteEndObject();
    }
}
