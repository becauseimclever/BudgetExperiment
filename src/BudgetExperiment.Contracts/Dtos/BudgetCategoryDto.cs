// <copyright file="BudgetCategoryDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Data transfer object for a budget category.
/// </summary>
public sealed class BudgetCategoryDto
{
    /// <summary>
    /// Gets or sets the category identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the icon identifier.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the hex color code.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets the category type (Expense, Income, Transfer).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sort order.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the category is active.
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Data transfer object for creating a budget category.
/// </summary>
public sealed class BudgetCategoryCreateDto
{
    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the icon identifier.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the hex color code.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets the category type (defaults to "Expense").
    /// </summary>
    public string Type { get; set; } = "Expense";

    /// <summary>
    /// Gets or sets the optional initial budget amount for the current month.
    /// When provided, a budget goal will be created for the current month.
    /// </summary>
    public MoneyDto? InitialBudget { get; set; }
}

/// <summary>
/// Data transfer object for updating a budget category.
/// </summary>
public sealed class BudgetCategoryUpdateDto
{
    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the icon identifier.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the hex color code.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets the sort order.
    /// </summary>
    public int? SortOrder { get; set; }
}
