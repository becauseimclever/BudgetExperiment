// <copyright file="ChatMessageRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IChatMessageRepository"/>.
/// </summary>
internal sealed class ChatMessageRepository : IChatMessageRepository
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessageRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public ChatMessageRepository(BudgetDbContext context)
    {
        this._context = context;
    }

    /// <inheritdoc />
    public async Task<ChatMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this._context.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatMessage>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await this._context.ChatMessages
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await this._context.ChatMessages.LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(ChatMessage entity, CancellationToken cancellationToken = default)
    {
        await this._context.ChatMessages.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(ChatMessage entity, CancellationToken cancellationToken = default)
    {
        this._context.ChatMessages.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatMessage>> GetBySessionAsync(
        Guid sessionId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        return await this._context.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatMessage>> GetPendingActionsAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return await this._context.ChatMessages
            .Where(m => m.SessionId == sessionId && m.ActionStatus == ChatActionStatus.Pending)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
