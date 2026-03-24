// <copyright file="ReconciliationDetailViewModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Pages.StatementReconciliation;

/// <summary>
/// ViewModel for the Reconciliation Detail page.
/// Shows a completed reconciliation record with its locked transactions.
/// </summary>
public sealed class ReconciliationDetailViewModel
{
    private readonly IBudgetApiService _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReconciliationDetailViewModel"/> class.
    /// </summary>
    /// <param name="api">The budget API service.</param>
    public ReconciliationDetailViewModel(IBudgetApiService api)
    {
        _api = api;
    }

    /// <summary>Gets or sets the callback to notify the Razor page that state has changed.</summary>
    public Action? OnStateChanged { get; set; }

    /// <summary>Gets a value indicating whether data is loading.</summary>
    public bool IsLoading { get; private set; } = true;

    /// <summary>Gets the current error message, if any.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Gets the reconciliation record identifier.</summary>
    public Guid RecordId { get; private set; }

    /// <summary>Gets the reconciliation history records (all, so we can find the specific one).</summary>
    public ReconciliationRecordDto? Record { get; private set; }

    /// <summary>Gets the transactions locked to this reconciliation record.</summary>
    public IReadOnlyList<TransactionDto> Transactions { get; private set; } = [];

    /// <summary>Loads the reconciliation detail for the given record.</summary>
    /// <param name="recordId">The reconciliation record identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LoadAsync(Guid recordId, CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        RecordId = recordId;
        NotifyStateChanged();

        var transactions = await _api.GetReconciliationTransactionsAsync(recordId);
        Transactions = transactions ?? [];

        IsLoading = false;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
