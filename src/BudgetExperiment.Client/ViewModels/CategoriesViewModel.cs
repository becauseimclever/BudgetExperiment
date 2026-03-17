// <copyright file="CategoriesViewModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.ViewModels;

/// <summary>
/// ViewModel for the Categories page. Contains all handler logic, state fields,
/// and computed properties extracted from the Categories.razor @code block.
/// </summary>
public sealed class CategoriesViewModel : IDisposable
{
    private readonly IBudgetApiService _apiService;
    private readonly IToastService _toastService;
    private readonly ScopeService _scopeService;
    private readonly IChatContextService _chatContextService;
    private readonly IApiErrorContext _apiErrorContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoriesViewModel"/> class.
    /// </summary>
    /// <param name="apiService">The budget API service.</param>
    /// <param name="toastService">The toast notification service.</param>
    /// <param name="scopeService">The budget scope service.</param>
    /// <param name="chatContextService">The chat context service.</param>
    /// <param name="apiErrorContext">The API error context for traceId capture.</param>
    public CategoriesViewModel(
        IBudgetApiService apiService,
        IToastService toastService,
        ScopeService scopeService,
        IChatContextService chatContextService,
        IApiErrorContext apiErrorContext)
    {
        this._apiService = apiService;
        this._toastService = toastService;
        this._scopeService = scopeService;
        this._chatContextService = chatContextService;
        this._apiErrorContext = apiErrorContext;
    }

    /// <summary>
    /// Gets or sets the callback to notify the Razor page that state has changed and it should re-render.
    /// </summary>
    public Action? OnStateChanged { get; set; }

    /// <summary>
    /// Gets a value indicating whether categories are loading.
    /// </summary>
    public bool IsLoading { get; private set; } = true;

    /// <summary>
    /// Gets a value indicating whether a retry load is in progress.
    /// </summary>
    public bool IsRetrying { get; private set; }

    /// <summary>
    /// Gets a value indicating whether a form submission is in progress.
    /// </summary>
    public bool IsSubmitting { get; private set; }

    /// <summary>
    /// Gets a value indicating whether a delete operation is in progress.
    /// </summary>
    public bool IsDeleting { get; private set; }

    /// <summary>
    /// Gets the current error message, if any.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the traceId from the API error response that caused the current error, if any.
    /// </summary>
    public string? ErrorTraceId { get; private set; }

    /// <summary>
    /// Gets the list of all categories.
    /// </summary>
    public List<BudgetCategoryDto> Categories { get; private set; } = new();

    /// <summary>
    /// Gets the expense categories filtered and sorted.
    /// </summary>
    public List<BudgetCategoryDto> ExpenseCategories =>
        this.Categories.Where(c => c.Type == "Expense").OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToList();

    /// <summary>
    /// Gets the income categories filtered and sorted.
    /// </summary>
    public List<BudgetCategoryDto> IncomeCategories =>
        this.Categories.Where(c => c.Type == "Income").OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToList();

    /// <summary>
    /// Gets the transfer categories filtered and sorted.
    /// </summary>
    public List<BudgetCategoryDto> TransferCategories =>
        this.Categories.Where(c => c.Type == "Transfer").OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToList();

    /// <summary>
    /// Gets a value indicating whether the add category modal is visible.
    /// </summary>
    public bool ShowAddForm { get; private set; }

    /// <summary>
    /// Gets or sets the new category form model.
    /// </summary>
    public BudgetCategoryCreateDto NewCategory { get; set; } = new() { Type = "Expense", Color = "#4CAF50" };

    /// <summary>
    /// Gets a value indicating whether the edit category modal is visible.
    /// </summary>
    public bool ShowEditForm { get; private set; }

    /// <summary>
    /// Gets or sets the edit category form model.
    /// </summary>
    public BudgetCategoryCreateDto EditCategory { get; set; } = new();

    /// <summary>
    /// Gets the ID of the category currently being edited.
    /// </summary>
    public Guid? EditingCategoryId { get; private set; }

    /// <summary>
    /// Gets the concurrency version of the category being edited.
    /// </summary>
    public string? EditingVersion { get; private set; }

    /// <summary>
    /// Gets or sets the sort order of the category being edited.
    /// </summary>
    public int EditSortOrder { get; set; }

    /// <summary>
    /// Gets a value indicating whether the delete confirmation dialog is visible.
    /// </summary>
    public bool ShowDeleteConfirm { get; private set; }

    /// <summary>
    /// Gets the category pending deletion.
    /// </summary>
    public BudgetCategoryDto? DeletingCategory { get; private set; }

    /// <summary>
    /// Initializes the ViewModel: subscribes to scope changes and loads categories.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task InitializeAsync()
    {
        this._scopeService.ScopeChanged += this.OnScopeChanged;
        this._chatContextService.SetPageType("categories");
        await this.LoadCategoriesAsync();
    }

    /// <summary>
    /// Loads categories from the API.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task LoadCategoriesAsync()
    {
        this.IsLoading = true;
        this.ErrorMessage = null;
        this.ErrorTraceId = null;

        try
        {
            this.Categories = (await this._apiService.GetCategoriesAsync(false)).ToList();
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to load categories: {ex.Message}";
            this.ErrorTraceId = this._apiErrorContext.LastTraceId;
        }
        finally
        {
            this.IsLoading = false;
        }
    }

    /// <summary>
    /// Retries loading categories after a failure.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task RetryLoadAsync()
    {
        this.IsRetrying = true;
        this.NotifyStateChanged();

        try
        {
            await this.LoadCategoriesAsync();
        }
        finally
        {
            this.IsRetrying = false;
        }
    }

    /// <summary>
    /// Dismisses the current error message.
    /// </summary>
    public void DismissError()
    {
        this.ErrorMessage = null;
        this.ErrorTraceId = null;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Opens the add category form.
    /// </summary>
    public void OpenAddCategory()
    {
        this.NewCategory = new BudgetCategoryCreateDto { Type = "Expense", Color = "#4CAF50" };
        this.ShowAddForm = true;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Closes the add category form.
    /// </summary>
    public void CloseAddCategory()
    {
        this.ShowAddForm = false;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Creates a new category via the API.
    /// </summary>
    /// <param name="model">The category creation data.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task CreateCategoryAsync(BudgetCategoryCreateDto model)
    {
        this.IsSubmitting = true;

        try
        {
            var created = await this._apiService.CreateCategoryAsync(model);
            if (created != null)
            {
                this.Categories.Add(created);
                this.ShowAddForm = false;
            }
            else
            {
                this.ErrorMessage = "Failed to create category.";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to create category: {ex.Message}";
        }
        finally
        {
            this.IsSubmitting = false;
        }
    }

    /// <summary>
    /// Opens the edit category form for the specified category.
    /// </summary>
    /// <param name="category">The category to edit.</param>
    public void OpenEditCategory(BudgetCategoryDto category)
    {
        this.EditingCategoryId = category.Id;
        this.EditingVersion = category.Version;
        this.EditCategory = new BudgetCategoryCreateDto
        {
            Name = category.Name,
            Type = category.Type,
            Icon = category.Icon,
            Color = category.Color ?? "#4CAF50",
        };
        this.EditSortOrder = category.SortOrder;
        this.ShowEditForm = true;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Closes the edit category form.
    /// </summary>
    public void CloseEditCategory()
    {
        this.ShowEditForm = false;
        this.EditingCategoryId = null;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Updates a category via the API.
    /// </summary>
    /// <param name="model">The category update data.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task UpdateCategoryAsync(BudgetCategoryCreateDto model)
    {
        if (this.EditingCategoryId is null)
        {
            return;
        }

        this.IsSubmitting = true;

        try
        {
            var updateDto = new BudgetCategoryUpdateDto
            {
                Name = model.Name,
                Icon = model.Icon,
                Color = model.Color,
                SortOrder = this.EditSortOrder,
            };

            var updated = await this._apiService.UpdateCategoryAsync(this.EditingCategoryId.Value, updateDto, this.EditingVersion);
            if (updated.IsConflict)
            {
                this._toastService.ShowWarning("This category was modified by another user. Data has been refreshed.", "Conflict");
                this.CloseEditCategory();
                await this.LoadCategoriesAsync();
                return;
            }

            if (updated.IsSuccess)
            {
                var index = this.Categories.FindIndex(c => c.Id == this.EditingCategoryId.Value);
                if (index >= 0)
                {
                    this.Categories[index] = updated.Data!;
                }

                this.ShowEditForm = false;
                this.EditingCategoryId = null;
            }
            else
            {
                this.ErrorMessage = "Failed to update category.";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to update category: {ex.Message}";
        }
        finally
        {
            this.IsSubmitting = false;
        }
    }

    /// <summary>
    /// Shows the delete confirmation dialog for the specified category.
    /// </summary>
    /// <param name="category">The category to delete.</param>
    public void ConfirmDeleteCategory(BudgetCategoryDto category)
    {
        this.DeletingCategory = category;
        this.ShowDeleteConfirm = true;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Cancels the delete confirmation.
    /// </summary>
    public void CancelDelete()
    {
        this.ShowDeleteConfirm = false;
        this.DeletingCategory = null;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Deletes the category pending deletion via the API.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task DeleteCategoryAsync()
    {
        if (this.DeletingCategory is null)
        {
            return;
        }

        this.IsDeleting = true;

        try
        {
            var success = await this._apiService.DeleteCategoryAsync(this.DeletingCategory.Id);
            if (success)
            {
                this.Categories.RemoveAll(c => c.Id == this.DeletingCategory.Id);
                this.ShowDeleteConfirm = false;
                this.DeletingCategory = null;
            }
            else
            {
                this.ErrorMessage = "Failed to delete category.";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to delete category: {ex.Message}";
        }
        finally
        {
            this.IsDeleting = false;
        }
    }

    /// <summary>
    /// Activates a category via the API and refreshes it in the list.
    /// </summary>
    /// <param name="category">The category to activate.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task ActivateCategoryAsync(BudgetCategoryDto category)
    {
        try
        {
            var success = await this._apiService.ActivateCategoryAsync(category.Id);
            if (success)
            {
                var index = this.Categories.FindIndex(c => c.Id == category.Id);
                if (index >= 0)
                {
                    var updated = await this._apiService.GetCategoryAsync(category.Id);
                    if (updated != null)
                    {
                        this.Categories[index] = updated;
                    }
                }
            }
            else
            {
                this.ErrorMessage = "Failed to activate category.";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to activate category: {ex.Message}";
        }
    }

    /// <summary>
    /// Deactivates a category via the API and refreshes it in the list.
    /// </summary>
    /// <param name="category">The category to deactivate.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task DeactivateCategoryAsync(BudgetCategoryDto category)
    {
        try
        {
            var success = await this._apiService.DeactivateCategoryAsync(category.Id);
            if (success)
            {
                var index = this.Categories.FindIndex(c => c.Id == category.Id);
                if (index >= 0)
                {
                    var updated = await this._apiService.GetCategoryAsync(category.Id);
                    if (updated != null)
                    {
                        this.Categories[index] = updated;
                    }
                }
            }
            else
            {
                this.ErrorMessage = "Failed to deactivate category.";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to deactivate category: {ex.Message}";
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this._scopeService.ScopeChanged -= this.OnScopeChanged;
        this._chatContextService.ClearContext();
    }

    private async void OnScopeChanged(BudgetScope? scope)
    {
        await this.LoadCategoriesAsync();
        this.NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        this.OnStateChanged?.Invoke();
    }
}
