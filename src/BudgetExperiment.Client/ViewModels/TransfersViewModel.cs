// <copyright file="TransfersViewModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Shared.Budgeting;

namespace BudgetExperiment.Client.ViewModels;

/// <summary>
/// ViewModel for the Transfers page. Contains all handler logic, state fields,
/// and filtering logic extracted from the Transfers.razor @code block.
/// </summary>
public sealed class TransfersViewModel : IDisposable
{
    private readonly IBudgetApiService _apiService;
    private readonly ScopeService _scopeService;
    private readonly IChatContextService _chatContextService;
    private readonly IApiErrorContext _apiErrorContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransfersViewModel"/> class.
    /// </summary>
    /// <param name="apiService">The budget API service.</param>
    /// <param name="scopeService">The budget scope service.</param>
    /// <param name="chatContextService">The chat context service.</param>
    /// <param name="apiErrorContext">The API error context for trace ID tracking.</param>
    public TransfersViewModel(
        IBudgetApiService apiService,
        ScopeService scopeService,
        IChatContextService chatContextService,
        IApiErrorContext apiErrorContext)
    {
        _apiService = apiService;
        _scopeService = scopeService;
        _chatContextService = chatContextService;
        _apiErrorContext = apiErrorContext;
    }

    /// <summary>
    /// Gets or sets the callback to notify the Razor page that state has changed and it should re-render.
    /// </summary>
    public Action? OnStateChanged
    {
        get; set;
    }

    /// <summary>
    /// Gets a value indicating whether data is loading.
    /// </summary>
    public bool IsLoading { get; private set; } = true;

    /// <summary>
    /// Gets a value indicating whether a retry load is in progress.
    /// </summary>
    public bool IsRetrying
    {
        get; private set;
    }

    /// <summary>
    /// Gets the current error message, if any.
    /// </summary>
    public string? ErrorMessage
    {
        get; private set;
    }

    /// <summary>
    /// Gets the trace ID associated with the current error, if any.
    /// </summary>
    public string? ErrorTraceId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the list of accounts for the filter dropdown.
    /// </summary>
    public List<AccountDto> Accounts { get; private set; } = new();

    /// <summary>
    /// Gets the full list of transfers (unfiltered).
    /// </summary>
    public List<TransferListItemResponse> Transfers { get; private set; } = new();

    /// <summary>
    /// Gets the filtered list of transfers currently displayed.
    /// </summary>
    public List<TransferListItemResponse> FilteredTransfers { get; private set; } = new();

    /// <summary>
    /// Gets or sets the selected account ID for filtering.
    /// </summary>
    public string SelectedAccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the from date filter.
    /// </summary>
    public DateOnly? FromDate
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the to date filter.
    /// </summary>
    public DateOnly? ToDate
    {
        get; set;
    }

    /// <summary>
    /// Gets a value indicating whether the transfer dialog is visible.
    /// </summary>
    public bool ShowTransferDialog
    {
        get; private set;
    }

    /// <summary>
    /// Gets or sets the create transfer request model.
    /// </summary>
    public CreateTransferRequest NewTransfer { get; set; } = new();

    /// <summary>
    /// Gets or sets the update transfer request model.
    /// </summary>
    public UpdateTransferRequest EditTransfer { get; set; } = new();

    /// <summary>
    /// Gets the transfer currently being edited, or null if creating.
    /// </summary>
    public TransferListItemResponse? EditingTransfer
    {
        get; private set;
    }

    /// <summary>
    /// Initializes the ViewModel: subscribes to scope changes, sets chat context, and loads data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        _scopeService.ScopeChanged += this.OnScopeChanged;
        _chatContextService.SetPageType("transfers");
        await this.LoadDataAsync();
    }

    /// <summary>
    /// Loads accounts and transfers from the API.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task LoadDataAsync()
    {
        try
        {
            this.IsLoading = true;
            this.ErrorMessage = null;
            this.ErrorTraceId = null;

            var accountsTask = _apiService.GetAccountsAsync();
            var transfersTask = _apiService.GetTransfersAsync();

            await Task.WhenAll(accountsTask, transfersTask);

            this.Accounts = accountsTask.Result.ToList();
            this.Transfers = transfersTask.Result.ToList();
            this.ApplyFilters();
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to load transfers: {ex.Message}";
            this.ErrorTraceId = _apiErrorContext.LastTraceId;
        }
        finally
        {
            this.IsLoading = false;
        }
    }

    /// <summary>
    /// Retries loading data after a failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task RetryLoadAsync()
    {
        this.IsRetrying = true;
        this.NotifyStateChanged();

        try
        {
            await this.LoadDataAsync();
        }
        finally
        {
            this.IsRetrying = false;
        }
    }

    /// <summary>
    /// Dismisses the current error message.
    /// </summary>
    public void DismissError()
    {
        this.ErrorMessage = null;
        this.ErrorTraceId = null;
    }

    /// <summary>
    /// Applies the current filter criteria to the transfers list.
    /// </summary>
    public void ApplyFilters()
    {
        this.FilteredTransfers = this.Transfers;

        if (!string.IsNullOrEmpty(this.SelectedAccountId) && Guid.TryParse(this.SelectedAccountId, out var accountId))
        {
            this.FilteredTransfers = this.FilteredTransfers
                .Where(t => t.SourceAccountId == accountId || t.DestinationAccountId == accountId)
                .ToList();
        }

        if (this.FromDate.HasValue)
        {
            this.FilteredTransfers = this.FilteredTransfers
                .Where(t => t.Date >= this.FromDate.Value)
                .ToList();
        }

        if (this.ToDate.HasValue)
        {
            this.FilteredTransfers = this.FilteredTransfers
                .Where(t => t.Date <= this.ToDate.Value)
                .ToList();
        }
    }

    /// <summary>
    /// Clears all filters and resets the filtered list.
    /// </summary>
    public void ClearFilters()
    {
        this.SelectedAccountId = string.Empty;
        this.FromDate = null;
        this.ToDate = null;
        this.ApplyFilters();
    }

    /// <summary>
    /// Opens the create transfer dialog.
    /// </summary>
    public void ShowCreateTransfer()
    {
        this.NewTransfer = new CreateTransferRequest
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
        };
        this.EditingTransfer = null;
        this.ShowTransferDialog = true;
    }

    /// <summary>
    /// Opens the edit transfer dialog for the specified transfer.
    /// </summary>
    /// <param name="transfer">The transfer to edit.</param>
    public void ShowEditTransfer(TransferListItemResponse transfer)
    {
        this.EditingTransfer = transfer;
        this.EditTransfer = new UpdateTransferRequest
        {
            Amount = transfer.Amount,
            Date = transfer.Date,
            Description = transfer.Description,
        };
        this.ShowTransferDialog = true;
    }

    /// <summary>
    /// Hides the transfer dialog.
    /// </summary>
    public void HideTransferDialog()
    {
        this.ShowTransferDialog = false;
        this.EditingTransfer = null;
    }

    /// <summary>
    /// Creates a new transfer via the API.
    /// </summary>
    /// <param name="model">The create transfer request.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task CreateTransferAsync(CreateTransferRequest model)
    {
        try
        {
            await _apiService.CreateTransferAsync(model);
            this.HideTransferDialog();
            await this.LoadDataAsync();
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to create transfer: {ex.Message}";
            this.ErrorTraceId = _apiErrorContext.LastTraceId;
        }
    }

    /// <summary>
    /// Updates an existing transfer via the API.
    /// </summary>
    /// <param name="model">The update transfer request.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task UpdateTransferAsync(UpdateTransferRequest model)
    {
        if (this.EditingTransfer == null)
        {
            return;
        }

        try
        {
            await _apiService.UpdateTransferAsync(this.EditingTransfer.TransferId, model);
            this.HideTransferDialog();
            await this.LoadDataAsync();
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to update transfer: {ex.Message}";
            this.ErrorTraceId = _apiErrorContext.LastTraceId;
        }
    }

    /// <summary>
    /// Deletes a transfer via the API.
    /// </summary>
    /// <param name="transferId">The ID of the transfer to delete.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task DeleteTransferAsync(Guid transferId)
    {
        try
        {
            await _apiService.DeleteTransferAsync(transferId);
            await this.LoadDataAsync();
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to delete transfer: {ex.Message}";
            this.ErrorTraceId = _apiErrorContext.LastTraceId;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _scopeService.ScopeChanged -= this.OnScopeChanged;
        _chatContextService.ClearContext();
    }

    private async void OnScopeChanged(BudgetScope? scope)
    {
        await this.LoadDataAsync();
        this.NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        this.OnStateChanged?.Invoke();
    }
}
