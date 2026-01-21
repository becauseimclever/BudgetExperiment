// <copyright file="ChatRole.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Chat;

/// <summary>
/// Defines the role of a chat message sender.
/// </summary>
public enum ChatRole
{
    /// <summary>
    /// Message from the user.
    /// </summary>
    User = 0,

    /// <summary>
    /// Message from the AI assistant.
    /// </summary>
    Assistant = 1,

    /// <summary>
    /// System message (e.g., context or instructions).
    /// </summary>
    System = 2,
}
