// <copyright file="TransfersViewModelTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Client.ViewModels;
using BudgetExperiment.Contracts.Dtos;

using Shouldly;

namespace BudgetExperiment.Client.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="TransfersViewModel"/>.
/// </summary>
public sealed class TransfersViewModelTests : IDisposable
{
    private readonly StubBudgetApiService _apiService = new();
    private readonly StubChatContextService _chatContext = new();
    private readonly StubApiErrorContext _apiErrorContext = new();
    private readonly TransfersViewModel _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransfersViewModelTests"/> class.
    /// </summary>
    public TransfersViewModelTests()
    {
        _sut = new TransfersViewModel(
            _apiService,
            _chatContext,
            _apiErrorContext);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _sut.Dispose();
    }

    // --- Initialization ---

    /// <summary>
    /// Verifies that InitializeAsync loads transfers from the API.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsTransfers()
    {
        _apiService.Transfers.Add(CreateTransfer("Monthly savings"));

        await _sut.InitializeAsync();

        _sut.Transfers.Count.ShouldBe(1);
        _sut.Transfers[0].Description.ShouldBe("Monthly savings");
    }

    /// <summary>
    /// Verifies that InitializeAsync loads accounts from the API.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsAccounts()
    {
        _apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Checking",
        });

        await _sut.InitializeAsync();

        _sut.Accounts.Count.ShouldBe(1);
        _sut.Accounts[0].Name.ShouldBe("Checking");
    }

    /// <summary>
    /// Verifies that InitializeAsync sets IsLoading to false after loading.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsIsLoadingToFalse()
    {
        await _sut.InitializeAsync();

        _sut.IsLoading.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that InitializeAsync sets the page type on the chat context service.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsPageTypeOnChatContext()
    {
        await _sut.InitializeAsync();

        _chatContext.CurrentContext.PageType.ShouldBe("transfers");
    }

    /// <summary>
    /// Verifies that InitializeAsync handles API failure gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsErrorMessage_WhenApiFails()
    {
        _apiService.GetTransfersException = new HttpRequestException("Server error");

        await _sut.InitializeAsync();

        _sut.ErrorMessage.ShouldNotBeNull();
        _sut.ErrorMessage!.ShouldContain("Failed to load transfers");
        _sut.IsLoading.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that FilteredTransfers equals Transfers after init (no filters applied).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsFilteredTransfersToAllTransfers()
    {
        _apiService.Transfers.Add(CreateTransfer("T1"));
        _apiService.Transfers.Add(CreateTransfer("T2"));

        await _sut.InitializeAsync();

        _sut.FilteredTransfers.Count.ShouldBe(2);
    }

    // --- LoadDataAsync ---

    /// <summary>
    /// Verifies that LoadDataAsync clears the error message before loading.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadDataAsync_ClearsErrorMessage()
    {
        _apiService.GetTransfersException = new HttpRequestException("fail");
        await _sut.InitializeAsync();
        _sut.ErrorMessage.ShouldNotBeNull();

        _apiService.GetTransfersException = null;
        await _sut.LoadDataAsync();

        _sut.ErrorMessage.ShouldBeNull();
    }

    // --- RetryLoadAsync ---

    /// <summary>
    /// Verifies that RetryLoadAsync reloads transfers.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RetryLoadAsync_ReloadsTransfers()
    {
        await _sut.InitializeAsync();
        _apiService.Transfers.Add(CreateTransfer("New Transfer"));

        await _sut.RetryLoadAsync();

        _sut.Transfers.Count.ShouldBe(1);
        _sut.IsRetrying.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that RetryLoadAsync notifies state changed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RetryLoadAsync_NotifiesStateChanged()
    {
        int callCount = 0;
        _sut.OnStateChanged = () => callCount++;

        await _sut.RetryLoadAsync();

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
        _apiService.GetTransfersException = new HttpRequestException("fail");
        await _sut.InitializeAsync();
        _sut.ErrorMessage.ShouldNotBeNull();

        _sut.DismissError();

        _sut.ErrorMessage.ShouldBeNull();
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
        _apiService.Transfers.Add(CreateTransfer("Match", sourceAccountId: accountId));
        _apiService.Transfers.Add(CreateTransfer("No match"));

        await _sut.InitializeAsync();
        _sut.SelectedAccountId = accountId.ToString();
        _sut.ApplyFilters();

        _sut.FilteredTransfers.Count.ShouldBe(1);
        _sut.FilteredTransfers[0].Description.ShouldBe("Match");
    }

    /// <summary>
    /// Verifies that ApplyFilters matches on destination account too.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyFilters_FiltersByDestinationAccountId()
    {
        var accountId = Guid.NewGuid();
        _apiService.Transfers.Add(CreateTransfer("Dest match", destinationAccountId: accountId));
        _apiService.Transfers.Add(CreateTransfer("No match"));

        await _sut.InitializeAsync();
        _sut.SelectedAccountId = accountId.ToString();
        _sut.ApplyFilters();

        _sut.FilteredTransfers.Count.ShouldBe(1);
        _sut.FilteredTransfers[0].Description.ShouldBe("Dest match");
    }

    /// <summary>
    /// Verifies that ApplyFilters filters by from date.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyFilters_FiltersByFromDate()
    {
        _apiService.Transfers.Add(CreateTransfer("Before", date: new DateOnly(2026, 1, 1)));
        _apiService.Transfers.Add(CreateTransfer("After", date: new DateOnly(2026, 6, 1)));

        await _sut.InitializeAsync();
        _sut.FromDate = new DateOnly(2026, 3, 1);
        _sut.ApplyFilters();

        _sut.FilteredTransfers.Count.ShouldBe(1);
        _sut.FilteredTransfers[0].Description.ShouldBe("After");
    }

    /// <summary>
    /// Verifies that ApplyFilters filters by to date.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyFilters_FiltersByToDate()
    {
        _apiService.Transfers.Add(CreateTransfer("Before", date: new DateOnly(2026, 1, 1)));
        _apiService.Transfers.Add(CreateTransfer("After", date: new DateOnly(2026, 6, 1)));

        await _sut.InitializeAsync();
        _sut.ToDate = new DateOnly(2026, 3, 1);
        _sut.ApplyFilters();

        _sut.FilteredTransfers.Count.ShouldBe(1);
        _sut.FilteredTransfers[0].Description.ShouldBe("Before");
    }

    /// <summary>
    /// Verifies that ApplyFilters applies date range filter.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyFilters_FiltersByDateRange()
    {
        _apiService.Transfers.Add(CreateTransfer("Jan", date: new DateOnly(2026, 1, 1)));
        _apiService.Transfers.Add(CreateTransfer("Mar", date: new DateOnly(2026, 3, 15)));
        _apiService.Transfers.Add(CreateTransfer("Jun", date: new DateOnly(2026, 6, 1)));

        await _sut.InitializeAsync();
        _sut.FromDate = new DateOnly(2026, 2, 1);
        _sut.ToDate = new DateOnly(2026, 4, 1);
        _sut.ApplyFilters();

        _sut.FilteredTransfers.Count.ShouldBe(1);
        _sut.FilteredTransfers[0].Description.ShouldBe("Mar");
    }

    /// <summary>
    /// Verifies that ClearFilters resets all filter state.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ClearFilters_ResetsAllFilters()
    {
        _apiService.Transfers.Add(CreateTransfer("T1"));
        _apiService.Transfers.Add(CreateTransfer("T2"));

        await _sut.InitializeAsync();
        _sut.SelectedAccountId = Guid.NewGuid().ToString();
        _sut.FromDate = new DateOnly(2026, 1, 1);
        _sut.ToDate = new DateOnly(2026, 12, 31);
        _sut.ApplyFilters();

        _sut.ClearFilters();

        _sut.SelectedAccountId.ShouldBe(string.Empty);
        _sut.FromDate.ShouldBeNull();
        _sut.ToDate.ShouldBeNull();
        _sut.FilteredTransfers.Count.ShouldBe(2);
    }

    // --- Dialog state ---

    /// <summary>
    /// Verifies that ShowCreateTransfer opens the dialog with a fresh model.
    /// </summary>
    [Fact]
    public void ShowCreateTransfer_OpensDialog()
    {
        _sut.ShowCreateTransfer();

        _sut.ShowTransferDialog.ShouldBeTrue();
        _sut.EditingTransfer.ShouldBeNull();
        _sut.NewTransfer.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies that ShowEditTransfer opens the dialog with the transfer data.
    /// </summary>
    [Fact]
    public void ShowEditTransfer_OpensDialogWithTransferData()
    {
        var transfer = CreateTransfer("Edit me");

        _sut.ShowEditTransfer(transfer);

        _sut.ShowTransferDialog.ShouldBeTrue();
        _sut.EditingTransfer.ShouldBe(transfer);
        _sut.EditTransfer.Amount.ShouldBe(transfer.Amount);
        _sut.EditTransfer.Date.ShouldBe(transfer.Date);
        _sut.EditTransfer.Description.ShouldBe(transfer.Description);
    }

    /// <summary>
    /// Verifies that HideTransferDialog closes the dialog.
    /// </summary>
    [Fact]
    public void HideTransferDialog_ClosesDialog()
    {
        _sut.ShowCreateTransfer();
        _sut.ShowTransferDialog.ShouldBeTrue();

        _sut.HideTransferDialog();

        _sut.ShowTransferDialog.ShouldBeFalse();
        _sut.EditingTransfer.ShouldBeNull();
    }

    // --- CRUD ---

    /// <summary>
    /// Verifies that CreateTransferAsync calls the API and reloads data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateTransferAsync_CallsApiAndReloads()
    {
        _apiService.CreateTransferResult = new TransferResponse
        {
            TransferId = Guid.NewGuid(),
            Amount = 100m,
            Date = new DateOnly(2026, 3, 1),
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
        };

        await _sut.InitializeAsync();
        _sut.ShowCreateTransfer();

        await _sut.CreateTransferAsync(new CreateTransferRequest
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100m,
            Date = new DateOnly(2026, 3, 1),
        });

        _sut.ShowTransferDialog.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that CreateTransferAsync sets error message on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateTransferAsync_SetsErrorMessage_OnFailure()
    {
        _apiService.CreateTransferException = new HttpRequestException("Create failed");

        await _sut.CreateTransferAsync(new CreateTransferRequest());

        _sut.ErrorMessage.ShouldNotBeNull();
        _sut.ErrorMessage!.ShouldContain("Failed to create transfer");
    }

    /// <summary>
    /// Verifies that UpdateTransferAsync calls the API and reloads data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateTransferAsync_CallsApiAndReloads()
    {
        var transfer = CreateTransfer("Original");
        _apiService.UpdateTransferResult = new TransferResponse
        {
            TransferId = transfer.TransferId,
            Amount = 200m,
            Date = new DateOnly(2026, 3, 1),
            SourceAccountId = transfer.SourceAccountId,
            DestinationAccountId = transfer.DestinationAccountId,
        };

        await _sut.InitializeAsync();
        _sut.ShowEditTransfer(transfer);

        await _sut.UpdateTransferAsync(new UpdateTransferRequest
        {
            Amount = 200m,
            Date = new DateOnly(2026, 3, 1),
        });

        _sut.ShowTransferDialog.ShouldBeFalse();
        _sut.EditingTransfer.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that UpdateTransferAsync does nothing when EditingTransfer is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateTransferAsync_DoesNothing_WhenNoEditingTransfer()
    {
        await _sut.InitializeAsync();

        await _sut.UpdateTransferAsync(new UpdateTransferRequest());

        _sut.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that UpdateTransferAsync sets error message on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateTransferAsync_SetsErrorMessage_OnFailure()
    {
        var transfer = CreateTransfer("Fail");
        _sut.ShowEditTransfer(transfer);
        _apiService.UpdateTransferException = new HttpRequestException("Update failed");

        await _sut.UpdateTransferAsync(new UpdateTransferRequest());

        _sut.ErrorMessage.ShouldNotBeNull();
        _sut.ErrorMessage!.ShouldContain("Failed to update transfer");
    }

    /// <summary>
    /// Verifies that DeleteTransferAsync calls the API and reloads data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteTransferAsync_CallsApiAndReloads()
    {
        var transfer = CreateTransfer("Delete me");
        _apiService.Transfers.Add(transfer);
        _apiService.DeleteTransferResult = true;

        await _sut.InitializeAsync();
        _sut.Transfers.Count.ShouldBe(1);

        // After delete, the stub will return the current contents of Transfers
        _apiService.Transfers.Clear();
        await _sut.DeleteTransferAsync(transfer.TransferId);

        _sut.Transfers.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that DeleteTransferAsync sets error message on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteTransferAsync_SetsErrorMessage_OnFailure()
    {
        _apiService.DeleteTransferException = new HttpRequestException("Delete failed");

        await _sut.DeleteTransferAsync(Guid.NewGuid());

        _sut.ErrorMessage.ShouldNotBeNull();
        _sut.ErrorMessage!.ShouldContain("Failed to delete transfer");
    }

    /// <summary>
    /// Verifies that Dispose clears the chat context.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Dispose_ClearsChatContext()
    {
        await _sut.InitializeAsync();
        _chatContext.CurrentContext.PageType.ShouldBe("transfers");

        _sut.Dispose();

        _chatContext.CurrentContext.PageType.ShouldBeNull();
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
        _sut.OnStateChanged = () => callCount++;

        await _sut.RetryLoadAsync();

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
}
