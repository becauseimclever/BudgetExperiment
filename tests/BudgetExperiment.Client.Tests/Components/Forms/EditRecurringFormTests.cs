// <copyright file="EditRecurringFormTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Forms;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Forms;

/// <summary>
/// Unit tests for the <see cref="EditRecurringForm"/> component.
/// </summary>
public sealed class EditRecurringFormTests : BunitContext, IAsyncLifetime
{
    private readonly Guid _testCategoryId = Guid.NewGuid();

    /// <summary>
    /// Initializes a new instance of the <see cref="EditRecurringFormTests"/> class.
    /// </summary>
    public EditRecurringFormTests()
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
        var cut = RenderEditRecurringForm();

        // Assert
        Assert.NotNull(cut.Find("#description"));
        Assert.NotNull(cut.Find("#amount"));
        Assert.NotNull(cut.Find("#endDate"));
        Assert.NotNull(cut.Find("#editRecCategory"));
    }

    /// <summary>
    /// Verifies that the end date converts from DateOnly to DateTime for display.
    /// </summary>
    [Fact]
    public void Render_WithEndDate_ShowsEndDateValue()
    {
        // Arrange
        var model = new RecurringTransactionUpdateDto
        {
            Description = "Test",
            Amount = new MoneyDto { Amount = 50m, Currency = "USD" },
            EndDate = new DateOnly(2026, 12, 31),
        };

        // Act
        var cut = Render<EditRecurringForm>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.Categories, CreateTestCategories()));

        // Assert
        var endDateInput = cut.Find("#endDate");
        Assert.Equal("2026-12-31", endDateInput.GetAttribute("value"));
    }

    /// <summary>
    /// Verifies that submitting converts DateTime inputs back and invokes OnSubmit.
    /// </summary>
    [Fact]
    public void Submit_ConvertsAndInvokesOnSubmit()
    {
        // Arrange
        RecurringTransactionUpdateDto? submitted = null;
        var model = new RecurringTransactionUpdateDto
        {
            Description = "Rent",
            Amount = new MoneyDto { Amount = 1500m, Currency = "USD" },
        };

        var cut = Render<EditRecurringForm>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.Categories, CreateTestCategories())
            .Add(x => x.OnSubmit, (RecurringTransactionUpdateDto dto) => submitted = dto));

        // Act
        cut.Find("form").Submit();

        // Assert
        Assert.NotNull(submitted);
        Assert.Equal("Rent", submitted!.Description);
        Assert.Equal(1500m, submitted.Amount.Amount);
    }

    /// <summary>
    /// Verifies that only active categories appear in the dropdown.
    /// </summary>
    [Fact]
    public void Render_OnlyShowsActiveCategories()
    {
        // Arrange
        var categories = new List<BudgetCategoryDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Active", IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Inactive", IsActive = false },
        };

        // Act
        var cut = Render<EditRecurringForm>(p => p
            .Add(x => x.Model, new RecurringTransactionUpdateDto { Amount = new MoneyDto { Amount = 0, Currency = "USD" } })
            .Add(x => x.Categories, categories));

        // Assert
        var categorySelect = cut.Find("#editRecCategory");
        var options = categorySelect.QuerySelectorAll("option");

        // "None" + 1 active category
        Assert.Equal(2, options.Length);
        Assert.Contains("Active", options[1].TextContent);
    }

    /// <summary>
    /// Verifies that clicking cancel invokes OnCancel.
    /// </summary>
    [Fact]
    public void ClickCancel_InvokesOnCancel()
    {
        // Arrange
        var cancelCalled = false;
        var cut = Render<EditRecurringForm>(p => p
            .Add(x => x.Model, new RecurringTransactionUpdateDto { Amount = new MoneyDto { Amount = 0, Currency = "USD" } })
            .Add(x => x.Categories, CreateTestCategories())
            .Add(x => x.OnCancel, () => cancelCalled = true));

        // Act
        var cancelBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
        cancelBtn.Click();

        // Assert
        Assert.True(cancelCalled);
    }

    /// <summary>
    /// Verifies that submitting with null end date sets EndDate to null on model.
    /// </summary>
    [Fact]
    public void Submit_WithNoEndDate_SetsEndDateNull()
    {
        // Arrange
        RecurringTransactionUpdateDto? submitted = null;
        var model = new RecurringTransactionUpdateDto
        {
            Description = "Test",
            Amount = new MoneyDto { Amount = 50m, Currency = "USD" },
            EndDate = null,
        };

        var cut = Render<EditRecurringForm>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.Categories, CreateTestCategories())
            .Add(x => x.OnSubmit, (RecurringTransactionUpdateDto dto) => submitted = dto));

        // Act
        cut.Find("form").Submit();

        // Assert
        Assert.NotNull(submitted);
        Assert.Null(submitted!.EndDate);
    }

    private IRenderedComponent<EditRecurringForm> RenderEditRecurringForm()
    {
        return Render<EditRecurringForm>(p => p
            .Add(x => x.Model, new RecurringTransactionUpdateDto
            {
                Description = "Test",
                Amount = new MoneyDto { Amount = 100m, Currency = "USD" },
            })
            .Add(x => x.Categories, CreateTestCategories()));
    }

    private IReadOnlyList<BudgetCategoryDto> CreateTestCategories()
    {
        return new List<BudgetCategoryDto>
        {
            new() { Id = _testCategoryId, Name = "Utilities", IsActive = true },
        };
    }
}
