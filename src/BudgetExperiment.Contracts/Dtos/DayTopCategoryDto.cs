// <copyright file="DayTopCategoryDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for a top category in a day summary.
/// </summary>
public sealed class DayTopCategoryDto
{
    /// <summary>Gets or sets the category name.</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount spent in this category.</summary>
    public MoneyDto Amount { get; set; } = new();
}
