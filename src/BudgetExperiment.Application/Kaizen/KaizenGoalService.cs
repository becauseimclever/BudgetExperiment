// <copyright file="KaizenGoalService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain.Kaizen;

namespace BudgetExperiment.Application.Kaizen;

/// <summary>
/// Application service for Kaizen micro-goal operations.
/// </summary>
public sealed class KaizenGoalService : IKaizenGoalService
{
    private readonly IKaizenGoalRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="KaizenGoalService"/> class.
    /// </summary>
    /// <param name="repository">The Kaizen goal repository.</param>
    public KaizenGoalService(IKaizenGoalRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public async Task<KaizenGoalDto?> GetByWeekAsync(DateOnly weekStart, Guid userId, CancellationToken ct = default)
    {
        var goal = await _repository.GetByUserWeekAsync(userId, weekStart, ct);
        return goal is null ? null : KaizenMapper.ToDto(goal);
    }

    /// <inheritdoc />
    public async Task<KaizenGoalDto> CreateAsync(DateOnly weekStart, CreateKaizenGoalDto dto, Guid userId, CancellationToken ct = default)
    {
        var kakeiboCategory = ParseKakeiboCategory(dto.KakeiboCategory);
        var goal = KaizenGoal.Create(userId, weekStart, dto.Description, dto.TargetAmount, kakeiboCategory);

        await _repository.AddAsync(goal, ct);
        await _repository.SaveChangesAsync(ct);

        return KaizenMapper.ToDto(goal);
    }

    /// <inheritdoc />
    public async Task<KaizenGoalDto?> UpdateAsync(Guid goalId, UpdateKaizenGoalDto dto, Guid userId, CancellationToken ct = default)
    {
        var goal = await _repository.GetByIdAsync(goalId, ct);
        if (goal is null || goal.UserId != userId)
        {
            return null;
        }

        var kakeiboCategory = ParseKakeiboCategory(dto.KakeiboCategory);
        goal.Update(dto.Description, dto.TargetAmount, kakeiboCategory, dto.IsAchieved);

        await _repository.SaveChangesAsync(ct);
        return KaizenMapper.ToDto(goal);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<KaizenGoalDto>> GetRangeAsync(DateOnly fromWeek, DateOnly toWeek, Guid userId, CancellationToken ct = default)
    {
        var goals = await _repository.GetRangeAsync(userId, fromWeek, toWeek, ct);
        return goals.Select(KaizenMapper.ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid goalId, Guid userId, CancellationToken ct = default)
    {
        var goal = await _repository.GetByIdAsync(goalId, ct);
        if (goal is null || goal.UserId != userId)
        {
            return false;
        }

        await _repository.RemoveAsync(goal, ct);
        await _repository.SaveChangesAsync(ct);
        return true;
    }

    private static global::BudgetExperiment.Shared.Budgeting.KakeiboCategory? ParseKakeiboCategory(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (Enum.TryParse<global::BudgetExperiment.Shared.Budgeting.KakeiboCategory>(value, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new DomainException($"Invalid Kakeibo category: '{value}'. Valid values are: Essentials, Wants, Culture, Unexpected.");
    }
}
