// <copyright file="BudgetGoal.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Represents a monthly budget target for a category.
/// </summary>
public sealed class BudgetGoal
{
    /// <summary>
    /// Minimum valid year for budget goals.
    /// </summary>
    public const int MinYear = 1900;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetGoal"/> class.
    /// </summary>
    /// <remarks>
    /// Private constructor for EF Core and factory method.
    /// </remarks>
    private BudgetGoal()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the category identifier.
    /// </summary>
    public Guid CategoryId { get; private set; }

    /// <summary>
    /// Gets the year for this budget goal.
    /// </summary>
    public int Year { get; private set; }

    /// <summary>
    /// Gets the month for this budget goal (1-12).
    /// </summary>
    public int Month { get; private set; }

    /// <summary>
    /// Gets the target amount for the budget.
    /// </summary>
    public MoneyValue TargetAmount { get; private set; } = MoneyValue.Zero("USD");

    /// <summary>
    /// Gets the UTC timestamp when the goal was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the goal was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the associated budget category.
    /// </summary>
    public BudgetCategory Category { get; private set; } = null!;

    /// <summary>
    /// Creates a new budget goal.
    /// </summary>
    /// <param name="categoryId">The category identifier.</param>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="targetAmount">The target amount.</param>
    /// <returns>A new <see cref="BudgetGoal"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static BudgetGoal Create(Guid categoryId, int year, int month, MoneyValue targetAmount)
    {
        if (categoryId == Guid.Empty)
        {
            throw new DomainException("Category ID is required.");
        }

        if (year < MinYear)
        {
            throw new DomainException($"Year must be {MinYear} or later.");
        }

        if (month < 1 || month > 12)
        {
            throw new DomainException("Month must be between 1 and 12.");
        }

        ValidateTargetAmount(targetAmount);

        var now = DateTime.UtcNow;
        return new BudgetGoal
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            Year = year,
            Month = month,
            TargetAmount = targetAmount,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    /// <summary>
    /// Updates the target amount for this goal.
    /// </summary>
    /// <param name="newTarget">The new target amount.</param>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public void UpdateTarget(MoneyValue newTarget)
    {
        ValidateTargetAmount(newTarget);

        this.TargetAmount = newTarget;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void ValidateTargetAmount(MoneyValue targetAmount)
    {
        if (targetAmount.Amount < 0)
        {
            throw new DomainException("Target amount cannot be negative.");
        }
    }
}
