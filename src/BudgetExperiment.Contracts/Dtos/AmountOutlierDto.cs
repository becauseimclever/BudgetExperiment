// <copyright file="AmountOutlierDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>Represents a transaction flagged as an amount outlier within its merchant group.</summary>
public sealed class AmountOutlierDto
{
    /// <summary>Gets or sets the outlier transaction.</summary>
    public TransactionDto Transaction { get; set; } = new();

    /// <summary>Gets or sets the historical mean amount for the merchant group.</summary>
    public decimal HistoricalMean { get; set; }

    /// <summary>Gets or sets how many standard deviations the transaction deviates from the mean.</summary>
    public decimal DeviationFactor { get; set; }

    /// <summary>Gets or sets the normalized merchant group key.</summary>
    public string MerchantGroup { get; set; } = string.Empty;
}
