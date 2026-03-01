// <copyright file="CategorySpendingDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for category spending within a report.
/// </summary>
public sealed class CategorySpendingDto
{
    /// <summary>Gets or sets the category identifier (null for uncategorized).</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Gets or sets the category name.</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Gets or sets the category color (hex code).</summary>
    public string? CategoryColor { get; set; }

    /// <summary>Gets or sets the total amount spent in this category.</summary>
    public MoneyDto Amount { get; set; } = new();

    /// <summary>Gets or sets the percentage of total spending this category represents.</summary>
    public decimal Percentage { get; set; }

    /// <summary>Gets or sets the number of transactions in this category.</summary>
    public int TransactionCount { get; set; }
}
