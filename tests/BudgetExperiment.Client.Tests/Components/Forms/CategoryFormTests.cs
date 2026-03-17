// <copyright file="CategoryFormTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Forms;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Forms;

/// <summary>
/// Unit tests for the <see cref="CategoryForm"/> component.
/// </summary>
public sealed class CategoryFormTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryFormTests"/> class.
    /// </summary>
    public CategoryFormTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
        Services.AddSingleton<CultureService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that the form renders all expected fields.
    /// </summary>
    [Fact]
    public void Render_ShowsAllFormFields()
    {
        // Act
        var cut = RenderCategoryForm();

        // Assert
        Assert.NotNull(cut.Find("#categoryName"));
        Assert.NotNull(cut.Find("#categoryType"));
        Assert.NotNull(cut.Find("#categoryIcon"));
        Assert.NotNull(cut.Find("#categoryColor"));
    }

    /// <summary>
    /// Verifies that the Expense type shows the correct help text.
    /// </summary>
    [Fact]
    public void Render_ExpenseType_ShowsExpenseHelpText()
    {
        // Act
        var cut = RenderCategoryForm(model: new BudgetCategoryCreateDto { Type = "Expense" });

        // Assert
        Assert.Contains("Expense categories track spending (money going out)", cut.Markup);
    }

    /// <summary>
    /// Verifies that the Income type shows the correct help text.
    /// </summary>
    [Fact]
    public void Render_IncomeType_ShowsIncomeHelpText()
    {
        // Act
        var cut = RenderCategoryForm(model: new BudgetCategoryCreateDto { Type = "Income" });

        // Assert
        Assert.Contains("Income categories track money coming in", cut.Markup);
    }

    /// <summary>
    /// Verifies that the Transfer type shows the correct help text.
    /// </summary>
    [Fact]
    public void Render_TransferType_ShowsTransferHelpText()
    {
        // Act
        var cut = RenderCategoryForm(model: new BudgetCategoryCreateDto { Type = "Transfer" });

        // Assert
        Assert.Contains("Transfer categories are for internal account transfers", cut.Markup);
    }

    /// <summary>
    /// Verifies that the initial budget field is shown for Expense type when not in edit mode.
    /// </summary>
    [Fact]
    public void Render_ExpenseTypeWithoutSortOrder_ShowsInitialBudgetField()
    {
        // Act
        var cut = Render<CategoryForm>(p => p
            .Add(x => x.Model, new BudgetCategoryCreateDto { Type = "Expense" })
            .Add(x => x.ShowSortOrder, false));

        // Assert
        Assert.NotNull(cut.Find("#initialBudget"));
    }

    /// <summary>
    /// Verifies that the initial budget field is hidden for non-Expense types.
    /// </summary>
    [Fact]
    public void Render_IncomeType_HidesInitialBudgetField()
    {
        // Act
        var cut = Render<CategoryForm>(p => p
            .Add(x => x.Model, new BudgetCategoryCreateDto { Type = "Income" })
            .Add(x => x.ShowSortOrder, false));

        // Assert
        Assert.Empty(cut.FindAll("#initialBudget"));
    }

    /// <summary>
    /// Verifies that the sort order field is shown in edit mode.
    /// </summary>
    [Fact]
    public void Render_ShowSortOrderTrue_ShowsSortOrderField()
    {
        // Act
        var cut = Render<CategoryForm>(p => p
            .Add(x => x.Model, new BudgetCategoryCreateDto { Type = "Expense" })
            .Add(x => x.ShowSortOrder, true));

        // Assert
        Assert.NotNull(cut.Find("#sortOrder"));
    }

    /// <summary>
    /// Verifies that the initial budget field is hidden when ShowSortOrder is true.
    /// </summary>
    [Fact]
    public void Render_ShowSortOrderTrue_HidesInitialBudgetField()
    {
        // Act
        var cut = Render<CategoryForm>(p => p
            .Add(x => x.Model, new BudgetCategoryCreateDto { Type = "Expense" })
            .Add(x => x.ShowSortOrder, true));

        // Assert
        Assert.Empty(cut.FindAll("#initialBudget"));
    }

    /// <summary>
    /// Verifies that submitting invokes OnSubmit callback.
    /// </summary>
    [Fact]
    public void Submit_InvokesOnSubmit()
    {
        // Arrange
        BudgetCategoryCreateDto? submitted = null;
        var cut = RenderCategoryForm(onSubmit: dto => submitted = dto);

        cut.Find("#categoryName").Input("Food");

        // Act
        cut.Find("form").Submit();

        // Assert
        Assert.NotNull(submitted);
        Assert.Equal("Food", submitted!.Name);
    }

    /// <summary>
    /// Verifies that the category type dropdown has three options.
    /// </summary>
    [Fact]
    public void Render_CategoryTypeDropdown_HasThreeOptions()
    {
        // Act
        var cut = RenderCategoryForm();

        // Assert
        var options = cut.Find("#categoryType").QuerySelectorAll("option");
        Assert.Equal(3, options.Length);
        Assert.Equal("Expense", options[0].GetAttribute("value"));
        Assert.Equal("Income", options[1].GetAttribute("value"));
        Assert.Equal("Transfer", options[2].GetAttribute("value"));
    }

    /// <summary>
    /// Verifies that clicking cancel invokes OnCancel.
    /// </summary>
    [Fact]
    public void ClickCancel_InvokesOnCancel()
    {
        // Arrange
        var cancelCalled = false;
        var cut = RenderCategoryForm(onCancel: () => cancelCalled = true);

        // Act
        var cancelBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
        cancelBtn.Click();

        // Assert
        Assert.True(cancelCalled);
    }

    /// <summary>
    /// Verifies that the default submit button text is "Create Category".
    /// </summary>
    [Fact]
    public void Render_DefaultSubmitButtonText_IsCreateCategory()
    {
        // Act
        var cut = RenderCategoryForm();

        // Assert
        Assert.Contains("Create Category", cut.Markup);
    }

    private IRenderedComponent<CategoryForm> RenderCategoryForm(
        Action<BudgetCategoryCreateDto>? onSubmit = null,
        Action? onCancel = null,
        BudgetCategoryCreateDto? model = null)
    {
        return Render<CategoryForm>(p => p
            .Add(x => x.Model, model ?? new BudgetCategoryCreateDto())
            .Add(x => x.OnSubmit, onSubmit ?? (_ => { }))
            .Add(x => x.OnCancel, onCancel ?? (() => { })));
    }
}
