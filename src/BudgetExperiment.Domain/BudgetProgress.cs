// <copyright file="BudgetProgress.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Represents the progress of spending against a budget target.
/// </summary>
public sealed record BudgetProgress
{
    /// <summary>
    /// Threshold percentage for warning status.
    /// </summary>
    public const decimal WarningThreshold = 80m;

    /// <summary>
    /// Threshold percentage for over budget status.
    /// </summary>
    public const decimal OverBudgetThreshold = 100m;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetProgress"/> class.
    /// </summary>
    private BudgetProgress()
    {
    }

    /// <summary>
    /// Gets the category identifier.
    /// </summary>
    public Guid CategoryId { get; init; }

    /// <summary>
    /// Gets the category name.
    /// </summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the category icon identifier.
    /// </summary>
    public string? CategoryIcon { get; init; }

    /// <summary>
    /// Gets the category hex color code.
    /// </summary>
    public string? CategoryColor { get; init; }

    /// <summary>
    /// Gets the target budget amount.
    /// </summary>
    public MoneyValue TargetAmount { get; init; } = MoneyValue.Zero("USD");

    /// <summary>
    /// Gets the amount spent.
    /// </summary>
    public MoneyValue SpentAmount { get; init; } = MoneyValue.Zero("USD");

    /// <summary>
    /// Gets the remaining budget amount.
    /// </summary>
    public MoneyValue RemainingAmount { get; init; } = MoneyValue.Zero("USD");

    /// <summary>
    /// Gets the percentage of budget used.
    /// </summary>
    public decimal PercentUsed { get; init; }

    /// <summary>
    /// Gets the budget status.
    /// </summary>
    public BudgetStatus Status { get; init; }

    /// <summary>
    /// Gets the number of transactions in this category.
    /// </summary>
    public int TransactionCount { get; init; }

    /// <summary>
    /// Creates a budget progress for a category with a budget target.
    /// </summary>
    /// <param name="categoryId">The category identifier.</param>
    /// <param name="categoryName">The category name.</param>
    /// <param name="categoryIcon">The category icon.</param>
    /// <param name="categoryColor">The category color.</param>
    /// <param name="targetAmount">The target budget amount.</param>
    /// <param name="spentAmount">The amount spent.</param>
    /// <param name="transactionCount">The number of transactions.</param>
    /// <returns>A new <see cref="BudgetProgress"/> instance.</returns>
    public static BudgetProgress Create(
        Guid categoryId,
        string categoryName,
        string? categoryIcon,
        string? categoryColor,
        MoneyValue targetAmount,
        MoneyValue spentAmount,
        int transactionCount)
    {
        var remainingAmount = targetAmount - spentAmount;
        var percentUsed = CalculatePercentUsed(targetAmount.Amount, spentAmount.Amount);
        var status = DetermineStatus(percentUsed);

        return new BudgetProgress
        {
            CategoryId = categoryId,
            CategoryName = categoryName,
            CategoryIcon = categoryIcon,
            CategoryColor = categoryColor,
            TargetAmount = targetAmount,
            SpentAmount = spentAmount,
            RemainingAmount = remainingAmount,
            PercentUsed = percentUsed,
            Status = status,
            TransactionCount = transactionCount,
        };
    }

    /// <summary>
    /// Creates a budget progress for a category without a budget target.
    /// </summary>
    /// <param name="categoryId">The category identifier.</param>
    /// <param name="categoryName">The category name.</param>
    /// <param name="categoryIcon">The category icon.</param>
    /// <param name="categoryColor">The category color.</param>
    /// <param name="spentAmount">The amount spent.</param>
    /// <param name="transactionCount">The number of transactions.</param>
    /// <returns>A new <see cref="BudgetProgress"/> instance with NoBudgetSet status.</returns>
    public static BudgetProgress CreateWithNoBudget(
        Guid categoryId,
        string categoryName,
        string? categoryIcon,
        string? categoryColor,
        MoneyValue spentAmount,
        int transactionCount)
    {
        return new BudgetProgress
        {
            CategoryId = categoryId,
            CategoryName = categoryName,
            CategoryIcon = categoryIcon,
            CategoryColor = categoryColor,
            TargetAmount = MoneyValue.Zero(spentAmount.Currency),
            SpentAmount = spentAmount,
            RemainingAmount = MoneyValue.Zero(spentAmount.Currency),
            PercentUsed = 0m,
            Status = BudgetStatus.NoBudgetSet,
            TransactionCount = transactionCount,
        };
    }

    private static decimal CalculatePercentUsed(decimal target, decimal spent)
    {
        if (target == 0)
        {
            return 0m;
        }

        return Math.Round((spent / target) * 100m, 0, MidpointRounding.AwayFromZero);
    }

    private static BudgetStatus DetermineStatus(decimal percentUsed)
    {
        if (percentUsed >= OverBudgetThreshold)
        {
            return BudgetStatus.OverBudget;
        }

        if (percentUsed >= WarningThreshold)
        {
            return BudgetStatus.Warning;
        }

        return BudgetStatus.OnTrack;
    }
}
