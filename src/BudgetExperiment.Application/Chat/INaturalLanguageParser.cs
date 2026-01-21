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

/// <summary>
/// The result of parsing a natural language command.
/// </summary>
/// <param name="Success">Whether parsing succeeded.</param>
/// <param name="Action">The parsed action, if successful.</param>
/// <param name="ResponseText">Natural language response to show the user.</param>
/// <param name="ErrorMessage">Error message if parsing failed.</param>
/// <param name="Confidence">Confidence level of the parse (0.0 to 1.0).</param>
public sealed record ParseResult(
    bool Success,
    ChatAction? Action,
    string ResponseText,
    string? ErrorMessage = null,
    decimal Confidence = 0m);

/// <summary>
/// Information about an account for natural language parsing.
/// </summary>
/// <param name="Id">The account identifier.</param>
/// <param name="Name">The account name.</param>
/// <param name="Type">The account type.</param>
public sealed record AccountInfo(Guid Id, string Name, AccountType Type);

/// <summary>
/// Information about a category for natural language parsing.
/// </summary>
/// <param name="Id">The category identifier.</param>
/// <param name="Name">The category name.</param>
public sealed record CategoryInfo(Guid Id, string Name);

/// <summary>
/// Context from the current UI state to inform AI responses.
/// </summary>
/// <param name="CurrentAccountId">The currently selected account ID.</param>
/// <param name="CurrentAccountName">The currently selected account name.</param>
/// <param name="CurrentCategoryId">The currently selected category ID.</param>
/// <param name="CurrentCategoryName">The currently selected category name.</param>
/// <param name="CurrentDate">The current date being viewed.</param>
/// <param name="CurrentPage">The current page/route in the app.</param>
public sealed record ChatContext(
    Guid? CurrentAccountId = null,
    string? CurrentAccountName = null,
    Guid? CurrentCategoryId = null,
    string? CurrentCategoryName = null,
    DateOnly? CurrentDate = null,
    string? CurrentPage = null);
