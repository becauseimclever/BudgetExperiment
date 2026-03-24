// <copyright file="IDataHealthService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.DataHealth;

/// <summary>Service interface for data health analysis and fix actions.</summary>
public interface IDataHealthService
{
    /// <summary>Runs all health checks concurrently and returns the aggregated report.</summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Aggregated data health report.</returns>
    Task<DataHealthReportDto> AnalyzeAsync(Guid? accountId, CancellationToken ct);

    /// <summary>Finds duplicate transaction clusters.</summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Duplicate clusters.</returns>
    Task<IReadOnlyList<DuplicateClusterDto>> FindDuplicatesAsync(Guid? accountId, CancellationToken ct);

    /// <summary>Finds amount outlier transactions (deviating > 3σ from merchant mean).</summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Amount outlier transactions.</returns>
    Task<IReadOnlyList<AmountOutlierDto>> FindOutliersAsync(Guid? accountId, CancellationToken ct);

    /// <summary>Finds date gaps exceeding the threshold in transaction history.</summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="minGapDays">Minimum gap size to report (in days).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Date gap records.</returns>
    Task<IReadOnlyList<DateGapDto>> FindDateGapsAsync(Guid? accountId, int minGapDays, CancellationToken ct);

    /// <summary>Gets summary of uncategorized transactions across all accounts.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Uncategorized summary.</returns>
    Task<UncategorizedSummaryDto> GetUncategorizedSummaryAsync(CancellationToken ct);

    /// <summary>Merges duplicate transactions into a primary transaction, preserving category.</summary>
    /// <param name="primaryTransactionId">The primary transaction to keep.</param>
    /// <param name="duplicateIds">The duplicate transaction IDs to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MergeDuplicatesAsync(Guid primaryTransactionId, IReadOnlyList<Guid> duplicateIds, CancellationToken ct);

    /// <summary>Dismisses a transaction outlier so it is no longer reported.</summary>
    /// <param name="transactionId">The transaction identifier to dismiss.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DismissOutlierAsync(Guid transactionId, CancellationToken ct);
}
