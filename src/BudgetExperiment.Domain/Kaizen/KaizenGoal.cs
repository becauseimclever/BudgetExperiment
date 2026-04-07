// <copyright file="KaizenGoal.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Kaizen;

/// <summary>
/// A Kaizen micro-improvement goal for a single ISO week.
/// One goal per user per week. The user sets a short description,
/// an optional target amount, and an optional Kakeibo category scope.
/// At week-end they mark it achieved or not.
/// </summary>
public sealed class KaizenGoal
{
    /// <summary>
    /// Maximum character length for the goal description.
    /// </summary>
    public const int MaxDescriptionLength = 500;

    /// <summary>
    /// Initializes a new instance of the <see cref="KaizenGoal"/> class.
    /// </summary>
    /// <remarks>Private constructor for EF Core and the factory method.</remarks>
    private KaizenGoal()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the user ID that owns this goal.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the Monday of the ISO week this goal applies to.
    /// </summary>
    public DateOnly WeekStartDate { get; private set; }

    /// <summary>
    /// Gets the short description of the improvement goal (max 500 chars).
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the optional numeric target (e.g., $10 under last week's spend).
    /// </summary>
    public decimal? TargetAmount { get; private set; }

    /// <summary>
    /// Gets the optional Kakeibo category this goal scopes to. Null means all categories.
    /// </summary>
    public global::BudgetExperiment.Shared.Budgeting.KakeiboCategory? KakeiboCategory { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the user achieved this goal. Set at week-end.
    /// </summary>
    public bool IsAchieved { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the goal was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the goal was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a new <see cref="KaizenGoal"/> instance.
    /// </summary>
    /// <param name="userId">The owning user's identifier.</param>
    /// <param name="weekStartDate">The Monday of the ISO week.</param>
    /// <param name="description">The goal description (required, max 500 chars).</param>
    /// <param name="targetAmount">Optional numeric target (must be non-negative if provided).</param>
    /// <param name="kakeiboCategory">Optional Kakeibo category scope.</param>
    /// <returns>A new <see cref="KaizenGoal"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static KaizenGoal Create(
        Guid userId,
        DateOnly weekStartDate,
        string description,
        decimal? targetAmount = null,
        global::BudgetExperiment.Shared.Budgeting.KakeiboCategory? kakeiboCategory = null)
    {
        ValidateDescription(description);
        ValidateTargetAmount(targetAmount);

        var now = DateTime.UtcNow;
        return new KaizenGoal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            WeekStartDate = weekStartDate,
            Description = description.Trim(),
            TargetAmount = targetAmount,
            KakeiboCategory = kakeiboCategory,
            IsAchieved = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    /// <summary>
    /// Updates the goal's mutable fields.
    /// </summary>
    /// <param name="description">The updated description (required, max 500 chars).</param>
    /// <param name="targetAmount">The updated optional target amount (non-negative if provided).</param>
    /// <param name="kakeiboCategory">The updated optional Kakeibo category scope.</param>
    /// <param name="isAchieved">Whether the goal has been achieved.</param>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public void Update(
        string description,
        decimal? targetAmount,
        global::BudgetExperiment.Shared.Budgeting.KakeiboCategory? kakeiboCategory,
        bool isAchieved)
    {
        ValidateDescription(description);
        ValidateTargetAmount(targetAmount);

        this.Description = description.Trim();
        this.TargetAmount = targetAmount;
        this.KakeiboCategory = kakeiboCategory;
        this.IsAchieved = isAchieved;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Goal description is required.");
        }

        if (description.Length > MaxDescriptionLength)
        {
            throw new DomainException($"Goal description cannot exceed {MaxDescriptionLength} characters.");
        }
    }

    private static void ValidateTargetAmount(decimal? targetAmount)
    {
        if (targetAmount is < 0)
        {
            throw new DomainException("Target amount must be non-negative.");
        }
    }
}
