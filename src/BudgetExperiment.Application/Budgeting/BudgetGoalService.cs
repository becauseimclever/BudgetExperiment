// <copyright file="BudgetGoalService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Budgeting;

/// <summary>
/// Application service for budget goal operations.
/// </summary>
public sealed class BudgetGoalService : IBudgetGoalService
{
    private readonly IBudgetGoalRepository _repository;
    private readonly IBudgetCategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetGoalService"/> class.
    /// </summary>
    /// <param name="repository">The budget goal repository.</param>
    /// <param name="categoryRepository">The budget category repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public BudgetGoalService(IBudgetGoalRepository repository, IBudgetCategoryRepository categoryRepository, IUnitOfWork unitOfWork)
    {
        this._repository = repository;
        this._categoryRepository = categoryRepository;
        this._unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<BudgetGoalDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var goal = await this._repository.GetByIdAsync(id, cancellationToken);
        return goal is null ? null : BudgetMapper.ToDto(goal);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BudgetGoalDto>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var goals = await this._repository.GetByMonthAsync(year, month, cancellationToken);
        return goals.Select(BudgetMapper.ToDto).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BudgetGoalDto>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var goals = await this._repository.GetByCategoryAsync(categoryId, cancellationToken);
        return goals.Select(BudgetMapper.ToDto).ToList();
    }

    /// <inheritdoc/>
    public async Task<BudgetGoalDto?> SetGoalAsync(Guid categoryId, BudgetGoalSetDto dto, CancellationToken cancellationToken = default)
    {
        var category = await this._categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        if (category is null)
        {
            return null;
        }

        var existingGoal = await this._repository.GetByCategoryAndMonthAsync(categoryId, dto.Year, dto.Month, cancellationToken);
        var targetAmount = MoneyValue.Create(dto.TargetAmount.Currency, dto.TargetAmount.Amount);

        if (existingGoal is not null)
        {
            existingGoal.UpdateTarget(targetAmount);
            await this._unitOfWork.SaveChangesAsync(cancellationToken);
            return BudgetMapper.ToDto(existingGoal);
        }

        var goal = BudgetGoal.Create(categoryId, dto.Year, dto.Month, targetAmount);
        await this._repository.AddAsync(goal, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return BudgetMapper.ToDto(goal);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteGoalAsync(Guid categoryId, int year, int month, CancellationToken cancellationToken = default)
    {
        var goal = await this._repository.GetByCategoryAndMonthAsync(categoryId, year, month, cancellationToken);
        if (goal is null)
        {
            return false;
        }

        await this._repository.RemoveAsync(goal, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<CopyBudgetGoalsResult> CopyGoalsAsync(CopyBudgetGoalsRequest request, CancellationToken cancellationToken = default)
    {
        var result = new CopyBudgetGoalsResult();

        // Get all goals from the source month
        var sourceGoals = await this._repository.GetByMonthAsync(request.SourceYear, request.SourceMonth, cancellationToken);
        result.SourceGoalsCount = sourceGoals.Count;

        if (sourceGoals.Count == 0)
        {
            return result;
        }

        // Get existing goals in the target month
        var targetGoals = await this._repository.GetByMonthAsync(request.TargetYear, request.TargetMonth, cancellationToken);
        var targetGoalsByCategoryId = targetGoals.ToDictionary(g => g.CategoryId);

        foreach (var sourceGoal in sourceGoals)
        {
            if (targetGoalsByCategoryId.TryGetValue(sourceGoal.CategoryId, out var existingGoal))
            {
                if (request.OverwriteExisting)
                {
                    // Create a fresh MoneyValue to avoid EF Core tracking issues with owned entities
                    var targetAmount = MoneyValue.Create(sourceGoal.TargetAmount.Currency, sourceGoal.TargetAmount.Amount);
                    existingGoal.UpdateTarget(targetAmount);
                    result.GoalsUpdated++;
                }
                else
                {
                    result.GoalsSkipped++;
                }
            }
            else
            {
                // Create a fresh MoneyValue to avoid EF Core tracking issues with owned entities
                var targetAmount = MoneyValue.Create(sourceGoal.TargetAmount.Currency, sourceGoal.TargetAmount.Amount);
                var newGoal = BudgetGoal.Create(
                    sourceGoal.CategoryId,
                    request.TargetYear,
                    request.TargetMonth,
                    targetAmount);
                await this._repository.AddAsync(newGoal, cancellationToken);
                result.GoalsCreated++;
            }
        }

        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }
}
