// <copyright file="SavingsProgressResponse.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Savings goal progress for a calendar month.
/// </summary>
public sealed class SavingsProgressResponse
{
    /// <summary>Gets or sets the savings goal the user set at month start.</summary>
    public decimal SavingsGoal
    {
        get; set;
    }

    /// <summary>Gets or sets the actual savings recorded (income minus expenses).</summary>
    public decimal ActualSavings
    {
        get; set;
    }

    /// <summary>Gets or sets the amount remaining to reach the savings goal.</summary>
    public decimal Remaining
    {
        get; set;
    }

    /// <summary>Gets or sets the progress as a percentage of the savings goal (0–100).</summary>
    public int ProgressPercentage
    {
        get; set;
    }
}
