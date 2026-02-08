// <copyright file="QuickAddFormTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Forms;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Forms;

/// <summary>
/// Unit tests for the <see cref="QuickAddForm"/> component.
/// </summary>
public sealed class QuickAddFormTests : BunitContext, IAsyncLifetime
{
    private readonly Guid _testAccountId = Guid.NewGuid();
    private readonly Guid _testCategoryId = Guid.NewGuid();

    /// <summary>
    /// Initializes a new instance of the <see cref="QuickAddFormTests"/> class.
    /// </summary>
    public QuickAddFormTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
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
        var cut = RenderQuickAddForm();

        // Assert
        Assert.NotNull(cut.Find("#qa-description"));
        Assert.NotNull(cut.Find("#qa-amount"));
        Assert.NotNull(cut.Find("#qa-category"));
        Assert.NotNull(cut.Find("#qa-account"));
        Assert.NotNull(cut.Find("#qa-date"));
    }

    /// <summary>
    /// Verifies that the date defaults to today.
    /// </summary>
    [Fact]
    public void Render_DateDefaultsToToday()
    {
        // Act
        var cut = RenderQuickAddForm();

        // Assert
        var dateInput = cut.Find("#qa-date");
        var todayStr = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        Assert.Equal(todayStr, dateInput.GetAttribute("value"));
    }

    /// <summary>
    /// Verifies that accounts are populated in the select dropdown.
    /// </summary>
    [Fact]
    public void Render_PopulatesAccountDropdown()
    {
        // Act
        var cut = RenderQuickAddForm();

        // Assert
        var accountSelect = cut.Find("#qa-account");
        var options = accountSelect.QuerySelectorAll("option");

        // "Select Account" + 1 actual account
        Assert.Equal(2, options.Length);
        Assert.Contains("Test Checking", options[1].TextContent);
    }

    /// <summary>
    /// Verifies that categories are populated in the select dropdown.
    /// </summary>
    [Fact]
    public void Render_PopulatesCategoryDropdown()
    {
        // Act
        var cut = RenderQuickAddForm();

        // Assert
        var categorySelect = cut.Find("#qa-category");
        var options = categorySelect.QuerySelectorAll("option");

        // "None" + 1 actual category
        Assert.Equal(2, options.Length);
        Assert.Contains("Dining", options[1].TextContent);
    }

    /// <summary>
    /// Verifies that inputs have touch-optimized CSS class.
    /// </summary>
    [Fact]
    public void Render_InputsHaveTouchOptimizedClass()
    {
        // Act
        var cut = RenderQuickAddForm();

        // Assert
        var inputs = cut.FindAll(".quick-add-input");
        Assert.Equal(5, inputs.Count);
    }

    /// <summary>
    /// Verifies that the amount input has decimal inputmode.
    /// </summary>
    [Fact]
    public void Render_AmountInput_HasDecimalInputmode()
    {
        // Act
        var cut = RenderQuickAddForm();

        // Assert
        var amountInput = cut.Find("#qa-amount");
        Assert.Equal("decimal", amountInput.GetAttribute("inputmode"));
    }

    /// <summary>
    /// Verifies that description input has text inputmode.
    /// </summary>
    [Fact]
    public void Render_DescriptionInput_HasTextInputmode()
    {
        // Act
        var cut = RenderQuickAddForm();

        // Assert
        var descInput = cut.Find("#qa-description");
        Assert.Equal("text", descInput.GetAttribute("inputmode"));
    }

    /// <summary>
    /// Verifies that OnSubmit fires with valid data.
    /// </summary>
    [Fact]
    public void Submit_WithValidData_InvokesOnSubmit()
    {
        // Arrange
        TransactionCreateDto? submitted = null;
        var cut = RenderQuickAddForm(onSubmit: dto => submitted = dto);

        // Fill in required fields
        cut.Find("#qa-description").Input("Coffee");
        cut.Find("#qa-amount").Change("-5.50");
        cut.Find("#qa-account").Change(_testAccountId.ToString());

        // Act
        cut.Find("form").Submit();

        // Assert
        Assert.NotNull(submitted);
        Assert.Equal("Coffee", submitted!.Description);
        Assert.Equal(-5.50m, submitted.Amount.Amount);
        Assert.Equal(_testAccountId, submitted.AccountId);
    }

    /// <summary>
    /// Verifies that submit is blocked when description is empty.
    /// </summary>
    [Fact]
    public void Submit_WithEmptyDescription_DoesNotInvokeOnSubmit()
    {
        // Arrange
        var submitCalled = false;
        var cut = RenderQuickAddForm(onSubmit: _ => submitCalled = true);

        cut.Find("#qa-amount").Change("-5.50");
        cut.Find("#qa-account").Change(_testAccountId.ToString());

        // Act
        cut.Find("form").Submit();

        // Assert
        Assert.False(submitCalled);
    }

    /// <summary>
    /// Verifies that submit is blocked when no account is selected.
    /// </summary>
    [Fact]
    public void Submit_WithNoAccount_DoesNotInvokeOnSubmit()
    {
        // Arrange
        var submitCalled = false;
        var cut = RenderQuickAddForm(onSubmit: _ => submitCalled = true);

        cut.Find("#qa-description").Input("Coffee");
        cut.Find("#qa-amount").Change("-5.50");

        // Act
        cut.Find("form").Submit();

        // Assert
        Assert.False(submitCalled);
    }

    /// <summary>
    /// Verifies that OnCancel is invoked when cancel button is clicked.
    /// </summary>
    [Fact]
    public void ClickCancel_InvokesOnCancel()
    {
        // Arrange
        var cancelCalled = false;
        var cut = RenderQuickAddForm(onCancel: () => cancelCalled = true);

        // Act - find the cancel button (secondary button)
        var cancelBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
        cancelBtn.Click();

        // Assert
        Assert.True(cancelCalled);
    }

    /// <summary>
    /// Verifies that error message is displayed when set.
    /// </summary>
    [Fact]
    public void Render_WithErrorMessage_DisplaysError()
    {
        // Act
        var cut = Render<QuickAddForm>(p => p
            .Add(x => x.Accounts, CreateTestAccounts())
            .Add(x => x.Categories, CreateTestCategories())
            .Add(x => x.ErrorMessage, "Something went wrong"));

        // Assert
        var errorBox = cut.Find(".form-error-box");
        Assert.Contains("Something went wrong", errorBox.TextContent);
    }

    /// <summary>
    /// Verifies that error message is not shown when null.
    /// </summary>
    [Fact]
    public void Render_WithoutErrorMessage_NoErrorBox()
    {
        // Act
        var cut = RenderQuickAddForm();

        // Assert
        Assert.Empty(cut.FindAll(".form-error-box"));
    }

    /// <summary>
    /// Verifies that save button shows loading state when submitting.
    /// </summary>
    [Fact]
    public void Render_WhenSubmitting_DisablesButtons()
    {
        // Act
        var cut = Render<QuickAddForm>(p => p
            .Add(x => x.Accounts, CreateTestAccounts())
            .Add(x => x.Categories, CreateTestCategories())
            .Add(x => x.IsSubmitting, true));

        // Assert - find submit button (the one that says "Save")
        var buttons = cut.FindAll("button");
        foreach (var button in buttons)
        {
            Assert.True(
                button.HasAttribute("disabled")
                || button.GetAttribute("type") == "button",
                $"Button '{button.TextContent.Trim()}' should be disabled when submitting");
        }
    }

    /// <summary>
    /// Verifies that the form has a category select with a "None" default option.
    /// </summary>
    [Fact]
    public void Render_CategorySelect_HasNoneOption()
    {
        // Act
        var cut = RenderQuickAddForm();

        // Assert
        var categorySelect = cut.Find("#qa-category");
        var firstOption = categorySelect.QuerySelectorAll("option")[0];
        Assert.Equal("None", firstOption.TextContent);
    }

    /// <summary>
    /// Verifies that Reset clears the form for a new entry.
    /// </summary>
    [Fact]
    public void Reset_ClearsFormModel()
    {
        // Arrange
        TransactionCreateDto? submitted = null;
        var cut = RenderQuickAddForm(onSubmit: dto => submitted = dto);

        cut.Find("#qa-description").Input("To be cleared");
        cut.Find("#qa-amount").Change("-10.00");

        // Act
        cut.Instance.Reset();
        cut.Render();

        // Submit to verify model was cleared (should not invoke since description is now empty)
        cut.Find("#qa-account").Change(_testAccountId.ToString());
        cut.Find("form").Submit();

        // Assert - submit should not fire because description was reset to empty
        Assert.Null(submitted);
    }

    private IRenderedComponent<QuickAddForm> RenderQuickAddForm(
        Action<TransactionCreateDto>? onSubmit = null,
        Action? onCancel = null)
    {
        return Render<QuickAddForm>(p => p
            .Add(x => x.Accounts, CreateTestAccounts())
            .Add(x => x.Categories, CreateTestCategories())
            .Add(x => x.OnSubmit, onSubmit ?? (_ => { }))
            .Add(x => x.OnCancel, onCancel ?? (() => { })));
    }

    private IReadOnlyList<AccountDto> CreateTestAccounts()
    {
        return new List<AccountDto>
        {
            new()
            {
                Id = _testAccountId,
                Name = "Test Checking",
                Type = "Checking",
            },
        };
    }

    private IReadOnlyList<BudgetCategoryDto> CreateTestCategories()
    {
        return new List<BudgetCategoryDto>
        {
            new()
            {
                Id = _testCategoryId,
                Name = "Dining",
                IsActive = true,
            },
        };
    }
}
