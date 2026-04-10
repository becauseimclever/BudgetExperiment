// <copyright file="RuleSuggestionJsonExtractor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.Json;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Extracts raw JSON content from AI response text that may contain
/// markdown fences, preamble text, or other non-JSON wrapping.
/// </summary>
internal static class RuleSuggestionJsonExtractor
{
    /// <summary>
    /// Extracts the first complete JSON object from raw AI response text.
    /// AI models frequently wrap JSON in markdown code blocks or add preamble
    /// text despite being instructed not to. This method strips that wrapping
    /// so <see cref="JsonSerializer"/> can parse the content.
    /// </summary>
    /// <param name="content">The raw AI response text.</param>
    /// <returns>The extracted JSON string.</returns>
    /// <exception cref="JsonException">Thrown when no JSON object is found in the content.</exception>
    internal static string ExtractJson(string content)
    {
        // Try direct parse first — fastest path for well-formed responses
        var trimmed = content.Trim();
        if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
        {
            return trimmed;
        }

        // Strip markdown code fences: ```json ... ``` or ``` ... ```
        var fenceStart = content.IndexOf("```", StringComparison.Ordinal);
        if (fenceStart >= 0)
        {
            content = ExtractFromCodeFence(content, fenceStart);
        }

        // Fall back to bracket matching
        var jsonStart = content.IndexOf('{');
        var jsonEnd = content.LastIndexOf('}');

        if (jsonStart < 0 || jsonEnd < 0 || jsonEnd <= jsonStart)
        {
            throw new JsonException("No JSON object found in AI response.");
        }

        return content.Substring(jsonStart, jsonEnd - jsonStart + 1);
    }

    private static string ExtractFromCodeFence(string content, int fenceStart)
    {
        var lineEnd = content.IndexOf('\n', fenceStart);
        if (lineEnd < 0)
        {
            return content;
        }

        var fenceEnd = content.IndexOf("```", lineEnd, StringComparison.Ordinal);
        if (fenceEnd <= lineEnd)
        {
            return content;
        }

        return content.Substring(lineEnd + 1, fenceEnd - lineEnd - 1);
    }
}
