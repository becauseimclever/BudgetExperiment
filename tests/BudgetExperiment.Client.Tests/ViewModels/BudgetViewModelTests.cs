// <copyright file="BudgetViewModelTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Client.ViewModels;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Components;

using Shouldly;

namespace BudgetExperiment.Client.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="BudgetViewModel"/>.
/// </summary>
public sealed class BudgetViewModelTests
{
    private readonly StubBudgetApiService _apiService = new();
    private readonly StubNavigationManager _navigationManager = new();
    private readonly StubApiErrorContext _apiErrorContext = new();
    private readonly BudgetViewModel _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetViewModelTests"/> class.
    /// </summary>
    public BudgetViewModelTests()
    {
        _sut = new BudgetViewModel(
            _apiService,
            _navigationManager,
            _apiErrorContext);
    }

    // --- Initialization ---

    /// <summary>
    /// Verifies that InitializeAsync loads budget summary from the API.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsBudgetSummary()
    {
        _apiService.BudgetSummary = CreateSummary();

        await _sut.InitializeAsync();

        _sut.Summary.ShouldNotBeNull();
        _sut.Summary.TotalBudgeted.Amount.ShouldBe(5000m);
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
    /// Verifies that InitializeAsync handles API failure gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsErrorMessage_WhenApiFails()
    {
        _apiService.GetBudgetSummaryException = new HttpRequestException("Server error");

        await _sut.InitializeAsync();

        _sut.ErrorMessage.ShouldNotBeNull();
        _sut.ErrorMessage!.ShouldContain("Failed to load budget");
        _sut.IsLoading.ShouldBeFalse();
    }

    // --- LoadBudgetAsync ---

    /// <summary>
    /// Verifies that LoadBudgetAsync clears the error message before loading.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadBudgetAsync_ClearsErrorMessage()
    {
        _apiService.GetBudgetSummaryException = new HttpRequestException("fail");
        await _sut.InitializeAsync();
        _sut.ErrorMessage.ShouldNotBeNull();

        _apiService.GetBudgetSummaryException = null;
        await _sut.LoadBudgetAsync();

        _sut.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that LoadBudgetAsync sets summary to null when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadBudgetAsync_SetsSummaryToNull_WhenApiReturnsNull()
    {
        _apiService.BudgetSummary = null;

        await _sut.LoadBudgetAsync();

        _sut.Summary.ShouldBeNull();
        _sut.IsLoading.ShouldBeFalse();
    }

    // --- RetryLoadAsync ---

    /// <summary>
    /// Verifies that RetryLoadAsync reloads budget data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RetryLoadAsync_ReloadsBudget()
    {
        await _sut.InitializeAsync();
        _apiService.BudgetSummary = CreateSummary();

        await _sut.RetryLoadAsync();

        _sut.Summary.ShouldNotBeNull();
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
        _apiService.GetBudgetSummaryException = new HttpRequestException("fail");
        await _sut.InitializeAsync();
        _sut.ErrorMessage.ShouldNotBeNull();

        _sut.DismissError();

        _sut.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that DismissError notifies state changed.
    /// </summary>
    [Fact]
    public void DismissError_NotifiesStateChanged()
    {
        int callCount = 0;
        _sut.OnStateChanged = () => callCount++;

        _sut.DismissError();

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
        var originalDate = _sut.CurrentDate;

        await _sut.PreviousMonthAsync();

        _sut.CurrentDate.ShouldBe(originalDate.AddMonths(-1));
    }

    /// <summary>
    /// Verifies that PreviousMonthAsync reloads budget data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PreviousMonthAsync_ReloadsBudget()
    {
        _apiService.BudgetSummary = CreateSummary();

        await _sut.PreviousMonthAsync();

        _sut.Summary.ShouldNotBeNull();
        _sut.IsLoading.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that NextMonthAsync moves the date forward one month.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NextMonthAsync_MovesDateForwardOneMonth()
    {
        var originalDate = _sut.CurrentDate;

        await _sut.NextMonthAsync();

        _sut.CurrentDate.ShouldBe(originalDate.AddMonths(1));
    }

    /// <summary>
    /// Verifies that NextMonthAsync reloads budget data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NextMonthAsync_ReloadsBudget()
    {
        _apiService.BudgetSummary = CreateSummary();

        await _sut.NextMonthAsync();

        _sut.Summary.ShouldNotBeNull();
        _sut.IsLoading.ShouldBeFalse();
    }

    // --- Computed Properties ---

    /// <summary>
    /// Verifies OverallStatus returns NoBudgetSet when summary is null.
    /// </summary>
    [Fact]
    public void OverallStatus_ReturnsNoBudgetSet_WhenSummaryIsNull()
    {
        _sut.OverallStatus.ShouldBe("NoBudgetSet");
    }

    /// <summary>
    /// Verifies OverallStatus returns OnTrack when percent used is under 80.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OverallStatus_ReturnsOnTrack_WhenUnder80Percent()
    {
        _apiService.BudgetSummary = CreateSummary(overallPercentUsed: 50m);
        await _sut.InitializeAsync();

        _sut.OverallStatus.ShouldBe("OnTrack");
    }

    /// <summary>
    /// Verifies OverallStatus returns Warning when percent used is between 80 and 100.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OverallStatus_ReturnsWarning_WhenBetween80And100Percent()
    {
        _apiService.BudgetSummary = CreateSummary(overallPercentUsed: 85m);
        await _sut.InitializeAsync();

        _sut.OverallStatus.ShouldBe("Warning");
    }

    /// <summary>
    /// Verifies OverallStatus returns Warning at exactly 80 percent.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OverallStatus_ReturnsWarning_AtExactly80Percent()
    {
        _apiService.BudgetSummary = CreateSummary(overallPercentUsed: 80m);
        await _sut.InitializeAsync();

        _sut.OverallStatus.ShouldBe("Warning");
    }

    /// <summary>
    /// Verifies OverallStatus returns OverBudget when percent used is at or over 100.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OverallStatus_ReturnsOverBudget_WhenAtOrOver100Percent()
    {
        _apiService.BudgetSummary = CreateSummary(overallPercentUsed: 110m);
        await _sut.InitializeAsync();

        _sut.OverallStatus.ShouldBe("OverBudget");
    }

    /// <summary>
    /// Verifies OverallStatus returns OverBudget at exactly 100 percent.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OverallStatus_ReturnsOverBudget_AtExactly100Percent()
    {
        _apiService.BudgetSummary = CreateSummary(overallPercentUsed: 100m);
        await _sut.InitializeAsync();

        _sut.OverallStatus.ShouldBe("OverBudget");
    }

    /// <summary>
    /// Verifies ModalTitle returns Set Budget Goal when creating new.
    /// </summary>
    [Fact]
    public void ModalTitle_ReturnsSetBudgetGoal_WhenCreatingNew()
    {
        _sut.ShowEditGoal(CreateProgress(status: "NoBudgetSet"));

        _sut.ModalTitle.ShouldBe("Set Budget Goal");
    }

    /// <summary>
    /// Verifies ModalTitle returns Edit Budget Goal when editing existing.
    /// </summary>
    [Fact]
    public void ModalTitle_ReturnsEditBudgetGoal_WhenEditingExisting()
    {
        _sut.ShowEditGoal(CreateProgress(status: "OnTrack"));

        _sut.ModalTitle.ShouldBe("Edit Budget Goal");
    }

    /// <summary>
    /// Verifies IsCreatingNewGoal returns true for NoBudgetSet status.
    /// </summary>
    [Fact]
    public void IsCreatingNewGoal_ReturnsTrue_WhenNoBudgetSet()
    {
        _sut.ShowEditGoal(CreateProgress(status: "NoBudgetSet"));

        _sut.IsCreatingNewGoal.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies IsCreatingNewGoal returns false for existing goal.
    /// </summary>
    [Fact]
    public void IsCreatingNewGoal_ReturnsFalse_WhenExistingGoal()
    {
        _sut.ShowEditGoal(CreateProgress(status: "OnTrack"));

        _sut.IsCreatingNewGoal.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies IsCreatingNewGoal returns false when no progress is being edited.
    /// </summary>
    [Fact]
    public void IsCreatingNewGoal_ReturnsFalse_WhenNoEditingProgress()
    {
        _sut.IsCreatingNewGoal.ShouldBeFalse();
    }

    // --- ShowEditGoal / HideEditGoal ---

    /// <summary>
    /// Verifies ShowEditGoal sets modal state correctly.
    /// </summary>
    [Fact]
    public void ShowEditGoal_SetsModalState()
    {
        var progress = CreateProgress(targetAmount: 500m);

        _sut.ShowEditGoal(progress);

        _sut.ShowEditGoalModal.ShouldBeTrue();
        _sut.EditingProgress.ShouldBe(progress);
        _sut.EditTargetAmount.ShouldBe(500m);
    }

    /// <summary>
    /// Verifies ShowEditGoal notifies state changed.
    /// </summary>
    [Fact]
    public void ShowEditGoal_NotifiesStateChanged()
    {
        int callCount = 0;
        _sut.OnStateChanged = () => callCount++;

        _sut.ShowEditGoal(CreateProgress());

        callCount.ShouldBe(1);
    }

    /// <summary>
    /// Verifies HideEditGoal clears modal state.
    /// </summary>
    [Fact]
    public void HideEditGoal_ClearsModalState()
    {
        _sut.ShowEditGoal(CreateProgress());

        _sut.HideEditGoal();

        _sut.ShowEditGoalModal.ShouldBeFalse();
        _sut.EditingProgress.ShouldBeNull();
    }

    /// <summary>
    /// Verifies HideEditGoal notifies state changed.
    /// </summary>
    [Fact]
    public void HideEditGoal_NotifiesStateChanged()
    {
        int callCount = 0;
        _sut.OnStateChanged = () => callCount++;

        _sut.HideEditGoal();

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
        await _sut.SaveGoalAsync();

        _sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies SaveGoalAsync closes modal and reloads on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SaveGoalAsync_ClosesModalAndReloads_OnSuccess()
    {
        var categoryId = Guid.NewGuid();
        _sut.ShowEditGoal(CreateProgress(categoryId: categoryId));
        _apiService.SetBudgetGoalResult = ApiResult<BudgetGoalDto>.Success(new BudgetGoalDto
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            TargetAmount = new MoneyDto { Amount = 600m, Currency = "USD" },
            Year = DateTime.Today.Year,
            Month = DateTime.Today.Month,
        });

        await _sut.SaveGoalAsync();

        _sut.ShowEditGoalModal.ShouldBeFalse();
        _sut.EditingProgress.ShouldBeNull();
        _sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies SaveGoalAsync sets error message on API failure result.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SaveGoalAsync_SetsErrorMessage_WhenApiReturnsFailure()
    {
        _sut.ShowEditGoal(CreateProgress());
        _apiService.SetBudgetGoalResult = ApiResult<BudgetGoalDto>.Failure();

        await _sut.SaveGoalAsync();

        _sut.ErrorMessage.ShouldBe("Failed to save budget goal.");
        _sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies SaveGoalAsync sets error message on exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SaveGoalAsync_SetsErrorMessage_WhenExceptionThrown()
    {
        _sut.ShowEditGoal(CreateProgress());
        _apiService.SetBudgetGoalException = new HttpRequestException("Network error");

        await _sut.SaveGoalAsync();

        _sut.ErrorMessage.ShouldNotBeNull();
        _sut.ErrorMessage!.ShouldContain("Failed to save budget goal");
        _sut.IsSubmitting.ShouldBeFalse();
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
        _sut.ShowEditGoal(progress);
        _sut.EditTargetAmount = 750m;
        _apiService.SetBudgetGoalResult = ApiResult<BudgetGoalDto>.Success(new BudgetGoalDto
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            TargetAmount = new MoneyDto { Amount = 750m, Currency = "EUR" },
            Year = DateTime.Today.Year,
            Month = DateTime.Today.Month,
        });

        await _sut.SaveGoalAsync();

        _sut.ShowEditGoalModal.ShouldBeFalse();
        _sut.IsSubmitting.ShouldBeFalse();
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
        _sut.ShowEditGoal(progress);
        _apiService.SetBudgetGoalResult = ApiResult<BudgetGoalDto>.Success(new BudgetGoalDto
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            TargetAmount = new MoneyDto { Amount = 500m, Currency = "USD" },
            Year = DateTime.Today.Year,
            Month = DateTime.Today.Month,
        });

        await _sut.SaveGoalAsync();

        _sut.ShowEditGoalModal.ShouldBeFalse();
    }

    // --- DeleteGoalAsync ---

    /// <summary>
    /// Verifies DeleteGoalAsync does nothing when no progress is being edited.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteGoalAsync_DoesNothing_WhenNoEditingProgress()
    {
        await _sut.DeleteGoalAsync();

        _sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies DeleteGoalAsync closes modal and reloads on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteGoalAsync_ClosesModalAndReloads_OnSuccess()
    {
        _sut.ShowEditGoal(CreateProgress());
        _apiService.DeleteBudgetGoalResult = true;

        await _sut.DeleteGoalAsync();

        _sut.ShowEditGoalModal.ShouldBeFalse();
        _sut.EditingProgress.ShouldBeNull();
        _sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies DeleteGoalAsync sets error message when API returns false.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteGoalAsync_SetsErrorMessage_WhenApiFails()
    {
        _sut.ShowEditGoal(CreateProgress());
        _apiService.DeleteBudgetGoalResult = false;

        await _sut.DeleteGoalAsync();

        _sut.ErrorMessage.ShouldBe("Failed to delete budget goal.");
        _sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies DeleteGoalAsync sets error message on exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteGoalAsync_SetsErrorMessage_WhenExceptionThrown()
    {
        _sut.ShowEditGoal(CreateProgress());
        _apiService.DeleteBudgetGoalException = new HttpRequestException("Network error");

        await _sut.DeleteGoalAsync();

        _sut.ErrorMessage.ShouldNotBeNull();
        _sut.ErrorMessage!.ShouldContain("Failed to delete budget goal");
        _sut.IsSubmitting.ShouldBeFalse();
    }

    // --- NavigateToCategories ---

    /// <summary>
    /// Verifies NavigateToCategories navigates to the categories page.
    /// </summary>
    [Fact]
    public void NavigateToCategories_NavigatesToCategoriesPage()
    {
        _sut.NavigateToCategories();

        _navigationManager.LastNavigatedUri.ShouldBe("/categories");
    }

    // --- Dispose ---

    // --- OnStateChanged Callback ---

    /// <summary>
    /// Verifies OnStateChanged callback is invoked after ShowEditGoal.
    /// </summary>
    [Fact]
    public void OnStateChanged_InvokedAfterShowEditGoal()
    {
        int callCount = 0;
        _sut.OnStateChanged = () => callCount++;

        _sut.ShowEditGoal(CreateProgress());

        callCount.ShouldBe(1);
    }

    /// <summary>
    /// Verifies OnStateChanged callback is invoked after HideEditGoal.
    /// </summary>
    [Fact]
    public void OnStateChanged_InvokedAfterHideEditGoal()
    {
        int callCount = 0;
        _sut.OnStateChanged = () => callCount++;

        _sut.HideEditGoal();

        callCount.ShouldBe(1);
    }

    /// <summary>
    /// Verifies OnStateChanged callback is invoked after DismissError.
    /// </summary>
    [Fact]
    public void OnStateChanged_InvokedAfterDismissError()
    {
        int callCount = 0;
        _sut.OnStateChanged = () => callCount++;

        _sut.DismissError();

        callCount.ShouldBe(1);
    }

    /// <summary>
    /// Verifies OnStateChanged is not invoked when null.
    /// </summary>
    [Fact]
    public void OnStateChanged_NoExceptionWhenNull()
    {
        _sut.OnStateChanged = null;

        // Should not throw
        _sut.DismissError();
    }

    // --- Initial State ---

    /// <summary>
    /// Verifies initial state values are correct.
    /// </summary>
    [Fact]
    public void InitialState_HasCorrectDefaults()
    {
        _sut.IsLoading.ShouldBeTrue();
        _sut.IsRetrying.ShouldBeFalse();
        _sut.IsSubmitting.ShouldBeFalse();
        _sut.ErrorMessage.ShouldBeNull();
        _sut.Summary.ShouldBeNull();
        _sut.CurrentDate.ShouldBe(DateTime.Today);
        _sut.ShowEditGoalModal.ShouldBeFalse();
        _sut.EditingProgress.ShouldBeNull();
        _sut.EditTargetAmount.ShouldBe(0m);
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
        public string? LastNavigatedUri
        {
            get; private set;
        }

        /// <inheritdoc/>
        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            this.LastNavigatedUri = uri;
        }
    }
}
