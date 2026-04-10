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
    private const string UncategorizedName = "Uncategorized";
    private const string UnknownCategoryName = "Unknown";
    private readonly ITransactionQueryRepository _transactionRepository;
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
        ITransactionQueryRepository transactionRepository,
        IBudgetCategoryRepository categoryRepository,
        ICurrencyProvider currencyProvider,
        ITrendReportBuilder trendReportBuilder,
        ILocationReportBuilder locationReportBuilder)
    {
        _transactionRepository = transactionRepository;
        _categoryRepository = categoryRepository;
        _currencyProvider = currencyProvider;
        _trendReportBuilder = trendReportBuilder;
        _locationReportBuilder = locationReportBuilder;
    }

    /// <inheritdoc/>
    public async Task<MonthlyCategoryReportDto> GetMonthlyCategoryReportAsync(
        int year,
        int month,
        bool groupByKakeibo = false,
        CancellationToken cancellationToken = default)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var currency = await _currencyProvider.GetCurrencyAsync(cancellationToken);
        var (totalSpending, totalIncome, categorySpending, kakeiboGroupedSummary) = await this.BuildCategoryReportAsync(
            startDate,
            endDate,
            accountId: null,
            currency,
            groupByKakeibo,
            cancellationToken);

        return new MonthlyCategoryReportDto
        {
            Year = year,
            Month = month,
            TotalSpending = new MoneyDto { Currency = currency, Amount = totalSpending },
            TotalIncome = new MoneyDto { Currency = currency, Amount = totalIncome },
            Categories = categorySpending,
            KakeiboGroupedSummary = kakeiboGroupedSummary,
        };
    }

    /// <inheritdoc/>
    public async Task<DateRangeCategoryReportDto> GetCategoryReportByRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? accountId = null,
        bool groupByKakeibo = false,
        CancellationToken cancellationToken = default)
    {
        var currency = await _currencyProvider.GetCurrencyAsync(cancellationToken);
        var (totalSpending, totalIncome, categorySpending, kakeiboGroupedSummary) = await this.BuildCategoryReportAsync(
            startDate,
            endDate,
            accountId,
            currency,
            groupByKakeibo,
            cancellationToken);

        return new DateRangeCategoryReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalSpending = new MoneyDto { Currency = currency, Amount = totalSpending },
            TotalIncome = new MoneyDto { Currency = currency, Amount = totalIncome },
            Categories = categorySpending,
            KakeiboGroupedSummary = kakeiboGroupedSummary,
        };
    }

    /// <inheritdoc/>
    public Task<SpendingTrendsReportDto> GetSpendingTrendsAsync(
        int months = 6,
        int? endYear = null,
        int? endMonth = null,
        Guid? categoryId = null,
        bool groupByKakeibo = false,
        CancellationToken cancellationToken = default)
    {
        return _trendReportBuilder.GetSpendingTrendsAsync(
            months, endYear, endMonth, categoryId, groupByKakeibo, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DaySummaryDto> GetDaySummaryAsync(
        DateOnly date,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var currency = await _currencyProvider.GetCurrencyAsync(cancellationToken);
        var transactions = await _transactionRepository.GetByDateRangeAsync(
            date, date, accountId, cancellationToken);

        var nonTransferTransactions = transactions.Where(t => !t.IsTransfer).ToList();
        var (totalSpending, totalIncome) = CalculateSpendingAndIncome(nonTransferTransactions);

        var expenseTransactions = nonTransferTransactions.Where(t => t.Amount.Amount < 0).ToList();
        var categoryLookup = this.BuildCategoryLookup(nonTransferTransactions);
        var topCategories = BuildTopCategories(expenseTransactions, categoryLookup, currency);

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
        return _locationReportBuilder.GetSpendingByLocationAsync(
            startDate, endDate, accountId, cancellationToken);
    }

    private static (decimal TotalSpending, decimal TotalIncome) CalculateSpendingAndIncome(
        List<Transaction> nonTransferTransactions)
    {
        var totalSpending = nonTransferTransactions
            .Where(t => t.Amount.Amount < 0)
            .Sum(t => Math.Abs(t.Amount.Amount));

        var totalIncome = nonTransferTransactions
            .Where(t => t.Amount.Amount > 0)
            .Sum(t => t.Amount.Amount);

        return (totalSpending, totalIncome);
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

    private static List<DayTopCategoryDto> BuildTopCategories(
        List<Transaction> expenseTransactions,
        IReadOnlyDictionary<Guid, BudgetCategory> categoryLookup,
        string currency)
    {
        var topCategories = new List<DayTopCategoryDto>();
        var categoryGroups = expenseTransactions
            .GroupBy(t => t.CategoryId)
            .OrderByDescending(g => g.Sum(t => Math.Abs(t.Amount.Amount)))
            .Take(3);

        foreach (var group in categoryGroups)
        {
            var categoryName = ResolveCategoryName(group.Key, categoryLookup);
            var amount = group.Sum(t => Math.Abs(t.Amount.Amount));

            topCategories.Add(new DayTopCategoryDto
            {
                CategoryName = categoryName,
                Amount = new MoneyDto { Currency = currency, Amount = amount },
            });
        }

        return topCategories;
    }

    private static List<CategorySpendingDto> BuildCategorySpendingList(
        List<Transaction> expenseTransactions,
        decimal totalSpending,
        string currency,
        IReadOnlyDictionary<Guid, BudgetCategory> categoryLookup)
    {
        var categoryGroups = expenseTransactions
            .GroupBy(t => t.CategoryId)
            .ToList();

        var categorySpending = new List<CategorySpendingDto>();

        foreach (var group in categoryGroups)
        {
            var dto = BuildCategorySpendingDto(
                group.Key, group.ToList(), totalSpending, currency, categoryLookup);
            categorySpending.Add(dto);
        }

        return categorySpending
            .OrderByDescending(c => c.Amount.Amount)
            .ToList();
    }

    private static CategorySpendingDto BuildCategorySpendingDto(
        Guid? categoryId,
        List<Transaction> transactions,
        decimal totalSpending,
        string currency,
        IReadOnlyDictionary<Guid, BudgetCategory> categoryLookup)
    {
        var amount = transactions.Sum(t => Math.Abs(t.Amount.Amount));
        var percentage = totalSpending > 0
            ? Math.Round(amount / totalSpending * 100, 2)
            : 0m;

        var (categoryName, categoryColor) = ResolveCategoryDetails(categoryId, categoryLookup);

        return new CategorySpendingDto
        {
            CategoryId = categoryId,
            CategoryName = categoryName,
            CategoryColor = categoryColor,
            Amount = new MoneyDto { Currency = currency, Amount = amount },
            Percentage = percentage,
            TransactionCount = transactions.Count,
        };
    }

    private static (string Name, string? Color) ResolveCategoryDetails(
        Guid? categoryId,
        IReadOnlyDictionary<Guid, BudgetCategory> categoryLookup)
    {
        if (!categoryId.HasValue)
        {
            return (UncategorizedName, null);
        }

        return categoryLookup.TryGetValue(categoryId.Value, out var category)
            ? (category.Name, category.Color)
            : (UnknownCategoryName, null);
    }

    private static string ResolveCategoryName(
        Guid? categoryId,
        IReadOnlyDictionary<Guid, BudgetCategory> categoryLookup)
    {
        if (!categoryId.HasValue)
        {
            return UncategorizedName;
        }

        return categoryLookup.TryGetValue(categoryId.Value, out var category)
            ? category.Name
            : UnknownCategoryName;
    }

    private async Task<(decimal TotalSpending, decimal TotalIncome, List<CategorySpendingDto> Categories, KakeiboGroupedSummaryDto? KakeiboGroupedSummary)> BuildCategoryReportAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? accountId,
        string currency,
        bool groupByKakeibo,
        CancellationToken cancellationToken)
    {
        var transactions = await _transactionRepository.GetByDateRangeAsync(
            startDate, endDate, accountId, cancellationToken);

        var nonTransferTransactions = transactions.Where(t => !t.IsTransfer).ToList();
        var (totalSpending, totalIncome) = CalculateSpendingAndIncome(nonTransferTransactions);

        var expenseTransactions = nonTransferTransactions
            .Where(t => t.Amount.Amount < 0)
            .ToList();

        var categoryLookup = this.BuildCategoryLookup(nonTransferTransactions);
        var categorySpending = BuildCategorySpendingList(
            expenseTransactions, totalSpending, currency, categoryLookup);

        var kakeiboGroupedSummary = groupByKakeibo
            ? BuildKakeiboGroupedSummary(expenseTransactions)
            : null;

        return (totalSpending, totalIncome, categorySpending, kakeiboGroupedSummary);
    }

    private Dictionary<Guid, BudgetCategory> BuildCategoryLookup(IEnumerable<Transaction> transactions)
    {
        if (_categoryRepository is null)
        {
            throw new InvalidOperationException("Category repository is required.");
        }

        return transactions
            .Where(t => t.Category is not null)
            .Select(t => t.Category!)
            .DistinctBy(c => c.Id)
            .ToDictionary(c => c.Id);
    }
}
