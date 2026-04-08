// <copyright file="TrendReportBuilder.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain.Settings;

namespace BudgetExperiment.Application.Reports;

/// <summary>
/// Builds spending trend reports by analyzing transaction data
/// across multiple months to identify spending patterns and direction.
/// </summary>
public sealed class TrendReportBuilder : ITrendReportBuilder
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICurrencyProvider _currencyProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrendReportBuilder"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="currencyProvider">The currency provider.</param>
    public TrendReportBuilder(
        ITransactionRepository transactionRepository,
        ICurrencyProvider currencyProvider)
    {
        _transactionRepository = transactionRepository;
        _currencyProvider = currencyProvider;
    }

    /// <inheritdoc/>
    public async Task<SpendingTrendsReportDto> GetSpendingTrendsAsync(
        int months = 6,
        int? endYear = null,
        int? endMonth = null,
        Guid? categoryId = null,
        bool groupByKakeibo = false,
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

        var currency = await _currencyProvider.GetCurrencyAsync(cancellationToken);
        var transactions = await _transactionRepository.GetByDateRangeAsync(
            startDate, endDate, accountId: null, cancellationToken);

        var nonTransferTransactions = transactions.Where(t => !t.IsTransfer).ToList();

        // Apply optional category filter
        if (categoryId.HasValue)
        {
            nonTransferTransactions = nonTransferTransactions
                .Where(t => t.CategoryId == categoryId.Value)
                .ToList();
        }

        var monthlyData = BuildMonthlyData(nonTransferTransactions, startDate, endDate, currency);

        var monthsWithData = monthlyData.Where(m => m.TransactionCount > 0).ToList();
        var avgSpending = monthsWithData.Count > 0
            ? Math.Round(monthsWithData.Average(m => m.TotalSpending.Amount), 2)
            : 0m;
        var avgIncome = monthsWithData.Count > 0
            ? Math.Round(monthsWithData.Average(m => m.TotalIncome.Amount), 2)
            : 0m;

        // Calculate trend by comparing second half to first half
        var (trendDirection, trendPercentage) = CalculateTrend(monthlyData);

        var kakeiboGroupedSummary = groupByKakeibo
            ? BuildKakeiboGroupedSummary(nonTransferTransactions)
            : null;

        return new SpendingTrendsReportDto
        {
            MonthlyData = monthlyData,
            AverageMonthlySpending = new MoneyDto { Currency = currency, Amount = avgSpending },
            AverageMonthlyIncome = new MoneyDto { Currency = currency, Amount = avgIncome },
            TrendDirection = trendDirection,
            TrendPercentage = trendPercentage,
            KakeiboGroupedSummary = kakeiboGroupedSummary,
        };
    }

    private static KakeiboGroupedSummaryDto BuildKakeiboGroupedSummary(IEnumerable<Transaction> nonTransferTransactions)
    {
        var summary = new KakeiboGroupedSummaryDto();

        foreach (var transaction in nonTransferTransactions.Where(t => t.Amount.Amount < 0))
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

    private static List<MonthlyTrendPointDto> BuildMonthlyData(
        List<Transaction> nonTransferTransactions,
        DateOnly startDate,
        DateOnly endDate,
        string currency)
    {
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
                TotalSpending = new MoneyDto { Currency = currency, Amount = spending },
                TotalIncome = new MoneyDto { Currency = currency, Amount = income },
                NetAmount = new MoneyDto { Currency = currency, Amount = income - spending },
                TransactionCount = monthTransactions.Count,
            });

            currentMonth = currentMonth.AddMonths(1);
        }

        return monthlyData;
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
}
