// <copyright file="PastDueReviewModalTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Forms;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Forms;

/// <summary>
/// Unit tests for the <see cref="PastDueReviewModal"/> component.
/// </summary>
public sealed class PastDueReviewModalTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PastDueReviewModalTests"/> class.
    /// </summary>
    public PastDueReviewModalTests()
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
    /// Verifies that loading state shows spinner.
    /// </summary>
    [Fact]
    public void Render_IsLoading_ShowsLoadingMessage()
    {
        // Act
        var cut = Render<PastDueReviewModal>(p => p
            .Add(x => x.IsVisible, true)
            .Add(x => x.IsLoading, true));

        // Assert
        Assert.Contains("Loading past-due items", cut.Markup);
    }

    /// <summary>
    /// Verifies that empty items shows empty state message.
    /// </summary>
    [Fact]
    public void Render_NoItems_ShowsEmptyState()
    {
        // Act
        var cut = Render<PastDueReviewModal>(p => p
            .Add(x => x.IsVisible, true)
            .Add(x => x.Items, new List<PastDueItemDto>()));

        // Assert
        Assert.Contains("No past-due items found", cut.Markup);
    }

    /// <summary>
    /// Verifies that items default to selected and selected count reflects all.
    /// </summary>
    [Fact]
    public void Render_WithItems_AllDefaultSelected()
    {
        // Arrange
        var items = CreateTestItems();

        // Act
        var cut = Render<PastDueReviewModal>(p => p
            .Add(x => x.IsVisible, true)
            .Add(x => x.Items, items));

        // Assert - "Selected: 2 of 2 items"
        Assert.Contains("Selected: 2 of 2 items", cut.Markup);
    }

    /// <summary>
    /// Verifies that GetRowClass returns critical class for items over 7 days past due.
    /// </summary>
    [Fact]
    public void Render_ItemOver7DaysPastDue_HasCriticalClass()
    {
        // Arrange
        var items = new List<PastDueItemDto>
        {
            new PastDueItemDto
            {
                Id = Guid.NewGuid(),
                Type = "recurring-transaction",
                InstanceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
                DaysPastDue = 10,
                Description = "Critical Item",
                Amount = new MoneyDto { Amount = 100m, Currency = "USD" },
                AccountName = "Checking",
            },
        };

        // Act
        var cut = Render<PastDueReviewModal>(p => p
            .Add(x => x.IsVisible, true)
            .Add(x => x.Items, items));

        // Assert
        Assert.Contains("row-past-due-critical", cut.Markup);
    }

    /// <summary>
    /// Verifies that GetRowClass returns normal past-due class for items 7 days or less.
    /// </summary>
    [Fact]
    public void Render_Item7DaysOrLess_HasNormalPastDueClass()
    {
        // Arrange
        var items = new List<PastDueItemDto>
        {
            new PastDueItemDto
            {
                Id = Guid.NewGuid(),
                Type = "recurring-transaction",
                InstanceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
                DaysPastDue = 5,
                Description = "Normal Item",
                Amount = new MoneyDto { Amount = 50m, Currency = "USD" },
                AccountName = "Checking",
            },
        };

        // Act
        var cut = Render<PastDueReviewModal>(p => p
            .Add(x => x.IsVisible, true)
            .Add(x => x.Items, items));

        // Assert
        Assert.Contains("row-past-due", cut.Markup);
        Assert.DoesNotContain("row-past-due-critical", cut.Markup);
    }

    /// <summary>
    /// Verifies that ConfirmSelected invokes OnConfirm with selected items.
    /// </summary>
    [Fact]
    public void ConfirmSelected_InvokesOnConfirmWithSelectedItems()
    {
        // Arrange
        var items = CreateTestItems();
        IReadOnlyList<PastDueItemDto>? confirmedItems = null;
        var cut = Render<PastDueReviewModal>(p => p
            .Add(x => x.IsVisible, true)
            .Add(x => x.Items, items)
            .Add(x => x.OnConfirm, (IReadOnlyList<PastDueItemDto> selected) => confirmedItems = selected));

        // Act
        var confirmBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Confirm Selected"));
        confirmBtn.Click();

        // Assert
        Assert.NotNull(confirmedItems);
        Assert.Equal(2, confirmedItems!.Count);
    }

    /// <summary>
    /// Verifies that SkipSelected invokes OnSkip with selected items.
    /// </summary>
    [Fact]
    public void SkipSelected_InvokesOnSkipWithSelectedItems()
    {
        // Arrange
        var items = CreateTestItems();
        IReadOnlyList<PastDueItemDto>? skippedItems = null;
        var cut = Render<PastDueReviewModal>(p => p
            .Add(x => x.IsVisible, true)
            .Add(x => x.Items, items)
            .Add(x => x.OnSkip, (IReadOnlyList<PastDueItemDto> selected) => skippedItems = selected));

        // Act
        var skipBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Skip Selected"));
        skipBtn.Click();

        // Assert
        Assert.NotNull(skippedItems);
        Assert.Equal(2, skippedItems!.Count);
    }

    /// <summary>
    /// Verifies that cancel invokes OnClose callback.
    /// </summary>
    [Fact]
    public void Cancel_InvokesOnCloseCallback()
    {
        // Arrange
        var items = CreateTestItems();
        var closeCalled = false;
        var cut = Render<PastDueReviewModal>(p => p
            .Add(x => x.IsVisible, true)
            .Add(x => x.Items, items)
            .Add(x => x.OnClose, () => closeCalled = true));

        // Act
        var cancelBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
        cancelBtn.Click();

        // Assert
        Assert.True(closeCalled);
    }

    /// <summary>
    /// Verifies that transfers show source/destination account display.
    /// </summary>
    [Fact]
    public void Render_TransferItem_ShowsSourceDestinationAccounts()
    {
        // Arrange
        var items = new List<PastDueItemDto>
        {
            new PastDueItemDto
            {
                Id = Guid.NewGuid(),
                Type = "recurring-transfer",
                InstanceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
                DaysPastDue = 3,
                Description = "Transfer to Savings",
                Amount = new MoneyDto { Amount = 200m, Currency = "USD" },
                SourceAccountName = "Checking",
                DestinationAccountName = "Savings",
            },
        };

        // Act
        var cut = Render<PastDueReviewModal>(p => p
            .Add(x => x.IsVisible, true)
            .Add(x => x.Items, items));

        // Assert
        Assert.Contains("Checking", cut.Markup);
        Assert.Contains("Savings", cut.Markup);
    }

    private static List<PastDueItemDto> CreateTestItems()
    {
        return new List<PastDueItemDto>
        {
            new PastDueItemDto
            {
                Id = Guid.NewGuid(),
                Type = "recurring-transaction",
                InstanceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
                DaysPastDue = 3,
                Description = "Mortgage",
                Amount = new MoneyDto { Amount = 1500m, Currency = "USD" },
                AccountName = "Checking",
            },
            new PastDueItemDto
            {
                Id = Guid.NewGuid(),
                Type = "recurring-transaction",
                InstanceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
                DaysPastDue = 5,
                Description = "Car Payment",
                Amount = new MoneyDto { Amount = 350m, Currency = "USD" },
                AccountName = "Checking",
            },
        };
    }
}
