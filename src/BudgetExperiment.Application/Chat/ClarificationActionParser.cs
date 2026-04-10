// <copyright file="ClarificationActionParser.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.Json;

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Chat;

/// <summary>
/// Parses clarification-needed JSON into a <see cref="ClarificationNeededAction"/>.
/// </summary>
internal static class ClarificationActionParser
{
    /// <summary>
    /// Builds a <see cref="ClarificationNeededAction"/> from a clarification JSON element.
    /// </summary>
    /// <param name="clarProp">The <c>clarification</c> JSON element from the AI response.</param>
    /// <returns>The constructed clarification action.</returns>
    internal static ClarificationNeededAction Parse(JsonElement clarProp)
    {
        var question = clarProp.TryGetProperty("question", out var qProp)
            ? qProp.GetString() ?? "Could you provide more details?"
            : "Could you provide more details?";

        var fieldName = clarProp.TryGetProperty("field", out var fProp)
            ? fProp.GetString() ?? "unknown"
            : "unknown";
        var clarificationType = ChatParserHelpers.ParseClarificationType(clarProp, fieldName);

        var options = new List<ClarificationOption>();
        if (clarProp.TryGetProperty("options", out var optsProp) && optsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var opt in optsProp.EnumerateArray())
            {
                options.Add(ChatParserHelpers.ParseClarificationOption(opt));
            }
        }

        return new ClarificationNeededAction
        {
            Question = question,
            ClarificationType = clarificationType,
            FieldName = fieldName,
            Options = options,
        };
    }
}
