// <copyright file="PastDueAlertTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Display;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Display;

/// <summary>
/// Unit tests for the <see cref="PastDueAlert"/> component.
/// </summary>
public class PastDueAlertTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PastDueAlertTests"/> class.
    /// </summary>
    public PastDueAlertTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies nothing renders when PastDueSummary is null.
    /// </summary>
    [Fact]
    public void NullSummary_RendersNothing()
    {
        var cut = Render<PastDueAlert>(p => p
            .Add(x => x.PastDueSummary, (PastDueSummaryDto?)null));

        Assert.Empty(cut.Markup.Trim());
    }

    /// <summary>
    /// Verifies nothing renders when TotalCount is zero.
    /// </summary>
    [Fact]
    public void ZeroCount_RendersNothing()
    {
        var cut = Render<PastDueAlert>(p => p
            .Add(x => x.PastDueSummary, new PastDueSummaryDto { TotalCount = 0 }));

        Assert.Empty(cut.Markup.Trim());
    }

    /// <summary>
    /// Verifies alert renders when items are past due.
    /// </summary>
    [Fact]
    public void WithPastDueItems_RendersAlert()
    {
        var cut = Render<PastDueAlert>(p => p
            .Add(x => x.PastDueSummary, new PastDueSummaryDto { TotalCount = 3 }));

        Assert.Contains("past-due-alert", cut.Markup);
        Assert.Contains("past due", cut.Markup);
    }

    /// <summary>
    /// Verifies singular item message.
    /// </summary>
    [Fact]
    public void SingleItem_UsesSingularForm()
    {
        var cut = Render<PastDueAlert>(p => p
            .Add(x => x.PastDueSummary, new PastDueSummaryDto { TotalCount = 1 }));

        Assert.Contains("1 recurring item is past due", cut.Markup);
    }

    /// <summary>
    /// Verifies plural items message.
    /// </summary>
    [Fact]
    public void MultipleItems_UsesPluralForm()
    {
        var cut = Render<PastDueAlert>(p => p
            .Add(x => x.PastDueSummary, new PastDueSummaryDto { TotalCount = 5 }));

        Assert.Contains("5 recurring items are past due", cut.Markup);
    }

    /// <summary>
    /// Verifies total amount is displayed when available.
    /// </summary>
    [Fact]
    public void WithTotalAmount_DisplaysAmount()
    {
        var cut = Render<PastDueAlert>(p => p
            .Add(x => x.PastDueSummary, new PastDueSummaryDto
            {
                TotalCount = 2,
                TotalAmount = new MoneyDto { Amount = 150.75m, Currency = "USD" },
            }));

        // The MoneyDisplay component should render the amount
        Assert.Contains("past-due-amount", cut.Markup);
    }

    /// <summary>
    /// Verifies Review button is present.
    /// </summary>
    [Fact]
    public void ShowsReviewButton()
    {
        var cut = Render<PastDueAlert>(p => p
            .Add(x => x.PastDueSummary, new PastDueSummaryDto { TotalCount = 1 }));

        Assert.Contains("Review", cut.Markup);
    }

    /// <summary>
    /// Verifies Review button fires callback.
    /// </summary>
    [Fact]
    public void ReviewButton_FiresCallback()
    {
        bool clicked = false;
        var cut = Render<PastDueAlert>(p => p
            .Add(x => x.PastDueSummary, new PastDueSummaryDto { TotalCount = 1 })
            .Add(x => x.OnReviewClick, () =>
            {
                clicked = true;
                return Task.CompletedTask;
            }));

        var btn = cut.Find("button");
        btn.Click();

        Assert.True(clicked);
    }
}
