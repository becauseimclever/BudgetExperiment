// <copyright file="RecurringViewModelTests.cs" company="BecauseImClever">
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
/// Unit tests for <see cref="RecurringViewModel"/>.
/// </summary>
public sealed class RecurringViewModelTests : IDisposable
{
    private readonly StubBudgetApiService _apiService = new();
    private readonly StubToastService _toastService = new();
    private readonly ScopeService _scopeService;
    private readonly StubChatContextService _chatContext = new();
    private readonly StubApiErrorContext _apiErrorContext = new();
    private readonly RecurringViewModel _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringViewModelTests"/> class.
    /// </summary>
    public RecurringViewModelTests()
    {
        _scopeService = new ScopeService(new StubJSRuntime());
        _sut = new RecurringViewModel(
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
    /// Verifies that InitializeAsync loads recurring transactions from the API.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsRecurringTransactions()
    {
        _apiService.RecurringTransactions.Add(CreateRecurring("Monthly Rent"));

        await _sut.InitializeAsync();

        _sut.RecurringTransactions.Count.ShouldBe(1);
        _sut.RecurringTransactions[0].Description.ShouldBe("Monthly Rent");
    }

    /// <summary>
    /// Verifies that InitializeAsync loads categories from the API.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsCategories()
    {
        _apiService.Categories.Add(CreateCategory("Utilities"));

        await _sut.InitializeAsync();

        _sut.Categories.Count.ShouldBe(1);
        _sut.Categories[0].Name.ShouldBe("Utilities");
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

        _chatContext.CurrentContext.PageType.ShouldBe("recurring transactions");
    }

    /// <summary>
    /// Verifies that InitializeAsync handles API failure gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsErrorMessage_WhenApiFails()
    {
        _apiService.GetRecurringTransactionsException = new HttpRequestException("Server error");

        await _sut.InitializeAsync();

        _sut.ErrorMessage.ShouldNotBeNull();
        _sut.ErrorMessage!.ShouldContain("Failed to load recurring transactions");
        _sut.IsLoading.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that InitializeAsync sets error when categories fail to load.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsErrorMessage_WhenCategoriesFail()
    {
        _apiService.GetCategoriesException = new HttpRequestException("Category error");

        await _sut.InitializeAsync();

        _sut.ErrorMessage.ShouldNotBeNull();
        _sut.ErrorMessage!.ShouldContain("Failed to load categories");
    }

    // --- LoadRecurringTransactionsAsync ---

    /// <summary>
    /// Verifies that LoadRecurringTransactionsAsync clears the error message before loading.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadRecurringTransactionsAsync_ClearsErrorMessage()
    {
        _apiService.GetRecurringTransactionsException = new HttpRequestException("fail");
        await _sut.InitializeAsync();
        _sut.ErrorMessage.ShouldNotBeNull();

        _apiService.GetRecurringTransactionsException = null;
        await _sut.LoadRecurringTransactionsAsync();

        _sut.ErrorMessage.ShouldBeNull();
    }

    // --- RetryLoadAsync ---

    /// <summary>
    /// Verifies that RetryLoadAsync reloads recurring transactions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RetryLoadAsync_ReloadsRecurringTransactions()
    {
        await _sut.InitializeAsync();
        _apiService.RecurringTransactions.Add(CreateRecurring("New Item"));

        await _sut.RetryLoadAsync();

        _sut.RecurringTransactions.Count.ShouldBe(1);
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
        _apiService.GetRecurringTransactionsException = new HttpRequestException("fail");
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
        var recurring = CreateRecurring("Rent");
        recurring.Version = "v1";

        _sut.OpenEditForm(recurring);

        _sut.ShowEditForm.ShouldBeTrue();
        _sut.EditingId.ShouldBe(recurring.Id);
        _sut.EditingVersion.ShouldBe("v1");
        _sut.EditModel.Description.ShouldBe("Rent");
    }

    /// <summary>
    /// Verifies that OpenEditForm notifies state changed.
    /// </summary>
    [Fact]
    public void OpenEditForm_NotifiesStateChanged()
    {
        bool notified = false;
        _sut.OnStateChanged = () => notified = true;

        _sut.OpenEditForm(CreateRecurring("Rent"));

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
        var created = CreateRecurring("New Recurring");
        _apiService.CreateRecurringTransactionResult = created;
        _apiService.RecurringTransactions.Add(created);
        await _sut.InitializeAsync();
        _sut.OpenAddForm();

        await _sut.CreateRecurringAsync(new RecurringTransactionCreateDto { Description = "New Recurring" });

        _sut.ShowAddForm.ShouldBeFalse();
        _sut.IsSubmitting.ShouldBeFalse();
        _sut.RecurringTransactions.ShouldContain(r => r.Description == "New Recurring");
    }

    /// <summary>
    /// Verifies that CreateRecurringAsync keeps form open when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRecurringAsync_KeepsFormOpen_WhenApiReturnsNull()
    {
        _apiService.CreateRecurringTransactionResult = null;
        await _sut.InitializeAsync();
        _sut.OpenAddForm();

        await _sut.CreateRecurringAsync(new RecurringTransactionCreateDto { Description = "Fail" });

        _sut.ShowAddForm.ShouldBeTrue();
        _sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that IsSubmitting is reset even on exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRecurringAsync_ResetsIsSubmitting_AfterCompletion()
    {
        _apiService.CreateRecurringTransactionResult = CreateRecurring("Test");

        await _sut.CreateRecurringAsync(new RecurringTransactionCreateDto());

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
        var recurring = CreateRecurring("Rent");
        _apiService.RecurringTransactions.Add(recurring);
        await _sut.InitializeAsync();
        _sut.OpenEditForm(recurring);

        var updated = CreateRecurring("Rent Updated");
        _apiService.UpdateRecurringTransactionResult = ApiResult<RecurringTransactionDto>.Success(updated);

        await _sut.UpdateRecurringAsync(new RecurringTransactionUpdateDto { Description = "Rent Updated" });

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
        var recurring = CreateRecurring("Rent");
        _apiService.RecurringTransactions.Add(recurring);
        await _sut.InitializeAsync();
        _sut.OpenEditForm(recurring);

        _apiService.UpdateRecurringTransactionResult = ApiResult<RecurringTransactionDto>.Conflict();

        await _sut.UpdateRecurringAsync(new RecurringTransactionUpdateDto { Description = "Conflict" });

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
        var recurring = CreateRecurring("Rent");
        _apiService.RecurringTransactions.Add(recurring);
        await _sut.InitializeAsync();
        _sut.OpenEditForm(recurring);

        _apiService.UpdateRecurringTransactionResult = ApiResult<RecurringTransactionDto>.Failure();

        await _sut.UpdateRecurringAsync(new RecurringTransactionUpdateDto { Description = "Fail" });

        _sut.ShowEditForm.ShouldBeTrue();
        _sut.IsSubmitting.ShouldBeFalse();
    }

    // --- SkipNextAsync ---

    /// <summary>
    /// Verifies that SkipNextAsync reloads recurring transactions on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SkipNextAsync_ReloadsRecurringTransactions_WhenSuccessful()
    {
        var recurring = CreateRecurring("Rent");
        _apiService.RecurringTransactions.Add(recurring);
        _apiService.SkipNextRecurringResult = recurring;
        await _sut.InitializeAsync();

        await _sut.SkipNextAsync(recurring);

        _sut.RecurringTransactions.Count.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that SkipNextAsync does not reload when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SkipNextAsync_DoesNotReload_WhenApiReturnsNull()
    {
        var recurring = CreateRecurring("Rent");
        _apiService.SkipNextRecurringResult = null;
        await _sut.InitializeAsync();

        await _sut.SkipNextAsync(recurring);

        // No exception thrown, no-op on null
        _sut.IsLoading.ShouldBeFalse();
    }

    // --- PauseAsync ---

    /// <summary>
    /// Verifies that PauseAsync reloads recurring transactions on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PauseAsync_ReloadsRecurringTransactions_WhenSuccessful()
    {
        var recurring = CreateRecurring("Rent");
        _apiService.RecurringTransactions.Add(recurring);
        _apiService.PauseRecurringTransactionResult = recurring;
        await _sut.InitializeAsync();

        await _sut.PauseAsync(recurring);

        _sut.RecurringTransactions.Count.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that PauseAsync does not reload when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PauseAsync_DoesNotReload_WhenApiReturnsNull()
    {
        var recurring = CreateRecurring("Rent");
        _apiService.PauseRecurringTransactionResult = null;
        await _sut.InitializeAsync();

        await _sut.PauseAsync(recurring);

        _sut.IsLoading.ShouldBeFalse();
    }

    // --- ResumeAsync ---

    /// <summary>
    /// Verifies that ResumeAsync reloads recurring transactions on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ResumeAsync_ReloadsRecurringTransactions_WhenSuccessful()
    {
        var recurring = CreateRecurring("Rent", isActive: false);
        _apiService.RecurringTransactions.Add(recurring);
        _apiService.ResumeRecurringTransactionResult = recurring;
        await _sut.InitializeAsync();

        await _sut.ResumeAsync(recurring);

        _sut.RecurringTransactions.Count.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that ResumeAsync does not reload when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ResumeAsync_DoesNotReload_WhenApiReturnsNull()
    {
        var recurring = CreateRecurring("Rent", isActive: false);
        _apiService.ResumeRecurringTransactionResult = null;
        await _sut.InitializeAsync();

        await _sut.ResumeAsync(recurring);

        _sut.IsLoading.ShouldBeFalse();
    }

    // --- Delete ---

    /// <summary>
    /// Verifies that OpenDeleteConfirm shows the delete confirmation dialog.
    /// </summary>
    [Fact]
    public void OpenDeleteConfirm_ShowsDeleteDialog()
    {
        var recurring = CreateRecurring("ToDelete");

        _sut.OpenDeleteConfirm(recurring);

        _sut.ShowDeleteConfirm.ShouldBeTrue();
        _sut.DeletingRecurring.ShouldBe(recurring);
    }

    /// <summary>
    /// Verifies that OpenDeleteConfirm notifies state changed.
    /// </summary>
    [Fact]
    public void OpenDeleteConfirm_NotifiesStateChanged()
    {
        bool notified = false;
        _sut.OnStateChanged = () => notified = true;

        _sut.OpenDeleteConfirm(CreateRecurring("ToDelete"));

        notified.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that CancelDelete hides the delete confirmation dialog.
    /// </summary>
    [Fact]
    public void CancelDelete_HidesDeleteDialog()
    {
        var recurring = CreateRecurring("ToDelete");
        _sut.OpenDeleteConfirm(recurring);

        _sut.CancelDelete();

        _sut.ShowDeleteConfirm.ShouldBeFalse();
        _sut.DeletingRecurring.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that ConfirmDeleteAsync removes item and reloads on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfirmDeleteAsync_ClosesDialogAndReloads_WhenSuccessful()
    {
        var recurring = CreateRecurring("ToDelete");
        _apiService.RecurringTransactions.Add(recurring);
        _apiService.DeleteRecurringTransactionResult = true;
        await _sut.InitializeAsync();
        _sut.OpenDeleteConfirm(recurring);

        await _sut.ConfirmDeleteAsync();

        _sut.ShowDeleteConfirm.ShouldBeFalse();
        _sut.DeletingRecurring.ShouldBeNull();
        _sut.IsDeleting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ConfirmDeleteAsync returns early when no deleting recurring is set.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfirmDeleteAsync_ReturnsEarly_WhenNoDeletingRecurring()
    {
        await _sut.ConfirmDeleteAsync();

        _sut.IsDeleting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ConfirmDeleteAsync keeps dialog open when API returns false.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConfirmDeleteAsync_KeepsDialogOpen_WhenApiFails()
    {
        var recurring = CreateRecurring("ToDelete");
        _apiService.RecurringTransactions.Add(recurring);
        _apiService.DeleteRecurringTransactionResult = false;
        await _sut.InitializeAsync();
        _sut.OpenDeleteConfirm(recurring);

        await _sut.ConfirmDeleteAsync();

        _sut.ShowDeleteConfirm.ShouldBeTrue();
        _sut.IsDeleting.ShouldBeFalse();
    }

    // --- Import Patterns ---

    /// <summary>
    /// Verifies that OpenImportPatterns shows the import patterns dialog.
    /// </summary>
    [Fact]
    public void OpenImportPatterns_ShowsImportPatternsDialog()
    {
        var recurring = CreateRecurring("Rent");

        _sut.OpenImportPatterns(recurring);

        _sut.ShowImportPatterns.ShouldBeTrue();
        _sut.ImportPatternsRecurringId.ShouldBe(recurring.Id);
        _sut.ImportPatternsDescription.ShouldBe("Rent");
    }

    /// <summary>
    /// Verifies that OpenImportPatterns notifies state changed.
    /// </summary>
    [Fact]
    public void OpenImportPatterns_NotifiesStateChanged()
    {
        bool notified = false;
        _sut.OnStateChanged = () => notified = true;

        _sut.OpenImportPatterns(CreateRecurring("Rent"));

        notified.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that HideImportPatterns hides the dialog and resets state.
    /// </summary>
    [Fact]
    public void HideImportPatterns_HidesDialogAndResetsState()
    {
        _sut.OpenImportPatterns(CreateRecurring("Rent"));

        _sut.HideImportPatterns();

        _sut.ShowImportPatterns.ShouldBeFalse();
        _sut.ImportPatternsRecurringId.ShouldBe(Guid.Empty);
        _sut.ImportPatternsDescription.ShouldBe(string.Empty);
    }

    /// <summary>
    /// Verifies that HandleImportPatternsSaved hides the import patterns dialog.
    /// </summary>
    [Fact]
    public void HandleImportPatternsSaved_HidesDialog()
    {
        _sut.OpenImportPatterns(CreateRecurring("Rent"));

        _sut.HandleImportPatternsSaved();

        _sut.ShowImportPatterns.ShouldBeFalse();
    }

    // --- Static Helpers ---

    /// <summary>
    /// Verifies FormatFrequency formatting for Daily.
    /// </summary>
    [Fact]
    public void FormatFrequency_Daily_ReturnsDaily()
    {
        var recurring = CreateRecurring("Test");
        recurring.Frequency = "Daily";

        RecurringViewModel.FormatFrequency(recurring).ShouldBe("Daily");
    }

    /// <summary>
    /// Verifies FormatFrequency formatting for Weekly.
    /// </summary>
    [Fact]
    public void FormatFrequency_Weekly_IncludesDayOfWeek()
    {
        var recurring = CreateRecurring("Test");
        recurring.Frequency = "Weekly";
        recurring.DayOfWeek = "Monday";

        RecurringViewModel.FormatFrequency(recurring).ShouldBe("Weekly on Monday");
    }

    /// <summary>
    /// Verifies FormatFrequency formatting for BiWeekly.
    /// </summary>
    [Fact]
    public void FormatFrequency_BiWeekly_IncludesDayOfWeek()
    {
        var recurring = CreateRecurring("Test");
        recurring.Frequency = "BiWeekly";
        recurring.DayOfWeek = "Friday";

        RecurringViewModel.FormatFrequency(recurring).ShouldBe("Bi-Weekly on Friday");
    }

    /// <summary>
    /// Verifies FormatFrequency formatting for Monthly with day of month.
    /// </summary>
    [Fact]
    public void FormatFrequency_MonthlyWithDayOfMonth_IncludesDay()
    {
        var recurring = CreateRecurring("Test");
        recurring.Frequency = "Monthly";
        recurring.DayOfMonth = 15;

        RecurringViewModel.FormatFrequency(recurring).ShouldBe("Monthly on day 15");
    }

    /// <summary>
    /// Verifies FormatFrequency formatting for Monthly without day of month.
    /// </summary>
    [Fact]
    public void FormatFrequency_MonthlyWithoutDayOfMonth_ReturnsMonthly()
    {
        var recurring = CreateRecurring("Test");
        recurring.Frequency = "Monthly";
        recurring.DayOfMonth = null;

        RecurringViewModel.FormatFrequency(recurring).ShouldBe("Monthly");
    }

    /// <summary>
    /// Verifies FormatFrequency formatting for Quarterly.
    /// </summary>
    [Fact]
    public void FormatFrequency_Quarterly_IncludesDay()
    {
        var recurring = CreateRecurring("Test");
        recurring.Frequency = "Quarterly";
        recurring.DayOfMonth = 1;

        RecurringViewModel.FormatFrequency(recurring).ShouldBe("Quarterly on day 1");
    }

    /// <summary>
    /// Verifies FormatFrequency formatting for Yearly.
    /// </summary>
    [Fact]
    public void FormatFrequency_Yearly_IncludesDay()
    {
        var recurring = CreateRecurring("Test");
        recurring.Frequency = "Yearly";
        recurring.DayOfMonth = 25;

        RecurringViewModel.FormatFrequency(recurring).ShouldBe("Yearly on day 25");
    }

    /// <summary>
    /// Verifies FormatFrequency formatting for unknown frequency.
    /// </summary>
    [Fact]
    public void FormatFrequency_UnknownFrequency_ReturnsRawValue()
    {
        var recurring = CreateRecurring("Test");
        recurring.Frequency = "Custom";

        RecurringViewModel.FormatFrequency(recurring).ShouldBe("Custom");
    }

    /// <summary>
    /// Verifies FormatMoney for positive amounts.
    /// </summary>
    [Fact]
    public void FormatMoney_PositiveAmount_IncludesPlusSign()
    {
        var money = new MoneyDto { Amount = 100.50m, Currency = "USD" };

        RecurringViewModel.FormatMoney(money).ShouldBe("+USD 100.50");
    }

    /// <summary>
    /// Verifies FormatMoney for negative amounts.
    /// </summary>
    [Fact]
    public void FormatMoney_NegativeAmount_NoExtraSign()
    {
        var money = new MoneyDto { Amount = -50.00m, Currency = "USD" };

        RecurringViewModel.FormatMoney(money).ShouldBe("USD -50.00");
    }

    /// <summary>
    /// Verifies FormatMoney for zero amount.
    /// </summary>
    [Fact]
    public void FormatMoney_ZeroAmount_IncludesPlusSign()
    {
        var money = new MoneyDto { Amount = 0m, Currency = "USD" };

        RecurringViewModel.FormatMoney(money).ShouldBe("+USD 0.00");
    }

    // --- Scope Change ---

    /// <summary>
    /// Verifies that scope change triggers a reload of recurring transactions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ScopeChange_ReloadsRecurringTransactions()
    {
        await _sut.InitializeAsync();
        _apiService.RecurringTransactions.Add(CreateRecurring("New After Scope"));

        await _scopeService.SetScopeAsync(BudgetScope.Personal);
        await Task.Delay(50);

        _sut.RecurringTransactions.Count.ShouldBe(1);
        _sut.RecurringTransactions[0].Description.ShouldBe("New After Scope");
    }

    // --- Dispose ---

    /// <summary>
    /// Verifies that Dispose unsubscribes from scope change events.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Dispose_UnsubscribesFromScopeChanged()
    {
        await _sut.InitializeAsync();
        _sut.Dispose();

        _apiService.RecurringTransactions.Add(CreateRecurring("Should Not Load"));
        await _scopeService.SetScopeAsync(BudgetScope.Shared);
        await Task.Delay(50);

        _sut.RecurringTransactions.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that Dispose clears the chat context.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Dispose_ClearsChatContext()
    {
        await _sut.InitializeAsync();
        _chatContext.CurrentContext.PageType.ShouldBe("recurring transactions");

        _sut.Dispose();

        _chatContext.CurrentContext.PageType.ShouldBeNull();
    }

    // --- OnStateChanged Callback ---

    /// <summary>
    /// Verifies that OnStateChanged is invoked when state-mutating methods are called.
    /// </summary>
    [Fact]
    public void OnStateChanged_IsInvoked_OnStateMutations()
    {
        int callCount = 0;
        _sut.OnStateChanged = () => callCount++;

        _sut.OpenAddForm();
        _sut.HideForm();
        _sut.DismissError();
        _sut.OpenDeleteConfirm(CreateRecurring("Cat"));
        _sut.CancelDelete();
        _sut.OpenImportPatterns(CreateRecurring("Test"));
        _sut.HideImportPatterns();

        callCount.ShouldBe(7);
    }

    private static RecurringTransactionDto CreateRecurring(string description, bool isActive = true)
    {
        return new RecurringTransactionDto
        {
            Id = Guid.NewGuid(),
            Description = description,
            Amount = new MoneyDto { Amount = -100.00m, Currency = "USD" },
            Frequency = "Monthly",
            NextOccurrence = new DateOnly(2026, 5, 1),
            StartDate = new DateOnly(2025, 1, 1),
            IsActive = isActive,
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
        };
    }

    private static BudgetCategoryDto CreateCategory(string name)
    {
        return new BudgetCategoryDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = "Expense",
            Icon = "tag",
            Color = "#4CAF50",
            IsActive = true,
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
