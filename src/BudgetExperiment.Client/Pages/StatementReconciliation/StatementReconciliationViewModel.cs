// <copyright file="StatementReconciliationViewModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Pages.StatementReconciliation;

/// <summary>
/// ViewModel for the Statement Reconciliation workspace page.
/// Tracks statement balance, cleared transactions, and balance difference.
/// </summary>
public sealed class StatementReconciliationViewModel
{
    private readonly IBudgetApiService _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatementReconciliationViewModel"/> class.
    /// </summary>
    /// <param name="api">The budget API service.</param>
    public StatementReconciliationViewModel(IBudgetApiService api)
    {
        _api = api;
    }

    /// <summary>Gets or sets the callback to notify the Razor page that state has changed.</summary>
    public Action? OnStateChanged { get; set; }

    /// <summary>Gets a value indicating whether initial data is loading.</summary>
    public bool IsLoading { get; private set; } = true;

    /// <summary>Gets a value indicating whether an action is in progress.</summary>
    public bool IsProcessing { get; private set; }

    /// <summary>Gets the current error message, if any.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Gets the list of accounts available for selection.</summary>
    public IReadOnlyList<AccountDto> Accounts { get; private set; } = [];

    /// <summary>Gets or sets the selected account identifier.</summary>
    public Guid? SelectedAccountId { get; set; }

    /// <summary>Gets or sets the statement closing date.</summary>
    public DateOnly StatementDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Gets or sets the balance from the bank statement.</summary>
    public decimal? StatementBalance { get; set; }

    /// <summary>Gets the computed cleared balance (InitialBalance + sum of cleared transactions).</summary>
    public decimal ClearedBalance { get; private set; }

    /// <summary>Gets the computed difference between statement balance and cleared balance.</summary>
    public decimal Difference => (StatementBalance ?? 0m) - ClearedBalance;

    /// <summary>Gets a value indicating whether the account is balanced (difference is zero).</summary>
    public bool IsBalanced => Difference == 0m && StatementBalance.HasValue;

    /// <summary>Gets the transactions for the current account (for display in the workspace).</summary>
    public IReadOnlyList<TransactionDto> Transactions { get; private set; } = [];

    /// <summary>Gets the active statement balance DTO, if one exists.</summary>
    public StatementBalanceDto? ActiveStatementBalance { get; private set; }

    /// <summary>Gets or sets the last completed reconciliation record, if recently completed.</summary>
    public ReconciliationRecordDto? LastCompletedRecord { get; set; }

    /// <summary>Loads accounts and resets state.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        NotifyStateChanged();

        var accounts = await _api.GetAccountsAsync();
        Accounts = accounts;
        IsLoading = false;
        NotifyStateChanged();
    }

    /// <summary>Loads account-specific data: transactions, cleared balance, and active statement balance.</summary>
    /// <param name="accountId">The account to load data for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LoadAccountDataAsync(Guid accountId, CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        NotifyStateChanged();

        SelectedAccountId = accountId;

        var activeBalance = await _api.GetActiveStatementBalanceAsync(accountId);
        ActiveStatementBalance = activeBalance;
        if (activeBalance is not null)
        {
            StatementDate = activeBalance.StatementDate;
            StatementBalance = activeBalance.Balance;
        }

        var clearedBalance = await _api.GetClearedBalanceAsync(accountId, StatementBalance.HasValue ? StatementDate : null);
        ClearedBalance = clearedBalance?.ClearedBalance ?? 0m;

        var account = await _api.GetAccountAsync(accountId);
        Transactions = account?.Transactions ?? [];

        IsLoading = false;
        NotifyStateChanged();
    }

    /// <summary>Saves or updates the statement balance for the selected account.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SaveStatementBalanceAsync(CancellationToken ct = default)
    {
        if (SelectedAccountId is null || !StatementBalance.HasValue)
        {
            return;
        }

        IsProcessing = true;
        ErrorMessage = null;
        NotifyStateChanged();

        var result = await _api.SetStatementBalanceAsync(new SetStatementBalanceRequest
        {
            AccountId = SelectedAccountId.Value,
            StatementDate = StatementDate,
            Balance = StatementBalance.Value,
        });

        if (result is not null)
        {
            ActiveStatementBalance = result;
            var clearedBalance = await _api.GetClearedBalanceAsync(SelectedAccountId.Value, StatementDate);
            ClearedBalance = clearedBalance?.ClearedBalance ?? 0m;
        }
        else
        {
            ErrorMessage = "Failed to save statement balance.";
        }

        IsProcessing = false;
        NotifyStateChanged();
    }

    /// <summary>Marks a single transaction as cleared.</summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="date">The cleared date.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task MarkClearedAsync(Guid transactionId, DateOnly date, CancellationToken ct = default)
    {
        IsProcessing = true;
        NotifyStateChanged();

        var result = await _api.MarkTransactionClearedAsync(new MarkClearedRequest
        {
            TransactionId = transactionId,
            ClearedDate = date,
        });

        await RefreshTransactionAndBalanceAsync(transactionId, result);

        IsProcessing = false;
        NotifyStateChanged();
    }

    /// <summary>Marks a single transaction as uncleared.</summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task MarkUnclearedAsync(Guid transactionId, CancellationToken ct = default)
    {
        IsProcessing = true;
        NotifyStateChanged();

        var result = await _api.MarkTransactionUnclearedAsync(new MarkUnclearedRequest { TransactionId = transactionId });
        await RefreshTransactionAndBalanceAsync(transactionId, result);

        IsProcessing = false;
        NotifyStateChanged();
    }

    /// <summary>Bulk-marks multiple transactions as cleared.</summary>
    /// <param name="ids">The transaction identifiers.</param>
    /// <param name="date">The cleared date.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task BulkMarkClearedAsync(IReadOnlyList<Guid> ids, DateOnly date, CancellationToken ct = default)
    {
        if (ids.Count == 0)
        {
            return;
        }

        IsProcessing = true;
        NotifyStateChanged();

        await _api.BulkMarkTransactionsClearedAsync(new BulkMarkClearedRequest { TransactionIds = ids, ClearedDate = date });
        await RefreshBalanceAsync();

        if (SelectedAccountId.HasValue)
        {
            var account = await _api.GetAccountAsync(SelectedAccountId.Value);
            Transactions = account?.Transactions ?? [];
        }

        IsProcessing = false;
        NotifyStateChanged();
    }

    /// <summary>Bulk-unclears multiple transactions.</summary>
    /// <param name="ids">The transaction identifiers.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task BulkMarkUnclearedAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0)
        {
            return;
        }

        IsProcessing = true;
        NotifyStateChanged();

        await _api.BulkMarkTransactionsUnclearedAsync(new BulkMarkUnclearedRequest { TransactionIds = ids });
        await RefreshBalanceAsync();

        if (SelectedAccountId.HasValue)
        {
            var account = await _api.GetAccountAsync(SelectedAccountId.Value);
            Transactions = account?.Transactions ?? [];
        }

        IsProcessing = false;
        NotifyStateChanged();
    }

    /// <summary>Completes the reconciliation for the selected account.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CompleteReconciliationAsync(CancellationToken ct = default)
    {
        if (SelectedAccountId is null)
        {
            return;
        }

        IsProcessing = true;
        ErrorMessage = null;
        NotifyStateChanged();

        var result = await _api.CompleteReconciliationAsync(new CompleteReconciliationRequest
        {
            AccountId = SelectedAccountId.Value,
        });

        if (result.IsSuccess && result.Data is not null)
        {
            LastCompletedRecord = result.Data;
            ActiveStatementBalance = null;
            StatementBalance = null;
            await RefreshBalanceAsync();
        }
        else
        {
            ErrorMessage = "Cannot complete reconciliation. Make sure all cleared transactions match the statement balance.";
        }

        IsProcessing = false;
        NotifyStateChanged();
    }

    private async Task RefreshTransactionAndBalanceAsync(Guid transactionId, TransactionDto? updated)
    {
        if (updated is not null)
        {
            var list = Transactions.ToList();
            var idx = list.FindIndex(t => t.Id == transactionId);
            if (idx >= 0)
            {
                list[idx] = updated;
            }

            Transactions = list;
        }

        await RefreshBalanceAsync();
    }

    private async Task RefreshBalanceAsync()
    {
        if (SelectedAccountId is null)
        {
            return;
        }

        var clearedBalance = await _api.GetClearedBalanceAsync(SelectedAccountId.Value, StatementBalance.HasValue ? StatementDate : null);
        ClearedBalance = clearedBalance?.ClearedBalance ?? 0m;
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
