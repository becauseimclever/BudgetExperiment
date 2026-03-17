// <copyright file="TransactionsViewModelTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Client.ViewModels;
using BudgetExperiment.Contracts.Dtos;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shouldly;

namespace BudgetExperiment.Client.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="TransactionsViewModel"/>.
/// </summary>
public sealed class TransactionsViewModelTests : IDisposable
{
    private readonly StubBudgetApiService _apiService = new();
    private readonly StubToastService _toastService = new();
    private readonly StubNavigationManager _navigationManager = new();
    private readonly ScopeService _scopeService;
    private readonly StubApiErrorContext _apiErrorContext = new();
    private readonly TransactionsViewModel _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionsViewModelTests"/> class.
    /// </summary>
    public TransactionsViewModelTests()
    {
        this._scopeService = new ScopeService(new StubJSRuntime());
        this._sut = new TransactionsViewModel(
            this._apiService,
            this._toastService,
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
    /// Verifies that InitializeAsync loads transactions from the API.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsTransactions()
    {
        this._apiService.UnifiedPage = CreatePageWithItems(3);

        await this._sut.InitializeAsync();

        this._sut.PageData.Items.Count.ShouldBe(3);
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
    /// Verifies that InitializeAsync loads accounts for the filter dropdown.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsAccounts()
    {
        this._apiService.Accounts.Add(new AccountDto { Id = Guid.NewGuid(), Name = "Checking" });

        await this._sut.InitializeAsync();

        this._sut.Accounts.Count.ShouldBe(1);
        this._sut.Accounts[0].Name.ShouldBe("Checking");
    }

    /// <summary>
    /// Verifies that InitializeAsync loads categories for the filter dropdown.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsCategories()
    {
        this._apiService.Categories.Add(new BudgetCategoryDto { Id = Guid.NewGuid(), Name = "Groceries" });

        await this._sut.InitializeAsync();

        this._sut.Categories.Count.ShouldBe(1);
        this._sut.Categories[0].Name.ShouldBe("Groceries");
    }

    /// <summary>
    /// Verifies that InitializeAsync handles API failure gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsErrorMessage_WhenApiFails()
    {
        this._apiService.GetAccountsException = new HttpRequestException("Server error");

        await this._sut.InitializeAsync();

        this._sut.ErrorMessage.ShouldNotBeNull();
        this._sut.IsLoading.ShouldBeFalse();
    }

    // --- Filter State ---

    /// <summary>
    /// Verifies that filter defaults are applied correctly.
    /// </summary>
    [Fact]
    public void Filter_HasCorrectDefaults()
    {
        this._sut.Filter.SortBy.ShouldBe("date");
        this._sut.Filter.SortDescending.ShouldBeTrue();
        this._sut.Filter.Page.ShouldBe(1);
        this._sut.Filter.PageSize.ShouldBe(50);
        this._sut.Filter.AccountId.ShouldBeNull();
        this._sut.Filter.CategoryId.ShouldBeNull();
        this._sut.Filter.Uncategorized.ShouldBeNull();
    }

    // --- ApplyFiltersAsync ---

    /// <summary>
    /// Verifies that ApplyFiltersAsync resets page to 1 and reloads.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyFiltersAsync_ResetsPageToOne()
    {
        this._sut.Filter.Page = 3;

        await this._sut.ApplyFiltersAsync();

        this._sut.Filter.Page.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that ApplyFiltersAsync passes the filter to the API service.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyFiltersAsync_PassesFilterToApi()
    {
        var accountId = Guid.NewGuid();
        this._sut.Filter.AccountId = accountId;
        this._sut.Filter.Description = "coffee";

        await this._sut.ApplyFiltersAsync();

        this._apiService.LastUnifiedFilter.ShouldNotBeNull();
        this._apiService.LastUnifiedFilter!.AccountId.ShouldBe(accountId);
        this._apiService.LastUnifiedFilter!.Description.ShouldBe("coffee");
    }

    /// <summary>
    /// Verifies that ApplyFiltersAsync clears selection.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyFiltersAsync_ClearsSelection()
    {
        this._sut.SelectedTransactionIds.Add(Guid.NewGuid());

        await this._sut.ApplyFiltersAsync();

        this._sut.SelectedTransactionIds.ShouldBeEmpty();
    }

    // --- Sorting ---

    /// <summary>
    /// Verifies that ToggleSortAsync sets sort field when different from current.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToggleSortAsync_SetsSortField_WhenNewField()
    {
        await this._sut.ToggleSortAsync("amount");

        this._apiService.LastUnifiedFilter.ShouldNotBeNull();
        this._apiService.LastUnifiedFilter!.SortBy.ShouldBe("amount");
        this._apiService.LastUnifiedFilter!.SortDescending.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that ToggleSortAsync toggles direction when same field.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToggleSortAsync_TogglesDirection_WhenSameField()
    {
        this._sut.Filter.SortBy = "date";
        this._sut.Filter.SortDescending = true;

        await this._sut.ToggleSortAsync("date");

        this._sut.Filter.SortDescending.ShouldBeFalse();
    }

    // --- Pagination ---

    /// <summary>
    /// Verifies that GoToPageAsync navigates to specified page.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GoToPageAsync_NavigatesToSpecifiedPage()
    {
        this._apiService.UnifiedPage = new UnifiedTransactionPageDto { TotalCount = 100, PageSize = 50, Page = 1 };

        await this._sut.GoToPageAsync(2);

        this._apiService.LastUnifiedFilter.ShouldNotBeNull();
        this._apiService.LastUnifiedFilter!.Page.ShouldBe(2);
    }

    /// <summary>
    /// Verifies that GoToPageAsync clears selection.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GoToPageAsync_ClearsSelection()
    {
        this._sut.SelectedTransactionIds.Add(Guid.NewGuid());

        await this._sut.GoToPageAsync(2);

        this._sut.SelectedTransactionIds.ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies that ChangePageSizeAsync updates page size and resets to page 1.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ChangePageSizeAsync_ResetsToPageOne()
    {
        this._sut.Filter.Page = 3;

        await this._sut.ChangePageSizeAsync(25);

        this._sut.Filter.PageSize.ShouldBe(25);
        this._apiService.LastUnifiedFilter.ShouldNotBeNull();
        this._apiService.LastUnifiedFilter!.Page.ShouldBe(1);
    }

    // --- Selection ---

    /// <summary>
    /// Verifies that ToggleSelection adds an item when not selected.
    /// </summary>
    [Fact]
    public void ToggleSelection_AddsItem_WhenNotSelected()
    {
        var id = Guid.NewGuid();

        this._sut.ToggleSelection(id);

        this._sut.SelectedTransactionIds.ShouldContain(id);
    }

    /// <summary>
    /// Verifies that ToggleSelection removes an item when already selected.
    /// </summary>
    [Fact]
    public void ToggleSelection_RemovesItem_WhenAlreadySelected()
    {
        var id = Guid.NewGuid();
        this._sut.SelectedTransactionIds.Add(id);

        this._sut.ToggleSelection(id);

        this._sut.SelectedTransactionIds.ShouldNotContain(id);
    }

    /// <summary>
    /// Verifies that ToggleSelectAll selects all current page items.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToggleSelectAll_SelectsAllItems_WhenNoneSelected()
    {
        this._apiService.UnifiedPage = CreatePageWithItems(3);
        await this._sut.InitializeAsync();

        this._sut.ToggleSelectAll();

        this._sut.SelectedTransactionIds.Count.ShouldBe(3);
    }

    /// <summary>
    /// Verifies that ToggleSelectAll deselects all when all are selected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToggleSelectAll_DeselectsAll_WhenAllSelected()
    {
        this._apiService.UnifiedPage = CreatePageWithItems(2);
        await this._sut.InitializeAsync();

        // Select all
        foreach (var item in this._sut.PageData.Items)
        {
            this._sut.SelectedTransactionIds.Add(item.Id);
        }

        this._sut.ToggleSelectAll();

        this._sut.SelectedTransactionIds.ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies IsSelected returns correct state.
    /// </summary>
    [Fact]
    public void IsSelected_ReturnsTrueForSelectedItem()
    {
        var id = Guid.NewGuid();
        this._sut.SelectedTransactionIds.Add(id);

        this._sut.IsSelected(id).ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that SelectedCount reflects the count of selected items.
    /// </summary>
    [Fact]
    public void SelectedCount_ReturnsCorrectCount()
    {
        this._sut.SelectedTransactionIds.Add(Guid.NewGuid());
        this._sut.SelectedTransactionIds.Add(Guid.NewGuid());

        this._sut.SelectedCount.ShouldBe(2);
    }

    /// <summary>
    /// Verifies that AllSelected returns true when all page items are selected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AllSelected_ReturnsTrue_WhenAllPageItemsSelected()
    {
        this._apiService.UnifiedPage = CreatePageWithItems(2);
        await this._sut.InitializeAsync();

        foreach (var item in this._sut.PageData.Items)
        {
            this._sut.SelectedTransactionIds.Add(item.Id);
        }

        this._sut.AllSelected.ShouldBeTrue();
    }

    // --- ClearFilters ---

    /// <summary>
    /// Verifies that ClearFiltersAsync resets all filter fields and reloads.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ClearFiltersAsync_ResetsAllFilters()
    {
        this._sut.Filter.AccountId = Guid.NewGuid();
        this._sut.Filter.CategoryId = Guid.NewGuid();
        this._sut.Filter.Description = "search";
        this._sut.Filter.MinAmount = 10m;
        this._sut.Filter.MaxAmount = 100m;
        this._sut.Filter.Uncategorized = true;
        this._sut.Filter.StartDate = new DateOnly(2025, 1, 1);
        this._sut.Filter.EndDate = new DateOnly(2025, 12, 31);

        await this._sut.ClearFiltersAsync();

        this._sut.Filter.AccountId.ShouldBeNull();
        this._sut.Filter.CategoryId.ShouldBeNull();
        this._sut.Filter.Description.ShouldBeNull();
        this._sut.Filter.MinAmount.ShouldBeNull();
        this._sut.Filter.MaxAmount.ShouldBeNull();
        this._sut.Filter.Uncategorized.ShouldBeNull();
        this._sut.Filter.StartDate.ShouldBeNull();
        this._sut.Filter.EndDate.ShouldBeNull();
        this._sut.Filter.Page.ShouldBe(1);
    }

    // --- ActiveFilterCount ---

    /// <summary>
    /// Verifies that ActiveFilterCount returns 0 when no filters are set.
    /// </summary>
    [Fact]
    public void ActiveFilterCount_ReturnsZero_WhenNoFilters()
    {
        this._sut.ActiveFilterCount.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that ActiveFilterCount counts each active filter.
    /// </summary>
    [Fact]
    public void ActiveFilterCount_CountsActiveFilters()
    {
        this._sut.Filter.AccountId = Guid.NewGuid();
        this._sut.Filter.Description = "test";
        this._sut.Filter.MinAmount = 5m;

        this._sut.ActiveFilterCount.ShouldBe(3);
    }

    // --- ShowBalanceColumn ---

    /// <summary>
    /// Verifies that ShowBalanceColumn is true when a single account is filtered.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ShowBalanceColumn_ReturnsTrue_WhenSingleAccountFiltered()
    {
        this._sut.Filter.AccountId = Guid.NewGuid();
        this._apiService.UnifiedPage = new UnifiedTransactionPageDto
        {
            BalanceInfo = new AccountBalanceInfoDto
            {
                InitialBalance = new MoneyDto { Amount = 1000m, Currency = "USD" },
                CurrentBalance = new MoneyDto { Amount = 1500m, Currency = "USD" },
            },
        };

        await this._sut.LoadTransactionsAsync();

        this._sut.ShowBalanceColumn.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that ShowBalanceColumn is false when no account filter is set.
    /// </summary>
    [Fact]
    public void ShowBalanceColumn_ReturnsFalse_WhenNoAccountFilter()
    {
        this._sut.ShowBalanceColumn.ShouldBeFalse();
    }

    // --- Error Handling ---

    /// <summary>
    /// Verifies that RetryLoadAsync reloads data and clears error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RetryLoadAsync_ReloadsData()
    {
        // Cause initial failure
        this._apiService.GetAccountsException = new HttpRequestException("fail");
        await this._sut.InitializeAsync();
        this._sut.ErrorMessage.ShouldNotBeNull();

        // Fix the API and retry
        this._apiService.GetAccountsException = null;
        this._apiService.UnifiedPage = CreatePageWithItems(2);
        await this._sut.RetryLoadAsync();

        this._sut.ErrorMessage.ShouldBeNull();
        this._sut.PageData.Items.Count.ShouldBe(2);
    }

    /// <summary>
    /// Verifies that DismissError clears the error message.
    /// </summary>
    [Fact]
    public void DismissError_ClearsErrorMessage()
    {
        // Simulate error state via reflection or public setter
        this._sut.DismissError();

        this._sut.ErrorMessage.ShouldBeNull();
    }

    // --- State Change Notification ---

    /// <summary>
    /// Verifies that OnStateChanged is invoked when state changes.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_InvokesOnStateChanged()
    {
        var stateChangedCount = 0;
        this._sut.OnStateChanged = () => stateChangedCount++;

        await this._sut.InitializeAsync();

        stateChangedCount.ShouldBeGreaterThan(0);
    }

    // --- URL Query Parameter Binding ---

    /// <summary>
    /// Verifies that ApplyQueryParameters sets account filter from query string.
    /// </summary>
    [Fact]
    public void ApplyQueryParameters_SetsAccountId()
    {
        var id = Guid.NewGuid();
        this._sut.ApplyQueryParameters(id.ToString(), null, null, null, null, null, null, null, null, null, null, null);

        this._sut.Filter.AccountId.ShouldBe(id);
    }

    /// <summary>
    /// Verifies that ApplyQueryParameters sets uncategorized filter.
    /// </summary>
    [Fact]
    public void ApplyQueryParameters_SetsUncategorizedFilter()
    {
        this._sut.ApplyQueryParameters(null, null, true, null, null, null, null, null, null, null, null, null);

        this._sut.Filter.Uncategorized.ShouldBe(true);
    }

    /// <summary>
    /// Verifies that ApplyQueryParameters sets date range from query strings.
    /// </summary>
    [Fact]
    public void ApplyQueryParameters_SetsDateRange()
    {
        this._sut.ApplyQueryParameters(null, null, null, "2026-01-01", "2026-01-31", null, null, null, null, null, null, null);

        this._sut.Filter.StartDate.ShouldBe(new DateOnly(2026, 1, 1));
        this._sut.Filter.EndDate.ShouldBe(new DateOnly(2026, 1, 31));
    }

    /// <summary>
    /// Verifies that ApplyQueryParameters sets description filter.
    /// </summary>
    [Fact]
    public void ApplyQueryParameters_SetsDescription()
    {
        this._sut.ApplyQueryParameters(null, null, null, null, null, "coffee", null, null, null, null, null, null);

        this._sut.Filter.Description.ShouldBe("coffee");
    }

    /// <summary>
    /// Verifies that ApplyQueryParameters sets amount range.
    /// </summary>
    [Fact]
    public void ApplyQueryParameters_SetsAmountRange()
    {
        this._sut.ApplyQueryParameters(null, null, null, null, null, null, 10m, 100m, null, null, null, null);

        this._sut.Filter.MinAmount.ShouldBe(10m);
        this._sut.Filter.MaxAmount.ShouldBe(100m);
    }

    /// <summary>
    /// Verifies that ApplyQueryParameters sets sort parameters.
    /// </summary>
    [Fact]
    public void ApplyQueryParameters_SetsSortParameters()
    {
        this._sut.ApplyQueryParameters(null, null, null, null, null, null, null, null, "amount", false, null, null);

        this._sut.Filter.SortBy.ShouldBe("amount");
        this._sut.Filter.SortDescending.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ApplyQueryParameters sets pagination.
    /// </summary>
    [Fact]
    public void ApplyQueryParameters_SetsPagination()
    {
        this._sut.ApplyQueryParameters(null, null, null, null, null, null, null, null, null, null, 3, 25);

        this._sut.Filter.Page.ShouldBe(3);
        this._sut.Filter.PageSize.ShouldBe(25);
    }

    /// <summary>
    /// Verifies that ApplyQueryParameters ignores invalid account ID strings.
    /// </summary>
    [Fact]
    public void ApplyQueryParameters_IgnoresInvalidAccountId()
    {
        this._sut.ApplyQueryParameters("not-a-guid", null, null, null, null, null, null, null, null, null, null, null);

        this._sut.Filter.AccountId.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that BuildUrlWithFilters returns base path when no filters active.
    /// </summary>
    [Fact]
    public void BuildUrlWithFilters_ReturnsBasePath_WhenNoFilters()
    {
        this._sut.BuildUrlWithFilters().ShouldBe("/transactions");
    }

    /// <summary>
    /// Verifies that BuildUrlWithFilters includes account filter in URL.
    /// </summary>
    [Fact]
    public void BuildUrlWithFilters_IncludesAccountFilter()
    {
        var id = Guid.NewGuid();
        this._sut.Filter.AccountId = id;

        this._sut.BuildUrlWithFilters().ShouldContain($"account={id}");
    }

    /// <summary>
    /// Verifies that BuildUrlWithFilters includes uncategorized flag.
    /// </summary>
    [Fact]
    public void BuildUrlWithFilters_IncludesUncategorizedFlag()
    {
        this._sut.Filter.Uncategorized = true;

        this._sut.BuildUrlWithFilters().ShouldContain("uncategorized=true");
    }

    /// <summary>
    /// Verifies that BuildUrlWithFilters includes date range.
    /// </summary>
    [Fact]
    public void BuildUrlWithFilters_IncludesDateRange()
    {
        this._sut.Filter.StartDate = new DateOnly(2026, 3, 1);
        this._sut.Filter.EndDate = new DateOnly(2026, 3, 31);

        var url = this._sut.BuildUrlWithFilters();
        url.ShouldContain("startDate=2026-03-01");
        url.ShouldContain("endDate=2026-03-31");
    }

    /// <summary>
    /// Verifies that ApplyFiltersAsync updates the URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyFiltersAsync_UpdatesUrl()
    {
        this._sut.Filter.AccountId = Guid.NewGuid();

        await this._sut.ApplyFiltersAsync();

        this._navigationManager.LastNavigatedUri.ShouldNotBeNull();
        this._navigationManager.LastNavigatedUri.ShouldContain("account=");
    }

    /// <summary>
    /// Verifies that ClearFiltersAsync resets URL to base path.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ClearFiltersAsync_ResetsUrl()
    {
        this._sut.Filter.AccountId = Guid.NewGuid();
        await this._sut.ApplyFiltersAsync();

        await this._sut.ClearFiltersAsync();

        this._navigationManager.LastNavigatedUri.ShouldBe("/transactions");
    }

    // --- Inline Actions ---

    /// <summary>
    /// Verifies that UpdateTransactionCategoryAsync shows success toast when API returns a result.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateTransactionCategoryAsync_ShowsSuccessToast_WhenApiReturnsResult()
    {
        var categoryId = Guid.NewGuid();
        this._apiService.UpdateTransactionCategoryResult = new TransactionDto { Id = Guid.NewGuid() };
        this._sut.Categories.Add(new BudgetCategoryDto { Id = categoryId, Name = "Groceries" });

        await this._sut.UpdateTransactionCategoryAsync(Guid.NewGuid(), categoryId);

        this._toastService.LastSuccessMessage.ShouldNotBeNull();
        this._toastService.LastSuccessMessage.ShouldContain("Groceries");
    }

    /// <summary>
    /// Verifies that UpdateTransactionCategoryAsync shows success toast with "Uncategorized" when clearing category.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateTransactionCategoryAsync_ShowsUncategorizedToast_WhenClearingCategory()
    {
        this._apiService.UpdateTransactionCategoryResult = new TransactionDto { Id = Guid.NewGuid() };

        await this._sut.UpdateTransactionCategoryAsync(Guid.NewGuid(), null);

        this._toastService.LastSuccessMessage.ShouldNotBeNull();
        this._toastService.LastSuccessMessage.ShouldContain("Uncategorized");
    }

    /// <summary>
    /// Verifies that UpdateTransactionCategoryAsync shows error toast when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateTransactionCategoryAsync_ShowsErrorToast_WhenApiFails()
    {
        this._apiService.UpdateTransactionCategoryResult = null;

        await this._sut.UpdateTransactionCategoryAsync(Guid.NewGuid(), Guid.NewGuid());

        this._toastService.LastErrorMessage.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies that UpdateTransactionCategoryAsync reloads transactions on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateTransactionCategoryAsync_ReloadsTransactions_OnSuccess()
    {
        this._apiService.UpdateTransactionCategoryResult = new TransactionDto { Id = Guid.NewGuid() };
        this._apiService.UnifiedPage = CreatePageWithItems(3);

        await this._sut.UpdateTransactionCategoryAsync(Guid.NewGuid(), Guid.NewGuid());

        this._sut.PageData.Items.Count.ShouldBe(3);
    }

    /// <summary>
    /// Verifies that DeleteTransactionAsync shows success toast and reloads.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteTransactionAsync_ShowsSuccessToast_WhenApiReturnsTrue()
    {
        this._apiService.DeleteTransactionResult = true;
        this._apiService.UnifiedPage = CreatePageWithItems(2);

        await this._sut.DeleteTransactionAsync(Guid.NewGuid());

        this._toastService.LastSuccessMessage.ShouldNotBeNull();
        this._toastService.LastSuccessMessage.ShouldContain("deleted");
    }

    /// <summary>
    /// Verifies that DeleteTransactionAsync shows error toast when API returns false.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteTransactionAsync_ShowsErrorToast_WhenApiFails()
    {
        this._apiService.DeleteTransactionResult = false;

        await this._sut.DeleteTransactionAsync(Guid.NewGuid());

        this._toastService.LastErrorMessage.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies that DeleteTransactionAsync removes the ID from selected set.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteTransactionAsync_RemovesFromSelection()
    {
        var txnId = Guid.NewGuid();
        this._apiService.DeleteTransactionResult = true;
        this._sut.SelectedTransactionIds.Add(txnId);

        await this._sut.DeleteTransactionAsync(txnId);

        this._sut.SelectedTransactionIds.ShouldNotContain(txnId);
    }

    // --- Bulk Categorize ---

    /// <summary>
    /// Verifies that OpenBulkCategorize clears category and shows picker.
    /// </summary>
    [Fact]
    public void OpenBulkCategorize_ShowsPickerAndClearsCategoryId()
    {
        this._sut.BulkCategoryId = Guid.NewGuid();

        this._sut.OpenBulkCategorize();

        this._sut.ShowBulkCategorize.ShouldBeTrue();
        this._sut.BulkCategoryId.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that CloseBulkCategorize hides the picker.
    /// </summary>
    [Fact]
    public void CloseBulkCategorize_HidesPicker()
    {
        this._sut.OpenBulkCategorize();

        this._sut.CloseBulkCategorize();

        this._sut.ShowBulkCategorize.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ExecuteBulkCategorizeAsync does nothing when no category selected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteBulkCategorizeAsync_DoesNothing_WhenNoCategorySelected()
    {
        this._sut.SelectedTransactionIds.Add(Guid.NewGuid());
        this._sut.BulkCategoryId = null;

        await this._sut.ExecuteBulkCategorizeAsync();

        this._apiService.LastBulkCategorizeRequest.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that ExecuteBulkCategorizeAsync does nothing when no transactions selected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteBulkCategorizeAsync_DoesNothing_WhenNoSelection()
    {
        this._sut.BulkCategoryId = Guid.NewGuid();

        await this._sut.ExecuteBulkCategorizeAsync();

        this._apiService.LastBulkCategorizeRequest.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that ExecuteBulkCategorizeAsync sends correct request, shows toast, clears selection, and reloads.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteBulkCategorizeAsync_CategorizesAndShowsToast()
    {
        var categoryId = Guid.NewGuid();
        var txnId1 = Guid.NewGuid();
        var txnId2 = Guid.NewGuid();
        this._sut.SelectedTransactionIds.Add(txnId1);
        this._sut.SelectedTransactionIds.Add(txnId2);
        this._sut.BulkCategoryId = categoryId;
        this._sut.Categories.Add(new BudgetCategoryDto { Id = categoryId, Name = "Food" });
        this._apiService.BulkCategorizeResult = new BulkCategorizeResponse { SuccessCount = 2 };
        this._apiService.UnifiedPage = CreatePageWithItems(1);

        await this._sut.ExecuteBulkCategorizeAsync();

        this._apiService.LastBulkCategorizeRequest.ShouldNotBeNull();
        this._apiService.LastBulkCategorizeRequest!.CategoryId.ShouldBe(categoryId);
        this._apiService.LastBulkCategorizeRequest.TransactionIds.ShouldContain(txnId1);
        this._apiService.LastBulkCategorizeRequest.TransactionIds.ShouldContain(txnId2);
        this._toastService.LastSuccessMessage.ShouldNotBeNull();
        this._toastService.LastSuccessMessage.ShouldContain("Food");
        this._sut.SelectedTransactionIds.ShouldBeEmpty();
        this._sut.ShowBulkCategorize.ShouldBeFalse();
        this._sut.IsBulkOperationInProgress.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ExecuteBulkCategorizeAsync resets IsBulkOperationInProgress on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteBulkCategorizeAsync_ShowsErrorToast_OnException()
    {
        this._sut.SelectedTransactionIds.Add(Guid.NewGuid());
        this._sut.BulkCategoryId = Guid.NewGuid();
        this._apiService.BulkCategorizeException = new InvalidOperationException("API error");

        await this._sut.ExecuteBulkCategorizeAsync();

        this._toastService.LastErrorMessage.ShouldNotBeNull();
        this._sut.IsBulkOperationInProgress.ShouldBeFalse();
    }

    // --- Bulk Delete ---

    /// <summary>
    /// Verifies that ExecuteBulkDeleteAsync does nothing when no transactions selected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteBulkDeleteAsync_DoesNothing_WhenNoSelection()
    {
        this._apiService.DeleteTransactionResult = true;

        await this._sut.ExecuteBulkDeleteAsync();

        this._toastService.LastSuccessMessage.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that ExecuteBulkDeleteAsync deletes all selected transactions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteBulkDeleteAsync_DeletesAndShowsToast()
    {
        this._sut.SelectedTransactionIds.Add(Guid.NewGuid());
        this._sut.SelectedTransactionIds.Add(Guid.NewGuid());
        this._apiService.DeleteTransactionResult = true;
        this._apiService.UnifiedPage = CreatePageWithItems(0);

        await this._sut.ExecuteBulkDeleteAsync();

        this._toastService.LastSuccessMessage.ShouldNotBeNull();
        this._toastService.LastSuccessMessage.ShouldContain("2");
        this._sut.SelectedTransactionIds.ShouldBeEmpty();
        this._sut.IsBulkOperationInProgress.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ExecuteBulkDeleteAsync shows error toast on exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteBulkDeleteAsync_ShowsErrorToast_OnException()
    {
        this._sut.SelectedTransactionIds.Add(Guid.NewGuid());
        this._apiService.DeleteTransactionException = new InvalidOperationException("fail");

        await this._sut.ExecuteBulkDeleteAsync();

        this._toastService.LastErrorMessage.ShouldNotBeNull();
        this._sut.IsBulkOperationInProgress.ShouldBeFalse();
    }

    // --- Create Rule ---

    /// <summary>
    /// Verifies that OpenCreateRule pre-fills rule with description and shows modal.
    /// </summary>
    [Fact]
    public void OpenCreateRule_PrefillsRuleAndShowsModal()
    {
        this._sut.OpenCreateRule("GROCERY STORE");

        this._sut.ShowCreateRule.ShouldBeTrue();
        this._sut.NewRule.Pattern.ShouldBe("GROCERY STORE");
        this._sut.NewRule.Name.ShouldBe("GROCERY STORE");
        this._sut.NewRule.MatchType.ShouldBe("Contains");
    }

    /// <summary>
    /// Verifies that CloseCreateRule hides the modal.
    /// </summary>
    [Fact]
    public void CloseCreateRule_HidesModal()
    {
        this._sut.OpenCreateRule("Test");

        this._sut.CloseCreateRule();

        this._sut.ShowCreateRule.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that CreateRuleAsync shows success toast and closes modal on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRuleAsync_ShowsSuccessToast_OnSuccess()
    {
        this._apiService.CreateRuleResult = new CategorizationRuleDto
        {
            Id = Guid.NewGuid(),
            Name = "Grocery Rule",
        };
        this._sut.OpenCreateRule("GROCERY STORE");

        await this._sut.CreateRuleAsync();

        this._toastService.LastSuccessMessage.ShouldNotBeNull();
        this._toastService.LastSuccessMessage.ShouldContain("Grocery Rule");
        this._sut.ShowCreateRule.ShouldBeFalse();
        this._sut.IsCreatingRule.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that CreateRuleAsync shows error toast when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRuleAsync_ShowsErrorToast_WhenApiReturnsNull()
    {
        this._apiService.CreateRuleResult = null;
        this._sut.OpenCreateRule("Test");

        await this._sut.CreateRuleAsync();

        this._toastService.LastErrorMessage.ShouldNotBeNull();
        this._sut.IsCreatingRule.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that CreateRuleAsync shows error toast on exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRuleAsync_ShowsErrorToast_OnException()
    {
        this._apiService.CreateRuleException = new InvalidOperationException("API down");
        this._sut.OpenCreateRule("Test");

        await this._sut.CreateRuleAsync();

        this._toastService.LastErrorMessage.ShouldNotBeNull();
        this._toastService.LastErrorMessage.ShouldContain("API down");
        this._sut.IsCreatingRule.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that CreateRuleAsync reloads transactions on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRuleAsync_ReloadsTransactions_OnSuccess()
    {
        this._apiService.CreateRuleResult = new CategorizationRuleDto { Id = Guid.NewGuid(), Name = "R" };
        this._apiService.UnifiedPage = CreatePageWithItems(5);
        this._sut.OpenCreateRule("Test");

        await this._sut.CreateRuleAsync();

        this._sut.PageData.Items.Count.ShouldBe(5);
    }

    /// <summary>
    /// Verifies that CreateRuleAsync shows the apply-rules prompt after successful creation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRuleAsync_ShowsApplyRulesPrompt_OnSuccess()
    {
        this._apiService.CreateRuleResult = new CategorizationRuleDto { Id = Guid.NewGuid(), Name = "Grocery Rule" };
        this._sut.OpenCreateRule("GROCERY STORE");

        await this._sut.CreateRuleAsync();

        this._sut.ShowApplyRulesPrompt.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that CreateRuleAsync does not show the apply-rules prompt on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateRuleAsync_DoesNotShowApplyRulesPrompt_OnFailure()
    {
        this._apiService.CreateRuleResult = null;
        this._sut.OpenCreateRule("Test");

        await this._sut.CreateRuleAsync();

        this._sut.ShowApplyRulesPrompt.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ApplyRulesAfterCreationAsync calls the API and shows a success toast.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyRulesAfterCreationAsync_CallsApi_ShowsSuccessToast()
    {
        this._apiService.ApplyRulesResult = new ApplyRulesResponse { Categorized = 3 };

        await this._sut.ApplyRulesAfterCreationAsync();

        this._toastService.LastSuccessMessage.ShouldNotBeNull();
        this._toastService.LastSuccessMessage.ShouldContain("3");
        this._sut.ShowApplyRulesPrompt.ShouldBeFalse();
        this._sut.IsApplyingRules.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that ApplyRulesAfterCreationAsync shows error toast on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ApplyRulesAfterCreationAsync_ShowsErrorToast_OnFailure()
    {
        this._apiService.ApplyRulesResult = null;

        await this._sut.ApplyRulesAfterCreationAsync();

        this._toastService.LastErrorMessage.ShouldNotBeNull();
        this._sut.ShowApplyRulesPrompt.ShouldBeFalse();
        this._sut.IsApplyingRules.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that DismissApplyRulesPrompt hides the prompt.
    /// </summary>
    [Fact]
    public void DismissApplyRulesPrompt_HidesPrompt()
    {
        this._sut.DismissApplyRulesPrompt();

        this._sut.ShowApplyRulesPrompt.ShouldBeFalse();
    }

    // --- Suggestions ---

    /// <summary>
    /// Verifies that LoadSuggestionsAsync populates Suggestions when uncategorized transactions exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadSuggestionsAsync_PopulatesSuggestions_WhenUncategorizedExist()
    {
        var txnId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        this._apiService.UnifiedPage = new UnifiedTransactionPageDto
        {
            Items = new List<UnifiedTransactionItemDto>
            {
                new() { Id = txnId, Date = new DateOnly(2026, 1, 15), Description = "WALMART", Amount = new MoneyDto { Amount = -25m, Currency = "USD" }, AccountId = Guid.NewGuid(), AccountName = "Checking", CategoryId = null },
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 50,
        };
        this._apiService.BatchSuggestionsResult = new BatchSuggestCategoriesResponse
        {
            Suggestions = new Dictionary<Guid, InlineCategorySuggestionDto>
            {
                [txnId] = new() { TransactionId = txnId, CategoryId = categoryId, CategoryName = "Groceries" },
            },
        };

        await this._sut.InitializeAsync();

        this._sut.Suggestions.ShouldContainKey(txnId);
        this._sut.Suggestions[txnId].CategoryName.ShouldBe("Groceries");
    }

    /// <summary>
    /// Verifies that LoadSuggestionsAsync clears suggestions when no uncategorized transactions exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadSuggestionsAsync_ClearsSuggestions_WhenNoUncategorized()
    {
        this._apiService.UnifiedPage = new UnifiedTransactionPageDto
        {
            Items = new List<UnifiedTransactionItemDto>
            {
                new() { Id = Guid.NewGuid(), Date = new DateOnly(2026, 1, 15), Description = "WALMART", Amount = new MoneyDto { Amount = -25m, Currency = "USD" }, AccountId = Guid.NewGuid(), AccountName = "Checking", CategoryId = Guid.NewGuid(), CategoryName = "Groceries" },
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 50,
        };

        await this._sut.InitializeAsync();

        this._sut.Suggestions.ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies that LoadSuggestionsAsync sets IsLoadingSuggestions to false after loading.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadSuggestionsAsync_SetsIsLoadingSuggestionsToFalse()
    {
        this._apiService.UnifiedPage = CreatePageWithItems(1);

        await this._sut.InitializeAsync();

        this._sut.IsLoadingSuggestions.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that AcceptSuggestionAsync assigns the suggested category and removes the suggestion.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AcceptSuggestionAsync_AssignsCategoryAndRemovesSuggestion()
    {
        var txnId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        this._apiService.UnifiedPage = new UnifiedTransactionPageDto
        {
            Items = new List<UnifiedTransactionItemDto>
            {
                new() { Id = txnId, Date = new DateOnly(2026, 1, 15), Description = "WALMART", Amount = new MoneyDto { Amount = -25m, Currency = "USD" }, AccountId = Guid.NewGuid(), AccountName = "Checking", CategoryId = null },
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 50,
        };
        this._apiService.BatchSuggestionsResult = new BatchSuggestCategoriesResponse
        {
            Suggestions = new Dictionary<Guid, InlineCategorySuggestionDto>
            {
                [txnId] = new() { TransactionId = txnId, CategoryId = categoryId, CategoryName = "Groceries" },
            },
        };
        this._apiService.UpdateTransactionCategoryResult = new TransactionDto
        {
            Id = txnId,
            Date = new DateOnly(2026, 1, 15),
            Description = "WALMART",
            Amount = new MoneyDto { Amount = -25m, Currency = "USD" },
            AccountId = Guid.NewGuid(),
        };

        await this._sut.InitializeAsync();
        await this._sut.AcceptSuggestionAsync(txnId);

        this._sut.Suggestions.ShouldNotContainKey(txnId);
        this._toastService.LastSuccessMessage.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies that AcceptSuggestionAsync does nothing when no suggestion exists for the transaction.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AcceptSuggestionAsync_DoesNothing_WhenNoSuggestion()
    {
        this._apiService.UnifiedPage = CreatePageWithItems(1);

        await this._sut.InitializeAsync();
        await this._sut.AcceptSuggestionAsync(Guid.NewGuid());

        this._sut.Suggestions.ShouldBeEmpty();
    }

    private static UnifiedTransactionPageDto CreatePageWithItems(int count)
    {
        var items = new List<UnifiedTransactionItemDto>();
        for (int i = 0; i < count; i++)
        {
            items.Add(new UnifiedTransactionItemDto
            {
                Id = Guid.NewGuid(),
                Date = new DateOnly(2026, 1, 15),
                Description = $"Transaction {i + 1}",
                Amount = new MoneyDto { Amount = -25.00m + i, Currency = "USD" },
                AccountId = Guid.NewGuid(),
                AccountName = "Checking",
            });
        }

        return new UnifiedTransactionPageDto
        {
            Items = items,
            TotalCount = count,
            Page = 1,
            PageSize = 50,
        };
    }

    /// <summary>
    /// Stub toast service for ViewModel testing.
    /// </summary>
    private sealed class StubToastService : IToastService
    {
        /// <inheritdoc/>
        public event Action? OnChange;

        /// <inheritdoc/>
        public IReadOnlyList<ToastItem> Toasts { get; } = [];

        public string? LastSuccessMessage { get; private set; }

        public string? LastErrorMessage { get; private set; }

        /// <inheritdoc/>
        public void ShowSuccess(string message, string? title = null)
        {
            this.LastSuccessMessage = message;
        }

        /// <inheritdoc/>
        public void ShowError(string message, string? title = null)
        {
            this.LastErrorMessage = message;
        }

        /// <inheritdoc/>
        public void ShowInfo(string message, string? title = null)
        {
        }

        /// <inheritdoc/>
        public void ShowWarning(string message, string? title = null)
        {
            this.OnChange?.Invoke();
        }

        /// <inheritdoc/>
        public void Remove(Guid id)
        {
        }
    }

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
        /// Gets the last URI navigated to.
        /// </summary>
        public string? LastNavigatedUri { get; private set; }

        /// <inheritdoc/>
        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            this.LastNavigatedUri = uri;
        }

        /// <inheritdoc/>
        protected override void NavigateToCore(string uri, NavigationOptions options)
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
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            new(default(TValue)!);

        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            new(default(TValue)!);
    }
}
