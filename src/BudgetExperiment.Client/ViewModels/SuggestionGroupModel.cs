// <copyright file="SuggestionGroupModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.ViewModels;

/// <summary>
/// Represents a group of AI suggestions with a header and batch action support.
/// </summary>
public sealed class SuggestionGroupModel
{
    /// <summary>
    /// Gets or sets the group key (e.g., "NewCategories", "NewRules", "Optimizations", "ConflictsCleanup").
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// Gets or sets the display title for the group header.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Gets or sets the icon name for the group header.
    /// </summary>
    public required string IconName { get; set; }

    /// <summary>
    /// Gets or sets the suggestion items in this group (can be RuleSuggestionDto or CategorySuggestionDto).
    /// </summary>
    public required IReadOnlyList<object> Items { get; set; }

    /// <summary>
    /// Gets or sets the count of high-confidence items in this group (confidence >= 80%).
    /// </summary>
    public int HighConfidenceCount { get; set; }
}
