// <copyright file="MonthlyReflection.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Reflection;

/// <summary>
/// A monthly Kakeibo reflection capturing savings goals and journal entries.
/// One record per user per month. Covers month-start intention and month-end review.
/// </summary>
public sealed class MonthlyReflection
{
    /// <summary>
    /// Maximum character length for intention text (month-start field).
    /// </summary>
    public const int MaxIntentionLength = 280;

    /// <summary>
    /// Maximum character length for gratitude and improvement journal fields.
    /// </summary>
    public const int MaxJournalLength = 2000;

    /// <summary>
    /// Initializes a new instance of the <see cref="MonthlyReflection"/> class.
    /// </summary>
    /// <remarks>
    /// Private constructor for EF Core and factory method.
    /// </remarks>
    private MonthlyReflection()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the user ID that owns this reflection.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the year this reflection covers.
    /// </summary>
    public int Year { get; private set; }

    /// <summary>
    /// Gets the month (1-12) this reflection covers.
    /// </summary>
    public int Month { get; private set; }

    /// <summary>
    /// Gets the savings goal the user set at month start.
    /// </summary>
    public decimal SavingsGoal { get; private set; }

    /// <summary>
    /// Gets the actual savings recorded at month end (income minus expenses).
    /// Null until the user records a month-end review.
    /// </summary>
    public decimal? ActualSavings { get; private set; }

    /// <summary>
    /// Gets the month-start intention text (max 280 characters).
    /// </summary>
    public string? IntentionText { get; private set; }

    /// <summary>
    /// Gets the month-end gratitude journal entry.
    /// </summary>
    public string? GratitudeText { get; private set; }

    /// <summary>
    /// Gets the month-end improvement journal entry.
    /// </summary>
    public string? ImprovementText { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the reflection was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the reflection was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a new monthly reflection for a given user and month.
    /// </summary>
    /// <param name="userId">The owning user's identifier.</param>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="savingsGoal">The savings goal amount (must be non-negative).</param>
    /// <param name="intentionText">Optional month-start intention text (max 280 chars).</param>
    /// <returns>A new <see cref="MonthlyReflection"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static MonthlyReflection Create(
        Guid userId,
        int year,
        int month,
        decimal savingsGoal,
        string? intentionText = null)
    {
        ValidateSavingsGoal(savingsGoal);
        ValidateIntentionText(intentionText);

        var now = DateTime.UtcNow;
        return new MonthlyReflection
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Year = year,
            Month = month,
            SavingsGoal = savingsGoal,
            IntentionText = intentionText?.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    /// <summary>
    /// Updates the reflection with new values for all editable fields.
    /// </summary>
    /// <param name="savingsGoal">The updated savings goal (must be non-negative).</param>
    /// <param name="intentionText">The updated intention text (max 280 chars).</param>
    /// <param name="gratitudeText">The updated gratitude journal entry (max 2000 chars).</param>
    /// <param name="improvementText">The updated improvement journal entry (max 2000 chars).</param>
    /// <param name="actualSavings">Optional computed actual savings to record.</param>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public void Update(
        decimal savingsGoal,
        string? intentionText,
        string? gratitudeText,
        string? improvementText,
        decimal? actualSavings = null)
    {
        ValidateSavingsGoal(savingsGoal);
        ValidateIntentionText(intentionText);
        ValidateJournalText(gratitudeText, "Gratitude text");
        ValidateJournalText(improvementText, "Improvement text");

        this.SavingsGoal = savingsGoal;
        this.IntentionText = intentionText?.Trim();
        this.GratitudeText = gratitudeText?.Trim();
        this.ImprovementText = improvementText?.Trim();
        this.ActualSavings = actualSavings;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void ValidateSavingsGoal(decimal savingsGoal)
    {
        if (savingsGoal < 0)
        {
            throw new DomainException("Savings goal must be non-negative.");
        }
    }

    private static void ValidateIntentionText(string? intentionText)
    {
        if (intentionText?.Length > MaxIntentionLength)
        {
            throw new DomainException($"Intention text cannot exceed {MaxIntentionLength} characters.");
        }
    }

    private static void ValidateJournalText(string? text, string fieldLabel)
    {
        if (text?.Length > MaxJournalLength)
        {
            throw new DomainException($"{fieldLabel} cannot exceed {MaxJournalLength} characters.");
        }
    }
}
