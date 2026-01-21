// <copyright file="IChatSessionRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Repository interface for ChatSession entity.
/// </summary>
public interface IChatSessionRepository : IReadRepository<ChatSession>, IWriteRepository<ChatSession>
{
    /// <summary>
    /// Gets the active chat session, if one exists.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active session, or null if none exists.</returns>
    Task<ChatSession?> GetActiveSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a session with its messages loaded.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="messageLimit">Maximum number of messages to load (most recent).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The session with messages, or null if not found.</returns>
    Task<ChatSession?> GetWithMessagesAsync(
        Guid sessionId,
        int messageLimit = 50,
        CancellationToken cancellationToken = default);
}
