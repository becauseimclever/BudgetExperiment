// <copyright file="SendMessageResponse.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

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
