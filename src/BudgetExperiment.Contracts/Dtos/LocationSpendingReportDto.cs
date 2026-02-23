// <copyright file="LocationSpendingReportDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for location-based spending report.
/// </summary>
public sealed class LocationSpendingReportDto
{
    /// <summary>Gets or sets the start date of the report range.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Gets or sets the end date of the report range.</summary>
    public DateOnly EndDate { get; set; }

    /// <summary>Gets or sets the total spending amount across all transactions.</summary>
    public decimal TotalSpending { get; set; }

    /// <summary>Gets or sets the total number of non-transfer transactions.</summary>
    public int TotalTransactions { get; set; }

    /// <summary>Gets or sets the count of transactions that have location data.</summary>
    public int TransactionsWithLocation { get; set; }

    /// <summary>Gets or sets the list of spending aggregated by region.</summary>
    public List<RegionSpendingDto> Regions { get; set; } = new();
}
