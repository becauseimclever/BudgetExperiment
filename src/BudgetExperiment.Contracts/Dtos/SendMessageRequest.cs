// <copyright file="SendMessageRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request to send a message to a chat session.
/// </summary>
public sealed class SendMessageRequest
{
    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional context from the client UI.
    /// </summary>
    public ChatContextDto? Context
    {
        get; set;
    }
}
