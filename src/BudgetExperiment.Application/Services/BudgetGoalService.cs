// <copyright file="BudgetGoalService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Mapping;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

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
        return goal is null ? null : DomainToDtoMapper.ToDto(goal);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BudgetGoalDto>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var goals = await this._repository.GetByMonthAsync(year, month, cancellationToken);
        return goals.Select(DomainToDtoMapper.ToDto).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BudgetGoalDto>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var goals = await this._repository.GetByCategoryAsync(categoryId, cancellationToken);
        return goals.Select(DomainToDtoMapper.ToDto).ToList();
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
            return DomainToDtoMapper.ToDto(existingGoal);
        }

        var goal = BudgetGoal.Create(categoryId, dto.Year, dto.Month, targetAmount);
        await this._repository.AddAsync(goal, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return DomainToDtoMapper.ToDto(goal);
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
}
