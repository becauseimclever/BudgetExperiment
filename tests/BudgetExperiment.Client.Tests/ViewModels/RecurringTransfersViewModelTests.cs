// <copyright file="RecurringTransfersViewModelTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Client.ViewModels;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Shared.Budgeting;

using Microsoft.JSInterop;

using Shouldly;

namespace BudgetExperiment.Client.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="RecurringTransfersViewModel"/>.
/// </summary>
public sealed class RecurringTransfersViewModelTests : IDisposable
{
    private readonly StubBudgetApiService _apiService = new();
    private readonly StubToastService _toastService = new();
    private readonly ScopeService _scopeService;
    private readonly StubChatContextService _chatContext = new();
    private readonly StubApiErrorContext _apiErrorContext = new();
    private readonly RecurringTransfersViewModel _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransfersViewModelTests"/> class.
    /// </summary>
    public RecurringTransfersViewModelTests()
    {
        _scopeService = new ScopeService(new StubJSRuntime());
        _sut = new RecurringTransfersViewModel(
            _apiService,
            _toastService,
            _scopeService,
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
    /// Verifies that InitializeAsync loads recurring transfers from the API.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsRecurringTransfers()
    {
        _apiService.RecurringTransfers.Add(CreateTransfer("Monthly Savings"));

        await _sut.InitializeAsync();

        _sut.RecurringTransfers.Count.ShouldBe(1);
        _sut.RecurringTransfers[0].Description.ShouldBe("Monthly Savings");
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

        _chatContext.CurrentContext.PageType.ShouldBe("recurring transfers");
    }

    /// <summary>
    /// Verifies that InitializeAsync handles API failure gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsErrorMessage_WhenApiFails()
    {
        _apiService.GetRecurringTransfersException = new HttpRequestException("Server error");

        await _sut.InitializeAsync();

        _sut.ErrorMessage.ShouldNotBeNull();
        _sut.ErrorMessage!.ShouldContain("Failed to load recurring transfers");
        _sut.IsLoading.ShouldBeFalse();
    }

    // --- LoadRecurringTransfersAsync ---

    /// <summary>
    /// Verifies that LoadRecurringTransfersAsync clears the error message before loading.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadRecurringTransfersAsync_ClearsErrorMessage()
    {
        _apiService.GetRecurringTransfersException = new HttpRequestException("fail");
        await _sut.InitializeAsync();
        _sut.ErrorMessage.ShouldNotBeNull();

        _apiService.GetRecurringTransfersException = null;
        await _sut.LoadRecurringTransfersAsync();

        _sut.ErrorMessage.ShouldBeNull();
    }

    // --- RetryLoadAsync ---

    /// <summary>
    /// Verifies that RetryLoadAsync reloads recurring transfers.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RetryLoadAsync_ReloadsRecurringTransfers()
    {
        await _sut.InitializeAsync();
        _apiService.RecurringTransfers.Add(CreateTransfer("New Item"));

        await _sut.RetryLoadAsync();

        _sut.RecurringTransfers.Count.ShouldBe(1);
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
        _apiService.GetRecurringTransfersException = new HttpRequestException("fail");
        await _sut.InitializeAsync();

        _sut.DismissError();

        _sut.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that DismissError notifies state changed.
    /// </summary>
    [Fact]
    public void DismissError_NotifiesStateChanged()
    {
        bool notified = false;
        _sut.OnStateChanged = () => notified = true;

        _sut.DismissError();

        notified.ShouldBeTrue();
    }

    // --- Add Form ---

    /// <summary>
    /// Verifies that OpenAddForm shows the add form and resets the model.
    /// </summary>
    [Fact]
    public void OpenAddForm_ShowsFormAndResetsModel()
    {
        _sut.OpenAddForm();

        _sut.ShowAddForm.ShouldBeTrue();
        _sut.NewRecurring.Frequency.ShouldBe("Monthly");
        _sut.NewRecurring.Amount.ShouldNotBeNull();
        _sut.NewRecurring.Amount!.Currency.ShouldBe("USD");
    }

    /// <summary>
    /// Verifies that OpenAddForm notifies state changed.
    /// </summary>
    [Fact]
    public void OpenAddForm_NotifiesStateChanged()
    {
        bool notified = false;
        _sut.OnStateChanged = () => notified = true;

        _sut.OpenAddForm();

        notified.ShouldBeTrue();
    }

    // --- Edit Form ---

    /// <summary>
    /// Verifies that OpenEditForm populates edit form state.
    /// </summary>
    [Fact]
    public void OpenEditForm_PopulatesEditState()
    {
        var transfer = CreateTransfer("Savings Transfer");
        transfer.Version = "v1";

        _sut.OpenEditForm(transfer);

        _sut.ShowEditForm.ShouldBeTrue();
        _sut.EditingId.ShouldBe(transfer.Id);
        _sut.EditingVersion.ShouldBe("v1");
        _sut.EditModel.Description.ShouldBe("Savings Transfer");
    }

    /// <summary>
    /// Verifies that OpenEditForm copies all relevant fields.
    /// </summary>
    [Fact]
    public void OpenEditForm_CopiesAllFields()
    {
        var transfer = CreateTransfer("Full Copy");
        transfer.Interval = 2;
        transfer.DayOfMonth = 15;
        transfer.DayOfWeek = "Monday";
        transfer.MonthOfYear = 6;
        transfer.EndDate = new DateOnly(2027, 12, 31);

        _sut.OpenEditForm(transfer);

        _sut.EditModel.Frequency.ShouldBe("Monthly");
        _sut.EditModel.Interval.ShouldBe(2);
        _sut.EditModel.DayOfMonth.ShouldBe(15);
        _sut.EditModel.DayOfWeek.ShouldBe("Monday");
        _sut.EditModel.MonthOfYear.ShouldBe(6);
        _sut.EditModel.EndDate.ShouldBe(new DateOnly(2027, 12, 31));
    }

    /// <summary>
    /// Verifies that OpenEditForm notifies state changed.
    /// </summary>
    [Fact]
    public void OpenEditForm_NotifiesStateChanged()
    {
        bool notified = false;
        _sut.OnStateChanged = () => notified = true;

        _sut.OpenEditForm(CreateTransfer("Transfer"));

        notified.ShouldBeTrue();
    }

    // --- HideForm ---

    /// <summary>
    /// Verifies that HideForm hides both add and edit forms.
    /// </summary>
    [Fact]
    public void HideForm_HidesBothForms()
    {
        _sut.OpenAddForm();
        _sut.HideForm();

        _sut.ShowAddForm.ShouldBeFalse();
        _sut.ShowEditForm.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that HideForm notifies state changed.
    /// </summary>
    [Fact]
    public void HideForm_NotifiesStateChanged()
    {
        bool notified = false;
        _sut.OnStateChanged = () => notified = true;

        _sut.HideForm();

        notified.ShouldBeTrue();
    }

    // --- CreateRecurringAsync ---

    /// <summary>
    /// Verifies that CreateRecurringAsync closes form and reloads on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRecurringAsync_ClosesFormAndReloads_WhenSuccessful()
    {
        var created = CreateTransfer("New Transfer");
        _apiService.CreateRecurringTransferResult = created;
        _apiService.RecurringTransfers.Add(created);
        await _sut.InitializeAsync();
        _sut.OpenAddForm();

        await _sut.CreateRecurringAsync(new RecurringTransferCreateDto { Description = "New Transfer" });

        _sut.ShowAddForm.ShouldBeFalse();
        _sut.IsSubmitting.ShouldBeFalse();
        _sut.RecurringTransfers.ShouldContain(r => r.Description == "New Transfer");
    }

    /// <summary>
    /// Verifies that CreateRecurringAsync keeps form open when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRecurringAsync_KeepsFormOpen_WhenApiReturnsNull()
    {
        _apiService.CreateRecurringTransferResult = null;
        await _sut.InitializeAsync();
        _sut.OpenAddForm();

        await _sut.CreateRecurringAsync(new RecurringTransferCreateDto { Description = "Fail" });

        _sut.ShowAddForm.ShouldBeTrue();
        _sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that IsSubmitting is reset after completion.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRecurringAsync_ResetsIsSubmitting_AfterCompletion()
    {
        _apiService.CreateRecurringTransferResult = CreateTransfer("Test");

        await _sut.CreateRecurringAsync(new RecurringTransferCreateDto());

        _sut.IsSubmitting.ShouldBeFalse();
    }

    // --- UpdateRecurringAsync ---

    /// <summary>
    /// Verifies that UpdateRecurringAsync closes form and reloads on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateRecurringAsync_ClosesFormAndReloads_WhenSuccessful()
    {
        var transfer = CreateTransfer("Savings");
        _apiService.RecurringTransfers.Add(transfer);
        await _sut.InitializeAsync();
        _sut.OpenEditForm(transfer);

        var updated = CreateTransfer("Savings Updated");
        _apiService.UpdateRecurringTransferResult = ApiResult<RecurringTransferDto>.Success(updated);

        await _sut.UpdateRecurringAsync(new RecurringTransferUpdateDto { Description = "Savings Updated" });

        _sut.ShowEditForm.ShouldBeFalse();
        _sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that UpdateRecurringAsync handles conflict correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateRecurringAsync_HandlesConflict()
    {
        var transfer = CreateTransfer("Savings");
        _apiService.RecurringTransfers.Add(transfer);
        await _sut.InitializeAsync();
        _sut.OpenEditForm(transfer);

        _apiService.UpdateRecurringTransferResult = ApiResult<RecurringTransferDto>.Conflict();

        await _sut.UpdateRecurringAsync(new RecurringTransferUpdateDto { Description = "Conflict" });

        _toastService.WarningShown.ShouldBeTrue();
        _sut.ShowEditForm.ShouldBeFalse();
        _sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that UpdateRecurringAsync does not close form on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateRecurringAsync_KeepsFormOpen_WhenApiReturnsFailure()
    {
        var transfer = CreateTransfer("Savings");
        _apiService.RecurringTransfers.Add(transfer);
        await _sut.InitializeAsync();
        _sut.OpenEditForm(transfer);

        _apiService.UpdateRecurringTransferResult = ApiResult<RecurringTransferDto>.Failure();

        await _sut.UpdateRecurringAsync(new RecurringTransferUpdateDto { Description = "Fail" });

        _sut.ShowEditForm.ShouldBeTrue();
        _sut.IsSubmitting.ShouldBeFalse();
    }

    // --- SkipNextAsync ---

    /// <summary>
    /// Verifies that SkipNextAsync reloads recurring transfers on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SkipNextAsync_ReloadsRecurringTransfers_WhenSuccessful()
    {
        var transfer = CreateTransfer("Savings");
        _apiService.RecurringTransfers.Add(transfer);
        _apiService.SkipNextRecurringTransferResult = transfer;
        await _sut.InitializeAsync();

        await _sut.SkipNextAsync(transfer);

        _sut.RecurringTransfers.Count.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that SkipNextAsync does not reload when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SkipNextAsync_DoesNotReload_WhenApiReturnsNull()
    {
        var transfer = CreateTransfer("Savings");
        _apiService.SkipNextRecurringTransferResult = null;
        await _sut.InitializeAsync();

        await _sut.SkipNextAsync(transfer);

        _sut.IsLoading.ShouldBeFalse();
    }

    // --- PauseAsync ---

    /// <summary>
    /// Verifies that PauseAsync reloads recurring transfers on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PauseAsync_ReloadsRecurringTransfers_WhenSuccessful()
    {
        var transfer = CreateTransfer("Savings");
        _apiService.RecurringTransfers.Add(transfer);
        _apiService.PauseRecurringTransferTransferResult = transfer;
        await _sut.InitializeAsync();

        await _sut.PauseAsync(transfer);

        _sut.RecurringTransfers.Count.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that PauseAsync does not reload when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PauseAsync_DoesNotReload_WhenApiReturnsNull()
    {
        var transfer = CreateTransfer("Savings");
        _apiService.PauseRecurringTransferTransferResult = null;
        await _sut.InitializeAsync();

        await _sut.PauseAsync(transfer);

        _sut.IsLoading.ShouldBeFalse();
    }

    // --- ResumeAsync ---

    /// <summary>
    /// Verifies that ResumeAsync reloads recurring transfers on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ResumeAsync_ReloadsRecurringTransfers_WhenSuccessful()
    {
        var transfer = CreateTransfer("Savings");
        _apiService.RecurringTransfers.Add(transfer);
        _apiService.ResumeRecurringTransferTransferResult = transfer;
        await _sut.InitializeAsync();

        await _sut.ResumeAsync(transfer);

        _sut.RecurringTransfers.Count.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that ResumeAsync does not reload when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ResumeAsync_DoesNotReload_WhenApiReturnsNull()
    {
        var transfer = CreateTransfer("Savings");
        _apiService.ResumeRecurringTransferTransferResult = null;
        await _sut.InitializeAsync();

        await _sut.ResumeAsync(transfer);

        _sut.IsLoading.ShouldBeFalse();
    }

    // --- Delete ---

    /// <summary>
    /// Verifies that OpenDeleteConfirm shows the delete confirmation dialog.
    /// </summary>
    [Fact]
    public void OpenDeleteConfirm_ShowsDialog()
    {
        var transfer = CreateTransfer("To Delete");

        _sut.OpenDeleteConfirm(transfer);

        _sut.ShowDeleteConfirm.ShouldBeTrue();
        _sut.DeletingTransfer.ShouldBe(transfer);
    }

    /// <summary>
    /// Verifies that OpenDeleteConfirm notifies state changed.
    /// </summary>
    [Fact]
    public void OpenDeleteConfirm_NotifiesStateChanged()
    {
        bool notified = false;
        _sut.OnStateChanged = () => notified = true;

        _sut.OpenDeleteConfirm(CreateTransfer("To Delete"));

        notified.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that ConfirmDeleteAsync deletes and reloads on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfirmDeleteAsync_DeletesAndReloads_WhenSuccessful()
    {
        var transfer = CreateTransfer("To Delete");
        _apiService.RecurringTransfers.Add(transfer);
        _apiService.DeleteRecurringTransferResult = true;
        await _sut.InitializeAsync();
        _sut.OpenDeleteConfirm(transfer);

        await _sut.ConfirmDeleteAsync();

        _sut.ShowDeleteConfirm.ShouldBeFalse();
        _sut.DeletingTransfer.ShouldBeNull();
        _sut.IsDeleting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ConfirmDeleteAsync returns early when DeletingTransfer is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfirmDeleteAsync_ReturnsEarly_WhenDeletingTransferIsNull()
    {
        await _sut.ConfirmDeleteAsync();

        _sut.IsDeleting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ConfirmDeleteAsync keeps dialog open when delete fails.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfirmDeleteAsync_KeepsDialogOpen_WhenDeleteFails()
    {
        var transfer = CreateTransfer("To Delete");
        _apiService.DeleteRecurringTransferResult = false;
        await _sut.InitializeAsync();
        _sut.OpenDeleteConfirm(transfer);

        await _sut.ConfirmDeleteAsync();

        _sut.ShowDeleteConfirm.ShouldBeTrue();
        _sut.IsDeleting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that CancelDelete hides the confirm dialog.
    /// </summary>
    [Fact]
    public void CancelDelete_HidesDialog()
    {
        _sut.OpenDeleteConfirm(CreateTransfer("To Cancel"));

        _sut.CancelDelete();

        _sut.ShowDeleteConfirm.ShouldBeFalse();
        _sut.DeletingTransfer.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that CancelDelete notifies state changed.
    /// </summary>
    [Fact]
    public void CancelDelete_NotifiesStateChanged()
    {
        bool notified = false;
        _sut.OnStateChanged = () => notified = true;

        _sut.CancelDelete();

        notified.ShouldBeTrue();
    }

    // --- Static Helpers ---

    /// <summary>
    /// Verifies that FormatFrequency returns Daily for daily frequency.
    /// </summary>
    [Fact]
    public void FormatFrequency_Daily_ReturnsDaily()
    {
        var transfer = CreateTransfer("Daily");
        transfer.Frequency = "Daily";

        RecurringTransfersViewModel.FormatFrequency(transfer).ShouldBe("Daily");
    }

    /// <summary>
    /// Verifies that FormatFrequency returns Weekly with day of week.
    /// </summary>
    [Fact]
    public void FormatFrequency_Weekly_ReturnsDayOfWeek()
    {
        var transfer = CreateTransfer("Weekly");
        transfer.Frequency = "Weekly";
        transfer.DayOfWeek = "Monday";

        RecurringTransfersViewModel.FormatFrequency(transfer).ShouldBe("Weekly on Monday");
    }

    /// <summary>
    /// Verifies that FormatFrequency returns BiWeekly with day of week.
    /// </summary>
    [Fact]
    public void FormatFrequency_BiWeekly_ReturnsDayOfWeek()
    {
        var transfer = CreateTransfer("BiWeekly");
        transfer.Frequency = "BiWeekly";
        transfer.DayOfWeek = "Friday";

        RecurringTransfersViewModel.FormatFrequency(transfer).ShouldBe("Bi-Weekly on Friday");
    }

    /// <summary>
    /// Verifies that FormatFrequency returns Monthly with day of month.
    /// </summary>
    [Fact]
    public void FormatFrequency_Monthly_WithDayOfMonth()
    {
        var transfer = CreateTransfer("Monthly");
        transfer.Frequency = "Monthly";
        transfer.DayOfMonth = 15;

        RecurringTransfersViewModel.FormatFrequency(transfer).ShouldBe("Monthly on day 15");
    }

    /// <summary>
    /// Verifies that FormatFrequency returns Monthly without day of month.
    /// </summary>
    [Fact]
    public void FormatFrequency_Monthly_WithoutDayOfMonth()
    {
        var transfer = CreateTransfer("Monthly");
        transfer.Frequency = "Monthly";
        transfer.DayOfMonth = null;

        RecurringTransfersViewModel.FormatFrequency(transfer).ShouldBe("Monthly");
    }

    /// <summary>
    /// Verifies that FormatFrequency returns Quarterly with day of month.
    /// </summary>
    [Fact]
    public void FormatFrequency_Quarterly_WithDayOfMonth()
    {
        var transfer = CreateTransfer("Quarterly");
        transfer.Frequency = "Quarterly";
        transfer.DayOfMonth = 1;

        RecurringTransfersViewModel.FormatFrequency(transfer).ShouldBe("Quarterly on day 1");
    }

    /// <summary>
    /// Verifies that FormatFrequency returns Yearly with day of month.
    /// </summary>
    [Fact]
    public void FormatFrequency_Yearly_WithDayOfMonth()
    {
        var transfer = CreateTransfer("Yearly");
        transfer.Frequency = "Yearly";
        transfer.DayOfMonth = 25;

        RecurringTransfersViewModel.FormatFrequency(transfer).ShouldBe("Yearly on day 25");
    }

    /// <summary>
    /// Verifies that FormatFrequency returns the raw frequency for unknown values.
    /// </summary>
    [Fact]
    public void FormatFrequency_Unknown_ReturnsRawFrequency()
    {
        var transfer = CreateTransfer("Custom");
        transfer.Frequency = "EveryOtherDay";

        RecurringTransfersViewModel.FormatFrequency(transfer).ShouldBe("EveryOtherDay");
    }

    /// <summary>
    /// Verifies that FormatMoney formats a money value correctly.
    /// </summary>
    [Fact]
    public void FormatMoney_FormatsCorrectly()
    {
        var money = new MoneyDto { Amount = 1234.56m, Currency = "USD" };

        RecurringTransfersViewModel.FormatMoney(money).ShouldBe("USD 1,234.56");
    }

    // --- Scope Change ---

    /// <summary>
    /// Verifies that scope change reloads recurring transfers.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ScopeChange_ReloadsRecurringTransfers()
    {
        await _sut.InitializeAsync();
        _apiService.RecurringTransfers.Add(CreateTransfer("New After Scope"));

        await _scopeService.SetScopeAsync(BudgetScope.Personal);

        // Allow the async void handler to complete
        await Task.Delay(50);

        _sut.RecurringTransfers.Count.ShouldBe(1);
    }

    // --- Dispose ---

    /// <summary>
    /// Verifies that Dispose unsubscribes from scope changes.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Dispose_UnsubscribesFromScopeChanges()
    {
        await _sut.InitializeAsync();
        _sut.Dispose();

        // Should not reload after dispose
        _apiService.RecurringTransfers.Add(CreateTransfer("After Dispose"));
        await _scopeService.SetScopeAsync(BudgetScope.Shared);
        await Task.Delay(50);

        _sut.RecurringTransfers.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that Dispose clears the chat context.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Dispose_ClearsChatContext()
    {
        await _sut.InitializeAsync();
        _chatContext.CurrentContext.PageType.ShouldBe("recurring transfers");

        _sut.Dispose();

        _chatContext.CurrentContext.PageType.ShouldBeNull();
    }

    // --- OnStateChanged ---

    /// <summary>
    /// Verifies that OnStateChanged callback is invoked after state mutations.
    /// </summary>
    [Fact]
    public void OnStateChanged_IsInvokedAfterStateMutations()
    {
        int callCount = 0;
        _sut.OnStateChanged = () => callCount++;

        _sut.OpenAddForm();
        _sut.HideForm();
        _sut.DismissError();

        callCount.ShouldBe(3);
    }

    // --- Helper Methods ---
    private static RecurringTransferDto CreateTransfer(string description)
    {
        return new RecurringTransferDto
        {
            Id = Guid.NewGuid(),
            Description = description,
            Amount = new MoneyDto { Amount = 500.00m, Currency = "USD" },
            Frequency = "Monthly",
            NextOccurrence = new DateOnly(2026, 6, 1),
            StartDate = new DateOnly(2025, 1, 1),
            IsActive = true,
            SourceAccountId = Guid.NewGuid(),
            SourceAccountName = "Checking",
            DestinationAccountId = Guid.NewGuid(),
            DestinationAccountName = "Savings",
        };
    }

    /// <summary>
    /// Stub toast service for ViewModel testing.
    /// </summary>
    private sealed class StubToastService : IToastService
    {
        /// <inheritdoc/>
        public event Action? OnChange;

        /// <summary>
        /// Gets a value indicating whether a warning was shown.
        /// </summary>
        public bool WarningShown
        {
            get; private set;
        }

        /// <inheritdoc/>
        public IReadOnlyList<ToastItem> Toasts { get; } = [];

        /// <inheritdoc/>
        public void ShowSuccess(string message, string? title = null)
        {
        }

        /// <inheritdoc/>
        public void ShowError(string message, string? title = null)
        {
        }

        /// <inheritdoc/>
        public void ShowInfo(string message, string? title = null)
        {
        }

        /// <inheritdoc/>
        public void ShowWarning(string message, string? title = null)
        {
            this.WarningShown = true;
            this.OnChange?.Invoke();
        }

        /// <inheritdoc/>
        public void Remove(Guid id)
        {
        }
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
