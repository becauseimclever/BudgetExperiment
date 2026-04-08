// <copyright file="ClarificationNeededActionType.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Shared.Chat;

/// <summary>
/// Defines the type of clarification request from the chat assistant.
/// </summary>
public enum ClarificationNeededActionType
{
    /// <summary>
    /// General clarification with options provided by the assistant.
    /// </summary>
    General = 0,

    /// <summary>
    /// Asks the user to select a Kakeibo spending category.
    /// </summary>
    AskKakeiboCategory = 1,
}
