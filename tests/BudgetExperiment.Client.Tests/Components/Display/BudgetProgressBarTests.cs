// <copyright file="BudgetProgressBarTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Display;
using Bunit;
using Shouldly;

namespace BudgetExperiment.Client.Tests.Components.Display;

/// <summary>
/// Unit tests for the <see cref="BudgetProgressBar"/> component.
/// </summary>
public class BudgetProgressBarTests : BunitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetProgressBarTests"/> class.
    /// </summary>
    public BudgetProgressBarTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>
    /// Verifies the component renders without errors.
    /// </summary>
    [Fact]
    public void Renders_WithoutErrors()
    {
        var cut = Render<BudgetProgressBar>(p => p.Add(x => x.PercentUsed, 50m));

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies OnTrack status uses the success class.
    /// </summary>
    [Fact]
    public void OnTrack_HasSuccessClass()
    {
        var cut = Render<BudgetProgressBar>(p => p
            .Add(x => x.PercentUsed, 50m)
            .Add(x => x.Status, "OnTrack"));

        cut.Markup.ShouldContain("progress-success");
    }

    /// <summary>
    /// Verifies Warning status uses the warning class.
    /// </summary>
    [Fact]
    public void Warning_HasWarningClass()
    {
        var cut = Render<BudgetProgressBar>(p => p
            .Add(x => x.PercentUsed, 85m)
            .Add(x => x.Status, "Warning"));

        cut.Markup.ShouldContain("progress-warning");
    }

    /// <summary>
    /// Verifies OverBudget status uses the danger class.
    /// </summary>
    [Fact]
    public void OverBudget_HasDangerClass()
    {
        var cut = Render<BudgetProgressBar>(p => p
            .Add(x => x.PercentUsed, 120m)
            .Add(x => x.Status, "OverBudget"));

        cut.Markup.ShouldContain("progress-danger");
    }

    /// <summary>
    /// Verifies the percentage label is displayed.
    /// </summary>
    [Fact]
    public void ShowsPercentageLabel()
    {
        var cut = Render<BudgetProgressBar>(p => p.Add(x => x.PercentUsed, 75m));

        cut.Markup.ShouldContain("75");
    }

    /// <summary>
    /// Verifies the over budget status shows the red circle icon.
    /// </summary>
    [Fact]
    public void OverBudget_ShowsStatusIcon()
    {
        var cut = Render<BudgetProgressBar>(p => p
            .Add(x => x.PercentUsed, 120m)
            .Add(x => x.Status, "OverBudget"));

        cut.Markup.ShouldContain("budget-progress-status-icon");
    }
}
