// <copyright file="AccountFormTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Forms;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Forms;

/// <summary>
/// Unit tests for the <see cref="AccountForm"/> component.
/// </summary>
public sealed class AccountFormTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountFormTests"/> class.
    /// </summary>
    public AccountFormTests()
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
        var cut = RenderAccountForm();

        // Assert
        Assert.NotNull(cut.Find("#accountName"));
        Assert.NotNull(cut.Find("#accountType"));
        Assert.NotNull(cut.Find("#initialBalance"));
        Assert.NotNull(cut.Find("#initialBalanceDate"));
    }

    /// <summary>
    /// Verifies that the form explains accounts use the household ledger.
    /// </summary>
    [Fact]
    public void Render_ShowsHouseholdLedgerHint()
    {
        // Act
        var cut = RenderAccountForm();

        // Assert
        Assert.Contains("Accounts are created in the household ledger.", cut.Markup);
        Assert.DoesNotContain("name=\"scope\"", cut.Markup);
    }

    /// <summary>
    /// Verifies that a blank scope is normalized to Shared for compatibility.
    /// </summary>
    [Fact]
    public void Render_BlankScope_DefaultsToShared()
    {
        // Arrange
        var model = new AccountCreateDto { Scope = string.Empty };

        // Act
        _ = RenderAccountForm(model: model);

        // Assert
        Assert.Equal("Shared", model.Scope);
    }

    /// <summary>
    /// Verifies that a legacy Personal scope is coerced back to Shared.
    /// </summary>
    [Fact]
    public void Render_PersonalScope_DefaultsToShared()
    {
        // Arrange
        var model = new AccountCreateDto { Scope = "Personal" };

        // Act
        _ = RenderAccountForm(model: model);

        // Assert
        Assert.Equal("Shared", model.Scope);
    }

    /// <summary>
    /// Verifies that the account type dropdown has all five options.
    /// </summary>
    [Fact]
    public void Render_AccountTypeDropdown_HasFiveOptions()
    {
        // Act
        var cut = RenderAccountForm();

        // Assert
        var options = cut.Find("#accountType").QuerySelectorAll("option");
        Assert.Equal(5, options.Length);
        Assert.Equal("Checking", options[0].GetAttribute("value"));
        Assert.Equal("Savings", options[1].GetAttribute("value"));
        Assert.Equal("CreditCard", options[2].GetAttribute("value"));
        Assert.Equal("Cash", options[3].GetAttribute("value"));
        Assert.Equal("Other", options[4].GetAttribute("value"));
    }

    /// <summary>
    /// Verifies that submitting with a valid name invokes OnSubmit.
    /// </summary>
    [Fact]
    public void Submit_WithValidName_InvokesOnSubmit()
    {
        // Arrange
        AccountCreateDto? submitted = null;
        var cut = RenderAccountForm(onSubmit: dto => submitted = dto);

        cut.Find("#accountName").Input("My Savings");

        // Act
        cut.Find("form").Submit();

        // Assert
        Assert.NotNull(submitted);
        Assert.Equal("My Savings", submitted!.Name);
    }

    /// <summary>
    /// Verifies that submitting with an empty name does not invoke OnSubmit.
    /// </summary>
    [Fact]
    public void Submit_WithEmptyName_DoesNotInvokeOnSubmit()
    {
        // Arrange
        var submitCalled = false;
        var cut = RenderAccountForm(onSubmit: _ => submitCalled = true);

        // Leave name empty (default)

        // Act
        cut.Find("form").Submit();

        // Assert
        Assert.False(submitCalled);
    }

    /// <summary>
    /// Verifies that submitting with a whitespace-only name does not invoke OnSubmit.
    /// </summary>
    [Fact]
    public void Submit_WithWhitespaceName_DoesNotInvokeOnSubmit()
    {
        // Arrange
        var submitCalled = false;
        var cut = RenderAccountForm(onSubmit: _ => submitCalled = true);

        cut.Find("#accountName").Input("   ");

        // Act
        cut.Find("form").Submit();

        // Assert
        Assert.False(submitCalled);
    }

    /// <summary>
    /// Verifies that clicking cancel invokes OnCancel.
    /// </summary>
    [Fact]
    public void ClickCancel_InvokesOnCancel()
    {
        // Arrange
        var cancelCalled = false;
        var cut = RenderAccountForm(onCancel: () => cancelCalled = true);

        // Act
        var cancelBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
        cancelBtn.Click();

        // Assert
        Assert.True(cancelCalled);
    }

    /// <summary>
    /// Verifies that the submit button text is customizable.
    /// </summary>
    [Fact]
    public void Render_CustomSubmitButtonText_IsDisplayed()
    {
        // Act
        var cut = Render<AccountForm>(p => p
            .Add(x => x.Model, new AccountCreateDto())
            .Add(x => x.SubmitButtonText, "Create Account"));

        // Assert
        Assert.Contains("Create Account", cut.Markup);
    }

    /// <summary>
    /// Verifies that the default initial balance date is set to today.
    /// </summary>
    [Fact]
    public void Render_InitialBalanceDate_DefaultsToToday()
    {
        // Act
        var cut = RenderAccountForm();

        // Assert
        var dateInput = cut.Find("#initialBalanceDate");
        var todayStr = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");
        Assert.Equal(todayStr, dateInput.GetAttribute("value"));
    }

    private IRenderedComponent<AccountForm> RenderAccountForm(
        Action<AccountCreateDto>? onSubmit = null,
        Action? onCancel = null,
        AccountCreateDto? model = null)
    {
        return Render<AccountForm>(p => p
            .Add(x => x.Model, model ?? new AccountCreateDto())
            .Add(x => x.OnSubmit, onSubmit ?? (_ => { }))
            .Add(x => x.OnCancel, onCancel ?? (() => { })));
    }
}
