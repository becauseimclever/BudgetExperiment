// <copyright file="IImportPreviewEnricher.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Import;

/// <summary>
/// Enriches import preview rows with recurring match and location data.
/// </summary>
public interface IImportPreviewEnricher
{
    /// <summary>
    /// Enriches preview rows with recurring transaction match information.
    /// </summary>
    /// <param name="rows">The preview rows to enrich.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Enriched preview rows.</returns>
    Task<List<ImportPreviewRow>> EnrichWithRecurringMatchesAsync(
        List<ImportPreviewRow> rows,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enriches preview rows with parsed location data from descriptions.
    /// </summary>
    /// <param name="rows">The preview rows to enrich.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Enriched preview rows.</returns>
    Task<List<ImportPreviewRow>> EnrichWithLocationDataAsync(
        List<ImportPreviewRow> rows,
        CancellationToken cancellationToken = default);
}
