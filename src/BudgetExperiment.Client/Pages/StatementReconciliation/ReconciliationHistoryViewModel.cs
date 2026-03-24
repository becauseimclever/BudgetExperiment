// <copyright file="ReconciliationHistoryViewModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Pages.StatementReconciliation;

/// <summary>
/// ViewModel for the Reconciliation History page.
/// Manages paginated list of completed reconciliation records.
/// </summary>
public sealed class ReconciliationHistoryViewModel
{
    private readonly IBudgetApiService _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReconciliationHistoryViewModel"/> class.
    /// </summary>
    /// <param name="api">The budget API service.</param>
    public ReconciliationHistoryViewModel(IBudgetApiService api)
    {
        _api = api;
    }

    /// <summary>Gets or sets the callback to notify the Razor page that state has changed.</summary>
    public Action? OnStateChanged
    {
        get; set;
    }

    /// <summary>Gets a value indicating whether data is loading.</summary>
    public bool IsLoading { get; private set; } = true;

    /// <summary>Gets the current error message, if any.</summary>
    public string? ErrorMessage
    {
        get; private set;
    }

    /// <summary>Gets the available accounts.</summary>
    public IReadOnlyList<AccountDto> Accounts { get; private set; } = [];

    /// <summary>Gets or sets the selected account identifier.</summary>
    public Guid? SelectedAccountId
    {
        get; set;
    }

    /// <summary>Gets the current page of reconciliation records.</summary>
    public IReadOnlyList<ReconciliationRecordDto> Records { get; private set; } = [];

    /// <summary>Gets or sets the current page number (1-based).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Gets the page size.</summary>
    public int PageSize { get; } = 20;

    /// <summary>Loads accounts for the account selector.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        NotifyStateChanged();

        Accounts = await _api.GetAccountsAsync();
        IsLoading = false;
        NotifyStateChanged();
    }

    /// <summary>Loads reconciliation history for the given account and page.</summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LoadHistoryAsync(Guid accountId, CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        SelectedAccountId = accountId;
        NotifyStateChanged();

        var records = await _api.GetReconciliationHistoryAsync(accountId, Page, PageSize);
        Records = records ?? [];
        IsLoading = false;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
