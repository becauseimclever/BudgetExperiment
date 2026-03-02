// <copyright file="ChatMessageDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

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
