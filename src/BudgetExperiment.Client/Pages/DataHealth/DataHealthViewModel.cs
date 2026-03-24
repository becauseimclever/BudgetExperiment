// <copyright file="DataHealthViewModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Pages.DataHealth;

/// <summary>
/// ViewModel for the Data Health dashboard page.
/// Loads and exposes the full data health report and supports merge/dismiss actions.
/// </summary>
public sealed class DataHealthViewModel
{
    private readonly IBudgetApiService _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataHealthViewModel"/> class.
    /// </summary>
    /// <param name="api">The budget API service.</param>
    public DataHealthViewModel(IBudgetApiService api)
    {
        _api = api;
    }

    /// <summary>Gets or sets the callback to notify the Razor page that state has changed.</summary>
    public Action? OnStateChanged { get; set; }

    /// <summary>Gets a value indicating whether data is currently loading.</summary>
    public bool IsLoading { get; private set; }

    /// <summary>Gets the current error message, if any.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Gets the full data health report.</summary>
    public DataHealthReportDto? Report { get; private set; }

    /// <summary>Gets the count of duplicate clusters.</summary>
    public int DuplicateCount => Report?.Duplicates.Count ?? 0;

    /// <summary>Gets the count of outlier transactions.</summary>
    public int OutlierCount => Report?.Outliers.Count ?? 0;

    /// <summary>Gets the count of date gaps.</summary>
    public int DateGapCount => Report?.DateGaps.Count ?? 0;

    /// <summary>Gets the count of uncategorized transactions.</summary>
    public int UncategorizedCount => Report?.Uncategorized.TotalCount ?? 0;

    /// <summary>Loads the data health report, optionally filtered by account.</summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LoadAsync(Guid? accountId, CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            Report = await _api.GetDataHealthReportAsync(accountId);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    /// <summary>Merges duplicate transactions into a primary, then refreshes the report.</summary>
    /// <param name="primaryTransactionId">The primary transaction identifier.</param>
    /// <param name="duplicateIds">The duplicate transaction identifiers to merge.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task MergeDuplicatesAsync(Guid primaryTransactionId, IReadOnlyList<Guid> duplicateIds, CancellationToken ct = default)
    {
        await _api.MergeDuplicatesAsync(new MergeDuplicatesRequest
        {
            PrimaryTransactionId = primaryTransactionId,
            DuplicateIds = duplicateIds,
        });
        await LoadAsync(null, ct);
    }

    /// <summary>Dismisses an outlier transaction, then refreshes the report.</summary>
    /// <param name="transactionId">The transaction identifier to dismiss.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DismissOutlierAsync(Guid transactionId, CancellationToken ct = default)
    {
        await _api.DismissOutlierAsync(transactionId);
        await LoadAsync(null, ct);
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
