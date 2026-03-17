// <copyright file="ColumnMappingEditorTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Import;
using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.Import;

/// <summary>
/// Unit tests for the <see cref="ColumnMappingEditor"/> component.
/// </summary>
public sealed class ColumnMappingEditorTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnMappingEditorTests"/> class.
    /// </summary>
    public ColumnMappingEditorTests()
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
    /// Verifies that the editor renders header row and column mappings.
    /// </summary>
    [Fact]
    public void ColumnMappingEditor_RendersHeaderAndMappings()
    {
        // Arrange
        var mappings = CreateTestMappings();

        // Act
        var cut = Render<ColumnMappingEditor>(parameters => parameters
            .Add(p => p.Mappings, mappings));

        // Assert
        Assert.Contains("Column Mappings", cut.Markup);
        var rows = cut.FindAll(".mapping-row");
        Assert.Equal(3, rows.Count);
    }

    /// <summary>
    /// Verifies that column names are displayed.
    /// </summary>
    [Fact]
    public void ColumnMappingEditor_DisplaysColumnNames()
    {
        // Arrange
        var mappings = CreateTestMappings();

        // Act
        var cut = Render<ColumnMappingEditor>(parameters => parameters
            .Add(p => p.Mappings, mappings));

        // Assert
        Assert.Contains("Date", cut.Markup);
        Assert.Contains("Description", cut.Markup);
        Assert.Contains("Amount", cut.Markup);
    }

    /// <summary>
    /// Verifies sample values are shown for each column.
    /// </summary>
    [Fact]
    public void ColumnMappingEditor_DisplaysSampleValues()
    {
        // Arrange
        var mappings = CreateTestMappings();

        // Act
        var cut = Render<ColumnMappingEditor>(parameters => parameters
            .Add(p => p.Mappings, mappings));

        // Assert
        Assert.Contains("01/15/2026", cut.Markup);
        Assert.Contains("Coffee Shop", cut.Markup);
    }

    /// <summary>
    /// Verifies the required field badge shows warning when not mapped.
    /// </summary>
    [Fact]
    public void ColumnMappingEditor_ShowsRequiredBadgeWarning_WhenNotMapped()
    {
        // Arrange
        var mappings = new List<ColumnMappingState>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Col1", SampleValues = new[] { "data" } },
        };

        // Act
        var cut = Render<ColumnMappingEditor>(parameters => parameters
            .Add(p => p.Mappings, mappings));

        // Assert
        var badges = cut.FindAll(".badge-warning");
        Assert.True(badges.Count >= 3, "Should show warning badges for Date, Description, Amount");
    }

    /// <summary>
    /// Verifies the required field badge shows success when mapped.
    /// </summary>
    [Fact]
    public void ColumnMappingEditor_ShowsSuccessBadge_WhenRequiredFieldsMapped()
    {
        // Arrange
        var mappings = new List<ColumnMappingState>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date, SampleValues = new[] { "01/01/2026" } },
            new() { ColumnIndex = 1, ColumnHeader = "Desc", TargetField = ImportField.Description, SampleValues = new[] { "Test" } },
            new() { ColumnIndex = 2, ColumnHeader = "Amt", TargetField = ImportField.Amount, SampleValues = new[] { "10.00" } },
        };

        // Act
        var cut = Render<ColumnMappingEditor>(parameters => parameters
            .Add(p => p.Mappings, mappings));

        // Assert
        var successBadges = cut.FindAll(".badge-success");
        Assert.Equal(3, successBadges.Count);
    }

    /// <summary>
    /// Verifies that mapped fields show a checkmark indicator.
    /// </summary>
    [Fact]
    public void ColumnMappingEditor_ShowsCheckmark_WhenFieldMapped()
    {
        // Arrange
        var mappings = new List<ColumnMappingState>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date, SampleValues = new[] { "01/01/2026" } },
        };

        // Act
        var cut = Render<ColumnMappingEditor>(parameters => parameters
            .Add(p => p.Mappings, mappings));

        // Assert
        var indicators = cut.FindAll(".mapped-indicator");
        Assert.Single(indicators);
    }

    /// <summary>
    /// Verifies that OnMappingChanged is fired when a field is selected.
    /// </summary>
    [Fact]
    public void ColumnMappingEditor_FiresOnMappingChanged()
    {
        // Arrange
        var mappingChanged = false;
        var mappings = CreateTestMappings();
        var cut = Render<ColumnMappingEditor>(parameters => parameters
            .Add(p => p.Mappings, mappings)
            .Add(p => p.OnMappingChanged, () => { mappingChanged = true; }));

        // Act
        var selects = cut.FindAll("select");
        selects[0].Change(ImportField.Date.ToString());

        // Assert
        Assert.True(mappingChanged);
    }

    private static List<ColumnMappingState> CreateTestMappings()
    {
        return new List<ColumnMappingState>
        {
            new() { ColumnIndex = 0, ColumnHeader = "Date", SampleValues = new[] { "01/15/2026", "01/16/2026" } },
            new() { ColumnIndex = 1, ColumnHeader = "Description", SampleValues = new[] { "Coffee Shop", "Grocery Store" } },
            new() { ColumnIndex = 2, ColumnHeader = "Amount", SampleValues = new[] { "-5.50", "-42.99" } },
        };
    }
}
