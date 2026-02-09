// <copyright file="ReportService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Reports;

/// <summary>
/// Application service for generating financial reports.
/// </summary>
public sealed class ReportService : IReportService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IBudgetCategoryRepository _categoryRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportService"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="categoryRepository">The category repository.</param>
    public ReportService(
        ITransactionRepository transactionRepository,
        IBudgetCategoryRepository categoryRepository)
    {
        this._transactionRepository = transactionRepository;
        this._categoryRepository = categoryRepository;
    }

    /// <inheritdoc/>
    public async Task<MonthlyCategoryReportDto> GetMonthlyCategoryReportAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var (totalSpending, totalIncome, categorySpending) = await this.BuildCategoryReportAsync(
            startDate, endDate, accountId: null, cancellationToken);

        return new MonthlyCategoryReportDto
        {
            Year = year,
            Month = month,
            TotalSpending = new MoneyDto { Currency = "USD", Amount = totalSpending },
            TotalIncome = new MoneyDto { Currency = "USD", Amount = totalIncome },
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
        var (totalSpending, totalIncome, categorySpending) = await this.BuildCategoryReportAsync(
            startDate, endDate, accountId, cancellationToken);

        return new DateRangeCategoryReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalSpending = new MoneyDto { Currency = "USD", Amount = totalSpending },
            TotalIncome = new MoneyDto { Currency = "USD", Amount = totalIncome },
            Categories = categorySpending,
        };
    }

    /// <inheritdoc/>
    public async Task<SpendingTrendsReportDto> GetSpendingTrendsAsync(
        int months = 6,
        int? endYear = null,
        int? endMonth = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        months = Math.Clamp(months, 1, 24);

        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var resolvedEndYear = endYear ?? now.Year;
        var resolvedEndMonth = endMonth ?? now.Month;

        var endDate = new DateOnly(resolvedEndYear, resolvedEndMonth, 1)
            .AddMonths(1).AddDays(-1);
        var startDate = new DateOnly(resolvedEndYear, resolvedEndMonth, 1)
            .AddMonths(-(months - 1));

        var transactions = await this._transactionRepository.GetByDateRangeAsync(
            startDate, endDate, accountId: null, cancellationToken);

        var nonTransferTransactions = transactions.Where(t => !t.IsTransfer).ToList();

        // Apply optional category filter
        if (categoryId.HasValue)
        {
            nonTransferTransactions = nonTransferTransactions
                .Where(t => t.CategoryId == categoryId.Value)
                .ToList();
        }

        var monthlyData = new List<MonthlyTrendPointDto>();
        var currentMonth = startDate;

        while (currentMonth <= endDate)
        {
            var monthStart = new DateOnly(currentMonth.Year, currentMonth.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var monthTransactions = nonTransferTransactions
                .Where(t => t.Date >= monthStart && t.Date <= monthEnd)
                .ToList();

            var spending = monthTransactions
                .Where(t => t.Amount.Amount < 0)
                .Sum(t => Math.Abs(t.Amount.Amount));

            var income = monthTransactions
                .Where(t => t.Amount.Amount > 0)
                .Sum(t => t.Amount.Amount);

            monthlyData.Add(new MonthlyTrendPointDto
            {
                Year = currentMonth.Year,
                Month = currentMonth.Month,
                TotalSpending = new MoneyDto { Currency = "USD", Amount = spending },
                TotalIncome = new MoneyDto { Currency = "USD", Amount = income },
                NetAmount = new MoneyDto { Currency = "USD", Amount = income - spending },
                TransactionCount = monthTransactions.Count,
            });

            currentMonth = currentMonth.AddMonths(1);
        }

        var monthsWithData = monthlyData.Where(m => m.TransactionCount > 0).ToList();
        var avgSpending = monthsWithData.Count > 0
            ? Math.Round(monthsWithData.Average(m => m.TotalSpending.Amount), 2)
            : 0m;
        var avgIncome = monthsWithData.Count > 0
            ? Math.Round(monthsWithData.Average(m => m.TotalIncome.Amount), 2)
            : 0m;

        // Calculate trend by comparing second half to first half
        var (trendDirection, trendPercentage) = CalculateTrend(monthlyData);

        return new SpendingTrendsReportDto
        {
            MonthlyData = monthlyData,
            AverageMonthlySpending = new MoneyDto { Currency = "USD", Amount = avgSpending },
            AverageMonthlyIncome = new MoneyDto { Currency = "USD", Amount = avgIncome },
            TrendDirection = trendDirection,
            TrendPercentage = trendPercentage,
        };
    }

    /// <inheritdoc/>
    public async Task<DaySummaryDto> GetDaySummaryAsync(
        DateOnly date,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
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
                Amount = new MoneyDto { Currency = "USD", Amount = amount },
            });
        }

        return new DaySummaryDto
        {
            Date = date,
            TotalSpending = new MoneyDto { Currency = "USD", Amount = totalSpending },
            TotalIncome = new MoneyDto { Currency = "USD", Amount = totalIncome },
            NetAmount = new MoneyDto { Currency = "USD", Amount = totalIncome - totalSpending },
            TransactionCount = nonTransferTransactions.Count,
            TopCategories = topCategories,
        };
    }

    private static (string Direction, decimal Percentage) CalculateTrend(
        List<MonthlyTrendPointDto> monthlyData)
    {
        if (monthlyData.Count < 2)
        {
            return ("stable", 0m);
        }

        var midpoint = monthlyData.Count / 2;
        var firstHalf = monthlyData.Take(midpoint).ToList();
        var secondHalf = monthlyData.Skip(midpoint).ToList();

        var firstHalfAvg = firstHalf.Count > 0
            ? firstHalf.Average(m => m.TotalSpending.Amount)
            : 0m;
        var secondHalfAvg = secondHalf.Count > 0
            ? secondHalf.Average(m => m.TotalSpending.Amount)
            : 0m;

        if (firstHalfAvg == 0m)
        {
            return secondHalfAvg > 0 ? ("increasing", 100m) : ("stable", 0m);
        }

        var changePercent = Math.Round(((secondHalfAvg - firstHalfAvg) / firstHalfAvg) * 100m, 1);

        var direction = changePercent switch
        {
            > 5m => "increasing",
            < -5m => "decreasing",
            _ => "stable",
        };

        return (direction, changePercent);
    }

    private async Task<(decimal TotalSpending, decimal TotalIncome, List<CategorySpendingDto> Categories)> BuildCategoryReportAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? accountId,
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
                Amount = new MoneyDto { Currency = "USD", Amount = amount },
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
