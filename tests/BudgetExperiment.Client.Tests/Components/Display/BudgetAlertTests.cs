// <copyright file="BudgetAlertTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Display;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Display;

/// <summary>
/// Unit tests for the <see cref="BudgetAlert"/> component.
/// </summary>
public class BudgetAlertTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetAlertTests"/> class.
    /// </summary>
    public BudgetAlertTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies nothing renders when BudgetSummary is null.
    /// </summary>
    [Fact]
    public void NullSummary_RendersNothing()
    {
        var cut = Render<BudgetAlert>(p => p
            .Add(x => x.BudgetSummary, (BudgetSummaryDto?)null));

        Assert.Empty(cut.Markup.Trim());
    }

    /// <summary>
    /// Verifies nothing renders when no categories are over budget or warning.
    /// </summary>
    [Fact]
    public void NoAlerts_RendersNothing()
    {
        var cut = Render<BudgetAlert>(p => p
            .Add(x => x.BudgetSummary, new BudgetSummaryDto
            {
                CategoriesOverBudget = 0,
                CategoriesWarning = 0,
            }));

        Assert.Empty(cut.Markup.Trim());
    }

    /// <summary>
    /// Verifies over budget alert renders with danger class.
    /// </summary>
    [Fact]
    public void OverBudget_ShowsDangerAlert()
    {
        var cut = Render<BudgetAlert>(p => p
            .Add(x => x.BudgetSummary, new BudgetSummaryDto
            {
                CategoriesOverBudget = 2,
            }));

        Assert.Contains("budget-alert-danger", cut.Markup);
        Assert.Contains("over budget", cut.Markup);
    }

    /// <summary>
    /// Verifies warning-only alert renders with warning class.
    /// </summary>
    [Fact]
    public void WarningOnly_ShowsWarningAlert()
    {
        var cut = Render<BudgetAlert>(p => p
            .Add(x => x.BudgetSummary, new BudgetSummaryDto
            {
                CategoriesOverBudget = 0,
                CategoriesWarning = 3,
            }));

        Assert.Contains("budget-alert-warning", cut.Markup);
        Assert.Contains("approaching limit", cut.Markup);
    }

    /// <summary>
    /// Verifies singular category message.
    /// </summary>
    [Fact]
    public void SingleCategory_UsesSingularForm()
    {
        var cut = Render<BudgetAlert>(p => p
            .Add(x => x.BudgetSummary, new BudgetSummaryDto
            {
                CategoriesOverBudget = 1,
            }));

        Assert.Contains("1 category is over budget", cut.Markup);
    }

    /// <summary>
    /// Verifies plural categories message.
    /// </summary>
    [Fact]
    public void MultipleCategories_UsesPluralForm()
    {
        var cut = Render<BudgetAlert>(p => p
            .Add(x => x.BudgetSummary, new BudgetSummaryDto
            {
                CategoriesOverBudget = 3,
            }));

        Assert.Contains("3 categories are over budget", cut.Markup);
    }

    /// <summary>
    /// Verifies combined over budget and warning message.
    /// </summary>
    [Fact]
    public void OverBudgetAndWarning_ShowsBothMessages()
    {
        var cut = Render<BudgetAlert>(p => p
            .Add(x => x.BudgetSummary, new BudgetSummaryDto
            {
                CategoriesOverBudget = 2,
                CategoriesWarning = 1,
            }));

        Assert.Contains("over budget", cut.Markup);
        Assert.Contains("approaching limit", cut.Markup);
    }

    /// <summary>
    /// Verifies View Budget button is present.
    /// </summary>
    [Fact]
    public void ShowsViewBudgetButton()
    {
        var cut = Render<BudgetAlert>(p => p
            .Add(x => x.BudgetSummary, new BudgetSummaryDto
            {
                CategoriesOverBudget = 1,
            }));

        Assert.Contains("View Budget", cut.Markup);
    }

    /// <summary>
    /// Verifies View Budget button fires callback.
    /// </summary>
    [Fact]
    public void ViewBudgetButton_FiresCallback()
    {
        bool clicked = false;
        var cut = Render<BudgetAlert>(p => p
            .Add(x => x.BudgetSummary, new BudgetSummaryDto
            {
                CategoriesOverBudget = 1,
            })
            .Add(x => x.OnViewBudgetClick, () =>
            {
                clicked = true;
                return Task.CompletedTask;
            }));

        var btn = cut.Find("button");
        btn.Click();

        Assert.True(clicked);
    }

    /// <summary>
    /// Verifies over-budget gets danger button class.
    /// </summary>
    [Fact]
    public void OverBudget_HasDangerButton()
    {
        var cut = Render<BudgetAlert>(p => p
            .Add(x => x.BudgetSummary, new BudgetSummaryDto
            {
                CategoriesOverBudget = 1,
            }));

        Assert.Contains("btn-danger", cut.Markup);
    }

    /// <summary>
    /// Verifies warning-only gets warning button class.
    /// </summary>
    [Fact]
    public void WarningOnly_HasWarningButton()
    {
        var cut = Render<BudgetAlert>(p => p
            .Add(x => x.BudgetSummary, new BudgetSummaryDto
            {
                CategoriesWarning = 1,
            }));

        Assert.Contains("btn-warning", cut.Markup);
    }
}
