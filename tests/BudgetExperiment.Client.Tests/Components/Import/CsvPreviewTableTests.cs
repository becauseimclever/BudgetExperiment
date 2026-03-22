// <copyright file="CsvPreviewTableTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Import;
using BudgetExperiment.Client.Services;

using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.Import;

/// <summary>
/// Unit tests for the <see cref="CsvPreviewTable"/> component.
/// </summary>
public sealed class CsvPreviewTableTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CsvPreviewTableTests"/> class.
    /// </summary>
    public CsvPreviewTableTests()
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
    /// Verifies that the table renders column headers.
    /// </summary>
    [Fact]
    public void CsvPreviewTable_RendersHeaders()
    {
        // Arrange & Act
        var cut = Render<CsvPreviewTable>(parameters => parameters
            .Add(p => p.Headers, new List<string> { "Date", "Description", "Amount" })
            .Add(p => p.Rows, CreateSampleRows(2)));

        // Assert
        var headers = cut.FindAll("thead th");
        Assert.Contains(headers, h => h.TextContent == "Date");
        Assert.Contains(headers, h => h.TextContent == "Description");
        Assert.Contains(headers, h => h.TextContent == "Amount");
    }

    /// <summary>
    /// Verifies that the table renders row data.
    /// </summary>
    [Fact]
    public void CsvPreviewTable_RendersRowData()
    {
        // Arrange
        var rows = new List<IReadOnlyList<string>>
        {
            new List<string> { "01/15/2026", "Coffee Shop", "5.50" },
        };

        // Act
        var cut = Render<CsvPreviewTable>(parameters => parameters
            .Add(p => p.Headers, new List<string> { "Date", "Description", "Amount" })
            .Add(p => p.Rows, rows));

        // Assert
        var cells = cut.FindAll("tbody td");
        Assert.Contains(cells, c => c.TextContent.Contains("01/15/2026"));
        Assert.Contains(cells, c => c.TextContent.Contains("Coffee Shop"));
        Assert.Contains(cells, c => c.TextContent.Contains("5.50"));
    }

    /// <summary>
    /// Verifies that the table limits displayed rows to MaxRows.
    /// </summary>
    [Fact]
    public void CsvPreviewTable_LimitsToMaxRows()
    {
        // Arrange & Act
        var cut = Render<CsvPreviewTable>(parameters => parameters
            .Add(p => p.Headers, new List<string> { "Col1" })
            .Add(p => p.Rows, CreateSampleRows(10))
            .Add(p => p.MaxRows, 3));

        // Assert
        var dataRows = cut.FindAll("tbody tr");
        Assert.Equal(3, dataRows.Count);
    }

    /// <summary>
    /// Verifies that the table shows row numbers.
    /// </summary>
    [Fact]
    public void CsvPreviewTable_ShowsRowNumbers()
    {
        // Arrange & Act
        var cut = Render<CsvPreviewTable>(parameters => parameters
            .Add(p => p.Headers, new List<string> { "Col1" })
            .Add(p => p.Rows, CreateSampleRows(3)));

        // Assert
        var rowNumberHeader = cut.Find("th.row-number");
        Assert.Equal("#", rowNumberHeader.TextContent);
    }

    /// <summary>
    /// Verifies that empty cells are displayed as "(empty)".
    /// </summary>
    [Fact]
    public void CsvPreviewTable_ShowsEmptyPlaceholder()
    {
        // Arrange
        var rows = new List<IReadOnlyList<string>>
        {
            new List<string> { string.Empty, "Data" },
        };

        // Act
        var cut = Render<CsvPreviewTable>(parameters => parameters
            .Add(p => p.Headers, new List<string> { "Col1", "Col2" })
            .Add(p => p.Rows, rows));

        // Assert
        Assert.Contains("(empty)", cut.Markup);
    }

    /// <summary>
    /// Verifies that the header shows the correct preview count text.
    /// </summary>
    [Fact]
    public void CsvPreviewTable_ShowsPreviewCountText()
    {
        // Arrange & Act
        var cut = Render<CsvPreviewTable>(parameters => parameters
            .Add(p => p.Headers, new List<string> { "Col1" })
            .Add(p => p.Rows, CreateSampleRows(10))
            .Add(p => p.MaxRows, 5));

        // Assert
        Assert.Contains("showing first 5 of 10 rows", cut.Markup);
    }

    private static List<IReadOnlyList<string>> CreateSampleRows(int count)
    {
        var rows = new List<IReadOnlyList<string>>();
        for (int i = 0; i < count; i++)
        {
            rows.Add(new List<string> { $"Value{i}" });
        }

        return rows;
    }
}
