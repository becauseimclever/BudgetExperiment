// <copyright file="ChatActionDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for a chat action preview.
/// </summary>
public sealed class ChatActionDto
{
    /// <summary>
    /// Gets or sets the action type.
    /// </summary>
    public ChatActionType Type
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the preview summary of the action.
    /// </summary>
    public string PreviewSummary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the account ID for the action.
    /// </summary>
    public Guid? AccountId
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the account name for the action.
    /// </summary>
    public string? AccountName
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the amount for the action.
    /// </summary>
    public decimal? Amount
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the date for the action.
    /// </summary>
    public DateOnly? Date
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the description for the action.
    /// </summary>
    public string? Description
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the category ID for the action.
    /// </summary>
    public Guid? CategoryId
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the category name for the action.
    /// </summary>
    public string? CategoryName
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the source account ID for transfer actions.
    /// </summary>
    public Guid? FromAccountId
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the source account name for transfer actions.
    /// </summary>
    public string? FromAccountName
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the destination account ID for transfer actions.
    /// </summary>
    public Guid? ToAccountId
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the destination account name for transfer actions.
    /// </summary>
    public string? ToAccountName
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the clarification question for clarification needed actions.
    /// </summary>
    public string? ClarificationQuestion
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the field name that needs clarification.
    /// </summary>
    public string? ClarificationFieldName
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the options for clarification needed actions.
    /// </summary>
    public IReadOnlyList<ClarificationOptionDto>? Options
    {
        get; set;
    }
}
