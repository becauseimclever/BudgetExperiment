// <copyright file="DuplicateWarningCardTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Import;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.Import;

/// <summary>
/// Unit tests for the <see cref="DuplicateWarningCard"/> component.
/// </summary>
public sealed class DuplicateWarningCardTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateWarningCardTests"/> class.
    /// </summary>
    public DuplicateWarningCardTests()
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
    /// Verifies that the card shows the duplicate count.
    /// </summary>
    [Fact]
    public void DuplicateWarningCard_ShowsDuplicateCount()
    {
        // Arrange & Act
        var cut = Render<DuplicateWarningCard>(parameters => parameters
            .Add(p => p.DuplicateCount, 3)
            .Add(p => p.DuplicateRows, CreateDuplicateRows(3)));

        // Assert
        Assert.Contains("3 Potential Duplicates Found", cut.Markup);
    }

    /// <summary>
    /// Verifies singular text for single duplicate.
    /// </summary>
    [Fact]
    public void DuplicateWarningCard_ShowsSingularText_ForOneDuplicate()
    {
        // Arrange & Act
        var cut = Render<DuplicateWarningCard>(parameters => parameters
            .Add(p => p.DuplicateCount, 1)
            .Add(p => p.DuplicateRows, CreateDuplicateRows(1)));

        // Assert
        Assert.Contains("1 Potential Duplicate Found", cut.Markup);
    }

    /// <summary>
    /// Verifies the details are initially hidden.
    /// </summary>
    [Fact]
    public void DuplicateWarningCard_DetailsInitiallyHidden()
    {
        // Arrange & Act
        var cut = Render<DuplicateWarningCard>(parameters => parameters
            .Add(p => p.DuplicateCount, 2)
            .Add(p => p.DuplicateRows, CreateDuplicateRows(2)));

        // Assert
        Assert.Empty(cut.FindAll(".duplicate-details"));
        Assert.Contains("Show Details", cut.Markup);
    }

    /// <summary>
    /// Verifies that clicking Show Details toggles visibility.
    /// </summary>
    [Fact]
    public void DuplicateWarningCard_ToggleDetails()
    {
        // Arrange
        var cut = Render<DuplicateWarningCard>(parameters => parameters
            .Add(p => p.DuplicateCount, 2)
            .Add(p => p.DuplicateRows, CreateDuplicateRows(2)));

        // Act - show details
        var toggleButton = cut.FindAll("button").First(b => b.TextContent.Contains("Show Details"));
        toggleButton.Click();

        // Assert
        Assert.Single(cut.FindAll(".duplicate-details"));
        Assert.Contains("Hide Details", cut.Markup);
    }

    /// <summary>
    /// Verifies that Include All Duplicates adds all row indices.
    /// </summary>
    [Fact]
    public void DuplicateWarningCard_IncludeAllDuplicates_AddsIndices()
    {
        // Arrange
        var selectedIndices = new HashSet<int>();
        HashSet<int>? updatedIndices = null;
        var rows = CreateDuplicateRows(3);

        var cut = Render<DuplicateWarningCard>(parameters => parameters
            .Add(p => p.DuplicateCount, 3)
            .Add(p => p.DuplicateRows, rows)
            .Add(p => p.SelectedIndices, selectedIndices)
            .Add(p => p.SelectedIndicesChanged, (HashSet<int> indices) => { updatedIndices = indices; }));

        // Act
        var includeButton = cut.FindAll("button").First(b => b.TextContent.Contains("Include All"));
        includeButton.Click();

        // Assert
        Assert.NotNull(updatedIndices);
        Assert.Equal(3, updatedIndices!.Count);
    }

    /// <summary>
    /// Verifies that Exclude All Duplicates removes all row indices.
    /// </summary>
    [Fact]
    public void DuplicateWarningCard_ExcludeAllDuplicates_RemovesIndices()
    {
        // Arrange
        var rows = CreateDuplicateRows(3);
        var selectedIndices = new HashSet<int>(rows.Select(r => r.RowIndex));
        HashSet<int>? updatedIndices = null;

        var cut = Render<DuplicateWarningCard>(parameters => parameters
            .Add(p => p.DuplicateCount, 3)
            .Add(p => p.DuplicateRows, rows)
            .Add(p => p.SelectedIndices, selectedIndices)
            .Add(p => p.SelectedIndicesChanged, (HashSet<int> indices) => { updatedIndices = indices; }));

        // Act
        var excludeButton = cut.FindAll("button").First(b => b.TextContent.Contains("Exclude All"));
        excludeButton.Click();

        // Assert
        Assert.NotNull(updatedIndices);
        Assert.Empty(updatedIndices!);
    }

    /// <summary>
    /// Verifies that only 5 duplicates are shown in details, with "more" text for extras.
    /// </summary>
    [Fact]
    public void DuplicateWarningCard_ShowsMaxFiveDuplicates_WithMoreText()
    {
        // Arrange
        var cut = Render<DuplicateWarningCard>(parameters => parameters
            .Add(p => p.DuplicateCount, 8)
            .Add(p => p.DuplicateRows, CreateDuplicateRows(8)));

        // Act - show details
        var toggleButton = cut.FindAll("button").First(b => b.TextContent.Contains("Show Details"));
        toggleButton.Click();

        // Assert
        var tableRows = cut.FindAll(".duplicate-details tbody tr");
        Assert.Equal(5, tableRows.Count);
        Assert.Contains("3 more", cut.Markup);
    }

    private static List<ImportPreviewRow> CreateDuplicateRows(int count)
    {
        var rows = new List<ImportPreviewRow>();
        for (int i = 0; i < count; i++)
        {
            rows.Add(new ImportPreviewRow
            {
                RowIndex = i,
                Date = new DateOnly(2026, 1, 15 + (i % 15)),
                Description = $"Duplicate Transaction {i}",
                Amount = -(10.00m + i),
                Status = ImportRowStatus.Duplicate,
                StatusMessage = "Possible duplicate",
            });
        }

        return rows;
    }
}
