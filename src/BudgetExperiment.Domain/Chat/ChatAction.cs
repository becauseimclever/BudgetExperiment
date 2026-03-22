// <copyright file="ChatAction.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Chat;

/// <summary>
/// Base record for chat actions that can be executed.
/// </summary>
public abstract record ChatAction
{
    /// <summary>
    /// Gets the type of this action.
    /// </summary>
    public abstract ChatActionType Type
    {
        get;
    }

    /// <summary>
    /// Gets a human-readable summary of the action for preview.
    /// </summary>
    /// <returns>A formatted summary string.</returns>
    public abstract string GetPreviewSummary();
}
