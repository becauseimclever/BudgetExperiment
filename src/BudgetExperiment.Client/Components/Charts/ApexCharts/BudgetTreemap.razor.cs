// <copyright file="BudgetTreemap.razor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using ApexCharts;

using BudgetExperiment.Client.Components.Charts.Models;
using BudgetExperiment.Client.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// ApexCharts treemap chart for visualizing spending distribution by category.
/// </summary>
public partial class BudgetTreemap
{
    private ApexChartOptions<TreemapDataPoint> _options = new();

    /// <summary>
    /// Gets or sets the data points defining each treemap segment.
    /// </summary>
    [Parameter]
    public IReadOnlyList<TreemapDataPoint>? DataPoints
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the accessibility label for the chart container.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Category spending treemap";

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        var themeService = Services.GetService<IChartThemeService>();
        var colorProvider = Services.GetService<IChartColorProvider>();
        var themeMode = themeService?.GetApexChartsThemeMode() ?? "light";
        var palette = colorProvider?.GetDefaultPalette().ToList() ?? new List<string>();

        _options = new ApexChartOptions<TreemapDataPoint>
        {
            Chart = new Chart { Type = ChartType.Treemap, Background = "transparent" },
            Theme = new Theme { Mode = themeMode == "dark" ? Mode.Dark : Mode.Light },
            Colors = palette,
            NoData = new NoData { Text = "No data" },
        };
    }
}
