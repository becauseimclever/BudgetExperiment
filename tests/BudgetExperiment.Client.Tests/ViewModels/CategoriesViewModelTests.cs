// <copyright file="CategoriesViewModelTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Client.ViewModels;
using BudgetExperiment.Contracts.Dtos;

using Shouldly;

namespace BudgetExperiment.Client.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="CategoriesViewModel"/>.
/// </summary>
public sealed class CategoriesViewModelTests : IDisposable
{
    private readonly StubBudgetApiService _apiService = new();
    private readonly StubToastService _toastService = new();
    private readonly StubChatContextService _chatContext = new();
    private readonly StubApiErrorContext _apiErrorContext = new();
    private readonly CategoriesViewModel _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoriesViewModelTests"/> class.
    /// </summary>
    public CategoriesViewModelTests()
    {
        _sut = new CategoriesViewModel(
            _apiService,
            _toastService,
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
    /// Verifies that InitializeAsync loads categories from the API.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsCategories()
    {
        _apiService.Categories.Add(CreateCategory("Groceries", "Expense"));

        await _sut.InitializeAsync();

        _sut.Categories.Count.ShouldBe(1);
        _sut.Categories[0].Name.ShouldBe("Groceries");
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

        _chatContext.CurrentContext.PageType.ShouldBe("categories");
    }

    /// <summary>
    /// Verifies that InitializeAsync handles API failure gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsErrorMessage_WhenApiFails()
    {
        _apiService.GetCategoriesException = new HttpRequestException("Server error");

        await _sut.InitializeAsync();

        _sut.ErrorMessage.ShouldNotBeNull();
        _sut.ErrorMessage!.ShouldContain("Failed to load categories");
        _sut.IsLoading.ShouldBeFalse();
    }

    // --- LoadCategoriesAsync ---

    /// <summary>
    /// Verifies that LoadCategoriesAsync clears the error message before loading.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LoadCategoriesAsync_ClearsErrorMessage()
    {
        _apiService.GetCategoriesException = new HttpRequestException("fail");
        await _sut.InitializeAsync();
        _sut.ErrorMessage.ShouldNotBeNull();

        _apiService.GetCategoriesException = null;
        await _sut.LoadCategoriesAsync();

        _sut.ErrorMessage.ShouldBeNull();
    }

    // --- RetryLoadAsync ---

    /// <summary>
    /// Verifies that RetryLoadAsync reloads categories.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RetryLoadAsync_ReloadsCategories()
    {
        await _sut.InitializeAsync();
        _apiService.Categories.Add(CreateCategory("NewCat", "Expense"));

        await _sut.RetryLoadAsync();

        _sut.Categories.Count.ShouldBe(1);
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
        _apiService.GetCategoriesException = new HttpRequestException("fail");
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

    // --- Computed Properties ---

    /// <summary>
    /// Verifies that ExpenseCategories filters only expense type categories.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExpenseCategories_FiltersExpenseType()
    {
        _apiService.Categories.Add(CreateCategory("Groceries", "Expense"));
        _apiService.Categories.Add(CreateCategory("Salary", "Income"));
        await _sut.InitializeAsync();

        _sut.ExpenseCategories.Count.ShouldBe(1);
        _sut.ExpenseCategories[0].Name.ShouldBe("Groceries");
    }

    /// <summary>
    /// Verifies that IncomeCategories filters only income type categories.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task IncomeCategories_FiltersIncomeType()
    {
        _apiService.Categories.Add(CreateCategory("Groceries", "Expense"));
        _apiService.Categories.Add(CreateCategory("Salary", "Income"));
        await _sut.InitializeAsync();

        _sut.IncomeCategories.Count.ShouldBe(1);
        _sut.IncomeCategories[0].Name.ShouldBe("Salary");
    }

    /// <summary>
    /// Verifies that TransferCategories filters only transfer type categories.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TransferCategories_FiltersTransferType()
    {
        _apiService.Categories.Add(CreateCategory("Transfer Out", "Transfer"));
        _apiService.Categories.Add(CreateCategory("Groceries", "Expense"));
        await _sut.InitializeAsync();

        _sut.TransferCategories.Count.ShouldBe(1);
        _sut.TransferCategories[0].Name.ShouldBe("Transfer Out");
    }

    /// <summary>
    /// Verifies that computed categories are sorted by SortOrder then Name.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ComputedCategories_SortBySortOrderThenName()
    {
        _apiService.Categories.Add(CreateCategory("Utilities", "Expense", sortOrder: 2));
        _apiService.Categories.Add(CreateCategory("Groceries", "Expense", sortOrder: 1));
        _apiService.Categories.Add(CreateCategory("Dining", "Expense", sortOrder: 1));
        await _sut.InitializeAsync();

        var expenses = _sut.ExpenseCategories;
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
        _sut.OpenAddCategory();

        _sut.ShowAddForm.ShouldBeTrue();
        _sut.NewCategory.Type.ShouldBe("Expense");
        _sut.NewCategory.Color.ShouldBe("#4CAF50");
    }

    /// <summary>
    /// Verifies that CloseAddCategory hides the add form.
    /// </summary>
    [Fact]
    public void CloseAddCategory_HidesForm()
    {
        _sut.OpenAddCategory();

        _sut.CloseAddCategory();

        _sut.ShowAddForm.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies state change notification on OpenAddCategory.
    /// </summary>
    [Fact]
    public void OpenAddCategory_NotifiesStateChanged()
    {
        bool notified = false;
        _sut.OnStateChanged = () => notified = true;

        _sut.OpenAddCategory();

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
        _apiService.CreateCategoryResult = created;
        await _sut.InitializeAsync();
        _sut.OpenAddCategory();

        await _sut.CreateCategoryAsync(new BudgetCategoryCreateDto { Name = "Groceries", Type = "Expense" });

        _sut.Categories.ShouldContain(c => c.Name == "Groceries");
        _sut.ShowAddForm.ShouldBeFalse();
        _sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that CreateCategoryAsync sets error when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateCategoryAsync_SetsError_WhenApiReturnsNull()
    {
        _apiService.CreateCategoryResult = null;
        await _sut.InitializeAsync();

        await _sut.CreateCategoryAsync(new BudgetCategoryCreateDto { Name = "Fail" });

        _sut.ErrorMessage.ShouldBe("Failed to create category.");
        _sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that CreateCategoryAsync sets error when API throws.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateCategoryAsync_SetsError_WhenApiThrows()
    {
        _apiService.CreateCategoryException = new HttpRequestException("Network error");
        await _sut.InitializeAsync();

        await _sut.CreateCategoryAsync(new BudgetCategoryCreateDto { Name = "Fail" });

        _sut.ErrorMessage!.ShouldContain("Failed to create category");
        _sut.ErrorMessage!.ShouldContain("Network error");
        _sut.IsSubmitting.ShouldBeFalse();
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

        _sut.OpenEditCategory(cat);

        _sut.ShowEditForm.ShouldBeTrue();
        _sut.EditingCategoryId.ShouldBe(cat.Id);
        _sut.EditingVersion.ShouldBe("v1");
        _sut.EditCategory.Name.ShouldBe("Groceries");
        _sut.EditCategory.Type.ShouldBe("Expense");
        _sut.EditSortOrder.ShouldBe(5);
    }

    /// <summary>
    /// Verifies that OpenEditCategory uses default color when category has null color.
    /// </summary>
    [Fact]
    public void OpenEditCategory_UsesDefaultColor_WhenNullColor()
    {
        var cat = CreateCategory("NullColor", "Expense");
        cat.Color = null;

        _sut.OpenEditCategory(cat);

        _sut.EditCategory.Color.ShouldBe("#4CAF50");
    }

    /// <summary>
    /// Verifies that CloseEditCategory resets edit state.
    /// </summary>
    [Fact]
    public void CloseEditCategory_ResetsEditState()
    {
        var cat = CreateCategory("Groceries", "Expense");
        _sut.OpenEditCategory(cat);

        _sut.CloseEditCategory();

        _sut.ShowEditForm.ShouldBeFalse();
        _sut.EditingCategoryId.ShouldBeNull();
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
        _apiService.Categories.Add(cat);
        await _sut.InitializeAsync();

        var updatedCat = CreateCategory("Groceries Updated", "Expense");
        updatedCat.Id = cat.Id;
        _apiService.UpdateCategoryResult = ApiResult<BudgetCategoryDto>.Success(updatedCat);
        _sut.OpenEditCategory(cat);

        await _sut.UpdateCategoryAsync(new BudgetCategoryCreateDto { Name = "Groceries Updated" });

        _sut.Categories.ShouldContain(c => c.Name == "Groceries Updated");
        _sut.ShowEditForm.ShouldBeFalse();
        _sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that UpdateCategoryAsync returns early when no editing category is set.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateCategoryAsync_ReturnsEarly_WhenNoEditingCategory()
    {
        await _sut.UpdateCategoryAsync(new BudgetCategoryCreateDto());

        _sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that UpdateCategoryAsync handles conflict (409) correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateCategoryAsync_HandlesConflict()
    {
        var cat = CreateCategory("Groceries", "Expense");
        _apiService.Categories.Add(cat);
        await _sut.InitializeAsync();

        _apiService.UpdateCategoryResult = ApiResult<BudgetCategoryDto>.Conflict();
        _sut.OpenEditCategory(cat);

        await _sut.UpdateCategoryAsync(new BudgetCategoryCreateDto { Name = "Conflict" });

        _toastService.WarningShown.ShouldBeTrue();
        _sut.ShowEditForm.ShouldBeFalse();
        _sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that UpdateCategoryAsync sets error when API returns failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateCategoryAsync_SetsError_WhenApiReturnsFailure()
    {
        var cat = CreateCategory("Groceries", "Expense");
        _apiService.Categories.Add(cat);
        await _sut.InitializeAsync();

        _apiService.UpdateCategoryResult = ApiResult<BudgetCategoryDto>.Failure();
        _sut.OpenEditCategory(cat);

        await _sut.UpdateCategoryAsync(new BudgetCategoryCreateDto { Name = "Fail" });

        _sut.ErrorMessage.ShouldBe("Failed to update category.");
        _sut.IsSubmitting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that UpdateCategoryAsync sets error when API throws.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateCategoryAsync_SetsError_WhenApiThrows()
    {
        var cat = CreateCategory("Groceries", "Expense");
        _apiService.Categories.Add(cat);
        await _sut.InitializeAsync();

        _apiService.UpdateCategoryException = new HttpRequestException("Network error");
        _sut.OpenEditCategory(cat);

        await _sut.UpdateCategoryAsync(new BudgetCategoryCreateDto { Name = "Fail" });

        _sut.ErrorMessage!.ShouldContain("Failed to update category");
        _sut.IsSubmitting.ShouldBeFalse();
    }

    // --- Delete Category ---

    /// <summary>
    /// Verifies that ConfirmDeleteCategory shows the delete confirmation dialog.
    /// </summary>
    [Fact]
    public void ConfirmDeleteCategory_ShowsDeleteDialog()
    {
        var cat = CreateCategory("ToDelete", "Expense");

        _sut.ConfirmDeleteCategory(cat);

        _sut.ShowDeleteConfirm.ShouldBeTrue();
        _sut.DeletingCategory.ShouldBe(cat);
    }

    /// <summary>
    /// Verifies that CancelDelete hides the delete confirmation dialog.
    /// </summary>
    [Fact]
    public void CancelDelete_HidesDeleteDialog()
    {
        var cat = CreateCategory("ToDelete", "Expense");
        _sut.ConfirmDeleteCategory(cat);

        _sut.CancelDelete();

        _sut.ShowDeleteConfirm.ShouldBeFalse();
        _sut.DeletingCategory.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that DeleteCategoryAsync removes the category from the list on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteCategoryAsync_RemovesCategoryFromList_WhenSuccessful()
    {
        var cat = CreateCategory("ToDelete", "Expense");
        _apiService.Categories.Add(cat);
        _apiService.DeleteCategoryResult = true;
        await _sut.InitializeAsync();
        _sut.ConfirmDeleteCategory(cat);

        await _sut.DeleteCategoryAsync();

        _sut.Categories.ShouldNotContain(c => c.Id == cat.Id);
        _sut.ShowDeleteConfirm.ShouldBeFalse();
        _sut.DeletingCategory.ShouldBeNull();
        _sut.IsDeleting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that DeleteCategoryAsync returns early when no deleting category is set.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteCategoryAsync_ReturnsEarly_WhenNoDeletingCategory()
    {
        await _sut.DeleteCategoryAsync();

        _sut.IsDeleting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that DeleteCategoryAsync sets error when API returns false.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteCategoryAsync_SetsError_WhenApiFails()
    {
        var cat = CreateCategory("ToDelete", "Expense");
        _apiService.Categories.Add(cat);
        _apiService.DeleteCategoryResult = false;
        await _sut.InitializeAsync();
        _sut.ConfirmDeleteCategory(cat);

        await _sut.DeleteCategoryAsync();

        _sut.ErrorMessage.ShouldBe("Failed to delete category.");
        _sut.IsDeleting.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that DeleteCategoryAsync sets error when API throws.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteCategoryAsync_SetsError_WhenApiThrows()
    {
        var cat = CreateCategory("ToDelete", "Expense");
        _apiService.Categories.Add(cat);
        _apiService.DeleteCategoryException = new HttpRequestException("Network error");
        await _sut.InitializeAsync();
        _sut.ConfirmDeleteCategory(cat);

        await _sut.DeleteCategoryAsync();

        _sut.ErrorMessage!.ShouldContain("Failed to delete category");
        _sut.IsDeleting.ShouldBeFalse();
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
        _apiService.Categories.Add(cat);
        _apiService.ActivateCategoryResult = true;
        _apiService.GetCategoryResult = refreshed;
        await _sut.InitializeAsync();

        await _sut.ActivateCategoryAsync(cat);

        _sut.Categories[0].IsActive.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that ActivateCategoryAsync sets error when API returns false.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ActivateCategoryAsync_SetsError_WhenApiFails()
    {
        var cat = CreateCategory("Cat", "Expense");
        _apiService.Categories.Add(cat);
        _apiService.ActivateCategoryResult = false;
        await _sut.InitializeAsync();

        await _sut.ActivateCategoryAsync(cat);

        _sut.ErrorMessage.ShouldBe("Failed to activate category.");
    }

    /// <summary>
    /// Verifies that ActivateCategoryAsync sets error when API throws.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ActivateCategoryAsync_SetsError_WhenApiThrows()
    {
        var cat = CreateCategory("Cat", "Expense");
        _apiService.Categories.Add(cat);
        _apiService.ActivateCategoryException = new HttpRequestException("fail");
        await _sut.InitializeAsync();

        await _sut.ActivateCategoryAsync(cat);

        _sut.ErrorMessage!.ShouldContain("Failed to activate category");
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
        _apiService.Categories.Add(cat);
        _apiService.DeactivateCategoryResult = true;
        _apiService.GetCategoryResult = refreshed;
        await _sut.InitializeAsync();

        await _sut.DeactivateCategoryAsync(cat);

        _sut.Categories[0].IsActive.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that DeactivateCategoryAsync sets error when API returns false.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeactivateCategoryAsync_SetsError_WhenApiFails()
    {
        var cat = CreateCategory("Cat", "Expense");
        _apiService.Categories.Add(cat);
        _apiService.DeactivateCategoryResult = false;
        await _sut.InitializeAsync();

        await _sut.DeactivateCategoryAsync(cat);

        _sut.ErrorMessage.ShouldBe("Failed to deactivate category.");
    }

    /// <summary>
    /// Verifies that DeactivateCategoryAsync sets error when API throws.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeactivateCategoryAsync_SetsError_WhenApiThrows()
    {
        var cat = CreateCategory("Cat", "Expense");
        _apiService.Categories.Add(cat);
        _apiService.DeactivateCategoryException = new HttpRequestException("fail");
        await _sut.InitializeAsync();

        await _sut.DeactivateCategoryAsync(cat);

        _sut.ErrorMessage!.ShouldContain("Failed to deactivate category");
    }

    // --- Dispose ---

    /// <summary>
    /// Verifies that Dispose clears the chat context.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Dispose_ClearsChatContext()
    {
        await _sut.InitializeAsync();
        _chatContext.CurrentContext.PageType.ShouldBe("categories");

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

        _sut.OpenAddCategory();
        _sut.CloseAddCategory();
        _sut.DismissError();
        _sut.ConfirmDeleteCategory(CreateCategory("Cat", "Expense"));
        _sut.CancelDelete();

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
}
