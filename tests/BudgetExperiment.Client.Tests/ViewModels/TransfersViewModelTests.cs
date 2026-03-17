// <copyright file="TransfersViewModelTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Client.ViewModels;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Shared.Budgeting;
using Microsoft.JSInterop;
using Shouldly;

namespace BudgetExperiment.Client.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="TransfersViewModel"/>.
/// </summary>
public sealed class TransfersViewModelTests : IDisposable
{
    private readonly StubBudgetApiService _apiService = new();
    private readonly ScopeService _scopeService;
    private readonly StubChatContextService _chatContext = new();
    private readonly StubApiErrorContext _apiErrorContext = new();
    private readonly TransfersViewModel _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransfersViewModelTests"/> class.
    /// </summary>
    public TransfersViewModelTests()
    {
        this._scopeService = new ScopeService(new StubJSRuntime());
        this._sut = new TransfersViewModel(
            this._apiService,
            this._scopeService,
            this._chatContext,
            this._apiErrorContext);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this._sut.Dispose();
    }

    // --- Initialization ---

    /// <summary>
    /// Verifies that InitializeAsync loads transfers from the API.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsTransfers()
    {
        this._apiService.Transfers.Add(CreateTransfer("Monthly savings"));

        await this._sut.InitializeAsync();

        this._sut.Transfers.Count.ShouldBe(1);
        this._sut.Transfers[0].Description.ShouldBe("Monthly savings");
    }

    /// <summary>
    /// Verifies that InitializeAsync loads accounts from the API.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsAccounts()
    {
        this._apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Checking",
        });

        await this._sut.InitializeAsync();

        this._sut.Accounts.Count.ShouldBe(1);
        this._sut.Accounts[0].Name.ShouldBe("Checking");
    }

    /// <summary>
    /// Verifies that InitializeAsync sets IsLoading to false after loading.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsIsLoadingToFalse()
    {
        await this._sut.InitializeAsync();

        this._sut.IsLoading.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that InitializeAsync sets the page type on the chat context service.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsPageTypeOnChatContext()
    {
        await this._sut.InitializeAsync();

        this._chatContext.CurrentContext.PageType.ShouldBe("transfers");
    }

    /// <summary>
    /// Verifies that InitializeAsync handles API failure gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsErrorMessage_WhenApiFails()
    {
        this._apiService.GetTransfersException = new HttpRequestException("Server error");

        await this._sut.InitializeAsync();

        this._sut.ErrorMessage.ShouldNotBeNull();
        this._sut.ErrorMessage!.ShouldContain("Failed to load transfers");
        this._sut.IsLoading.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that FilteredTransfers equals Transfers after init (no filters applied).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsFilteredTransfersToAllTransfers()
    {
        this._apiService.Transfers.Add(CreateTransfer("T1"));
        this._apiService.Transfers.Add(CreateTransfer("T2"));

        await this._sut.InitializeAsync();

        this._sut.FilteredTransfers.Count.ShouldBe(2);
    }

    // --- LoadDataAsync ---

    /// <summary>
    /// Verifies that LoadDataAsync clears the error message before loading.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadDataAsync_ClearsErrorMessage()
    {
        this._apiService.GetTransfersException = new HttpRequestException("fail");
        await this._sut.InitializeAsync();
        this._sut.ErrorMessage.ShouldNotBeNull();

        this._apiService.GetTransfersException = null;
        await this._sut.LoadDataAsync();

        this._sut.ErrorMessage.ShouldBeNull();
    }

    // --- RetryLoadAsync ---

    /// <summary>
    /// Verifies that RetryLoadAsync reloads transfers.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RetryLoadAsync_ReloadsTransfers()
    {
        await this._sut.InitializeAsync();
        this._apiService.Transfers.Add(CreateTransfer("New Transfer"));

        await this._sut.RetryLoadAsync();

        this._sut.Transfers.Count.ShouldBe(1);
        this._sut.IsRetrying.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that RetryLoadAsync notifies state changed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RetryLoadAsync_NotifiesStateChanged()
    {
        int callCount = 0;
        this._sut.OnStateChanged = () => callCount++;

        await this._sut.RetryLoadAsync();

        callCount.ShouldBeGreaterThan(0);
    }

    // --- DismissError ---

    /// <summary>
    /// Verifies that DismissError clears the error message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DismissError_ClearsErrorMessage()
    {
        this._apiService.GetTransfersException = new HttpRequestException("fail");
        await this._sut.InitializeAsync();
        this._sut.ErrorMessage.ShouldNotBeNull();

        this._sut.DismissError();

        this._sut.ErrorMessage.ShouldBeNull();
    }

    // --- Filtering ---

    /// <summary>
    /// Verifies that ApplyFilters filters by account ID.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyFilters_FiltersByAccountId()
    {
        var accountId = Guid.NewGuid();
        this._apiService.Transfers.Add(CreateTransfer("Match", sourceAccountId: accountId));
        this._apiService.Transfers.Add(CreateTransfer("No match"));

        await this._sut.InitializeAsync();
        this._sut.SelectedAccountId = accountId.ToString();
        this._sut.ApplyFilters();

        this._sut.FilteredTransfers.Count.ShouldBe(1);
        this._sut.FilteredTransfers[0].Description.ShouldBe("Match");
    }

    /// <summary>
    /// Verifies that ApplyFilters matches on destination account too.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyFilters_FiltersByDestinationAccountId()
    {
        var accountId = Guid.NewGuid();
        this._apiService.Transfers.Add(CreateTransfer("Dest match", destinationAccountId: accountId));
        this._apiService.Transfers.Add(CreateTransfer("No match"));

        await this._sut.InitializeAsync();
        this._sut.SelectedAccountId = accountId.ToString();
        this._sut.ApplyFilters();

        this._sut.FilteredTransfers.Count.ShouldBe(1);
        this._sut.FilteredTransfers[0].Description.ShouldBe("Dest match");
    }

    /// <summary>
    /// Verifies that ApplyFilters filters by from date.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyFilters_FiltersByFromDate()
    {
        this._apiService.Transfers.Add(CreateTransfer("Before", date: new DateOnly(2026, 1, 1)));
        this._apiService.Transfers.Add(CreateTransfer("After", date: new DateOnly(2026, 6, 1)));

        await this._sut.InitializeAsync();
        this._sut.FromDate = new DateOnly(2026, 3, 1);
        this._sut.ApplyFilters();

        this._sut.FilteredTransfers.Count.ShouldBe(1);
        this._sut.FilteredTransfers[0].Description.ShouldBe("After");
    }

    /// <summary>
    /// Verifies that ApplyFilters filters by to date.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyFilters_FiltersByToDate()
    {
        this._apiService.Transfers.Add(CreateTransfer("Before", date: new DateOnly(2026, 1, 1)));
        this._apiService.Transfers.Add(CreateTransfer("After", date: new DateOnly(2026, 6, 1)));

        await this._sut.InitializeAsync();
        this._sut.ToDate = new DateOnly(2026, 3, 1);
        this._sut.ApplyFilters();

        this._sut.FilteredTransfers.Count.ShouldBe(1);
        this._sut.FilteredTransfers[0].Description.ShouldBe("Before");
    }

    /// <summary>
    /// Verifies that ApplyFilters applies date range filter.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyFilters_FiltersByDateRange()
    {
        this._apiService.Transfers.Add(CreateTransfer("Jan", date: new DateOnly(2026, 1, 1)));
        this._apiService.Transfers.Add(CreateTransfer("Mar", date: new DateOnly(2026, 3, 15)));
        this._apiService.Transfers.Add(CreateTransfer("Jun", date: new DateOnly(2026, 6, 1)));

        await this._sut.InitializeAsync();
        this._sut.FromDate = new DateOnly(2026, 2, 1);
        this._sut.ToDate = new DateOnly(2026, 4, 1);
        this._sut.ApplyFilters();

        this._sut.FilteredTransfers.Count.ShouldBe(1);
        this._sut.FilteredTransfers[0].Description.ShouldBe("Mar");
    }

    /// <summary>
    /// Verifies that ClearFilters resets all filter state.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ClearFilters_ResetsAllFilters()
    {
        this._apiService.Transfers.Add(CreateTransfer("T1"));
        this._apiService.Transfers.Add(CreateTransfer("T2"));

        await this._sut.InitializeAsync();
        this._sut.SelectedAccountId = Guid.NewGuid().ToString();
        this._sut.FromDate = new DateOnly(2026, 1, 1);
        this._sut.ToDate = new DateOnly(2026, 12, 31);
        this._sut.ApplyFilters();

        this._sut.ClearFilters();

        this._sut.SelectedAccountId.ShouldBe(string.Empty);
        this._sut.FromDate.ShouldBeNull();
        this._sut.ToDate.ShouldBeNull();
        this._sut.FilteredTransfers.Count.ShouldBe(2);
    }

    // --- Dialog state ---

    /// <summary>
    /// Verifies that ShowCreateTransfer opens the dialog with a fresh model.
    /// </summary>
    [Fact]
    public void ShowCreateTransfer_OpensDialog()
    {
        this._sut.ShowCreateTransfer();

        this._sut.ShowTransferDialog.ShouldBeTrue();
        this._sut.EditingTransfer.ShouldBeNull();
        this._sut.NewTransfer.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies that ShowEditTransfer opens the dialog with the transfer data.
    /// </summary>
    [Fact]
    public void ShowEditTransfer_OpensDialogWithTransferData()
    {
        var transfer = CreateTransfer("Edit me");

        this._sut.ShowEditTransfer(transfer);

        this._sut.ShowTransferDialog.ShouldBeTrue();
        this._sut.EditingTransfer.ShouldBe(transfer);
        this._sut.EditTransfer.Amount.ShouldBe(transfer.Amount);
        this._sut.EditTransfer.Date.ShouldBe(transfer.Date);
        this._sut.EditTransfer.Description.ShouldBe(transfer.Description);
    }

    /// <summary>
    /// Verifies that HideTransferDialog closes the dialog.
    /// </summary>
    [Fact]
    public void HideTransferDialog_ClosesDialog()
    {
        this._sut.ShowCreateTransfer();
        this._sut.ShowTransferDialog.ShouldBeTrue();

        this._sut.HideTransferDialog();

        this._sut.ShowTransferDialog.ShouldBeFalse();
        this._sut.EditingTransfer.ShouldBeNull();
    }

    // --- CRUD ---

    /// <summary>
    /// Verifies that CreateTransferAsync calls the API and reloads data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateTransferAsync_CallsApiAndReloads()
    {
        this._apiService.CreateTransferResult = new TransferResponse
        {
            TransferId = Guid.NewGuid(),
            Amount = 100m,
            Date = new DateOnly(2026, 3, 1),
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
        };

        await this._sut.InitializeAsync();
        this._sut.ShowCreateTransfer();

        await this._sut.CreateTransferAsync(new CreateTransferRequest
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100m,
            Date = new DateOnly(2026, 3, 1),
        });

        this._sut.ShowTransferDialog.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that CreateTransferAsync sets error message on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateTransferAsync_SetsErrorMessage_OnFailure()
    {
        this._apiService.CreateTransferException = new HttpRequestException("Create failed");

        await this._sut.CreateTransferAsync(new CreateTransferRequest());

        this._sut.ErrorMessage.ShouldNotBeNull();
        this._sut.ErrorMessage!.ShouldContain("Failed to create transfer");
    }

    /// <summary>
    /// Verifies that UpdateTransferAsync calls the API and reloads data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateTransferAsync_CallsApiAndReloads()
    {
        var transfer = CreateTransfer("Original");
        this._apiService.UpdateTransferResult = new TransferResponse
        {
            TransferId = transfer.TransferId,
            Amount = 200m,
            Date = new DateOnly(2026, 3, 1),
            SourceAccountId = transfer.SourceAccountId,
            DestinationAccountId = transfer.DestinationAccountId,
        };

        await this._sut.InitializeAsync();
        this._sut.ShowEditTransfer(transfer);

        await this._sut.UpdateTransferAsync(new UpdateTransferRequest
        {
            Amount = 200m,
            Date = new DateOnly(2026, 3, 1),
        });

        this._sut.ShowTransferDialog.ShouldBeFalse();
        this._sut.EditingTransfer.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that UpdateTransferAsync does nothing when EditingTransfer is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateTransferAsync_DoesNothing_WhenNoEditingTransfer()
    {
        await this._sut.InitializeAsync();

        await this._sut.UpdateTransferAsync(new UpdateTransferRequest());

        this._sut.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that UpdateTransferAsync sets error message on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateTransferAsync_SetsErrorMessage_OnFailure()
    {
        var transfer = CreateTransfer("Fail");
        this._sut.ShowEditTransfer(transfer);
        this._apiService.UpdateTransferException = new HttpRequestException("Update failed");

        await this._sut.UpdateTransferAsync(new UpdateTransferRequest());

        this._sut.ErrorMessage.ShouldNotBeNull();
        this._sut.ErrorMessage!.ShouldContain("Failed to update transfer");
    }

    /// <summary>
    /// Verifies that DeleteTransferAsync calls the API and reloads data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteTransferAsync_CallsApiAndReloads()
    {
        var transfer = CreateTransfer("Delete me");
        this._apiService.Transfers.Add(transfer);
        this._apiService.DeleteTransferResult = true;

        await this._sut.InitializeAsync();
        this._sut.Transfers.Count.ShouldBe(1);

        // After delete, the stub will return the current contents of Transfers
        this._apiService.Transfers.Clear();
        await this._sut.DeleteTransferAsync(transfer.TransferId);

        this._sut.Transfers.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that DeleteTransferAsync sets error message on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteTransferAsync_SetsErrorMessage_OnFailure()
    {
        this._apiService.DeleteTransferException = new HttpRequestException("Delete failed");

        await this._sut.DeleteTransferAsync(Guid.NewGuid());

        this._sut.ErrorMessage.ShouldNotBeNull();
        this._sut.ErrorMessage!.ShouldContain("Failed to delete transfer");
    }

    // --- Scope changes ---

    /// <summary>
    /// Verifies that scope change triggers a reload of data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ScopeChange_ReloadsTransfers()
    {
        await this._sut.InitializeAsync();
        this._sut.Transfers.Count.ShouldBe(0);

        this._apiService.Transfers.Add(CreateTransfer("After scope change"));

        // Trigger scope change
        await this._scopeService.SetScopeAsync(BudgetScope.Personal);

        // Allow async void handler to complete
        await Task.Delay(50);

        this._sut.Transfers.Count.ShouldBe(1);
    }

    // --- Dispose ---

    /// <summary>
    /// Verifies that Dispose unsubscribes from scope change events.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Dispose_UnsubscribesFromScopeChanged()
    {
        await this._sut.InitializeAsync();
        this._sut.Dispose();

        this._apiService.Transfers.Add(CreateTransfer("Should not load"));
        await this._scopeService.SetScopeAsync(BudgetScope.Personal);
        await Task.Delay(50);

        this._sut.Transfers.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that Dispose clears the chat context.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Dispose_ClearsChatContext()
    {
        await this._sut.InitializeAsync();
        this._chatContext.CurrentContext.PageType.ShouldBe("transfers");

        this._sut.Dispose();

        this._chatContext.CurrentContext.PageType.ShouldBeNull();
    }

    // --- OnStateChanged ---

    /// <summary>
    /// Verifies that OnStateChanged is invoked after state mutations during retry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnStateChanged_IsInvoked_OnRetry()
    {
        int callCount = 0;
        this._sut.OnStateChanged = () => callCount++;

        await this._sut.RetryLoadAsync();

        callCount.ShouldBeGreaterThan(0);
    }

    private static TransferListItemResponse CreateTransfer(
        string description = "Test Transfer",
        DateOnly? date = null,
        Guid? sourceAccountId = null,
        Guid? destinationAccountId = null)
    {
        return new TransferListItemResponse
        {
            TransferId = Guid.NewGuid(),
            Date = date ?? new DateOnly(2026, 3, 1),
            SourceAccountId = sourceAccountId ?? Guid.NewGuid(),
            SourceAccountName = "Source Account",
            DestinationAccountId = destinationAccountId ?? Guid.NewGuid(),
            DestinationAccountName = "Destination Account",
            Amount = 500.00m,
            Description = description,
        };
    }

    /// <summary>
    /// Minimal stub for IJSRuntime to satisfy ScopeService constructor.
    /// </summary>
    private sealed class StubJSRuntime : IJSRuntime
    {
        /// <inheritdoc/>
        /// <returns>A default value.</returns>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => default;

        /// <inheritdoc/>
        /// <returns>A default value.</returns>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => default;
    }
}
