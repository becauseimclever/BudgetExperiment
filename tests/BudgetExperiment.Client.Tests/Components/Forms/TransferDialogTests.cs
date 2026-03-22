// <copyright file="TransferDialogTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Forms;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Forms;

/// <summary>
/// Unit tests for the <see cref="TransferDialog"/> component.
/// </summary>
public sealed class TransferDialogTests : BunitContext, IAsyncLifetime
{
    private readonly Guid _sourceAccountId = Guid.NewGuid();
    private readonly Guid _destAccountId = Guid.NewGuid();

    /// <summary>
    /// Initializes a new instance of the <see cref="TransferDialogTests"/> class.
    /// </summary>
    public TransferDialogTests()
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
    /// Verifies that in create mode, account selectors are shown.
    /// </summary>
    [Fact]
    public void Render_CreateMode_ShowsAccountSelectors()
    {
        // Act
        var cut = RenderCreateTransferDialog();

        // Assert
        Assert.NotNull(cut.Find("#fromAccount"));
        Assert.NotNull(cut.Find("#toAccount"));
    }

    /// <summary>
    /// Verifies that in edit mode, account selectors are hidden and info box is shown.
    /// </summary>
    [Fact]
    public void Render_EditMode_ShowsInfoBoxInsteadOfSelectors()
    {
        // Act
        var cut = RenderEditTransferDialog();

        // Assert
        Assert.Empty(cut.FindAll("#fromAccount"));
        Assert.Empty(cut.FindAll("#toAccount"));
        Assert.Contains("From:", cut.Markup);
        Assert.Contains("To:", cut.Markup);
    }

    /// <summary>
    /// Verifies that in edit mode, the button text is "Update Transfer".
    /// </summary>
    [Fact]
    public void Render_EditMode_ShowsUpdateTransferButton()
    {
        // Act
        var cut = RenderEditTransferDialog();

        // Assert
        Assert.Contains("Update Transfer", cut.Markup);
    }

    /// <summary>
    /// Verifies that in create mode, the button text is "Transfer".
    /// </summary>
    [Fact]
    public void Render_CreateMode_ShowsTransferButton()
    {
        // Act
        var cut = RenderCreateTransferDialog();

        // Assert
        var submitBtn = cut.FindAll("button").First(b => b.GetAttribute("type") == "submit");
        Assert.Contains("Transfer", submitBtn.TextContent);
    }

    /// <summary>
    /// Verifies that submitting in create mode without a source account shows error.
    /// </summary>
    [Fact]
    public void Submit_CreateMode_NoSourceAccount_ShowsError()
    {
        // Arrange
        var cut = RenderCreateTransferDialog();

        cut.Find("#amount").Change("100.00");

        // Act
        cut.Find("form").Submit();

        // Assert
        var errorBox = cut.Find(".form-error-box");
        Assert.Contains("source account", errorBox.TextContent);
    }

    /// <summary>
    /// Verifies that submitting with same source and destination shows error.
    /// </summary>
    [Fact]
    public void Submit_CreateMode_SameAccounts_ShowsError()
    {
        // Arrange
        var cut = RenderCreateTransferDialog();

        cut.Find("#fromAccount").Change(_sourceAccountId.ToString());
        cut.Find("#toAccount").Change(_sourceAccountId.ToString());
        cut.Find("#amount").Change("100.00");

        // Act
        cut.Find("form").Submit();

        // Assert
        var errorBox = cut.Find(".form-error-box");
        Assert.Contains("different", errorBox.TextContent);
    }

    /// <summary>
    /// Verifies that submitting with amount zero shows error.
    /// </summary>
    [Fact]
    public void Submit_AmountZero_ShowsError()
    {
        // Arrange
        var cut = RenderCreateTransferDialog();

        cut.Find("#fromAccount").Change(_sourceAccountId.ToString());
        cut.Find("#toAccount").Change(_destAccountId.ToString());
        cut.Find("#amount").Change("0");

        // Act
        cut.Find("form").Submit();

        // Assert
        var errorBox = cut.Find(".form-error-box");
        Assert.Contains("greater than zero", errorBox.TextContent);
    }

    /// <summary>
    /// Verifies that clicking cancel invokes OnCancel.
    /// </summary>
    [Fact]
    public void ClickCancel_InvokesOnCancel()
    {
        // Arrange
        var cancelCalled = false;
        var cut = Render<TransferDialog>(p => p
            .Add(x => x.Accounts, CreateTestAccounts())
            .Add(x => x.OnCancel, () => cancelCalled = true));

        // Act
        var cancelBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
        cancelBtn.Click();

        // Assert
        Assert.True(cancelCalled);
    }

    /// <summary>
    /// Verifies that error message is not shown initially.
    /// </summary>
    [Fact]
    public void Render_InitialState_NoErrorMessage()
    {
        // Act
        var cut = RenderCreateTransferDialog();

        // Assert
        Assert.Empty(cut.FindAll(".form-error-box"));
    }

    /// <summary>
    /// Verifies that PreSelectedSourceAccountId is applied to the model.
    /// </summary>
    [Fact]
    public void Render_WithPreSelectedSource_SetsSourceAccountId()
    {
        // Arrange
        var model = new CreateTransferRequest();

        // Act
        Render<TransferDialog>(p => p
            .Add(x => x.Accounts, CreateTestAccounts())
            .Add(x => x.Model, model)
            .Add(x => x.PreSelectedSourceAccountId, _sourceAccountId));

        // Assert
        Assert.Equal(_sourceAccountId, model.SourceAccountId);
    }

    private IRenderedComponent<TransferDialog> RenderCreateTransferDialog()
    {
        return Render<TransferDialog>(p => p
            .Add(x => x.Accounts, CreateTestAccounts())
            .Add(x => x.IsEdit, false)
            .Add(x => x.Model, new CreateTransferRequest()));
    }

    private IRenderedComponent<TransferDialog> RenderEditTransferDialog()
    {
        return Render<TransferDialog>(p => p
            .Add(x => x.Accounts, CreateTestAccounts())
            .Add(x => x.IsEdit, true)
            .Add(x => x.SourceAccountName, "Source Account")
            .Add(x => x.DestinationAccountName, "Dest Account")
            .Add(x => x.UpdateModel, new UpdateTransferRequest
            {
                Amount = 100m,
                Currency = "USD",
                Date = new DateOnly(2026, 1, 15),
            }));
    }

    private IReadOnlyList<AccountDto> CreateTestAccounts()
    {
        return new List<AccountDto>
        {
            new() { Id = _sourceAccountId, Name = "Source Account", Type = "Checking" },
            new() { Id = _destAccountId, Name = "Dest Account", Type = "Savings" },
        };
    }
}
