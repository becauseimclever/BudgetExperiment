// <copyright file="RegionSpendingDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for spending aggregated by geographic region (state/province).
/// </summary>
public sealed class RegionSpendingDto
{
    /// <summary>Gets or sets the region code (e.g., "US-WA").</summary>
    public string RegionCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the region display name (e.g., "Washington").</summary>
    public string RegionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the ISO country code.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>Gets or sets the total spending in this region.</summary>
    public decimal TotalSpending { get; set; }

    /// <summary>Gets or sets the number of transactions in this region.</summary>
    public int TransactionCount { get; set; }

    /// <summary>Gets or sets the percentage of total spending this region represents.</summary>
    public decimal Percentage { get; set; }

    /// <summary>Gets or sets the optional city-level breakdown within this region.</summary>
    public List<CitySpendingDto>? Cities { get; set; }
}
