// <copyright file="DataHealthReportDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>Aggregated data health report combining all health checks.</summary>
public sealed class DataHealthReportDto
{
    /// <summary>Gets or sets the duplicate transaction clusters.</summary>
    public IReadOnlyList<DuplicateClusterDto> Duplicates { get; set; } = [];

    /// <summary>Gets or sets the amount outlier transactions.</summary>
    public IReadOnlyList<AmountOutlierDto> Outliers { get; set; } = [];

    /// <summary>Gets or sets the date gaps per account.</summary>
    public IReadOnlyList<DateGapDto> DateGaps { get; set; } = [];

    /// <summary>Gets or sets the uncategorized transaction summary.</summary>
    public UncategorizedSummaryDto Uncategorized { get; set; } = new();
}
