// <copyright file="RuleSuggestionRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRuleSuggestionRepository"/>.
/// </summary>
internal sealed class RuleSuggestionRepository : IRuleSuggestionRepository
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleSuggestionRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public RuleSuggestionRepository(BudgetDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<RuleSuggestion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.RuleSuggestions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RuleSuggestion>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.RuleSuggestions
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.RuleSuggestions.LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(RuleSuggestion entity, CancellationToken cancellationToken = default)
    {
        await _context.RuleSuggestions.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(RuleSuggestion entity, CancellationToken cancellationToken = default)
    {
        _context.RuleSuggestions.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RuleSuggestion>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _context.RuleSuggestions
            .Where(s => s.Status == SuggestionStatus.Pending)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RuleSuggestion>> GetPendingByTypeAsync(SuggestionType type, CancellationToken cancellationToken = default)
    {
        return await _context.RuleSuggestions
            .Where(s => s.Status == SuggestionStatus.Pending && s.Type == type)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RuleSuggestion>> GetByStatusAsync(SuggestionStatus status, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.RuleSuggestions
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsPendingWithPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        return await _context.RuleSuggestions
            .AnyAsync(s => s.Status == SuggestionStatus.Pending && s.SuggestedPattern == pattern, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsPendingForRuleAsync(Guid ruleId, SuggestionType type, CancellationToken cancellationToken = default)
    {
        return await _context.RuleSuggestions
            .AnyAsync(
                s => s.Status == SuggestionStatus.Pending &&
                     s.Type == type &&
                     s.TargetRuleId == ruleId,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsPendingForRulesAsync(IReadOnlyList<Guid> ruleIds, SuggestionType type, CancellationToken cancellationToken = default)
    {
        if (ruleIds == null || ruleIds.Count < 2)
        {
            return false;
        }

        // Check if there's a pending suggestion that involves the same set of rules
        // For RuleConflict and RuleConsolidation types, we check ConflictingRuleIds
        var pendingSuggestions = await _context.RuleSuggestions
            .Where(s => s.Status == SuggestionStatus.Pending && s.Type == type)
            .ToListAsync(cancellationToken);

        foreach (var suggestion in pendingSuggestions)
        {
            var existingIds = suggestion.ConflictingRuleIds;
            if (existingIds.Count == ruleIds.Count &&
                existingIds.All(id => ruleIds.Contains(id)))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<RuleSuggestion> suggestions, CancellationToken cancellationToken = default)
    {
        await _context.RuleSuggestions.AddRangeAsync(suggestions, cancellationToken);
    }
}
