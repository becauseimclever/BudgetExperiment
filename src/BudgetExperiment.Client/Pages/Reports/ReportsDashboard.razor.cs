// <copyright file="ReportsDashboard.razor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Charts.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.Pages.Reports;

/// <summary>
/// Aggregate financial dashboard page combining treemap, waterfall, radar, heatmap, and radial bar
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
            }
        }
        catch (Exception)
        {
            // Fault-tolerant: use empty radar and radial bar data.
        }

        _isLoading = false;
    }

    private void ClearFilter()
    {
        _selectedCategoryId = null;
    }
}
