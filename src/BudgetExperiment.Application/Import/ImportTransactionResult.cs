// <copyright file="ImportTransactionResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Import;

/// <summary>
/// Result of creating transactions from import data.
/// </summary>
/// <param name="CreatedIds">IDs of successfully created transactions.</param>
/// <param name="AutoCategorized">Count of transactions auto-categorized by rules.</param>
/// <param name="CsvCategorized">Count of transactions categorized from CSV column.</param>
/// <param name="Uncategorized">Count of uncategorized transactions.</param>
/// <param name="Skipped">Count of skipped transactions due to domain errors.</param>
/// <param name="LocationEnriched">Count of transactions enriched with location data.</param>
public sealed record ImportTransactionResult(
    List<Guid> CreatedIds,
    int AutoCategorized,
    int CsvCategorized,
    int Uncategorized,
    int Skipped,
    int LocationEnriched);
