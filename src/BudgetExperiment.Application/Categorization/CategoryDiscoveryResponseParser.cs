// <copyright file="CategoryDiscoveryResponseParser.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.Json;
using System.Text.RegularExpressions;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Parses AI responses from category discovery prompts into typed results.
/// </summary>
public static partial class CategoryDiscoveryResponseParser
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Parses an AI response string into a list of discovered categories.
    /// Handles raw JSON arrays and JSON embedded in markdown code blocks.
    /// </summary>
    /// <param name="aiResponse">The raw AI response content.</param>
    /// <returns>Parsed discovered categories, or empty list if parsing fails.</returns>
    public static IReadOnlyList<DiscoveredCategory> Parse(string aiResponse)
    {
        if (string.IsNullOrWhiteSpace(aiResponse))
        {
            return [];
        }

        var json = ExtractJson(aiResponse);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<CategoryDiscoveryItem>>(json, _jsonOptions);
            if (items is null || items.Count == 0)
            {
                return [];
            }

            return items
                .Where(IsValid)
                .Select(ToDiscoveredCategory)
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? ExtractJson(string response)
    {
        // Try to extract JSON from markdown code block first
        var codeBlockMatch = JsonCodeBlockPattern().Match(response);
        if (codeBlockMatch.Success)
        {
            return codeBlockMatch.Groups["json"].Value.Trim();
        }

        // Try to find a JSON array directly
        var trimmed = response.Trim();
        if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
        {
            return trimmed;
        }

        // Try to find an embedded JSON array
        var arrayStart = trimmed.IndexOf('[');
        var arrayEnd = trimmed.LastIndexOf(']');
        if (arrayStart >= 0 && arrayEnd > arrayStart)
        {
            return trimmed[arrayStart..(arrayEnd + 1)];
        }

        return null;
    }

    private static bool IsValid(CategoryDiscoveryItem item)
    {
        return !string.IsNullOrWhiteSpace(item.CategoryName)
            && item.Confidence is >= 0 and <= 1
            && item.MatchedDescriptions is { Count: > 0 }
            && !string.IsNullOrWhiteSpace(item.Reasoning);
    }

    private static DiscoveredCategory ToDiscoveredCategory(CategoryDiscoveryItem item)
    {
        return new DiscoveredCategory(
            CategoryName: item.CategoryName!.Trim(),
            Icon: item.Icon?.Trim(),
            Color: item.Color?.Trim(),
            Confidence: item.Confidence,
            Reasoning: item.Reasoning!.Trim(),
            MatchedDescriptions: item.MatchedDescriptions!);
    }

    [GeneratedRegex(@"```(?:json)?\s*(?<json>\[[\s\S]*?\])\s*```", RegexOptions.None)]
    private static partial Regex JsonCodeBlockPattern();

    /// <summary>
    /// Internal DTO for deserializing AI JSON responses.
    /// </summary>
    private sealed class CategoryDiscoveryItem
    {
        public string? CategoryName
        {
            get; set;
        }

        public string? Icon
        {
            get; set;
        }

        public string? Color
        {
            get; set;
        }

        public decimal Confidence
        {
            get; set;
        }

        public string? Reasoning
        {
            get; set;
        }

        public List<string>? MatchedDescriptions
        {
            get; set;
        }
    }
}
