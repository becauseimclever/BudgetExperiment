// <copyright file="MoneyDisplayTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;
using BudgetExperiment.Client.Components.Display;
using BudgetExperiment.Client.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BudgetExperiment.Client.Tests.Components.Display;

/// <summary>
/// Unit tests for the <see cref="MoneyDisplay"/> component.
/// </summary>
public class MoneyDisplayTests : BunitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyDisplayTests"/> class.
    /// </summary>
    public MoneyDisplayTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<CultureService>();
    }

    /// <summary>
    /// Verifies a positive amount gets the positive CSS class.
    /// </summary>
    [Fact]
    public void PositiveAmount_HasPositiveClass()
    {
        var cut = Render<MoneyDisplay>(p => p.Add(x => x.Amount, 100m));

        cut.Markup.ShouldContain("money-display-positive");
    }

    /// <summary>
    /// Verifies a negative amount gets the negative CSS class.
    /// </summary>
    [Fact]
    public void NegativeAmount_HasNegativeClass()
    {
        var cut = Render<MoneyDisplay>(p => p.Add(x => x.Amount, -50m));

        cut.Markup.ShouldContain("money-display-negative");
    }

    /// <summary>
    /// Verifies zero amount gets the positive CSS class.
    /// </summary>
    [Fact]
    public void ZeroAmount_HasPositiveClass()
    {
        var cut = Render<MoneyDisplay>(p => p.Add(x => x.Amount, 0m));

        cut.Markup.ShouldContain("money-display-positive");
    }

    /// <summary>
    /// Verifies no color class when ShowColor is false.
    /// </summary>
    [Fact]
    public void ShowColorFalse_HasNeutralClass()
    {
        var cut = Render<MoneyDisplay>(p => p
            .Add(x => x.Amount, 100m)
            .Add(x => x.ShowColor, false));

        cut.Markup.ShouldContain("money-display-neutral");
    }

    /// <summary>
    /// Verifies positive sign is shown when requested.
    /// </summary>
    [Fact]
    public void ShowPositiveSign_PrependsPlusSign()
    {
        var cut = Render<MoneyDisplay>(p => p
            .Add(x => x.Amount, 100m)
            .Add(x => x.ShowPositiveSign, true));

        cut.Markup.ShouldContain("+");
    }

    /// <summary>
    /// Verifies positive sign is not shown by default.
    /// </summary>
    [Fact]
    public void DefaultSettings_NoPlusSign()
    {
        var cut = Render<MoneyDisplay>(p => p.Add(x => x.Amount, 100m));

        cut.Markup.ShouldNotContain("+$");
    }

    /// <summary>
    /// Verifies the amount is formatted as currency.
    /// </summary>
    [Fact]
    public void FormatsAsCurrency()
    {
        var cut = Render<MoneyDisplay>(p => p.Add(x => x.Amount, 1234.56m));

        cut.Markup.ShouldContain("$1,234.56");
    }
}
