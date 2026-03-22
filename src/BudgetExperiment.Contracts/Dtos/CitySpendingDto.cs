// <copyright file="CitySpendingDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for spending aggregated by city within a region.
/// </summary>
public sealed class CitySpendingDto
{
    /// <summary>Gets or sets the city name.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the total spending in this city.</summary>
    public decimal TotalSpending
    {
        get; set;
    }

    /// <summary>Gets or sets the number of transactions in this city.</summary>
    public int TransactionCount
    {
        get; set;
    }

    /// <summary>Gets or sets the percentage of region spending this city represents.</summary>
    public decimal Percentage
    {
        get; set;
    }
}
