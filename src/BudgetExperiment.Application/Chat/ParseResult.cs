// <copyright file="ParseResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Chat;

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
