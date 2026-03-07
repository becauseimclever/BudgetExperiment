// <copyright file="DateFormatSelectorTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Import;
using Bunit;

namespace BudgetExperiment.Client.Tests.Components.Import;

/// <summary>
/// Unit tests for the <see cref="DateFormatSelector"/> component.
/// </summary>
public sealed class DateFormatSelectorTests : BunitContext
{
    /// <summary>
    /// Verifies that the selector renders with a label.
    /// </summary>
    [Fact]
    public void DateFormatSelector_RendersLabel()
    {
        // Arrange & Act
        var cut = Render<DateFormatSelector>();

        // Assert
        var label = cut.Find(".form-label");
        Assert.Equal("Date Format", label.TextContent);
    }

    /// <summary>
    /// Verifies that the selector renders nine format options.
    /// </summary>
    [Fact]
    public void DateFormatSelector_RendersNineOptions()
    {
        // Arrange & Act
        var cut = Render<DateFormatSelector>();

        // Assert
        var options = cut.FindAll("option");
        Assert.Equal(9, options.Count);
    }

    /// <summary>
    /// Verifies that the default value is MM/dd/yyyy.
    /// </summary>
    [Fact]
    public void DateFormatSelector_DefaultsToUSFormat()
    {
        // Arrange & Act
        var cut = Render<DateFormatSelector>();

        // Assert
        var select = cut.Find("select");
        Assert.Equal("MM/dd/yyyy", select.GetAttribute("value"));
    }

    /// <summary>
    /// Verifies that changing the selection fires the ValueChanged callback.
    /// </summary>
    [Fact]
    public void DateFormatSelector_FiresValueChanged_OnChange()
    {
        // Arrange
        string? selectedFormat = null;
        var cut = Render<DateFormatSelector>(parameters => parameters
            .Add(p => p.ValueChanged, (string format) => { selectedFormat = format; }));

        // Act
        cut.Find("select").Change("yyyy-MM-dd");

        // Assert
        Assert.Equal("yyyy-MM-dd", selectedFormat);
    }

    /// <summary>
    /// Verifies that the selector renders with the specified value.
    /// </summary>
    [Fact]
    public void DateFormatSelector_RendersWithSpecifiedValue()
    {
        // Arrange & Act
        var cut = Render<DateFormatSelector>(parameters => parameters
            .Add(p => p.Value, "dd/MM/yyyy"));

        // Assert
        var select = cut.Find("select");
        Assert.Equal("dd/MM/yyyy", select.GetAttribute("value"));
    }
}
