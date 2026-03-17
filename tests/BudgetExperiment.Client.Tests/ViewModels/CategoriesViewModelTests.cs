// <copyright file="CategoriesViewModelTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Client.ViewModels;
using BudgetExperiment.Contracts.Dtos;
using Microsoft.JSInterop;
using Shouldly;

namespace BudgetExperiment.Client.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="CategoriesViewModel"/>.
/// </summary>
public sealed class CategoriesViewModelTests : IDisposable
{
    private readonly StubBudgetApiService _apiService = new();
    private readonly StubToastService _toastService = new();
    private readonly ScopeService _scopeService;
    private readonly StubChatContextService _chatContext = new();
    private readonly StubApiErrorContext _apiErrorContext = new();
    private readonly CategoriesViewModel _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoriesViewModelTests"/> class.
    /// </summary>
    public CategoriesViewModelTests()
    {
        this._scopeService = new ScopeService(new StubJSRuntime());
        this._sut = new CategoriesViewModel(
            this._apiService,
            this._toastService,
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
    /// Verifies that InitializeAsync loads categories from the API.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsCategories()
    {
        this._apiService.Categories.Add(CreateCategory("Groceries", "Expense"));

        await this._sut.InitializeAsync();

        this._sut.Categories.Count.ShouldBe(1);
        this._sut.Categories[0].Name.ShouldBe("Groceries");
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

        this._chatContext.CurrentContext.PageType.ShouldBe("categories");
    }

    /// <summary>
    /// Verifies that InitializeAsync handles API failure gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsErrorMessage_WhenApiFails()
    {
        this._apiService.GetCategoriesException = new HttpRequestException("Server error");

        await this._sut.InitializeAsync();

        this._sut.ErrorMessage.ShouldNotBeNull();
        this._sut.ErrorMessage!.ShouldContain("Failed to load categories");
        this._sut.IsLoading.ShouldBeFalse();
    }

    // --- LoadCategoriesAsync ---

    /// <summary>
    /// Verifies that LoadCategoriesAsync clears the error message before loading.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadCategoriesAsync_ClearsErrorMessage()
    {
        this._apiService.GetCategoriesException = new HttpRequestException("fail");
        await this._sut.InitializeAsync();
        this._sut.ErrorMessage.ShouldNotBeNull();

        this._apiService.GetCategoriesException = null;
        await this._sut.LoadCategoriesAsync();

        this._sut.ErrorMessage.ShouldBeNull();
    }

    // --- RetryLoadAsync ---

    /// <summary>
    /// Verifies that RetryLoadAsync reloads categories.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RetryLoadAsync_ReloadsCategories()
    {
        await this._sut.InitializeAsync();
        this._apiService.Categories.Add(CreateCategory("NewCat", "Expense"));

        await this._sut.RetryLoadAsync();

        this._sut.Categories.Count.ShouldBe(1);
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
        this._apiService.GetCategoriesException = new HttpRequestException("fail");
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

    // --- Computed Properties ---

    /// <summary>
    /// Verifies that ExpenseCategories filters only expense type categories.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExpenseCategories_FiltersExpenseType()
    {
        this._apiService.Categories.Add(CreateCategory("Groceries", "Expense"));
        this._apiService.Categories.Add(CreateCategory("Salary", "Income"));
        await this._sut.InitializeAsync();

        this._sut.ExpenseCategories.Count.ShouldBe(1);
        this._sut.ExpenseCategories[0].Name.ShouldBe("Groceries");
    }

    /// <summary>
    /// Verifies that IncomeCategories filters only income type categories.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task IncomeCategories_FiltersIncomeType()
    {
        this._apiService.Categories.Add(CreateCategory("Groceries", "Expense"));
        this._apiService.Categories.Add(CreateCategory("Salary", "Income"));
        await this._sut.InitializeAsync();

        this._sut.IncomeCategories.Count.ShouldBe(1);
        this._sut.IncomeCategories[0].Name.ShouldBe("Salary");
    }

    /// <summary>
    /// Verifies that TransferCategories filters only transfer type categories.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TransferCategories_FiltersTransferType()
    {
        this._apiService.Categories.Add(CreateCategory("Transfer Out", "Transfer"));
        this._apiService.Categories.Add(CreateCategory("Groceries", "Expense"));
        await this._sut.InitializeAsync();

        this._sut.TransferCategories.Count.ShouldBe(1);
        this._sut.TransferCategories[0].Name.ShouldBe("Transfer Out");
    }

    /// <summary>
    /// Verifies that computed categories are sorted by SortOrder then Name.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ComputedCategories_SortBySortOrderThenName()
    {
        this._apiService.Categories.Add(CreateCategory("Utilities", "Expense", sortOrder: 2));
        this._apiService.Categories.Add(CreateCategory("Groceries", "Expense", sortOrder: 1));
        this._apiService.Categories.Add(CreateCategory("Dining", "Expense", sortOrder: 1));
        await this._sut.InitializeAsync();

        var expenses = this._sut.ExpenseCategories;
        expenses[0].Name.ShouldBe("Dining");
        expenses[1].Name.ShouldBe("Groceries");
        expenses[2].Name.ShouldBe("Utilities");
    }

    // --- Add Category ---

    /// <summary>
    /// Verifies that OpenAddCategory shows the add form and resets the model.
    /// </summary>
    [Fact]
    public void OpenAddCategory_ShowsFormAndResetsModel()
    {
        this._sut.OpenAddCategory();

        this._sut.ShowAddForm.ShouldBeTrue();
        this._sut.NewCategory.Type.ShouldBe("Expense");
        this._sut.NewCategory.Color.ShouldBe("#4CAF50");
    }

    /// <summary>
    /// Verifies that CloseAddCategory hides the add form.
    /// </summary>
    [Fact]
    public void CloseAddCategory_HidesForm()
    {
        this._sut.OpenAddCategory();

        this._sut.CloseAddCategory();

        this._sut.ShowAddForm.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies state change notification on OpenAddCategory.
    /// </summary>
    [Fact]
    public void OpenAddCategory_NotifiesStateChanged()
    {
        bool notified = false;
        this._sut.OnStateChanged = () => notified = true;

        this._sut.OpenAddCategory();

        notified.ShouldBeTrue();
    }

    // --- CreateCategoryAsync ---

    /// <summary>
    /// Verifies that CreateCategoryAsync adds the category to the list on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateCategoryAsync_AddsCategoryToList_WhenSuccessful()
    {
        var created = CreateCategory("Groceries", "Expense");
        this._apiService.CreateCategoryResult = created;
        await this._sut.InitializeAsync();
        this._sut.OpenAddCategory();

        await this._sut.CreateCategoryAsync(new BudgetCategoryCreateDto { Name = "Groceries", Type = "Expense" });

        this._sut.Categories.ShouldContain(c => c.Name == "Groceries");
        this._sut.ShowAddForm.ShouldBeFalse();
        this._sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that CreateCategoryAsync sets error when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateCategoryAsync_SetsError_WhenApiReturnsNull()
    {
        this._apiService.CreateCategoryResult = null;
        await this._sut.InitializeAsync();

        await this._sut.CreateCategoryAsync(new BudgetCategoryCreateDto { Name = "Fail" });

        this._sut.ErrorMessage.ShouldBe("Failed to create category.");
        this._sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that CreateCategoryAsync sets error when API throws.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateCategoryAsync_SetsError_WhenApiThrows()
    {
        this._apiService.CreateCategoryException = new HttpRequestException("Network error");
        await this._sut.InitializeAsync();

        await this._sut.CreateCategoryAsync(new BudgetCategoryCreateDto { Name = "Fail" });

        this._sut.ErrorMessage!.ShouldContain("Failed to create category");
        this._sut.ErrorMessage!.ShouldContain("Network error");
        this._sut.IsSubmitting.ShouldBeFalse();
    }

    // --- Edit Category ---

    /// <summary>
    /// Verifies that OpenEditCategory populates edit form state.
    /// </summary>
    [Fact]
    public void OpenEditCategory_PopulatesEditState()
    {
        var cat = CreateCategory("Groceries", "Expense");
        cat.Version = "v1";
        cat.SortOrder = 5;

        this._sut.OpenEditCategory(cat);

        this._sut.ShowEditForm.ShouldBeTrue();
        this._sut.EditingCategoryId.ShouldBe(cat.Id);
        this._sut.EditingVersion.ShouldBe("v1");
        this._sut.EditCategory.Name.ShouldBe("Groceries");
        this._sut.EditCategory.Type.ShouldBe("Expense");
        this._sut.EditSortOrder.ShouldBe(5);
    }

    /// <summary>
    /// Verifies that OpenEditCategory uses default color when category has null color.
    /// </summary>
    [Fact]
    public void OpenEditCategory_UsesDefaultColor_WhenNullColor()
    {
        var cat = CreateCategory("NullColor", "Expense");
        cat.Color = null;

        this._sut.OpenEditCategory(cat);

        this._sut.EditCategory.Color.ShouldBe("#4CAF50");
    }

    /// <summary>
    /// Verifies that CloseEditCategory resets edit state.
    /// </summary>
    [Fact]
    public void CloseEditCategory_ResetsEditState()
    {
        var cat = CreateCategory("Groceries", "Expense");
        this._sut.OpenEditCategory(cat);

        this._sut.CloseEditCategory();

        this._sut.ShowEditForm.ShouldBeFalse();
        this._sut.EditingCategoryId.ShouldBeNull();
    }

    // --- UpdateCategoryAsync ---

    /// <summary>
    /// Verifies that UpdateCategoryAsync updates the category in the list on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateCategoryAsync_UpdatesCategoryInList_WhenSuccessful()
    {
        var cat = CreateCategory("Groceries", "Expense");
        this._apiService.Categories.Add(cat);
        await this._sut.InitializeAsync();

        var updatedCat = CreateCategory("Groceries Updated", "Expense");
        updatedCat.Id = cat.Id;
        this._apiService.UpdateCategoryResult = ApiResult<BudgetCategoryDto>.Success(updatedCat);
        this._sut.OpenEditCategory(cat);

        await this._sut.UpdateCategoryAsync(new BudgetCategoryCreateDto { Name = "Groceries Updated" });

        this._sut.Categories.ShouldContain(c => c.Name == "Groceries Updated");
        this._sut.ShowEditForm.ShouldBeFalse();
        this._sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that UpdateCategoryAsync returns early when no editing category is set.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateCategoryAsync_ReturnsEarly_WhenNoEditingCategory()
    {
        await this._sut.UpdateCategoryAsync(new BudgetCategoryCreateDto());

        this._sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that UpdateCategoryAsync handles conflict (409) correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateCategoryAsync_HandlesConflict()
    {
        var cat = CreateCategory("Groceries", "Expense");
        this._apiService.Categories.Add(cat);
        await this._sut.InitializeAsync();

        this._apiService.UpdateCategoryResult = ApiResult<BudgetCategoryDto>.Conflict();
        this._sut.OpenEditCategory(cat);

        await this._sut.UpdateCategoryAsync(new BudgetCategoryCreateDto { Name = "Conflict" });

        this._toastService.WarningShown.ShouldBeTrue();
        this._sut.ShowEditForm.ShouldBeFalse();
        this._sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that UpdateCategoryAsync sets error when API returns failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateCategoryAsync_SetsError_WhenApiReturnsFailure()
    {
        var cat = CreateCategory("Groceries", "Expense");
        this._apiService.Categories.Add(cat);
        await this._sut.InitializeAsync();

        this._apiService.UpdateCategoryResult = ApiResult<BudgetCategoryDto>.Failure();
        this._sut.OpenEditCategory(cat);

        await this._sut.UpdateCategoryAsync(new BudgetCategoryCreateDto { Name = "Fail" });

        this._sut.ErrorMessage.ShouldBe("Failed to update category.");
        this._sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that UpdateCategoryAsync sets error when API throws.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateCategoryAsync_SetsError_WhenApiThrows()
    {
        var cat = CreateCategory("Groceries", "Expense");
        this._apiService.Categories.Add(cat);
        await this._sut.InitializeAsync();

        this._apiService.UpdateCategoryException = new HttpRequestException("Network error");
        this._sut.OpenEditCategory(cat);

        await this._sut.UpdateCategoryAsync(new BudgetCategoryCreateDto { Name = "Fail" });

        this._sut.ErrorMessage!.ShouldContain("Failed to update category");
        this._sut.IsSubmitting.ShouldBeFalse();
    }

    // --- Delete Category ---

    /// <summary>
    /// Verifies that ConfirmDeleteCategory shows the delete confirmation dialog.
    /// </summary>
    [Fact]
    public void ConfirmDeleteCategory_ShowsDeleteDialog()
    {
        var cat = CreateCategory("ToDelete", "Expense");

        this._sut.ConfirmDeleteCategory(cat);

        this._sut.ShowDeleteConfirm.ShouldBeTrue();
        this._sut.DeletingCategory.ShouldBe(cat);
    }

    /// <summary>
    /// Verifies that CancelDelete hides the delete confirmation dialog.
    /// </summary>
    [Fact]
    public void CancelDelete_HidesDeleteDialog()
    {
        var cat = CreateCategory("ToDelete", "Expense");
        this._sut.ConfirmDeleteCategory(cat);

        this._sut.CancelDelete();

        this._sut.ShowDeleteConfirm.ShouldBeFalse();
        this._sut.DeletingCategory.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that DeleteCategoryAsync removes the category from the list on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteCategoryAsync_RemovesCategoryFromList_WhenSuccessful()
    {
        var cat = CreateCategory("ToDelete", "Expense");
        this._apiService.Categories.Add(cat);
        this._apiService.DeleteCategoryResult = true;
        await this._sut.InitializeAsync();
        this._sut.ConfirmDeleteCategory(cat);

        await this._sut.DeleteCategoryAsync();

        this._sut.Categories.ShouldNotContain(c => c.Id == cat.Id);
        this._sut.ShowDeleteConfirm.ShouldBeFalse();
        this._sut.DeletingCategory.ShouldBeNull();
        this._sut.IsDeleting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that DeleteCategoryAsync returns early when no deleting category is set.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteCategoryAsync_ReturnsEarly_WhenNoDeletingCategory()
    {
        await this._sut.DeleteCategoryAsync();

        this._sut.IsDeleting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that DeleteCategoryAsync sets error when API returns false.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteCategoryAsync_SetsError_WhenApiFails()
    {
        var cat = CreateCategory("ToDelete", "Expense");
        this._apiService.Categories.Add(cat);
        this._apiService.DeleteCategoryResult = false;
        await this._sut.InitializeAsync();
        this._sut.ConfirmDeleteCategory(cat);

        await this._sut.DeleteCategoryAsync();

        this._sut.ErrorMessage.ShouldBe("Failed to delete category.");
        this._sut.IsDeleting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that DeleteCategoryAsync sets error when API throws.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteCategoryAsync_SetsError_WhenApiThrows()
    {
        var cat = CreateCategory("ToDelete", "Expense");
        this._apiService.Categories.Add(cat);
        this._apiService.DeleteCategoryException = new HttpRequestException("Network error");
        await this._sut.InitializeAsync();
        this._sut.ConfirmDeleteCategory(cat);

        await this._sut.DeleteCategoryAsync();

        this._sut.ErrorMessage!.ShouldContain("Failed to delete category");
        this._sut.IsDeleting.ShouldBeFalse();
    }

    // --- Activate / Deactivate ---

    /// <summary>
    /// Verifies that ActivateCategoryAsync refreshes the category on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ActivateCategoryAsync_RefreshesCategoryInList_WhenSuccessful()
    {
        var cat = CreateCategory("Inactive", "Expense", isActive: false);
        var refreshed = CreateCategory("Inactive", "Expense", isActive: true);
        refreshed.Id = cat.Id;
        this._apiService.Categories.Add(cat);
        this._apiService.ActivateCategoryResult = true;
        this._apiService.GetCategoryResult = refreshed;
        await this._sut.InitializeAsync();

        await this._sut.ActivateCategoryAsync(cat);

        this._sut.Categories[0].IsActive.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that ActivateCategoryAsync sets error when API returns false.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ActivateCategoryAsync_SetsError_WhenApiFails()
    {
        var cat = CreateCategory("Cat", "Expense");
        this._apiService.Categories.Add(cat);
        this._apiService.ActivateCategoryResult = false;
        await this._sut.InitializeAsync();

        await this._sut.ActivateCategoryAsync(cat);

        this._sut.ErrorMessage.ShouldBe("Failed to activate category.");
    }

    /// <summary>
    /// Verifies that ActivateCategoryAsync sets error when API throws.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ActivateCategoryAsync_SetsError_WhenApiThrows()
    {
        var cat = CreateCategory("Cat", "Expense");
        this._apiService.Categories.Add(cat);
        this._apiService.ActivateCategoryException = new HttpRequestException("fail");
        await this._sut.InitializeAsync();

        await this._sut.ActivateCategoryAsync(cat);

        this._sut.ErrorMessage!.ShouldContain("Failed to activate category");
    }

    /// <summary>
    /// Verifies that DeactivateCategoryAsync refreshes the category on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeactivateCategoryAsync_RefreshesCategoryInList_WhenSuccessful()
    {
        var cat = CreateCategory("Active", "Expense");
        var refreshed = CreateCategory("Active", "Expense", isActive: false);
        refreshed.Id = cat.Id;
        this._apiService.Categories.Add(cat);
        this._apiService.DeactivateCategoryResult = true;
        this._apiService.GetCategoryResult = refreshed;
        await this._sut.InitializeAsync();

        await this._sut.DeactivateCategoryAsync(cat);

        this._sut.Categories[0].IsActive.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that DeactivateCategoryAsync sets error when API returns false.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeactivateCategoryAsync_SetsError_WhenApiFails()
    {
        var cat = CreateCategory("Cat", "Expense");
        this._apiService.Categories.Add(cat);
        this._apiService.DeactivateCategoryResult = false;
        await this._sut.InitializeAsync();

        await this._sut.DeactivateCategoryAsync(cat);

        this._sut.ErrorMessage.ShouldBe("Failed to deactivate category.");
    }

    /// <summary>
    /// Verifies that DeactivateCategoryAsync sets error when API throws.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeactivateCategoryAsync_SetsError_WhenApiThrows()
    {
        var cat = CreateCategory("Cat", "Expense");
        this._apiService.Categories.Add(cat);
        this._apiService.DeactivateCategoryException = new HttpRequestException("fail");
        await this._sut.InitializeAsync();

        await this._sut.DeactivateCategoryAsync(cat);

        this._sut.ErrorMessage!.ShouldContain("Failed to deactivate category");
    }

    // --- Scope Change ---

    /// <summary>
    /// Verifies that scope change triggers a reload of categories.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ScopeChange_ReloadsCategories()
    {
        await this._sut.InitializeAsync();
        this._apiService.Categories.Add(CreateCategory("New After Scope", "Expense"));

        await this._scopeService.SetScopeAsync(BudgetScope.Personal);

        // Allow the async void handler to complete
        await Task.Delay(50);

        this._sut.Categories.Count.ShouldBe(1);
        this._sut.Categories[0].Name.ShouldBe("New After Scope");
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

        // After dispose, scope change should not reload
        this._apiService.Categories.Add(CreateCategory("Should Not Load", "Expense"));
        await this._scopeService.SetScopeAsync(BudgetScope.Shared);
        await Task.Delay(50);

        this._sut.Categories.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that Dispose clears the chat context.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Dispose_ClearsChatContext()
    {
        await this._sut.InitializeAsync();
        this._chatContext.CurrentContext.PageType.ShouldBe("categories");

        this._sut.Dispose();

        this._chatContext.CurrentContext.PageType.ShouldBeNull();
    }

    // --- OnStateChanged Callback ---

    /// <summary>
    /// Verifies that OnStateChanged is invoked when state-mutating methods are called.
    /// </summary>
    [Fact]
    public void OnStateChanged_IsInvoked_OnStateMutations()
    {
        int callCount = 0;
        this._sut.OnStateChanged = () => callCount++;

        this._sut.OpenAddCategory();
        this._sut.CloseAddCategory();
        this._sut.DismissError();
        this._sut.ConfirmDeleteCategory(CreateCategory("Cat", "Expense"));
        this._sut.CancelDelete();

        callCount.ShouldBe(5);
    }

    private static BudgetCategoryDto CreateCategory(string name, string type, bool isActive = true, int sortOrder = 0)
    {
        return new BudgetCategoryDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            Icon = "tag",
            Color = "#4CAF50",
            IsActive = isActive,
            SortOrder = sortOrder,
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
