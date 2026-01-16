// <copyright file="BudgetProgressService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Mapping;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Application service for budget progress tracking operations.
/// </summary>
public sealed class BudgetProgressService : IBudgetProgressService
{
    private readonly IBudgetGoalRepository _goalRepository;
    private readonly IBudgetCategoryRepository _categoryRepository;
    private readonly ITransactionRepository _transactionRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetProgressService"/> class.
    /// </summary>
    /// <param name="goalRepository">The budget goal repository.</param>
    /// <param name="categoryRepository">The budget category repository.</param>
    /// <param name="transactionRepository">The transaction repository.</param>
    public BudgetProgressService(
        IBudgetGoalRepository goalRepository,
        IBudgetCategoryRepository categoryRepository,
        ITransactionRepository transactionRepository)
    {
        this._goalRepository = goalRepository;
        this._categoryRepository = categoryRepository;
        this._transactionRepository = transactionRepository;
    }

    /// <inheritdoc/>
    public async Task<BudgetProgressDto?> GetProgressAsync(Guid categoryId, int year, int month, CancellationToken cancellationToken = default)
    {
        var goal = await this._goalRepository.GetByCategoryAndMonthAsync(categoryId, year, month, cancellationToken);
        if (goal is null)
        {
            return null;
        }

        var category = await this._categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        if (category is null)
        {
            return null;
        }

        var spent = await this._transactionRepository.GetSpendingByCategoryAsync(categoryId, year, month, cancellationToken);
        var progress = BudgetProgress.Create(
            category.Id,
            category.Name,
            category.Icon,
            category.Color,
            goal.TargetAmount,
            spent,
            transactionCount: 0);
        return DomainToDtoMapper.ToDto(progress);
    }

    /// <inheritdoc/>
    public async Task<BudgetSummaryDto> GetMonthlySummaryAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var goals = await this._goalRepository.GetByMonthAsync(year, month, cancellationToken);
        var categoryProgress = new List<BudgetProgressDto>();
        var totalBudgeted = MoneyValue.Create("USD", 0m);
        var totalSpent = MoneyValue.Create("USD", 0m);

        foreach (var goal in goals)
        {
            var category = await this._categoryRepository.GetByIdAsync(goal.CategoryId, cancellationToken);
            if (category is null)
            {
                continue;
            }

            var spent = await this._transactionRepository.GetSpendingByCategoryAsync(goal.CategoryId, year, month, cancellationToken);
            var progress = BudgetProgress.Create(
                category.Id,
                category.Name,
                category.Icon,
                category.Color,
                goal.TargetAmount,
                spent,
                transactionCount: 0);
            categoryProgress.Add(DomainToDtoMapper.ToDto(progress));
            totalBudgeted = MoneyValue.Create(totalBudgeted.Currency, totalBudgeted.Amount + goal.TargetAmount.Amount);
            totalSpent = MoneyValue.Create(totalSpent.Currency, totalSpent.Amount + spent.Amount);
        }

        // Calculate category status counts
        var categoriesOnTrack = categoryProgress.Count(p => p.Status == nameof(BudgetStatus.OnTrack));
        var categoriesWarning = categoryProgress.Count(p => p.Status == nameof(BudgetStatus.Warning));
        var categoriesOverBudget = categoryProgress.Count(p => p.Status == nameof(BudgetStatus.OverBudget));

        return new BudgetSummaryDto
        {
            Year = year,
            Month = month,
            CategoryProgress = categoryProgress,
            TotalBudgeted = DomainToDtoMapper.ToDto(totalBudgeted),
            TotalSpent = DomainToDtoMapper.ToDto(totalSpent),
            TotalRemaining = DomainToDtoMapper.ToDto(MoneyValue.Create(totalBudgeted.Currency, totalBudgeted.Amount - totalSpent.Amount)),
            CategoriesOnTrack = categoriesOnTrack,
            CategoriesWarning = categoriesWarning,
            CategoriesOverBudget = categoriesOverBudget,
        };
    }
}
