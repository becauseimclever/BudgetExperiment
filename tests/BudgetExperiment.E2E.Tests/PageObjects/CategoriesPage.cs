// <copyright file="CategoriesPage.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.E2E.Tests.PageObjects;

/// <summary>
/// Page object for the Categories page.
/// </summary>
public class CategoriesPage : BasePage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CategoriesPage"/> class.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    public CategoriesPage(IPage page)
        : base(page)
    {
    }

    /// <summary>
    /// Gets the Add Category button.
    /// </summary>
    public ILocator AddCategoryButton => Page.GetByRole(AriaRole.Button, new() { Name = "Add Category" });

    /// <summary>
    /// Gets all category cards/items.
    /// </summary>
    public ILocator CategoryItems => Page.Locator(".category-item, .card");

    /// <summary>
    /// Gets the empty message.
    /// </summary>
    public ILocator EmptyMessage => Page.Locator(".empty-message");

    /// <summary>
    /// Gets the expense categories section.
    /// </summary>
    public ILocator ExpenseSection => Page.Locator("[data-category-type='expense'], .expense-categories");

    /// <summary>
    /// Gets the income categories section.
    /// </summary>
    public ILocator IncomeSection => Page.Locator("[data-category-type='income'], .income-categories");

    /// <summary>
    /// Clicks the Add Category button.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ClickAddCategoryAsync()
    {
        await AddCategoryButton.ClickAsync();
        var modal = new ModalComponent(Page, "Add Category");
        await modal.WaitForVisibleAsync();
    }

    /// <summary>
    /// Creates a new category.
    /// </summary>
    /// <param name="name">Category name.</param>
    /// <param name="type">Category type (Expense/Income).</param>
    /// <param name="monthlyBudget">Optional monthly budget amount.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task CreateCategoryAsync(string name, string type, decimal? monthlyBudget = null)
    {
        await ClickAddCategoryAsync();

        await Page.GetByLabel("Category Name").FillAsync(name);
        await Page.GetByLabel("Type").SelectOptionAsync(type);

        if (monthlyBudget.HasValue)
        {
            await Page.GetByLabel("Monthly Budget").FillAsync(monthlyBudget.Value.ToString());
        }

        await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        var modal = new ModalComponent(Page, "Add Category");
        await modal.WaitForHiddenAsync();
    }

    /// <summary>
    /// Finds a category by name.
    /// </summary>
    /// <param name="name">The category name to find.</param>
    /// <returns>The category item locator.</returns>
    public ILocator GetCategoryByName(string name)
    {
        return CategoryItems.Filter(new() { HasText = name });
    }

    /// <summary>
    /// Gets the count of categories displayed.
    /// </summary>
    /// <returns>The number of category items.</returns>
    public async Task<int> GetCategoryCountAsync()
    {
        return await CategoryItems.CountAsync();
    }
}
