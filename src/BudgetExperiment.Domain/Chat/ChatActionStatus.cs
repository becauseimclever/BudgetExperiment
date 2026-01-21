// <copyright file="ChatActionStatus.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Chat;

/// <summary>
/// Defines the status of a chat action.
/// </summary>
public enum ChatActionStatus
{
    /// <summary>
    /// No action associated with the message.
    /// </summary>
    None = 0,

    /// <summary>
    /// Action is awaiting user confirmation.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// User confirmed the action, entity was created.
    /// </summary>
    Confirmed = 2,

    /// <summary>
    /// User cancelled the action.
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Action execution failed.
    /// </summary>
    Failed = 4,
}
