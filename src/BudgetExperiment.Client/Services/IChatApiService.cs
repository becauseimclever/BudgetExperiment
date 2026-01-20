// <copyright file="IChatApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Service interface for communicating with the Chat API endpoints.
/// </summary>
public interface IChatApiService
{
    /// <summary>
    /// Gets or creates an active chat session.
    /// </summary>
    /// <returns>The active chat session.</returns>
    Task<ChatSessionDto?> GetOrCreateSessionAsync();

    /// <summary>
    /// Gets a chat session by ID.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>The chat session, or null if not found.</returns>
    Task<ChatSessionDto?> GetSessionAsync(Guid sessionId);

    /// <summary>
    /// Gets messages for a chat session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="limit">Maximum number of messages to return.</param>
    /// <returns>The list of messages.</returns>
    Task<IReadOnlyList<ChatMessageDto>> GetMessagesAsync(Guid sessionId, int limit = 50);

    /// <summary>
    /// Sends a message to a chat session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="content">The message content.</param>
    /// <returns>The response containing user and assistant messages.</returns>
    Task<SendMessageResponse?> SendMessageAsync(Guid sessionId, string content);

    /// <summary>
    /// Confirms a pending action.
    /// </summary>
    /// <param name="messageId">The message identifier containing the action.</param>
    /// <returns>The result of the action execution.</returns>
    Task<ConfirmActionResponse?> ConfirmActionAsync(Guid messageId);

    /// <summary>
    /// Cancels a pending action.
    /// </summary>
    /// <param name="messageId">The message identifier containing the action.</param>
    /// <returns>True if cancelled successfully.</returns>
    Task<bool> CancelActionAsync(Guid messageId);

    /// <summary>
    /// Closes a chat session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>True if closed successfully.</returns>
    Task<bool> CloseSessionAsync(Guid sessionId);
}
