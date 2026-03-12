// <copyright file="InlineCategorySuggestionDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// A lightweight category suggestion for inline display in the transaction list.
/// </summary>
public sealed class InlineCategorySuggestionDto
{
    /// <summary>
    /// Gets or sets the transaction ID this suggestion is for.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Gets or sets the suggested category ID.
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the suggested category name.
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;
}
