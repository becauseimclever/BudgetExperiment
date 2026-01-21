// <copyright file="IChatService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Chat;

/// <summary>
/// Service for managing chat sessions and processing chat commands.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Gets or creates a chat session for the specified user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active chat session.</returns>
    Task<ChatSession> GetOrCreateSessionAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an existing chat session by ID.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The chat session, or null if not found.</returns>
    Task<ChatSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sessions for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of chat sessions.</returns>
    Task<IReadOnlyList<ChatSession>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all messages for a chat session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="limit">Maximum number of messages to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of messages ordered by timestamp, or null if session not found.</returns>
    Task<IReadOnlyList<ChatMessage>?> GetMessagesAsync(Guid sessionId, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a user message and gets an AI response.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="content">The message content.</param>
    /// <param name="context">Optional UI context for the chat.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of processing the message.</returns>
    Task<ChatResult> SendMessageAsync(
        Guid sessionId,
        string content,
        ChatContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a pending action from a chat message.
    /// </summary>
    /// <param name="messageId">The message identifier containing the action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of executing the action.</returns>
    Task<ActionExecutionResult> ConfirmActionAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a pending action from a chat message.
    /// </summary>
    /// <param name="messageId">The message identifier containing the action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the action was cancelled, false if not found.</returns>
    Task<bool> CancelActionAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a chat session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the session was closed, false if not found.</returns>
    Task<bool> CloseSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

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
