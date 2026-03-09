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
    private readonly RecurringTransfersViewModel _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransfersViewModelTests"/> class.
    /// </summary>
    public RecurringTransfersViewModelTests()
    {
        this._scopeService = new ScopeService(new StubJSRuntime());
        this._sut = new RecurringTransfersViewModel(
            this._apiService,
            this._toastService,
            this._scopeService,
            this._chatContext);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this._sut.Dispose();
    }

    // --- Initialization ---

    /// <summary>
    /// Verifies that InitializeAsync loads recurring transfers from the API.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsRecurringTransfers()
    {
        this._apiService.RecurringTransfers.Add(CreateTransfer("Monthly Savings"));

        await this._sut.InitializeAsync();

        this._sut.RecurringTransfers.Count.ShouldBe(1);
        this._sut.RecurringTransfers[0].Description.ShouldBe("Monthly Savings");
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

        this._chatContext.CurrentContext.PageType.ShouldBe("recurring transfers");
    }

    /// <summary>
    /// Verifies that InitializeAsync handles API failure gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsErrorMessage_WhenApiFails()
    {
        this._apiService.GetRecurringTransfersException = new HttpRequestException("Server error");

        await this._sut.InitializeAsync();

        this._sut.ErrorMessage.ShouldNotBeNull();
        this._sut.ErrorMessage!.ShouldContain("Failed to load recurring transfers");
        this._sut.IsLoading.ShouldBeFalse();
    }

    // --- LoadRecurringTransfersAsync ---

    /// <summary>
    /// Verifies that LoadRecurringTransfersAsync clears the error message before loading.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadRecurringTransfersAsync_ClearsErrorMessage()
    {
        this._apiService.GetRecurringTransfersException = new HttpRequestException("fail");
        await this._sut.InitializeAsync();
        this._sut.ErrorMessage.ShouldNotBeNull();

        this._apiService.GetRecurringTransfersException = null;
        await this._sut.LoadRecurringTransfersAsync();

        this._sut.ErrorMessage.ShouldBeNull();
    }

    // --- RetryLoadAsync ---

    /// <summary>
    /// Verifies that RetryLoadAsync reloads recurring transfers.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RetryLoadAsync_ReloadsRecurringTransfers()
    {
        await this._sut.InitializeAsync();
        this._apiService.RecurringTransfers.Add(CreateTransfer("New Item"));

        await this._sut.RetryLoadAsync();

        this._sut.RecurringTransfers.Count.ShouldBe(1);
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
        this._apiService.GetRecurringTransfersException = new HttpRequestException("fail");
        await this._sut.InitializeAsync();

        this._sut.DismissError();

        this._sut.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that DismissError notifies state changed.
    /// </summary>
    [Fact]
    public void DismissError_NotifiesStateChanged()
    {
        bool notified = false;
        this._sut.OnStateChanged = () => notified = true;

        this._sut.DismissError();

        notified.ShouldBeTrue();
    }

    // --- Add Form ---

    /// <summary>
    /// Verifies that OpenAddForm shows the add form and resets the model.
    /// </summary>
    [Fact]
    public void OpenAddForm_ShowsFormAndResetsModel()
    {
        this._sut.OpenAddForm();

        this._sut.ShowAddForm.ShouldBeTrue();
        this._sut.NewRecurring.Frequency.ShouldBe("Monthly");
        this._sut.NewRecurring.Amount.ShouldNotBeNull();
        this._sut.NewRecurring.Amount!.Currency.ShouldBe("USD");
    }

    /// <summary>
    /// Verifies that OpenAddForm notifies state changed.
    /// </summary>
    [Fact]
    public void OpenAddForm_NotifiesStateChanged()
    {
        bool notified = false;
        this._sut.OnStateChanged = () => notified = true;

        this._sut.OpenAddForm();

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

        this._sut.OpenEditForm(transfer);

        this._sut.ShowEditForm.ShouldBeTrue();
        this._sut.EditingId.ShouldBe(transfer.Id);
        this._sut.EditingVersion.ShouldBe("v1");
        this._sut.EditModel.Description.ShouldBe("Savings Transfer");
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

        this._sut.OpenEditForm(transfer);

        this._sut.EditModel.Frequency.ShouldBe("Monthly");
        this._sut.EditModel.Interval.ShouldBe(2);
        this._sut.EditModel.DayOfMonth.ShouldBe(15);
        this._sut.EditModel.DayOfWeek.ShouldBe("Monday");
        this._sut.EditModel.MonthOfYear.ShouldBe(6);
        this._sut.EditModel.EndDate.ShouldBe(new DateOnly(2027, 12, 31));
    }

    /// <summary>
    /// Verifies that OpenEditForm notifies state changed.
    /// </summary>
    [Fact]
    public void OpenEditForm_NotifiesStateChanged()
    {
        bool notified = false;
        this._sut.OnStateChanged = () => notified = true;

        this._sut.OpenEditForm(CreateTransfer("Transfer"));

        notified.ShouldBeTrue();
    }

    // --- HideForm ---

    /// <summary>
    /// Verifies that HideForm hides both add and edit forms.
    /// </summary>
    [Fact]
    public void HideForm_HidesBothForms()
    {
        this._sut.OpenAddForm();
        this._sut.HideForm();

        this._sut.ShowAddForm.ShouldBeFalse();
        this._sut.ShowEditForm.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that HideForm notifies state changed.
    /// </summary>
    [Fact]
    public void HideForm_NotifiesStateChanged()
    {
        bool notified = false;
        this._sut.OnStateChanged = () => notified = true;

        this._sut.HideForm();

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
        this._apiService.CreateRecurringTransferResult = created;
        this._apiService.RecurringTransfers.Add(created);
        await this._sut.InitializeAsync();
        this._sut.OpenAddForm();

        await this._sut.CreateRecurringAsync(new RecurringTransferCreateDto { Description = "New Transfer" });

        this._sut.ShowAddForm.ShouldBeFalse();
        this._sut.IsSubmitting.ShouldBeFalse();
        this._sut.RecurringTransfers.ShouldContain(r => r.Description == "New Transfer");
    }

    /// <summary>
    /// Verifies that CreateRecurringAsync keeps form open when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRecurringAsync_KeepsFormOpen_WhenApiReturnsNull()
    {
        this._apiService.CreateRecurringTransferResult = null;
        await this._sut.InitializeAsync();
        this._sut.OpenAddForm();

        await this._sut.CreateRecurringAsync(new RecurringTransferCreateDto { Description = "Fail" });

        this._sut.ShowAddForm.ShouldBeTrue();
        this._sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that IsSubmitting is reset after completion.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRecurringAsync_ResetsIsSubmitting_AfterCompletion()
    {
        this._apiService.CreateRecurringTransferResult = CreateTransfer("Test");

        await this._sut.CreateRecurringAsync(new RecurringTransferCreateDto());

        this._sut.IsSubmitting.ShouldBeFalse();
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
        this._apiService.RecurringTransfers.Add(transfer);
        await this._sut.InitializeAsync();
        this._sut.OpenEditForm(transfer);

        var updated = CreateTransfer("Savings Updated");
        this._apiService.UpdateRecurringTransferResult = ApiResult<RecurringTransferDto>.Success(updated);

        await this._sut.UpdateRecurringAsync(new RecurringTransferUpdateDto { Description = "Savings Updated" });

        this._sut.ShowEditForm.ShouldBeFalse();
        this._sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that UpdateRecurringAsync handles conflict correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateRecurringAsync_HandlesConflict()
    {
        var transfer = CreateTransfer("Savings");
        this._apiService.RecurringTransfers.Add(transfer);
        await this._sut.InitializeAsync();
        this._sut.OpenEditForm(transfer);

        this._apiService.UpdateRecurringTransferResult = ApiResult<RecurringTransferDto>.Conflict();

        await this._sut.UpdateRecurringAsync(new RecurringTransferUpdateDto { Description = "Conflict" });

        this._toastService.WarningShown.ShouldBeTrue();
        this._sut.ShowEditForm.ShouldBeFalse();
        this._sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that UpdateRecurringAsync does not close form on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateRecurringAsync_KeepsFormOpen_WhenApiReturnsFailure()
    {
        var transfer = CreateTransfer("Savings");
        this._apiService.RecurringTransfers.Add(transfer);
        await this._sut.InitializeAsync();
        this._sut.OpenEditForm(transfer);

        this._apiService.UpdateRecurringTransferResult = ApiResult<RecurringTransferDto>.Failure();

        await this._sut.UpdateRecurringAsync(new RecurringTransferUpdateDto { Description = "Fail" });

        this._sut.ShowEditForm.ShouldBeTrue();
        this._sut.IsSubmitting.ShouldBeFalse();
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
        this._apiService.RecurringTransfers.Add(transfer);
        this._apiService.SkipNextRecurringTransferResult = transfer;
        await this._sut.InitializeAsync();

        await this._sut.SkipNextAsync(transfer);

        this._sut.RecurringTransfers.Count.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that SkipNextAsync does not reload when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SkipNextAsync_DoesNotReload_WhenApiReturnsNull()
    {
        var transfer = CreateTransfer("Savings");
        this._apiService.SkipNextRecurringTransferResult = null;
        await this._sut.InitializeAsync();

        await this._sut.SkipNextAsync(transfer);

        this._sut.IsLoading.ShouldBeFalse();
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
        this._apiService.RecurringTransfers.Add(transfer);
        this._apiService.PauseRecurringTransferTransferResult = transfer;
        await this._sut.InitializeAsync();

        await this._sut.PauseAsync(transfer);

        this._sut.RecurringTransfers.Count.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that PauseAsync does not reload when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PauseAsync_DoesNotReload_WhenApiReturnsNull()
    {
        var transfer = CreateTransfer("Savings");
        this._apiService.PauseRecurringTransferTransferResult = null;
        await this._sut.InitializeAsync();

        await this._sut.PauseAsync(transfer);

        this._sut.IsLoading.ShouldBeFalse();
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
        this._apiService.RecurringTransfers.Add(transfer);
        this._apiService.ResumeRecurringTransferTransferResult = transfer;
        await this._sut.InitializeAsync();

        await this._sut.ResumeAsync(transfer);

        this._sut.RecurringTransfers.Count.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that ResumeAsync does not reload when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ResumeAsync_DoesNotReload_WhenApiReturnsNull()
    {
        var transfer = CreateTransfer("Savings");
        this._apiService.ResumeRecurringTransferTransferResult = null;
        await this._sut.InitializeAsync();

        await this._sut.ResumeAsync(transfer);

        this._sut.IsLoading.ShouldBeFalse();
    }

    // --- Delete ---

    /// <summary>
    /// Verifies that OpenDeleteConfirm shows the delete confirmation dialog.
    /// </summary>
    [Fact]
    public void OpenDeleteConfirm_ShowsDialog()
    {
        var transfer = CreateTransfer("To Delete");

        this._sut.OpenDeleteConfirm(transfer);

        this._sut.ShowDeleteConfirm.ShouldBeTrue();
        this._sut.DeletingTransfer.ShouldBe(transfer);
    }

    /// <summary>
    /// Verifies that OpenDeleteConfirm notifies state changed.
    /// </summary>
    [Fact]
    public void OpenDeleteConfirm_NotifiesStateChanged()
    {
        bool notified = false;
        this._sut.OnStateChanged = () => notified = true;

        this._sut.OpenDeleteConfirm(CreateTransfer("To Delete"));

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
        this._apiService.RecurringTransfers.Add(transfer);
        this._apiService.DeleteRecurringTransferResult = true;
        await this._sut.InitializeAsync();
        this._sut.OpenDeleteConfirm(transfer);

        await this._sut.ConfirmDeleteAsync();

        this._sut.ShowDeleteConfirm.ShouldBeFalse();
        this._sut.DeletingTransfer.ShouldBeNull();
        this._sut.IsDeleting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ConfirmDeleteAsync returns early when DeletingTransfer is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfirmDeleteAsync_ReturnsEarly_WhenDeletingTransferIsNull()
    {
        await this._sut.ConfirmDeleteAsync();

        this._sut.IsDeleting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ConfirmDeleteAsync keeps dialog open when delete fails.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfirmDeleteAsync_KeepsDialogOpen_WhenDeleteFails()
    {
        var transfer = CreateTransfer("To Delete");
        this._apiService.DeleteRecurringTransferResult = false;
        await this._sut.InitializeAsync();
        this._sut.OpenDeleteConfirm(transfer);

        await this._sut.ConfirmDeleteAsync();

        this._sut.ShowDeleteConfirm.ShouldBeTrue();
        this._sut.IsDeleting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that CancelDelete hides the confirm dialog.
    /// </summary>
    [Fact]
    public void CancelDelete_HidesDialog()
    {
        this._sut.OpenDeleteConfirm(CreateTransfer("To Cancel"));

        this._sut.CancelDelete();

        this._sut.ShowDeleteConfirm.ShouldBeFalse();
        this._sut.DeletingTransfer.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that CancelDelete notifies state changed.
    /// </summary>
    [Fact]
    public void CancelDelete_NotifiesStateChanged()
    {
        bool notified = false;
        this._sut.OnStateChanged = () => notified = true;

        this._sut.CancelDelete();

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
        await this._sut.InitializeAsync();
        this._apiService.RecurringTransfers.Add(CreateTransfer("New After Scope"));

        await this._scopeService.SetScopeAsync(BudgetScope.Personal);

        // Allow the async void handler to complete
        await Task.Delay(50);

        this._sut.RecurringTransfers.Count.ShouldBe(1);
    }

    // --- Dispose ---

    /// <summary>
    /// Verifies that Dispose unsubscribes from scope changes.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Dispose_UnsubscribesFromScopeChanges()
    {
        await this._sut.InitializeAsync();
        this._sut.Dispose();

        // Should not reload after dispose
        this._apiService.RecurringTransfers.Add(CreateTransfer("After Dispose"));
        await this._scopeService.SetScopeAsync(BudgetScope.Shared);
        await Task.Delay(50);

        this._sut.RecurringTransfers.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that Dispose clears the chat context.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Dispose_ClearsChatContext()
    {
        await this._sut.InitializeAsync();
        this._chatContext.CurrentContext.PageType.ShouldBe("recurring transfers");

        this._sut.Dispose();

        this._chatContext.CurrentContext.PageType.ShouldBeNull();
    }

    // --- OnStateChanged ---

    /// <summary>
    /// Verifies that OnStateChanged callback is invoked after state mutations.
    /// </summary>
    [Fact]
    public void OnStateChanged_IsInvokedAfterStateMutations()
    {
        int callCount = 0;
        this._sut.OnStateChanged = () => callCount++;

        this._sut.OpenAddForm();
        this._sut.HideForm();
        this._sut.DismissError();

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
        public bool WarningShown { get; private set; }

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
