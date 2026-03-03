// <copyright file="ReportService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain.Settings;

namespace BudgetExperiment.Application.Reports;

/// <summary>
/// Application service for generating financial reports.
/// </summary>
public sealed class ReportService : IReportService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IBudgetCategoryRepository _categoryRepository;
    private readonly ICurrencyProvider _currencyProvider;
    private readonly ITrendReportBuilder _trendReportBuilder;
    private readonly ILocationReportBuilder _locationReportBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportService"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="categoryRepository">The category repository.</param>
    /// <param name="currencyProvider">The currency provider.</param>
    /// <param name="trendReportBuilder">The trend report builder.</param>
    /// <param name="locationReportBuilder">The location report builder.</param>
    public ReportService(
        ITransactionRepository transactionRepository,
        IBudgetCategoryRepository categoryRepository,
        ICurrencyProvider currencyProvider,
        ITrendReportBuilder trendReportBuilder,
        ILocationReportBuilder locationReportBuilder)
    {
        this._transactionRepository = transactionRepository;
        this._categoryRepository = categoryRepository;
        this._currencyProvider = currencyProvider;
        this._trendReportBuilder = trendReportBuilder;
        this._locationReportBuilder = locationReportBuilder;
    }

    /// <inheritdoc/>
    public async Task<MonthlyCategoryReportDto> GetMonthlyCategoryReportAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var currency = await this._currencyProvider.GetCurrencyAsync(cancellationToken);
        var (totalSpending, totalIncome, categorySpending) = await this.BuildCategoryReportAsync(
            startDate, endDate, accountId: null, currency, cancellationToken);

        return new MonthlyCategoryReportDto
        {
            Year = year,
            Month = month,
            TotalSpending = new MoneyDto { Currency = currency, Amount = totalSpending },
            TotalIncome = new MoneyDto { Currency = currency, Amount = totalIncome },
            Categories = categorySpending,
        };
    }

    /// <inheritdoc/>
    public async Task<DateRangeCategoryReportDto> GetCategoryReportByRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var currency = await this._currencyProvider.GetCurrencyAsync(cancellationToken);
        var (totalSpending, totalIncome, categorySpending) = await this.BuildCategoryReportAsync(
            startDate, endDate, accountId, currency, cancellationToken);

        return new DateRangeCategoryReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalSpending = new MoneyDto { Currency = currency, Amount = totalSpending },
            TotalIncome = new MoneyDto { Currency = currency, Amount = totalIncome },
            Categories = categorySpending,
        };
    }

    /// <inheritdoc/>
    public Task<SpendingTrendsReportDto> GetSpendingTrendsAsync(
        int months = 6,
        int? endYear = null,
        int? endMonth = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        return this._trendReportBuilder.GetSpendingTrendsAsync(
            months, endYear, endMonth, categoryId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DaySummaryDto> GetDaySummaryAsync(
        DateOnly date,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var currency = await this._currencyProvider.GetCurrencyAsync(cancellationToken);
        var transactions = await this._transactionRepository.GetByDateRangeAsync(
            date, date, accountId, cancellationToken);

        var nonTransferTransactions = transactions.Where(t => !t.IsTransfer).ToList();

        var expenseTransactions = nonTransferTransactions.Where(t => t.Amount.Amount < 0).ToList();
        var incomeTransactions = nonTransferTransactions.Where(t => t.Amount.Amount > 0).ToList();

        var totalSpending = expenseTransactions.Sum(t => Math.Abs(t.Amount.Amount));
        var totalIncome = incomeTransactions.Sum(t => t.Amount.Amount);

        // Get top 3 spending categories
        var topCategories = new List<DayTopCategoryDto>();
        var categoryGroups = expenseTransactions
            .GroupBy(t => t.CategoryId)
            .OrderByDescending(g => g.Sum(t => Math.Abs(t.Amount.Amount)))
            .Take(3);

        foreach (var group in categoryGroups)
        {
            var amount = group.Sum(t => Math.Abs(t.Amount.Amount));
            string categoryName;

            if (group.Key.HasValue)
            {
                var category = await this._categoryRepository.GetByIdAsync(group.Key.Value, cancellationToken);
                categoryName = category?.Name ?? "Unknown";
            }
            else
            {
                categoryName = "Uncategorized";
            }

            topCategories.Add(new DayTopCategoryDto
            {
                CategoryName = categoryName,
                Amount = new MoneyDto { Currency = currency, Amount = amount },
            });
        }

        return new DaySummaryDto
        {
            Date = date,
            TotalSpending = new MoneyDto { Currency = currency, Amount = totalSpending },
            TotalIncome = new MoneyDto { Currency = currency, Amount = totalIncome },
            NetAmount = new MoneyDto { Currency = currency, Amount = totalIncome - totalSpending },
            TransactionCount = nonTransferTransactions.Count,
            TopCategories = topCategories,
        };
    }

    /// <inheritdoc/>
    public Task<LocationSpendingReportDto> GetSpendingByLocationAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        return this._locationReportBuilder.GetSpendingByLocationAsync(
            startDate, endDate, accountId, cancellationToken);
    }

    private async Task<(decimal TotalSpending, decimal TotalIncome, List<CategorySpendingDto> Categories)> BuildCategoryReportAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? accountId,
        string currency,
        CancellationToken cancellationToken)
    {
        var transactions = await this._transactionRepository.GetByDateRangeAsync(
            startDate,
            endDate,
            accountId,
            cancellationToken);

        // Filter out transfers - they're internal movements, not spending/income
        var nonTransferTransactions = transactions.Where(t => !t.IsTransfer).ToList();

        // Separate income and expenses
        var expenseTransactions = nonTransferTransactions
            .Where(t => t.Amount.Amount < 0)
            .ToList();

        var incomeTransactions = nonTransferTransactions
            .Where(t => t.Amount.Amount > 0)
            .ToList();

        // Calculate totals (use absolute values for spending)
        var totalSpending = expenseTransactions.Sum(t => Math.Abs(t.Amount.Amount));
        var totalIncome = incomeTransactions.Sum(t => t.Amount.Amount);

        // Group expense transactions by category and calculate spending
        var categoryGroups = expenseTransactions
            .GroupBy(t => t.CategoryId)
            .ToList();

        var categorySpending = new List<CategorySpendingDto>();

        foreach (var group in categoryGroups)
        {
            var categoryId = group.Key;
            var amount = group.Sum(t => Math.Abs(t.Amount.Amount));
            var transactionCount = group.Count();
            var percentage = totalSpending > 0
                ? Math.Round(amount / totalSpending * 100, 2)
                : 0m;

            string categoryName;
            string? categoryColor = null;

            if (categoryId.HasValue)
            {
                var category = await this._categoryRepository.GetByIdAsync(categoryId.Value, cancellationToken);
                if (category != null)
                {
                    categoryName = category.Name;
                    categoryColor = category.Color;
                }
                else
                {
                    categoryName = "Unknown";
                }
            }
            else
            {
                categoryName = "Uncategorized";
            }

            categorySpending.Add(new CategorySpendingDto
            {
                CategoryId = categoryId,
                CategoryName = categoryName,
                CategoryColor = categoryColor,
                Amount = new MoneyDto { Currency = currency, Amount = amount },
                Percentage = percentage,
                TransactionCount = transactionCount,
            });
        }

        // Order by amount descending (largest spending first)
        categorySpending = categorySpending
            .OrderByDescending(c => c.Amount.Amount)
            .ToList();

        return (totalSpending, totalIncome, categorySpending);
    }
}
