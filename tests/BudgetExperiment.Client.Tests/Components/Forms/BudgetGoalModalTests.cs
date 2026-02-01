// <copyright file="BudgetGoalModalTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Forms;
using BudgetExperiment.Client.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Forms;

/// <summary>
/// Unit tests for the BudgetGoalModal component.
/// </summary>
public class BudgetGoalModalTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetGoalModalTests"/> class.
    /// </summary>
    public BudgetGoalModalTests()
    {
        // Mock JS interop for ThemeService
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that the modal does not render when not visible.
    /// </summary>
    [Fact]
    public void Modal_DoesNotRender_WhenNotVisible()
    {
        // Arrange & Act
        var cut = Render<BudgetGoalModal>(parameters => parameters
            .Add(p => p.IsVisible, false)
            .Add(p => p.CategoryName, "Test Category")
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Assert - Modal wrapper should not have visible content
        var modals = cut.FindAll(".modal-overlay");
        Assert.Empty(modals);
    }

    /// <summary>
    /// Verifies that the modal displays "Set Budget Goal" title in Create mode.
    /// </summary>
    [Fact]
    public void Modal_DisplaysSetBudgetGoalTitle_InCreateMode()
    {
        // Arrange & Act
        var cut = Render<BudgetGoalModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Mode, GoalEditMode.Create)
            .Add(p => p.CategoryName, "Test Category")
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Assert
        Assert.Contains("Set Budget Goal", cut.Markup);
    }

    /// <summary>
    /// Verifies that the modal displays "Edit Budget Goal" title in Edit mode.
    /// </summary>
    [Fact]
    public void Modal_DisplaysEditBudgetGoalTitle_InEditMode()
    {
        // Arrange & Act
        var cut = Render<BudgetGoalModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Mode, GoalEditMode.Edit)
            .Add(p => p.CategoryName, "Test Category")
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Assert
        Assert.Contains("Edit Budget Goal", cut.Markup);
    }

    /// <summary>
    /// Verifies that the modal displays the category name.
    /// </summary>
    [Fact]
    public void Modal_DisplaysCategoryName()
    {
        // Arrange & Act
        var cut = Render<BudgetGoalModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Mode, GoalEditMode.Create)
            .Add(p => p.CategoryName, "Food & Dining")
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Assert
        Assert.Contains("Food &amp; Dining", cut.Markup);
    }

    /// <summary>
    /// Verifies that the modal displays the month and year.
    /// </summary>
    [Fact]
    public void Modal_DisplaysMonthAndYear()
    {
        // Arrange & Act
        var cut = Render<BudgetGoalModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Mode, GoalEditMode.Create)
            .Add(p => p.CategoryName, "Test")
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Assert
        Assert.Contains("February 2026", cut.Markup);
    }

    /// <summary>
    /// Verifies that the delete button is hidden in Create mode.
    /// </summary>
    [Fact]
    public void Modal_HidesDeleteButton_InCreateMode()
    {
        // Arrange & Act
        var cut = Render<BudgetGoalModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Mode, GoalEditMode.Create)
            .Add(p => p.CategoryName, "Test")
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Assert
        Assert.DoesNotContain("Delete Goal", cut.Markup);
    }

    /// <summary>
    /// Verifies that the delete button is shown in Edit mode.
    /// </summary>
    [Fact]
    public void Modal_ShowsDeleteButton_InEditMode()
    {
        // Arrange & Act
        var cut = Render<BudgetGoalModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Mode, GoalEditMode.Edit)
            .Add(p => p.CategoryName, "Test")
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Assert
        Assert.Contains("Delete Goal", cut.Markup);
    }

    /// <summary>
    /// Verifies that OnSave is called with the correct amount when Save is clicked.
    /// </summary>
    [Fact]
    public void Modal_CallsOnSave_WithCorrectAmount()
    {
        // Arrange
        decimal? savedAmount = null;
        var cut = Render<BudgetGoalModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Mode, GoalEditMode.Create)
            .Add(p => p.CategoryName, "Test")
            .Add(p => p.InitialAmount, 0m)
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2)
            .Add(p => p.OnSave, EventCallback.Factory.Create<decimal>(this, a => savedAmount = a)));

        // Act - Set an amount and click save
        var input = cut.Find("input[type='number']");
        input.Change("250");

        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("Save"));
        saveButton.Click();

        // Assert
        Assert.Equal(250m, savedAmount);
    }

    /// <summary>
    /// Verifies that validation message is shown when amount is zero.
    /// </summary>
    [Fact]
    public void Modal_ShowsValidation_WhenAmountIsZero()
    {
        // Arrange
        var cut = Render<BudgetGoalModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Mode, GoalEditMode.Create)
            .Add(p => p.CategoryName, "Test")
            .Add(p => p.InitialAmount, 0m)
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Act - Click save without entering an amount
        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("Save"));
        saveButton.Click();

        // Assert
        Assert.Contains("Please enter a budget amount", cut.Markup);
    }

    /// <summary>
    /// Verifies that OnClose is called when Cancel is clicked.
    /// </summary>
    [Fact]
    public void Modal_CallsOnClose_WhenCancelClicked()
    {
        // Arrange
        var closeCalled = false;
        var cut = Render<BudgetGoalModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Mode, GoalEditMode.Create)
            .Add(p => p.CategoryName, "Test")
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2)
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => closeCalled = true)));

        // Act
        var cancelButton = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
        cancelButton.Click();

        // Assert
        Assert.True(closeCalled);
    }

    /// <summary>
    /// Verifies that the initial amount is pre-populated in the input.
    /// </summary>
    [Fact]
    public void Modal_PrePopulatesInitialAmount()
    {
        // Arrange & Act
        var cut = Render<BudgetGoalModal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Mode, GoalEditMode.Edit)
            .Add(p => p.CategoryName, "Test")
            .Add(p => p.InitialAmount, 350.50m)
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Assert
        var input = cut.Find("input[type='number']");
        Assert.Equal("350.50", input.GetAttribute("value"));
    }
}
