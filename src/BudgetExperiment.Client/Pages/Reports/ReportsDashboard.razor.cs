// <copyright file="ReportsDashboard.razor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Charts;
using BudgetExperiment.Client.Components.Charts.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.Pages.Reports;

/// <summary>
/// Aggregate financial dashboard page combining treemap, waterfall, radar, heatmap, radial bar,
/// line chart, candlestick, box plot, scatter, grouped bar, stacked bar, radial gauge, and sparkline
/// charts for a monthly spending overview (Feature 127, Slice 10).
/// </summary>
public partial class ReportsDashboard
{
    private bool _isLoading = true;

    private IReadOnlyList<TreemapDataPoint> _treemapData = [];

    private IReadOnlyList<WaterfallSegment> _waterfallData = [];

    private IReadOnlyList<RadialBarSegment> _radialData = [];

    private IReadOnlyList<RadarDataSeries> _radarSeries = [];

    private IReadOnlyList<string> _radarCategories = [];

    private HeatmapDataPoint[][] _heatmapData = [[], [], [], [], [], [], []];

    private IReadOnlyList<LineData> _lineChartData = [];

    private IReadOnlyList<LineSeriesDefinition> _lineChartSeries = [];

    private IReadOnlyList<CandlestickDataPoint> _candlestickData = [];

    private IReadOnlyList<BoxPlotSummary> _boxPlotData = [];

    private IReadOnlyList<ScatterDataPoint> _scatterPoints = [];

    private IReadOnlyList<GroupedBarData> _groupedBarData = [];

    private IReadOnlyList<BarSeriesDefinition> _groupedBarSeries = [];

    private IReadOnlyList<GroupedBarData> _stackedBarData = [];

    private IReadOnlyList<BarSeriesDefinition> _stackedBarSeries = [];

    private decimal _gaugeValue;

    private decimal _gaugeMax = 1m;

    private IReadOnlyList<decimal> _sparklineValues = [];

    private int _selectedYear = DateTime.UtcNow.Year;

    private int _selectedMonth = DateTime.UtcNow.Month;

    private Guid? _selectedCategoryId;

    [Inject]
    private IBudgetApiService BudgetApiService { get; set; } = default!;

    [Inject]
    private IChartDataService ChartDataService { get; set; } = default!;

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await LoadDashboardAsync();
    }

    private async Task LoadDashboardAsync()
    {
        _isLoading = true;

        MonthlyCategoryReportDto? categoryReport = null;
        try
        {
            categoryReport = await BudgetApiService.GetMonthlyCategoryReportAsync(_selectedYear, _selectedMonth);
        }
        catch (Exception)
        {
            // Fault-tolerant: continue with empty data.
        }

        if (categoryReport != null)
        {
            _treemapData = categoryReport.Categories
                .Where(c => c.Amount.Amount > 0)
                .Select(c => new TreemapDataPoint(c.CategoryName, c.Amount.Amount, c.CategoryColor))
                .ToArray();

            _waterfallData = ChartDataService.BuildBudgetWaterfall(
                categoryReport.TotalIncome.Amount,
                categoryReport.Categories);

            try
            {
                var monthStart = new DateOnly(_selectedYear, _selectedMonth, 1);
                var daysInMonth = DateTime.DaysInMonth(_selectedYear, _selectedMonth);
                var monthEnd = new DateOnly(_selectedYear, _selectedMonth, daysInMonth);
                var transactions = await BudgetApiService.GetTransactionsAsync(monthStart, monthEnd);
                _heatmapData = ChartDataService.BuildSpendingHeatmap(transactions);
                _boxPlotData = ChartDataService.BuildCategoryDistributions(transactions);
                _scatterPoints = transactions
                    .Select(t => new ScatterDataPoint(t.Date, t.Amount.Amount, t.CategoryName ?? "Unknown"))
                    .ToArray();
            }
            catch (Exception)
            {
                // Fault-tolerant: use empty heatmap data.
            }
        }

        try
        {
            var budgetSummary = await BudgetApiService.GetBudgetSummaryAsync(_selectedYear, _selectedMonth);
            if (budgetSummary != null)
            {
                _radialData = budgetSummary.CategoryProgress
                    .Where(c => c.TargetAmount.Amount > 0)
                    .Take(8)
                    .Select(c => new RadialBarSegment(
                        c.CategoryName,
                        c.SpentAmount.Amount,
                        c.TargetAmount.Amount,
                        c.CategoryColor ?? "var(--color-primary, #6366f1)"))
                    .ToArray();

                _radarCategories = budgetSummary.CategoryProgress
                    .Select(c => c.CategoryName)
                    .ToArray();

                _radarSeries =
                [
                    new RadarDataSeries("Budget", budgetSummary.CategoryProgress.Select(c => c.TargetAmount.Amount).ToArray()),
                    new RadarDataSeries("Actual", budgetSummary.CategoryProgress.Select(c => c.SpentAmount.Amount).ToArray()),
                ];

                _groupedBarData = budgetSummary.CategoryProgress
                    .Select(c => new GroupedBarData
                    {
                        GroupId = c.CategoryName,
                        GroupLabel = c.CategoryName,
                        Values = new Dictionary<string, decimal>
                        {
                            ["budget"] = c.TargetAmount.Amount,
                            ["actual"] = c.SpentAmount.Amount,
                        }.AsReadOnly(),
                    })
                    .ToArray();

                _groupedBarSeries =
                [
                    new BarSeriesDefinition { Id = "budget", Label = "Budget", Color = "#6366f1" },
                    new BarSeriesDefinition { Id = "actual", Label = "Actual", Color = "#22c55e" },
                ];

                _gaugeValue = budgetSummary.TotalSpent.Amount;
                _gaugeMax = budgetSummary.TotalBudgeted.Amount > 0 ? budgetSummary.TotalBudgeted.Amount : 1m;
            }
        }
        catch (Exception)
        {
            // Fault-tolerant: use empty radar and radial bar data.
        }

        try
        {
            var trends = await BudgetApiService.GetSpendingTrendsAsync(12);
            if (trends != null)
            {
                _lineChartData = trends.MonthlyData
                    .Select(m => new LineData { Label = $"{m.Year}-{m.Month:D2}", Value = m.TotalSpending.Amount })
                    .ToArray();

                _lineChartSeries =
                [
                    new LineSeriesDefinition { Id = "spending", Label = "Monthly Spending", Color = "#6366f1" },
                ];

                _candlestickData = ChartDataService.BuildTrendCandlesticks(trends.MonthlyData);

                _sparklineValues = trends.MonthlyData.Select(m => m.TotalSpending.Amount).ToArray();

                _stackedBarData = trends.MonthlyData
                    .Select(m => new GroupedBarData
                    {
                        GroupId = $"{m.Year}-{m.Month:D2}",
                        GroupLabel = $"{m.Year}-{m.Month:D2}",
                        Values = new Dictionary<string, decimal>
                        {
                            ["income"] = m.TotalIncome.Amount,
                            ["spending"] = m.TotalSpending.Amount,
                        }.AsReadOnly(),
                    })
                    .ToArray();

                _stackedBarSeries =
                [
                    new BarSeriesDefinition { Id = "income", Label = "Income", Color = "#22c55e" },
                    new BarSeriesDefinition { Id = "spending", Label = "Spending", Color = "#ef4444" },
                ];
            }
        }
        catch (Exception)
        {
            // Fault-tolerant: continue with empty trends data.
        }

        _isLoading = false;
    }

    private void ClearFilter()
    {
        _selectedCategoryId = null;
    }
}
