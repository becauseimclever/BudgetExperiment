// <copyright file="ChatDtos.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for a chat session.
/// </summary>
public sealed class ChatSessionDto
{
    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the session is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the session creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the last message timestamp.
    /// </summary>
    public DateTime LastMessageAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the recent messages in the session.
    /// </summary>
    public IReadOnlyList<ChatMessageDto> Messages { get; set; } = [];
}

/// <summary>
/// DTO for a chat message.
/// </summary>
public sealed class ChatMessageDto
{
    /// <summary>
    /// Gets or sets the message identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Gets or sets the message role.
    /// </summary>
    public ChatRole Role { get; set; }

    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the associated action, if any.
    /// </summary>
    public ChatActionDto? Action { get; set; }

    /// <summary>
    /// Gets or sets the action status.
    /// </summary>
    public ChatActionStatus ActionStatus { get; set; }
}

/// <summary>
/// DTO for a chat action preview.
/// </summary>
public sealed class ChatActionDto
{
    /// <summary>
    /// Gets or sets the action type.
    /// </summary>
    public ChatActionType Type { get; set; }

    /// <summary>
    /// Gets or sets the preview summary of the action.
    /// </summary>
    public string PreviewSummary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the account ID for the action.
    /// </summary>
    public Guid? AccountId { get; set; }

    /// <summary>
    /// Gets or sets the account name for the action.
    /// </summary>
    public string? AccountName { get; set; }

    /// <summary>
    /// Gets or sets the amount for the action.
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Gets or sets the date for the action.
    /// </summary>
    public DateOnly? Date { get; set; }

    /// <summary>
    /// Gets or sets the description for the action.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the category ID for the action.
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the category name for the action.
    /// </summary>
    public string? CategoryName { get; set; }

    /// <summary>
    /// Gets or sets the source account ID for transfer actions.
    /// </summary>
    public Guid? FromAccountId { get; set; }

    /// <summary>
    /// Gets or sets the source account name for transfer actions.
    /// </summary>
    public string? FromAccountName { get; set; }

    /// <summary>
    /// Gets or sets the destination account ID for transfer actions.
    /// </summary>
    public Guid? ToAccountId { get; set; }

    /// <summary>
    /// Gets or sets the destination account name for transfer actions.
    /// </summary>
    public string? ToAccountName { get; set; }

    /// <summary>
    /// Gets or sets the clarification question for clarification needed actions.
    /// </summary>
    public string? ClarificationQuestion { get; set; }

    /// <summary>
    /// Gets or sets the field name that needs clarification.
    /// </summary>
    public string? ClarificationFieldName { get; set; }

    /// <summary>
    /// Gets or sets the options for clarification needed actions.
    /// </summary>
    public IReadOnlyList<ClarificationOptionDto>? Options { get; set; }
}

/// <summary>
/// DTO for a clarification option.
/// </summary>
public sealed class ClarificationOptionDto
{
    /// <summary>
    /// Gets or sets the display label for the option.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value to use when selected.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional entity identifier.
    /// </summary>
    public Guid? EntityId { get; set; }
}

/// <summary>
/// Request to send a message to a chat session.
/// </summary>
public sealed class SendMessageRequest
{
    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Response from sending a message.
/// </summary>
public sealed class SendMessageResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the user message that was created.
    /// </summary>
    public ChatMessageDto? UserMessage { get; set; }

    /// <summary>
    /// Gets or sets the assistant's response message.
    /// </summary>
    public ChatMessageDto? AssistantMessage { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Response from confirming an action.
/// </summary>
public sealed class ConfirmActionResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the action type that was executed.
    /// </summary>
    public ChatActionType ActionType { get; set; }

    /// <summary>
    /// Gets or sets the ID of the created entity.
    /// </summary>
    public Guid? CreatedEntityId { get; set; }

    /// <summary>
    /// Gets or sets the success message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
