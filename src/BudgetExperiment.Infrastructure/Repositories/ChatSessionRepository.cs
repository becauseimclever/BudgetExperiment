// <copyright file="ChatSessionRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IChatSessionRepository"/>.
/// </summary>
internal sealed class ChatSessionRepository : IChatSessionRepository
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatSessionRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public ChatSessionRepository(BudgetDbContext context)
    {
        this._context = context;
    }

    /// <inheritdoc />
    public async Task<ChatSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this._context.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatSession>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await this._context.ChatSessions
            .OrderByDescending(s => s.LastMessageAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this._context.ChatSessions.LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(ChatSession entity, CancellationToken cancellationToken = default)
    {
        await this._context.ChatSessions.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(ChatSession entity, CancellationToken cancellationToken = default)
    {
        this._context.ChatSessions.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<ChatSession?> GetActiveSessionAsync(CancellationToken cancellationToken = default)
    {
        return await this._context.ChatSessions
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.LastMessageAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ChatSession?> GetWithMessagesAsync(
        Guid sessionId,
        int messageLimit = 50,
        CancellationToken cancellationToken = default)
    {
        var session = await this._context.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null)
        {
            return null;
        }

        // Load messages separately to support limiting
        var messages = await this._context.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(messageLimit)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        // EF Core will automatically fix up the navigation property since we're tracking
        return session;
    }
}
