// <copyright file="IChatActionTypeParser.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Chat;

/// <summary>
/// Parser for a specific chat action type.
/// Implementations handle intent detection and action construction for one action category.
/// </summary>
public interface IChatActionTypeParser
{
    /// <summary>
    /// Tries to parse the input as a specific action.
    /// </summary>
    /// <param name="input">The raw AI response content to parse.</param>
    /// <param name="action">The parsed action if successful; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if the intent was recognized and the action was built successfully.</returns>
    bool TryParse(string input, out ChatAction? action);
}
