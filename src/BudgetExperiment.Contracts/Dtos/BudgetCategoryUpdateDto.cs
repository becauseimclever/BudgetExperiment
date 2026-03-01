// <copyright file="BudgetCategoryUpdateDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

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
