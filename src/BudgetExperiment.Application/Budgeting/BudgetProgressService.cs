// <copyright file="BudgetProgressService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Identity;
using BudgetExperiment.Domain.Settings;

namespace BudgetExperiment.Application.Budgeting;

/// <summary>
/// Application service for budget progress tracking operations.
/// </summary>
public sealed class BudgetProgressService : IBudgetProgressService
{
    private readonly IBudgetGoalRepository _goalRepository;
    private readonly IBudgetCategoryRepository _categoryRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICurrencyProvider _currencyProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetProgressService"/> class.
    /// </summary>
    /// <param name="goalRepository">The budget goal repository.</param>
    /// <param name="categoryRepository">The budget category repository.</param>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="currencyProvider">The currency provider.</param>
    /// <param name="userContext">The current user context.</param>
    public BudgetProgressService(
        IBudgetGoalRepository goalRepository,
        IBudgetCategoryRepository categoryRepository,
        ITransactionRepository transactionRepository,
        ICurrencyProvider currencyProvider,
        IUserContext userContext)
    {
        _goalRepository = goalRepository;
        _categoryRepository = categoryRepository;
        _transactionRepository = transactionRepository;
        _currencyProvider = currencyProvider;
        _ = userContext;
    }

    /// <inheritdoc/>
    public async Task<BudgetProgressDto?> GetProgressAsync(Guid categoryId, int year, int month, CancellationToken cancellationToken = default)
    {
        var goal = await _goalRepository.GetByCategoryAndMonthAsync(categoryId, year, month, cancellationToken);
        if (goal is null)
        {
            return null;
        }

        var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        if (category is null)
        {
            return null;
        }

        var spent = await _transactionRepository.GetSpendingByCategoryAsync(categoryId, year, month, cancellationToken);
        var progress = BudgetProgress.Create(
            category.Id,
            category.Name,
            category.Icon,
            category.Color,
            goal.TargetAmount,
            spent,
            transactionCount: 0);
        return BudgetMapper.ToDto(progress);
    }

    /// <inheritdoc/>
    public async Task<BudgetSummaryDto> GetMonthlySummaryAsync(
        int year,
        int month,
        bool groupByKakeibo = false,
        CancellationToken cancellationToken = default)
    {
        var goals = await _goalRepository.GetByMonthAsync(year, month, cancellationToken)
            ?? [];
        var allExpenseCategories = await _categoryRepository.GetByTypeAsync(CategoryType.Expense, cancellationToken)
            ?? [];
        var categoryProgress = new List<BudgetProgressDto>();
        var currency = await _currencyProvider.GetCurrencyAsync(cancellationToken);
        var totalBudgeted = MoneyValue.Create(currency, 0m);
        var totalSpent = MoneyValue.Create(currency, 0m);
        var kakeiboGroupedSummary = groupByKakeibo
            ? await BuildKakeiboGroupedSummaryAsync(year, month, cancellationToken)
            : null;

        // Create a lookup of goals by category ID
        var goalByCategoryId = goals.ToDictionary(g => g.CategoryId);
        var groupedSpendingTask = _transactionRepository.GetSpendingByCategoriesAsync(
            year,
            month,
            cancellationToken);
        var usedGroupedQuery = groupedSpendingTask is not null;
        var spendingByCategory = groupedSpendingTask is null
            ? new Dictionary<Guid, decimal>()
            : await groupedSpendingTask ?? new Dictionary<Guid, decimal>();

        // Process all active expense categories
        foreach (var category in allExpenseCategories.Where(c => c.IsActive))
        {
            var spentAmount = spendingByCategory.TryGetValue(category.Id, out var amount)
                ? amount
                : usedGroupedQuery
                    ? 0m
                    : (await _transactionRepository.GetSpendingByCategoryAsync(
                        category.Id,
                        year,
                        month,
                        cancellationToken)).Amount;
            var spent = MoneyValue.Create(currency, spentAmount);
            BudgetProgress progress;

            if (goalByCategoryId.TryGetValue(category.Id, out var goal))
            {
                // Category has a budget goal
                progress = BudgetProgress.Create(
                    category.Id,
                    category.Name,
                    category.Icon,
                    category.Color,
                    goal.TargetAmount,
                    spent,
                    transactionCount: 0);
                totalBudgeted = MoneyValue.Create(totalBudgeted.Currency, totalBudgeted.Amount + goal.TargetAmount.Amount);
            }
            else
            {
                // Category has no budget goal
                progress = BudgetProgress.CreateWithNoBudget(
                    category.Id,
                    category.Name,
                    category.Icon,
                    category.Color,
                    spent,
                    transactionCount: 0);
            }

            categoryProgress.Add(BudgetMapper.ToDto(progress));
            totalSpent = MoneyValue.Create(totalSpent.Currency, totalSpent.Amount + spent.Amount);
        }

        // Calculate category status counts
        var categoriesOnTrack = categoryProgress.Count(p => p.Status == nameof(BudgetStatus.OnTrack));
        var categoriesWarning = categoryProgress.Count(p => p.Status == nameof(BudgetStatus.Warning));
        var categoriesOverBudget = categoryProgress.Count(p => p.Status == nameof(BudgetStatus.OverBudget));
        var categoriesNoBudgetSet = categoryProgress.Count(p => p.Status == nameof(BudgetStatus.NoBudgetSet));

        return new BudgetSummaryDto
        {
            Year = year,
            Month = month,
            CategoryProgress = categoryProgress,
            TotalBudgeted = CommonMapper.ToDto(totalBudgeted),
            TotalSpent = CommonMapper.ToDto(totalSpent),
            TotalRemaining = CommonMapper.ToDto(MoneyValue.Create(totalBudgeted.Currency, totalBudgeted.Amount - totalSpent.Amount)),
            CategoriesOnTrack = categoriesOnTrack,
            CategoriesWarning = categoriesWarning,
            CategoriesOverBudget = categoriesOverBudget,
            CategoriesNoBudgetSet = categoriesNoBudgetSet,
            KakeiboGroupedSummary = kakeiboGroupedSummary,
        };
    }

    private static KakeiboGroupedSummaryDto BuildKakeiboGroupedSummary(IEnumerable<Transaction> expenseTransactions)
    {
        var summary = new KakeiboGroupedSummaryDto();

        foreach (var transaction in expenseTransactions)
        {
            var effectiveCategory = transaction.KakeiboOverride ?? transaction.Category?.KakeiboCategory;
            if (effectiveCategory is null)
            {
                continue;
            }

            var amount = Math.Abs(transaction.Amount.Amount);
            switch (effectiveCategory)
            {
                case KakeiboCategory.Essentials:
                    summary.Essentials += amount;
                    break;
                case KakeiboCategory.Wants:
                    summary.Wants += amount;
                    break;
                case KakeiboCategory.Culture:
                    summary.Culture += amount;
                    break;
                case KakeiboCategory.Unexpected:
                    summary.Unexpected += amount;
                    break;
            }
        }

        summary.Total = summary.Essentials + summary.Wants + summary.Culture + summary.Unexpected;
        return summary;
    }

    private async Task<KakeiboGroupedSummaryDto> BuildKakeiboGroupedSummaryAsync(
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var transactions = await _transactionRepository.GetByDateRangeAsync(startDate, endDate, null, cancellationToken);
        var expenseTransactions = transactions
            .Where(t => !t.IsTransfer && t.Amount.Amount < 0)
            .ToList();

        return BuildKakeiboGroupedSummary(expenseTransactions);
    }
}
