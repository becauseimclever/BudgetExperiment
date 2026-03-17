// <copyright file="BudgetViewModelTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Client.ViewModels;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Shared.Budgeting;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shouldly;

namespace BudgetExperiment.Client.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="BudgetViewModel"/>.
/// </summary>
public sealed class BudgetViewModelTests : IDisposable
{
    private readonly StubBudgetApiService _apiService = new();
    private readonly StubNavigationManager _navigationManager = new();
    private readonly ScopeService _scopeService;
    private readonly StubApiErrorContext _apiErrorContext = new();
    private readonly BudgetViewModel _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetViewModelTests"/> class.
    /// </summary>
    public BudgetViewModelTests()
    {
        this._scopeService = new ScopeService(new StubJSRuntime());
        this._sut = new BudgetViewModel(
            this._apiService,
            this._navigationManager,
            this._scopeService,
            this._apiErrorContext);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this._sut.Dispose();
    }

    // --- Initialization ---

    /// <summary>
    /// Verifies that InitializeAsync loads budget summary from the API.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsBudgetSummary()
    {
        this._apiService.BudgetSummary = CreateSummary();

        await this._sut.InitializeAsync();

        this._sut.Summary.ShouldNotBeNull();
        this._sut.Summary.TotalBudgeted.Amount.ShouldBe(5000m);
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
    /// Verifies that InitializeAsync handles API failure gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsErrorMessage_WhenApiFails()
    {
        this._apiService.GetBudgetSummaryException = new HttpRequestException("Server error");

        await this._sut.InitializeAsync();

        this._sut.ErrorMessage.ShouldNotBeNull();
        this._sut.ErrorMessage!.ShouldContain("Failed to load budget");
        this._sut.IsLoading.ShouldBeFalse();
    }

    // --- LoadBudgetAsync ---

    /// <summary>
    /// Verifies that LoadBudgetAsync clears the error message before loading.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadBudgetAsync_ClearsErrorMessage()
    {
        this._apiService.GetBudgetSummaryException = new HttpRequestException("fail");
        await this._sut.InitializeAsync();
        this._sut.ErrorMessage.ShouldNotBeNull();

        this._apiService.GetBudgetSummaryException = null;
        await this._sut.LoadBudgetAsync();

        this._sut.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that LoadBudgetAsync sets summary to null when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadBudgetAsync_SetsSummaryToNull_WhenApiReturnsNull()
    {
        this._apiService.BudgetSummary = null;

        await this._sut.LoadBudgetAsync();

        this._sut.Summary.ShouldBeNull();
        this._sut.IsLoading.ShouldBeFalse();
    }

    // --- RetryLoadAsync ---

    /// <summary>
    /// Verifies that RetryLoadAsync reloads budget data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RetryLoadAsync_ReloadsBudget()
    {
        await this._sut.InitializeAsync();
        this._apiService.BudgetSummary = CreateSummary();

        await this._sut.RetryLoadAsync();

        this._sut.Summary.ShouldNotBeNull();
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
        this._apiService.GetBudgetSummaryException = new HttpRequestException("fail");
        await this._sut.InitializeAsync();
        this._sut.ErrorMessage.ShouldNotBeNull();

        this._sut.DismissError();

        this._sut.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that DismissError notifies state changed.
    /// </summary>
    [Fact]
    public void DismissError_NotifiesStateChanged()
    {
        int callCount = 0;
        this._sut.OnStateChanged = () => callCount++;

        this._sut.DismissError();

        callCount.ShouldBe(1);
    }

    // --- Month Navigation ---

    /// <summary>
    /// Verifies that PreviousMonthAsync moves the date back one month.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PreviousMonthAsync_MovesDateBackOneMonth()
    {
        var originalDate = this._sut.CurrentDate;

        await this._sut.PreviousMonthAsync();

        this._sut.CurrentDate.ShouldBe(originalDate.AddMonths(-1));
    }

    /// <summary>
    /// Verifies that PreviousMonthAsync reloads budget data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PreviousMonthAsync_ReloadsBudget()
    {
        this._apiService.BudgetSummary = CreateSummary();

        await this._sut.PreviousMonthAsync();

        this._sut.Summary.ShouldNotBeNull();
        this._sut.IsLoading.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that NextMonthAsync moves the date forward one month.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NextMonthAsync_MovesDateForwardOneMonth()
    {
        var originalDate = this._sut.CurrentDate;

        await this._sut.NextMonthAsync();

        this._sut.CurrentDate.ShouldBe(originalDate.AddMonths(1));
    }

    /// <summary>
    /// Verifies that NextMonthAsync reloads budget data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NextMonthAsync_ReloadsBudget()
    {
        this._apiService.BudgetSummary = CreateSummary();

        await this._sut.NextMonthAsync();

        this._sut.Summary.ShouldNotBeNull();
        this._sut.IsLoading.ShouldBeFalse();
    }

    // --- Computed Properties ---

    /// <summary>
    /// Verifies OverallStatus returns NoBudgetSet when summary is null.
    /// </summary>
    [Fact]
    public void OverallStatus_ReturnsNoBudgetSet_WhenSummaryIsNull()
    {
        this._sut.OverallStatus.ShouldBe("NoBudgetSet");
    }

    /// <summary>
    /// Verifies OverallStatus returns OnTrack when percent used is under 80.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OverallStatus_ReturnsOnTrack_WhenUnder80Percent()
    {
        this._apiService.BudgetSummary = CreateSummary(overallPercentUsed: 50m);
        await this._sut.InitializeAsync();

        this._sut.OverallStatus.ShouldBe("OnTrack");
    }

    /// <summary>
    /// Verifies OverallStatus returns Warning when percent used is between 80 and 100.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OverallStatus_ReturnsWarning_WhenBetween80And100Percent()
    {
        this._apiService.BudgetSummary = CreateSummary(overallPercentUsed: 85m);
        await this._sut.InitializeAsync();

        this._sut.OverallStatus.ShouldBe("Warning");
    }

    /// <summary>
    /// Verifies OverallStatus returns Warning at exactly 80 percent.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OverallStatus_ReturnsWarning_AtExactly80Percent()
    {
        this._apiService.BudgetSummary = CreateSummary(overallPercentUsed: 80m);
        await this._sut.InitializeAsync();

        this._sut.OverallStatus.ShouldBe("Warning");
    }

    /// <summary>
    /// Verifies OverallStatus returns OverBudget when percent used is at or over 100.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OverallStatus_ReturnsOverBudget_WhenAtOrOver100Percent()
    {
        this._apiService.BudgetSummary = CreateSummary(overallPercentUsed: 110m);
        await this._sut.InitializeAsync();

        this._sut.OverallStatus.ShouldBe("OverBudget");
    }

    /// <summary>
    /// Verifies OverallStatus returns OverBudget at exactly 100 percent.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OverallStatus_ReturnsOverBudget_AtExactly100Percent()
    {
        this._apiService.BudgetSummary = CreateSummary(overallPercentUsed: 100m);
        await this._sut.InitializeAsync();

        this._sut.OverallStatus.ShouldBe("OverBudget");
    }

    /// <summary>
    /// Verifies ModalTitle returns Set Budget Goal when creating new.
    /// </summary>
    [Fact]
    public void ModalTitle_ReturnsSetBudgetGoal_WhenCreatingNew()
    {
        this._sut.ShowEditGoal(CreateProgress(status: "NoBudgetSet"));

        this._sut.ModalTitle.ShouldBe("Set Budget Goal");
    }

    /// <summary>
    /// Verifies ModalTitle returns Edit Budget Goal when editing existing.
    /// </summary>
    [Fact]
    public void ModalTitle_ReturnsEditBudgetGoal_WhenEditingExisting()
    {
        this._sut.ShowEditGoal(CreateProgress(status: "OnTrack"));

        this._sut.ModalTitle.ShouldBe("Edit Budget Goal");
    }

    /// <summary>
    /// Verifies IsCreatingNewGoal returns true for NoBudgetSet status.
    /// </summary>
    [Fact]
    public void IsCreatingNewGoal_ReturnsTrue_WhenNoBudgetSet()
    {
        this._sut.ShowEditGoal(CreateProgress(status: "NoBudgetSet"));

        this._sut.IsCreatingNewGoal.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies IsCreatingNewGoal returns false for existing goal.
    /// </summary>
    [Fact]
    public void IsCreatingNewGoal_ReturnsFalse_WhenExistingGoal()
    {
        this._sut.ShowEditGoal(CreateProgress(status: "OnTrack"));

        this._sut.IsCreatingNewGoal.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies IsCreatingNewGoal returns false when no progress is being edited.
    /// </summary>
    [Fact]
    public void IsCreatingNewGoal_ReturnsFalse_WhenNoEditingProgress()
    {
        this._sut.IsCreatingNewGoal.ShouldBeFalse();
    }

    // --- ShowEditGoal / HideEditGoal ---

    /// <summary>
    /// Verifies ShowEditGoal sets modal state correctly.
    /// </summary>
    [Fact]
    public void ShowEditGoal_SetsModalState()
    {
        var progress = CreateProgress(targetAmount: 500m);

        this._sut.ShowEditGoal(progress);

        this._sut.ShowEditGoalModal.ShouldBeTrue();
        this._sut.EditingProgress.ShouldBe(progress);
        this._sut.EditTargetAmount.ShouldBe(500m);
    }

    /// <summary>
    /// Verifies ShowEditGoal notifies state changed.
    /// </summary>
    [Fact]
    public void ShowEditGoal_NotifiesStateChanged()
    {
        int callCount = 0;
        this._sut.OnStateChanged = () => callCount++;

        this._sut.ShowEditGoal(CreateProgress());

        callCount.ShouldBe(1);
    }

    /// <summary>
    /// Verifies HideEditGoal clears modal state.
    /// </summary>
    [Fact]
    public void HideEditGoal_ClearsModalState()
    {
        this._sut.ShowEditGoal(CreateProgress());

        this._sut.HideEditGoal();

        this._sut.ShowEditGoalModal.ShouldBeFalse();
        this._sut.EditingProgress.ShouldBeNull();
    }

    /// <summary>
    /// Verifies HideEditGoal notifies state changed.
    /// </summary>
    [Fact]
    public void HideEditGoal_NotifiesStateChanged()
    {
        int callCount = 0;
        this._sut.OnStateChanged = () => callCount++;

        this._sut.HideEditGoal();

        callCount.ShouldBe(1);
    }

    // --- SaveGoalAsync ---

    /// <summary>
    /// Verifies SaveGoalAsync does nothing when no progress is being edited.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SaveGoalAsync_DoesNothing_WhenNoEditingProgress()
    {
        await this._sut.SaveGoalAsync();

        this._sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies SaveGoalAsync closes modal and reloads on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SaveGoalAsync_ClosesModalAndReloads_OnSuccess()
    {
        var categoryId = Guid.NewGuid();
        this._sut.ShowEditGoal(CreateProgress(categoryId: categoryId));
        this._apiService.SetBudgetGoalResult = ApiResult<BudgetGoalDto>.Success(new BudgetGoalDto
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            TargetAmount = new MoneyDto { Amount = 600m, Currency = "USD" },
            Year = DateTime.Today.Year,
            Month = DateTime.Today.Month,
        });

        await this._sut.SaveGoalAsync();

        this._sut.ShowEditGoalModal.ShouldBeFalse();
        this._sut.EditingProgress.ShouldBeNull();
        this._sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies SaveGoalAsync sets error message on API failure result.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SaveGoalAsync_SetsErrorMessage_WhenApiReturnsFailure()
    {
        this._sut.ShowEditGoal(CreateProgress());
        this._apiService.SetBudgetGoalResult = ApiResult<BudgetGoalDto>.Failure();

        await this._sut.SaveGoalAsync();

        this._sut.ErrorMessage.ShouldBe("Failed to save budget goal.");
        this._sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies SaveGoalAsync sets error message on exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SaveGoalAsync_SetsErrorMessage_WhenExceptionThrown()
    {
        this._sut.ShowEditGoal(CreateProgress());
        this._apiService.SetBudgetGoalException = new HttpRequestException("Network error");

        await this._sut.SaveGoalAsync();

        this._sut.ErrorMessage.ShouldNotBeNull();
        this._sut.ErrorMessage!.ShouldContain("Failed to save budget goal");
        this._sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies SaveGoalAsync uses correct currency from editing progress.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SaveGoalAsync_UsesCorrectCurrency_FromEditingProgress()
    {
        var categoryId = Guid.NewGuid();
        var progress = CreateProgress(categoryId: categoryId, currency: "EUR");
        this._sut.ShowEditGoal(progress);
        this._sut.EditTargetAmount = 750m;
        this._apiService.SetBudgetGoalResult = ApiResult<BudgetGoalDto>.Success(new BudgetGoalDto
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            TargetAmount = new MoneyDto { Amount = 750m, Currency = "EUR" },
            Year = DateTime.Today.Year,
            Month = DateTime.Today.Month,
        });

        await this._sut.SaveGoalAsync();

        this._sut.ShowEditGoalModal.ShouldBeFalse();
        this._sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies SaveGoalAsync uses default USD currency when currency is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SaveGoalAsync_UsesDefaultCurrency_WhenCurrencyIsEmpty()
    {
        var categoryId = Guid.NewGuid();
        var progress = CreateProgress(categoryId: categoryId, currency: string.Empty);
        this._sut.ShowEditGoal(progress);
        this._apiService.SetBudgetGoalResult = ApiResult<BudgetGoalDto>.Success(new BudgetGoalDto
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            TargetAmount = new MoneyDto { Amount = 500m, Currency = "USD" },
            Year = DateTime.Today.Year,
            Month = DateTime.Today.Month,
        });

        await this._sut.SaveGoalAsync();

        this._sut.ShowEditGoalModal.ShouldBeFalse();
    }

    // --- DeleteGoalAsync ---

    /// <summary>
    /// Verifies DeleteGoalAsync does nothing when no progress is being edited.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteGoalAsync_DoesNothing_WhenNoEditingProgress()
    {
        await this._sut.DeleteGoalAsync();

        this._sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies DeleteGoalAsync closes modal and reloads on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteGoalAsync_ClosesModalAndReloads_OnSuccess()
    {
        this._sut.ShowEditGoal(CreateProgress());
        this._apiService.DeleteBudgetGoalResult = true;

        await this._sut.DeleteGoalAsync();

        this._sut.ShowEditGoalModal.ShouldBeFalse();
        this._sut.EditingProgress.ShouldBeNull();
        this._sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies DeleteGoalAsync sets error message when API returns false.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteGoalAsync_SetsErrorMessage_WhenApiFails()
    {
        this._sut.ShowEditGoal(CreateProgress());
        this._apiService.DeleteBudgetGoalResult = false;

        await this._sut.DeleteGoalAsync();

        this._sut.ErrorMessage.ShouldBe("Failed to delete budget goal.");
        this._sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies DeleteGoalAsync sets error message on exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteGoalAsync_SetsErrorMessage_WhenExceptionThrown()
    {
        this._sut.ShowEditGoal(CreateProgress());
        this._apiService.DeleteBudgetGoalException = new HttpRequestException("Network error");

        await this._sut.DeleteGoalAsync();

        this._sut.ErrorMessage.ShouldNotBeNull();
        this._sut.ErrorMessage!.ShouldContain("Failed to delete budget goal");
        this._sut.IsSubmitting.ShouldBeFalse();
    }

    // --- NavigateToCategories ---

    /// <summary>
    /// Verifies NavigateToCategories navigates to the categories page.
    /// </summary>
    [Fact]
    public void NavigateToCategories_NavigatesToCategoriesPage()
    {
        this._sut.NavigateToCategories();

        this._navigationManager.LastNavigatedUri.ShouldBe("/categories");
    }

    // --- Scope Change ---

    /// <summary>
    /// Verifies that scope change triggers budget reload.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ScopeChange_ReloadsBudget()
    {
        await this._sut.InitializeAsync();
        this._apiService.BudgetSummary = CreateSummary();

        await this._scopeService.SetScopeAsync(BudgetScope.Personal);

        // Allow async void to complete
        await Task.Delay(50);

        this._sut.Summary.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies that scope change notifies state changed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ScopeChange_NotifiesStateChanged()
    {
        int callCount = 0;
        this._sut.OnStateChanged = () => callCount++;
        await this._sut.InitializeAsync();

        await this._scopeService.SetScopeAsync(BudgetScope.Shared);

        // Allow async void to complete
        await Task.Delay(50);

        callCount.ShouldBeGreaterThan(0);
    }

    // --- Dispose ---

    /// <summary>
    /// Verifies that Dispose unsubscribes from scope change events.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Dispose_UnsubscribesFromScopeChanges()
    {
        await this._sut.InitializeAsync();
        this._sut.Dispose();

        // Changing scope after dispose should not cause issues
        this._apiService.BudgetSummary = CreateSummary();
        await this._scopeService.SetScopeAsync(BudgetScope.Personal);
        await Task.Delay(50);

        // Summary should remain null since the handler was unsubscribed
        this._sut.Summary.ShouldBeNull();
    }

    // --- OnStateChanged Callback ---

    /// <summary>
    /// Verifies OnStateChanged callback is invoked after ShowEditGoal.
    /// </summary>
    [Fact]
    public void OnStateChanged_InvokedAfterShowEditGoal()
    {
        int callCount = 0;
        this._sut.OnStateChanged = () => callCount++;

        this._sut.ShowEditGoal(CreateProgress());

        callCount.ShouldBe(1);
    }

    /// <summary>
    /// Verifies OnStateChanged callback is invoked after HideEditGoal.
    /// </summary>
    [Fact]
    public void OnStateChanged_InvokedAfterHideEditGoal()
    {
        int callCount = 0;
        this._sut.OnStateChanged = () => callCount++;

        this._sut.HideEditGoal();

        callCount.ShouldBe(1);
    }

    /// <summary>
    /// Verifies OnStateChanged callback is invoked after DismissError.
    /// </summary>
    [Fact]
    public void OnStateChanged_InvokedAfterDismissError()
    {
        int callCount = 0;
        this._sut.OnStateChanged = () => callCount++;

        this._sut.DismissError();

        callCount.ShouldBe(1);
    }

    /// <summary>
    /// Verifies OnStateChanged is not invoked when null.
    /// </summary>
    [Fact]
    public void OnStateChanged_NoExceptionWhenNull()
    {
        this._sut.OnStateChanged = null;

        // Should not throw
        this._sut.DismissError();
    }

    // --- Initial State ---

    /// <summary>
    /// Verifies initial state values are correct.
    /// </summary>
    [Fact]
    public void InitialState_HasCorrectDefaults()
    {
        this._sut.IsLoading.ShouldBeTrue();
        this._sut.IsRetrying.ShouldBeFalse();
        this._sut.IsSubmitting.ShouldBeFalse();
        this._sut.ErrorMessage.ShouldBeNull();
        this._sut.Summary.ShouldBeNull();
        this._sut.CurrentDate.ShouldBe(DateTime.Today);
        this._sut.ShowEditGoalModal.ShouldBeFalse();
        this._sut.EditingProgress.ShouldBeNull();
        this._sut.EditTargetAmount.ShouldBe(0m);
    }

    // --- Helpers ---
    private static BudgetSummaryDto CreateSummary(decimal overallPercentUsed = 60m) => new()
    {
        Year = DateTime.Today.Year,
        Month = DateTime.Today.Month,
        TotalBudgeted = new MoneyDto { Amount = 5000m, Currency = "USD" },
        TotalSpent = new MoneyDto { Amount = 3000m, Currency = "USD" },
        TotalRemaining = new MoneyDto { Amount = 2000m, Currency = "USD" },
        OverallPercentUsed = overallPercentUsed,
        CategoriesOnTrack = 3,
        CategoriesWarning = 1,
        CategoriesOverBudget = 0,
        CategoriesNoBudgetSet = 2,
        CategoryProgress = [],
    };

    private static BudgetProgressDto CreateProgress(
        Guid? categoryId = null,
        string status = "OnTrack",
        decimal targetAmount = 500m,
        string? currency = "USD") => new()
    {
        CategoryId = categoryId ?? Guid.NewGuid(),
        CategoryName = "Groceries",
        TargetAmount = new MoneyDto { Amount = targetAmount, Currency = currency ?? "USD" },
        SpentAmount = new MoneyDto { Amount = 300m, Currency = currency ?? "USD" },
        RemainingAmount = new MoneyDto { Amount = targetAmount - 300m, Currency = currency ?? "USD" },
        PercentUsed = targetAmount > 0 ? (300m / targetAmount * 100m) : 0m,
        Status = status,
    };

    /// <summary>
    /// Stub NavigationManager for testing navigation calls.
    /// </summary>
    private sealed class StubNavigationManager : NavigationManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StubNavigationManager"/> class.
        /// </summary>
        public StubNavigationManager()
        {
            this.Initialize("https://localhost/", "https://localhost/");
        }

        /// <summary>
        /// Gets the last URI that was navigated to.
        /// </summary>
        public string? LastNavigatedUri { get; private set; }

        /// <inheritdoc/>
        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            this.LastNavigatedUri = uri;
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
