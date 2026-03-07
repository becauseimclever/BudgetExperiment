// <copyright file="AmountModeSelectorTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Import;
using Bunit;

namespace BudgetExperiment.Client.Tests.Components.Import;

/// <summary>
/// Unit tests for the <see cref="AmountModeSelector"/> component.
/// </summary>
public sealed class AmountModeSelectorTests : BunitContext
{
    /// <summary>
    /// Verifies that the selector renders with a label.
    /// </summary>
    [Fact]
    public void AmountModeSelector_RendersLabel()
    {
        // Arrange & Act
        var cut = Render<AmountModeSelector>();

        // Assert
        var label = cut.Find(".form-label");
        Assert.Equal("Amount Format", label.TextContent);
    }

    /// <summary>
    /// Verifies that the selector renders all four options.
    /// </summary>
    [Fact]
    public void AmountModeSelector_RendersFourOptions()
    {
        // Arrange & Act
        var cut = Render<AmountModeSelector>();

        // Assert
        var options = cut.FindAll("option");
        Assert.Equal(4, options.Count);
    }

    /// <summary>
    /// Verifies that the default value is NegativeIsExpense.
    /// </summary>
    [Fact]
    public void AmountModeSelector_DefaultsToNegativeIsExpense()
    {
        // Arrange & Act
        var cut = Render<AmountModeSelector>();

        // Assert
        var select = cut.Find("select");
        Assert.Equal(((int)AmountParseMode.NegativeIsExpense).ToString(), select.GetAttribute("value"));
    }

    /// <summary>
    /// Verifies that changing the selection fires the ValueChanged callback.
    /// </summary>
    [Fact]
    public void AmountModeSelector_FiresValueChanged_OnChange()
    {
        // Arrange
        AmountParseMode? selectedMode = null;
        var cut = Render<AmountModeSelector>(parameters => parameters
            .Add(p => p.ValueChanged, (AmountParseMode mode) => { selectedMode = mode; }));

        // Act
        cut.Find("select").Change(((int)AmountParseMode.SeparateColumns).ToString());

        // Assert
        Assert.Equal(AmountParseMode.SeparateColumns, selectedMode);
    }

    /// <summary>
    /// Verifies that the help text updates based on the selected mode.
    /// </summary>
    /// <param name="mode">The amount parse mode to test.</param>
    /// <param name="expectedFragment">The expected help text fragment.</param>
    [Theory]
    [InlineData(AmountParseMode.NegativeIsExpense, "Negative amounts are expenses")]
    [InlineData(AmountParseMode.SeparateColumns, "separate Debit and Credit")]
    [InlineData(AmountParseMode.PositiveIsExpense, "Positive amounts are expenses")]
    [InlineData(AmountParseMode.IndicatorColumn, "a separate column indicates debit or credit")]
    public void AmountModeSelector_ShowsCorrectHelpText(AmountParseMode mode, string expectedFragment)
    {
        // Arrange & Act
        var cut = Render<AmountModeSelector>(parameters => parameters
            .Add(p => p.Value, mode));

        // Assert
        var helpText = cut.Find("small");
        Assert.Contains(expectedFragment, helpText.TextContent);
    }
}
