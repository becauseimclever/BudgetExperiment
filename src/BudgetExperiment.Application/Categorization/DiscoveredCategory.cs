// <copyright file="DiscoveredCategory.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Represents a category discovered by AI analysis of unmatched transaction descriptions.
/// </summary>
/// <param name="CategoryName">The suggested category name.</param>
/// <param name="Icon">Optional suggested icon (emoji).</param>
/// <param name="Color">Optional suggested color (hex code).</param>
/// <param name="Confidence">Confidence score (0.0–1.0) based on cluster coherence.</param>
/// <param name="Reasoning">Brief explanation of why these transactions belong together.</param>
/// <param name="MatchedDescriptions">Transaction descriptions the AI grouped into this category.</param>
public sealed record DiscoveredCategory(
    string CategoryName,
    string? Icon,
    string? Color,
    decimal Confidence,
    string Reasoning,
    IReadOnlyList<string> MatchedDescriptions);
