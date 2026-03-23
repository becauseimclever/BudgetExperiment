// <copyright file="ConfirmActionResponse.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Response from confirming an action.
/// </summary>
public sealed class ConfirmActionResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Success
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the action type that was executed.
    /// </summary>
    public ChatActionType ActionType
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the ID of the created entity.
    /// </summary>
    public Guid? CreatedEntityId
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the success message.
    /// </summary>
    public string? Message
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage
    {
        get; set;
    }
}
