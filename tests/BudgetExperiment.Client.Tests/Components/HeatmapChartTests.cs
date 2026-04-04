// <copyright file="HeatmapChartTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

// NOTE: These tests are intentionally RED until Lucius creates:
//   - BudgetExperiment.Client.Components.Charts.HeatmapChart (Razor component)
// They define the expected parameter API and rendered HTML contract.
using BudgetExperiment.Client.Components.Charts;
using BudgetExperiment.Client.Components.Charts.Models;

using Bunit;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the HeatmapChart component.
/// </summary>
public class HeatmapChartTests : BunitContext
{
    [Fact]
    public void HeatmapChart_Renders_EmptyState_WhenNullData()
    {
        // Act
        var cut = Render<HeatmapChart>(parameters => parameters
            .Add(p => p.Data, (HeatmapDataPoint[][]?)null));

        // Assert
        var empty = cut.Find(".heatmap-chart-empty");
        Assert.NotNull(empty);
        Assert.Contains("No data to display", empty.TextContent);
    }

    [Fact]
    public void HeatmapChart_Renders_EmptyState_WhenAllZeroData()
    {
        // Arrange — 7 rows but no cells in any row
        var data = new HeatmapDataPoint[7][];
        for (var i = 0; i < 7; i++)
        {
            data[i] = [];
        }

        // Act
        var cut = Render<HeatmapChart>(parameters => parameters
            .Add(p => p.Data, data));

        // Assert
        var empty = cut.Find(".heatmap-chart-empty");
        Assert.NotNull(empty);
        Assert.Contains("No data to display", empty.TextContent);
    }

    [Fact]
    public void HeatmapChart_Renders_SVG_WhenDataProvided()
    {
        // Arrange — one non-zero cell in week 0 so component has data
        var data = BuildWeekData(weekCount: 1, nonZeroDayIndex: 0, amount: 42m);

        // Act
        var cut = Render<HeatmapChart>(parameters => parameters
            .Add(p => p.Data, data));

        // Assert
        var svg = cut.Find("svg.heatmap-svg");
        Assert.NotNull(svg);
    }

    [Fact]
    public void HeatmapChart_Renders_7RowLabels()
    {
        // Arrange
        var data = BuildWeekData(weekCount: 1, nonZeroDayIndex: 0, amount: 10m);

        // Act
        var cut = Render<HeatmapChart>(parameters => parameters
            .Add(p => p.Data, data));

        // Assert
        var labels = cut.FindAll(".heatmap-row-label");
        Assert.Equal(7, labels.Count);
        Assert.Contains(labels, l => l.TextContent.Trim() == "Mon");
        Assert.Contains(labels, l => l.TextContent.Trim() == "Sun");
    }

    [Fact]
    public void HeatmapChart_Renders_CorrectCellCount()
    {
        // Arrange — 3 weeks of data → 7 rows × 3 columns = 21 cells
        var data = BuildWeekData(weekCount: 3, nonZeroDayIndex: 0, amount: 10m);

        // Act
        var cut = Render<HeatmapChart>(parameters => parameters
            .Add(p => p.Data, data));

        // Assert
        var cells = cut.FindAll("rect.heatmap-cell");
        Assert.Equal(21, cells.Count);
    }

    [Fact]
    public void HeatmapChart_Cell_HasCorrectDataAttributes()
    {
        // Arrange — non-zero cell at DayOfWeek=2, WeekIndex=1 alongside a surrounding grid
        var data = new HeatmapDataPoint[7][];
        for (var day = 0; day < 7; day++)
        {
            data[day] =
            [
                new HeatmapDataPoint(DayOfWeek: day, WeekIndex: 0, TotalAmount: 0m, TransactionCount: 0),
                new HeatmapDataPoint(DayOfWeek: day, WeekIndex: 1, TotalAmount: day == 2 ? 50m : 0m, TransactionCount: day == 2 ? 3 : 0),
            ];
        }

        // Act
        var cut = Render<HeatmapChart>(parameters => parameters
            .Add(p => p.Data, data));

        // Assert — locate the specific cell by its data attributes
        var cells = cut.FindAll("rect.heatmap-cell");
        var target = cells.FirstOrDefault(c =>
            c.GetAttribute("data-day") == "2" && c.GetAttribute("data-week") == "1");
        Assert.NotNull(target);
    }

    [Fact]
    public void HeatmapChart_UsesAriaLabel()
    {
        // Arrange
        var data = BuildWeekData(weekCount: 1, nonZeroDayIndex: 0, amount: 10m);
        const string label = "Monthly spending heatmap";

        // Act
        var cut = Render<HeatmapChart>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.AriaLabel, label));

        // Assert
        var container = cut.Find(".heatmap-chart");
        Assert.Equal(label, container.GetAttribute("aria-label"));
    }

    [Fact]
    public void HeatmapChart_ZeroCells_HaveMinimalFill()
    {
        // Arrange — all 7 rows have one cell each with TotalAmount=0 (arrays non-empty so SVG renders)
        var data = new HeatmapDataPoint[7][];
        for (var i = 0; i < 7; i++)
        {
            data[i] = [new HeatmapDataPoint(DayOfWeek: i, WeekIndex: 0, TotalAmount: 0m, TransactionCount: 0)];
        }

        // Act
        var cut = Render<HeatmapChart>(parameters => parameters
            .Add(p => p.Data, data));

        // Assert — zero-amount cells carry the empty marker class, differentiating them from active cells
        var emptyCells = cut.FindAll("rect.heatmap-cell-empty");
        Assert.True(emptyCells.Count > 0);
        foreach (var cell in emptyCells)
        {
            Assert.Contains("heatmap-cell", cell.ClassName ?? string.Empty);
        }
    }

    /// <summary>
    /// Builds a 7-row heatmap data array with <paramref name="weekCount"/> columns,
    /// placing <paramref name="amount"/> in row <paramref name="nonZeroDayIndex"/>, week 0.
    /// All other cells have TotalAmount=0.
    /// </summary>
    private static HeatmapDataPoint[][] BuildWeekData(int weekCount, int nonZeroDayIndex, decimal amount)
    {
        var data = new HeatmapDataPoint[7][];
        for (var day = 0; day < 7; day++)
        {
            var row = new HeatmapDataPoint[weekCount];
            for (var week = 0; week < weekCount; week++)
            {
                var cellAmount = (day == nonZeroDayIndex && week == 0) ? amount : 0m;
                row[week] = new HeatmapDataPoint(
                    DayOfWeek: day,
                    WeekIndex: week,
                    TotalAmount: cellAmount,
                    TransactionCount: cellAmount > 0 ? 1 : 0);
            }

            data[day] = row;
        }

        return data;
    }
}
