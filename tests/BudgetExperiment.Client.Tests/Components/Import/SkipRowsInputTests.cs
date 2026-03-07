// <copyright file="SkipRowsInputTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Import;
using Bunit;

namespace BudgetExperiment.Client.Tests.Components.Import;

/// <summary>
/// Unit tests for the <see cref="SkipRowsInput"/> component.
/// </summary>
public sealed class SkipRowsInputTests : BunitContext
{
    /// <summary>
    /// Verifies that the input renders with a label.
    /// </summary>
    [Fact]
    public void SkipRowsInput_RendersLabel()
    {
        // Arrange & Act
        var cut = Render<SkipRowsInput>();

        // Assert
        var label = cut.Find(".form-label");
        Assert.Equal("Skip Metadata Rows", label.TextContent);
    }

    /// <summary>
    /// Verifies that the input renders with the initial value.
    /// </summary>
    [Fact]
    public void SkipRowsInput_RendersWithValue()
    {
        // Arrange & Act
        var cut = Render<SkipRowsInput>(parameters => parameters
            .Add(p => p.Value, 3));

        // Assert
        var input = cut.Find("input[type='number']");
        Assert.Equal("3", input.GetAttribute("value"));
    }

    /// <summary>
    /// Verifies that changing the value fires the ValueChanged callback.
    /// </summary>
    [Fact]
    public void SkipRowsInput_FiresValueChanged_OnChange()
    {
        // Arrange
        int? changedValue = null;
        var cut = Render<SkipRowsInput>(parameters => parameters
            .Add(p => p.ValueChanged, (int val) => { changedValue = val; }));

        // Act
        cut.Find("input[type='number']").Change("5");

        // Assert
        Assert.Equal(5, changedValue);
    }

    /// <summary>
    /// Verifies that the value is clamped to 0 minimum.
    /// </summary>
    [Fact]
    public void SkipRowsInput_ClampsToMinimumZero()
    {
        // Arrange
        int? changedValue = null;
        var cut = Render<SkipRowsInput>(parameters => parameters
            .Add(p => p.ValueChanged, (int val) => { changedValue = val; }));

        // Act
        cut.Find("input[type='number']").Change("-5");

        // Assert
        Assert.Equal(0, changedValue);
    }

    /// <summary>
    /// Verifies that the value is clamped to 100 maximum.
    /// </summary>
    [Fact]
    public void SkipRowsInput_ClampsToMaximumHundred()
    {
        // Arrange
        int? changedValue = null;
        var cut = Render<SkipRowsInput>(parameters => parameters
            .Add(p => p.ValueChanged, (int val) => { changedValue = val; }));

        // Act
        cut.Find("input[type='number']").Change("150");

        // Assert
        Assert.Equal(100, changedValue);
    }

    /// <summary>
    /// Verifies that the OnAfterValueChanged callback is invoked.
    /// </summary>
    [Fact]
    public void SkipRowsInput_FiresOnAfterValueChanged()
    {
        // Arrange
        int? afterValue = null;
        var cut = Render<SkipRowsInput>(parameters => parameters
            .Add(p => p.ValueChanged, (int _) => { })
            .Add(p => p.OnAfterValueChanged, (int val) => { afterValue = val; }));

        // Act
        cut.Find("input[type='number']").Change("2");

        // Assert
        Assert.Equal(2, afterValue);
    }

    /// <summary>
    /// Verifies that the input has min/max attributes.
    /// </summary>
    [Fact]
    public void SkipRowsInput_HasMinMaxAttributes()
    {
        // Arrange & Act
        var cut = Render<SkipRowsInput>();

        // Assert
        var input = cut.Find("input[type='number']");
        Assert.Equal("0", input.GetAttribute("min"));
        Assert.Equal("100", input.GetAttribute("max"));
    }
}
