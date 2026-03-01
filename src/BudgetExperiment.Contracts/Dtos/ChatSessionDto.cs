// <copyright file="ChatSessionDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

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
