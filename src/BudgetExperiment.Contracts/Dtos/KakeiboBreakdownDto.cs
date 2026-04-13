// <copyright file="KakeiboBreakdownDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Expense breakdown by Kakeibo spending bucket.
/// </summary>
public sealed class KakeiboBreakdownDto
{
    /// <summary>
    /// Gets or sets the total spent on essential needs (housing, groceries, utilities).
    /// </summary>
    public decimal Essentials
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the total spent on wants (dining out, entertainment, shopping).
    /// </summary>
    public decimal Wants
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the total spent on culture (books, courses, experiences, self-development).
    /// </summary>
    public decimal Culture
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the total spent on unexpected expenses (medical, repairs, emergencies).
    /// </summary>
    public decimal Unexpected
    {
        get; set;
    }
}
