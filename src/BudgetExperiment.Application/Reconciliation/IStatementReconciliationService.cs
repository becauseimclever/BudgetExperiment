// <copyright file="IStatementReconciliationService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Reconciliation;

/// <summary>
/// Application service for statement-based reconciliation (Feature 125).
/// Handles clearing/unclearing of transactions and the reconciliation workflow.
/// </summary>
public interface IStatementReconciliationService
{
    /// <summary>
    /// Marks a single transaction as cleared on the specified date.
    /// </summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="clearedDate">The date the transaction was cleared.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated transaction DTO.</returns>
    Task<TransactionDto> MarkClearedAsync(Guid transactionId, DateOnly clearedDate, CancellationToken ct);

    /// <summary>
    /// Unclears a single transaction (only allowed when not reconciled).
    /// </summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated transaction DTO.</returns>
    Task<TransactionDto> MarkUnclearedAsync(Guid transactionId, CancellationToken ct);

    /// <summary>
    /// Bulk-marks multiple transactions as cleared on the specified date.
    /// </summary>
    /// <param name="transactionIds">The list of transaction identifiers.</param>
    /// <param name="clearedDate">The date to set as cleared date.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated transaction DTOs for all affected transactions.</returns>
    Task<IReadOnlyList<TransactionDto>> BulkMarkClearedAsync(IReadOnlyList<Guid> transactionIds, DateOnly clearedDate, CancellationToken ct);

    /// <summary>
    /// Bulk-unclears multiple transactions, skipping any that are locked to a reconciliation.
    /// </summary>
    /// <param name="transactionIds">The list of transaction identifiers.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated transaction DTOs for successfully uncleared transactions only.</returns>
    Task<IReadOnlyList<TransactionDto>> BulkMarkUnclearedAsync(IReadOnlyList<Guid> transactionIds, CancellationToken ct);

    /// <summary>
    /// Gets the active (non-completed) statement balance for an account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The active statement balance DTO, or null if none exists.</returns>
    Task<StatementBalanceDto?> GetActiveStatementBalanceAsync(Guid accountId, CancellationToken ct);

    /// <summary>
    /// Gets the computed cleared balance for an account up to an optional date.
    /// Formula: InitialBalance + sum(cleared transactions where Date &lt;= upToDate).
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="upToDate">Optional upper bound date (inclusive). Null returns balance for all cleared transactions.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Cleared balance DTO.</returns>
    Task<ClearedBalanceDto> GetClearedBalanceAsync(Guid accountId, DateOnly? upToDate, CancellationToken ct);

    /// <summary>
    /// Creates or updates the active statement balance for an account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="statementDate">The statement closing date.</param>
    /// <param name="balance">The balance as reported on the bank statement.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created or updated statement balance DTO.</returns>
    Task<StatementBalanceDto> SetStatementBalanceAsync(Guid accountId, DateOnly statementDate, decimal balance, CancellationToken ct);

    /// <summary>
    /// Completes reconciliation for an account: validates balance match, creates a reconciliation record,
    /// locks all cleared transactions, and marks the statement balance as completed.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created reconciliation record DTO.</returns>
    Task<ReconciliationRecordDto> CompleteReconciliationAsync(Guid accountId, CancellationToken ct);

    /// <summary>
    /// Gets the reconciliation history for an account, paged.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paged list of reconciliation record DTOs.</returns>
    Task<IReadOnlyList<ReconciliationRecordDto>> GetReconciliationHistoryAsync(Guid accountId, int page, int pageSize, CancellationToken ct);

    /// <summary>
    /// Gets all transactions locked to a specific reconciliation record.
    /// </summary>
    /// <param name="reconciliationRecordId">The reconciliation record identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Transaction DTOs locked to the reconciliation record.</returns>
    Task<IReadOnlyList<TransactionDto>> GetReconciliationTransactionsAsync(Guid reconciliationRecordId, CancellationToken ct);

    /// <summary>
    /// Unlocks a reconciled transaction from its reconciliation record, allowing it to be uncleared.
    /// </summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnlockTransactionAsync(Guid transactionId, CancellationToken ct);
}
