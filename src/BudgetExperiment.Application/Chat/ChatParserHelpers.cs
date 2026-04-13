// <copyright file="ChatParserHelpers.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;
using System.Text.Json;

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Chat;

/// <summary>
/// Shared JSON parsing utilities used by per-action-type chat parsers.
/// </summary>
internal static class ChatParserHelpers
{
    /// <summary>
    /// Reads a GUID property from a JSON element, returning <c>null</c> if absent or invalid.
    /// </summary>
    /// <param name="element">The JSON element to read from.</param>
    /// <param name="propertyName">The property name to look up.</param>
    /// <returns>The parsed GUID, or <c>null</c>.</returns>
    internal static Guid? ParseGuid(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return Guid.TryParse(prop.GetString(), out var id) ? id : null;
        }

        return null;
    }

    /// <summary>
    /// Reads a date-only property from a JSON element, returning <c>null</c> if absent or unparseable.
    /// </summary>
    /// <param name="element">The JSON element to read from.</param>
    /// <param name="propertyName">The property name to look up.</param>
    /// <returns>The parsed <see cref="DateOnly"/>, or <c>null</c>.</returns>
    internal static DateOnly? ParseDate(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            var dateStr = prop.GetString();
            if (DateOnly.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }
        }

        return null;
    }

    /// <summary>
    /// Reads an entity ID from a clarification option JSON element.
    /// </summary>
    /// <param name="opt">The option element.</param>
    /// <returns>The parsed entity GUID, or <c>null</c>.</returns>
    internal static Guid? ParseEntityId(JsonElement opt)
    {
        if (!opt.TryGetProperty("entityId", out var eProp) || eProp.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return Guid.TryParse(eProp.GetString(), out var eid) ? eid : null;
    }

    /// <summary>
    /// Parses a <see cref="ClarificationOption"/> from a JSON element.
    /// </summary>
    /// <param name="opt">The option element.</param>
    /// <returns>The parsed clarification option.</returns>
    internal static ClarificationOption ParseClarificationOption(JsonElement opt)
    {
        var label = opt.TryGetProperty("label", out var lProp) ? lProp.GetString() ?? string.Empty : string.Empty;
        var value = opt.TryGetProperty("value", out var vProp) ? vProp.GetString() ?? string.Empty : string.Empty;
        return new ClarificationOption { Label = label, Value = value, EntityId = ParseEntityId(opt) };
    }

    /// <summary>
    /// Determines the <see cref="ClarificationNeededActionType"/> from a clarification JSON element.
    /// </summary>
    /// <param name="clarProp">The clarification JSON element.</param>
    /// <param name="fieldName">The field name provided in the clarification.</param>
    /// <returns>The resolved clarification type.</returns>
    internal static ClarificationNeededActionType ParseClarificationType(JsonElement clarProp, string fieldName)
    {
        if (clarProp.TryGetProperty("clarificationType", out var typeProp) && typeProp.ValueKind == JsonValueKind.String)
        {
            var typeText = typeProp.GetString();
            if (!string.IsNullOrWhiteSpace(typeText) &&
                Enum.TryParse<ClarificationNeededActionType>(typeText, true, out var parsed))
            {
                return parsed;
            }
        }

        return string.Equals(fieldName, "kakeiboCategory", StringComparison.OrdinalIgnoreCase)
            ? ClarificationNeededActionType.AskKakeiboCategory
            : ClarificationNeededActionType.General;
    }

    /// <summary>
    /// Resolves the <see cref="KakeiboCategory"/> for a category ID from the known categories list.
    /// </summary>
    /// <param name="categoryId">The category ID to look up.</param>
    /// <param name="categories">The available categories.</param>
    /// <returns>The kakeibo category, or <c>null</c> if not found.</returns>
    internal static KakeiboCategory? ResolveKakeiboCategory(Guid? categoryId, IReadOnlyList<CategoryInfo> categories)
    {
        if (!categoryId.HasValue)
        {
            return null;
        }

        var category = categories.FirstOrDefault(c => c.Id == categoryId.Value);
        return category?.KakeiboCategory;
    }

    /// <summary>
    /// Parses a <see cref="RecurrencePatternValue"/> from a data JSON element.
    /// </summary>
    /// <param name="data">The JSON element containing recurrence fields.</param>
    /// <returns>The parsed recurrence pattern.</returns>
    internal static RecurrencePatternValue? ParseRecurrencePattern(JsonElement data)
    {
        var frequencyStr = data.TryGetProperty("frequency", out var freqProp)
            ? freqProp.GetString()?.ToLowerInvariant() ?? "monthly"
            : "monthly";

        var interval = data.TryGetProperty("interval", out var intProp) ? intProp.GetInt32() : 1;
        var dayOfMonth = data.TryGetProperty("dayOfMonth", out var domProp) && domProp.ValueKind == JsonValueKind.Number
            ? domProp.GetInt32()
            : (int?)null;
        var dayOfWeekStr = data.TryGetProperty("dayOfWeek", out var dowProp) && dowProp.ValueKind == JsonValueKind.String
            ? dowProp.GetString()
            : null;
        var dayOfWeek = !string.IsNullOrEmpty(dayOfWeekStr) && Enum.TryParse<DayOfWeek>(dayOfWeekStr, true, out var dow)
            ? dow
            : (DayOfWeek?)null;

        return frequencyStr switch
        {
            "daily" => RecurrencePatternValue.CreateDaily(interval),
            "weekly" when dayOfWeek.HasValue => RecurrencePatternValue.CreateWeekly(interval, dayOfWeek.Value),
            "weekly" => RecurrencePatternValue.CreateWeekly(interval, DayOfWeek.Monday),
            "biweekly" when dayOfWeek.HasValue => RecurrencePatternValue.CreateBiWeekly(dayOfWeek.Value),
            "biweekly" => RecurrencePatternValue.CreateBiWeekly(DayOfWeek.Friday),
            "monthly" when dayOfMonth.HasValue => RecurrencePatternValue.CreateMonthly(interval, dayOfMonth.Value),
            "monthly" => RecurrencePatternValue.CreateMonthly(interval, 1),
            "quarterly" when dayOfMonth.HasValue => RecurrencePatternValue.CreateQuarterly(dayOfMonth.Value),
            "quarterly" => RecurrencePatternValue.CreateQuarterly(1),
            "yearly" when dayOfMonth.HasValue => RecurrencePatternValue.CreateYearly(1, dayOfMonth.Value),
            "yearly" => RecurrencePatternValue.CreateYearly(1, 1),
            _ => RecurrencePatternValue.CreateMonthly(1, 1),
        };
    }
}
