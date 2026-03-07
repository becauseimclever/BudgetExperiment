// <copyright file="TransactionFormTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Forms;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Forms;

/// <summary>
/// Unit tests for the <see cref="TransactionForm"/> component.
/// </summary>
public sealed class TransactionFormTests : BunitContext, IAsyncLifetime
{
    private readonly Guid _testAccountId = Guid.NewGuid();
    private readonly Guid _testCategoryId = Guid.NewGuid();

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionFormTests"/> class.
    /// </summary>
    public TransactionFormTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that the account selector is shown when ShowAccountSelector is true.
    /// </summary>
    [Fact]
    public void Render_ShowAccountSelectorTrue_ShowsAccountDropdown()
    {
        // Act
        var cut = RenderTransactionForm(showAccountSelector: true);

        // Assert
        Assert.NotNull(cut.Find("#txnAccount"));
    }

    /// <summary>
    /// Verifies that the account selector is hidden when ShowAccountSelector is false.
    /// </summary>
    [Fact]
    public void Render_ShowAccountSelectorFalse_HidesAccountDropdown()
    {
        // Act
        var cut = RenderTransactionForm(showAccountSelector: false);

        // Assert
        Assert.Empty(cut.FindAll("#txnAccount"));
    }

    /// <summary>
    /// Verifies that the form renders all main fields.
    /// </summary>
    [Fact]
    public void Render_ShowsAllFormFields()
    {
        // Act
        var cut = RenderTransactionForm();

        // Assert
        Assert.NotNull(cut.Find("#txnDescription"));
        Assert.NotNull(cut.Find("#txnAmount"));
        Assert.NotNull(cut.Find("#txnCategory"));
        Assert.NotNull(cut.Find("#txnDate"));
    }

    /// <summary>
    /// Verifies that submitting with an empty description does not invoke OnSubmit.
    /// </summary>
    [Fact]
    public void Submit_WithEmptyDescription_DoesNotInvokeOnSubmit()
    {
        // Arrange
        var submitCalled = false;
        var cut = RenderTransactionForm(onSubmit: _ => submitCalled = true);

        cut.Find("#txnAmount").Change("-10.00");
        cut.Find("#txnAccount").Change(_testAccountId.ToString());

        // Act
        cut.Find("form").Submit();

        // Assert
        Assert.False(submitCalled);
    }

    /// <summary>
    /// Verifies that submitting with no account selected does not invoke OnSubmit.
    /// </summary>
    [Fact]
    public void Submit_WithNoAccount_DoesNotInvokeOnSubmit()
    {
        // Arrange
        var submitCalled = false;
        var cut = RenderTransactionForm(onSubmit: _ => submitCalled = true);

        cut.Find("#txnDescription").Input("Coffee");
        cut.Find("#txnAmount").Change("-5.00");

        // Act
        cut.Find("form").Submit();

        // Assert
        Assert.False(submitCalled);
    }

    /// <summary>
    /// Verifies that submit succeeds with valid data.
    /// </summary>
    [Fact]
    public void Submit_WithValidData_InvokesOnSubmit()
    {
        // Arrange
        TransactionCreateDto? submitted = null;
        var cut = RenderTransactionForm(onSubmit: dto => submitted = dto);

        cut.Find("#txnDescription").Input("Coffee");
        cut.Find("#txnAmount").Change("-5.50");
        cut.Find("#txnAccount").Change(_testAccountId.ToString());

        // Act
        cut.Find("form").Submit();

        // Assert
        Assert.NotNull(submitted);
        Assert.Equal("Coffee", submitted!.Description);
        Assert.Equal(-5.50m, submitted.Amount.Amount);
    }

    /// <summary>
    /// Verifies that when ShowAccountSelector is false, submit succeeds without account validation.
    /// </summary>
    [Fact]
    public void Submit_WithShowAccountSelectorFalse_DoesNotRequireAccount()
    {
        // Arrange
        TransactionCreateDto? submitted = null;
        var cut = RenderTransactionForm(
            showAccountSelector: false,
            fixedAccountId: _testAccountId,
            onSubmit: dto => submitted = dto);

        cut.Find("#txnDescription").Input("Coffee");
        cut.Find("#txnAmount").Change("-5.50");

        // Act
        cut.Find("form").Submit();

        // Assert
        Assert.NotNull(submitted);
        Assert.Equal(_testAccountId, submitted!.AccountId);
    }

    /// <summary>
    /// Verifies that InitialDate is applied to the model.
    /// </summary>
    [Fact]
    public void Render_WithInitialDate_SetsModelDate()
    {
        // Arrange
        var initialDate = new DateOnly(2026, 3, 15);
        var model = new TransactionCreateDto();

        // Act
        var cut = Render<TransactionForm>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.Accounts, CreateTestAccounts())
            .Add(x => x.Categories, CreateTestCategories())
            .Add(x => x.InitialDate, initialDate)
            .Add(x => x.ShowAccountSelector, true));

        // Assert
        Assert.Equal(initialDate, model.Date);
    }

    /// <summary>
    /// Verifies that InitialDescription is applied to the model.
    /// </summary>
    [Fact]
    public void Render_WithInitialDescription_SetsModelDescription()
    {
        // Arrange
        var model = new TransactionCreateDto();

        // Act
        Render<TransactionForm>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.Accounts, CreateTestAccounts())
            .Add(x => x.Categories, CreateTestCategories())
            .Add(x => x.InitialDescription, "Pre-filled"));

        // Assert
        Assert.Equal("Pre-filled", model.Description);
    }

    /// <summary>
    /// Verifies that InitialAmount is applied to the model.
    /// </summary>
    [Fact]
    public void Render_WithInitialAmount_SetsModelAmount()
    {
        // Arrange
        var model = new TransactionCreateDto();

        // Act
        Render<TransactionForm>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.Accounts, CreateTestAccounts())
            .Add(x => x.Categories, CreateTestCategories())
            .Add(x => x.InitialAmount, -42.50m));

        // Assert
        Assert.Equal(-42.50m, model.Amount.Amount);
        Assert.Equal("USD", model.Amount.Currency);
    }

    /// <summary>
    /// Verifies that error message is displayed when set.
    /// </summary>
    [Fact]
    public void Render_WithErrorMessage_DisplaysError()
    {
        // Act
        var cut = Render<TransactionForm>(p => p
            .Add(x => x.Model, new TransactionCreateDto())
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
        var cut = RenderTransactionForm();

        // Assert
        Assert.Empty(cut.FindAll(".form-error-box"));
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
        var cut = Render<TransactionForm>(p => p
            .Add(x => x.Model, new TransactionCreateDto())
            .Add(x => x.Accounts, CreateTestAccounts())
            .Add(x => x.Categories, categories));

        // Assert
        var categorySelect = cut.Find("#txnCategory");
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
        var cut = RenderTransactionForm(onCancel: () => cancelCalled = true);

        // Act
        var cancelBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
        cancelBtn.Click();

        // Assert
        Assert.True(cancelCalled);
    }

    private IRenderedComponent<TransactionForm> RenderTransactionForm(
        Action<TransactionCreateDto>? onSubmit = null,
        Action? onCancel = null,
        bool showAccountSelector = true,
        Guid? fixedAccountId = null)
    {
        return Render<TransactionForm>(p => p
            .Add(x => x.Model, new TransactionCreateDto())
            .Add(x => x.Accounts, CreateTestAccounts())
            .Add(x => x.Categories, CreateTestCategories())
            .Add(x => x.ShowAccountSelector, showAccountSelector)
            .Add(x => x.FixedAccountId, fixedAccountId)
            .Add(x => x.OnSubmit, onSubmit ?? (_ => { }))
            .Add(x => x.OnCancel, onCancel ?? (() => { })));
    }

    private IReadOnlyList<AccountDto> CreateTestAccounts()
    {
        return new List<AccountDto>
        {
            new() { Id = _testAccountId, Name = "Test Checking", Type = "Checking" },
        };
    }

    private IReadOnlyList<BudgetCategoryDto> CreateTestCategories()
    {
        return new List<BudgetCategoryDto>
        {
            new() { Id = _testCategoryId, Name = "Dining", IsActive = true },
        };
    }
}
