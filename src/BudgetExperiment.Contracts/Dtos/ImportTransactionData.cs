// <copyright file="ImportTransactionData.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Data for a single transaction to be imported.
/// </summary>
public sealed record ImportTransactionData
{
    /// <summary>
    /// Gets the transaction date.
    /// </summary>
    public DateOnly Date { get; init; }

    /// <summary>
    /// Gets the transaction description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the transaction amount.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Gets the category ID to assign.
    /// </summary>
    public Guid? CategoryId { get; init; }

    /// <summary>
    /// Gets the external reference from CSV.
    /// </summary>
    public string? Reference { get; init; }

    /// <summary>
    /// Gets the source of the category (for tracking).
    /// </summary>
    public CategorySource CategorySource { get; init; }

    /// <summary>
    /// Gets the matched rule ID (if auto-categorized).
    /// </summary>
    public Guid? MatchedRuleId { get; init; }

    /// <summary>
    /// Gets the city parsed from the description for location enrichment.
    /// </summary>
    public string? LocationCity { get; init; }

    /// <summary>
    /// Gets the state/region parsed from the description.
    /// </summary>
    public string? LocationStateOrRegion { get; init; }

    /// <summary>
    /// Gets the country code for location enrichment.
    /// </summary>
    public string? LocationCountry { get; init; }

    /// <summary>
    /// Gets the postal code for location enrichment.
    /// </summary>
    public string? LocationPostalCode { get; init; }

    /// <summary>
    /// Gets the source of the location data (e.g., "Parsed").
    /// </summary>
    public string? LocationSource { get; init; }
}
