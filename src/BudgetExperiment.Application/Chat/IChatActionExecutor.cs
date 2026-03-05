// <copyright file="IChatActionExecutor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Chat;

/// <summary>
/// Executes chat actions by dispatching to the appropriate domain service.
/// </summary>
public interface IChatActionExecutor
{
    /// <summary>
    /// Executes the specified chat action.
    /// </summary>
    /// <param name="action">The chat action to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of executing the action.</returns>
    Task<ActionExecutionResult> ExecuteActionAsync(ChatAction action, CancellationToken cancellationToken = default);
}
