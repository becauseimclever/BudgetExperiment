// <copyright file="ChatResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Chat;

/// <summary>
/// The result of processing a chat message.
/// </summary>
/// <param name="Success">Whether the message was processed successfully.</param>
/// <param name="UserMessage">The user's message that was saved.</param>
/// <param name="AssistantMessage">The assistant's response message.</param>
/// <param name="ErrorMessage">Error message if processing failed.</param>
public sealed record ChatResult(
    bool Success,
    ChatMessage? UserMessage,
    ChatMessage? AssistantMessage,
    string? ErrorMessage = null);
