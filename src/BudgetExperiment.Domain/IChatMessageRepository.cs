// <copyright file="IChatMessageRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Repository interface for ChatMessage entity.
/// </summary>
public interface IChatMessageRepository : IReadRepository<ChatMessage>, IWriteRepository<ChatMessage>
{
    /// <summary>
    /// Gets messages for a session in chronological order.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="limit">Maximum number of messages to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The messages ordered by creation time.</returns>
    Task<IReadOnlyList<ChatMessage>> GetBySessionAsync(
        Guid sessionId,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets messages with pending actions.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Messages that have pending actions awaiting user confirmation.</returns>
    Task<IReadOnlyList<ChatMessage>> GetPendingActionsAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}
