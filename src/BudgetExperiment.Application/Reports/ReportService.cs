// <copyright file="ReportService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

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

        var transactions = await this._transactionRepository.GetByDateRangeAsync(
            startDate,
            endDate,
            accountId: null,
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

        return new MonthlyCategoryReportDto
        {
            Year = year,
            Month = month,
            TotalSpending = new MoneyDto { Currency = "USD", Amount = totalSpending },
            TotalIncome = new MoneyDto { Currency = "USD", Amount = totalIncome },
            Categories = categorySpending,
        };
    }
}
