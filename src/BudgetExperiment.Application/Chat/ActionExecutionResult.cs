// <copyright file="ActionExecutionResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Chat;

/// <summary>
/// The result of executing a chat action.
/// </summary>
/// <param name="Success">Whether the action executed successfully.</param>
/// <param name="ActionType">The type of action that was executed.</param>
/// <param name="CreatedEntityId">The ID of any created entity.</param>
/// <param name="Message">A descriptive message about the result.</param>
/// <param name="ErrorMessage">Error message if execution failed.</param>
public sealed record ActionExecutionResult(
    bool Success,
    ChatActionType ActionType,
    Guid? CreatedEntityId,
    string Message,
    string? ErrorMessage = null);
