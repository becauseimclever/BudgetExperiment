// <copyright file="INaturalLanguageParser.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Chat;

/// <summary>
/// Service for parsing natural language commands into structured chat actions.
/// </summary>
public interface INaturalLanguageParser
{
    /// <summary>
    /// Parses a natural language command into a structured action.
    /// </summary>
    /// <param name="input">The user's natural language input.</param>
    /// <param name="accounts">Available accounts for matching.</param>
    /// <param name="categories">Available categories for matching.</param>
    /// <param name="context">Optional context from the current UI state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed result containing an action or error.</returns>
    Task<ParseResult> ParseCommandAsync(
        string input,
        IReadOnlyList<AccountInfo> accounts,
        IReadOnlyList<CategoryInfo> categories,
        ChatContext? context = null,
        CancellationToken cancellationToken = default);
}
