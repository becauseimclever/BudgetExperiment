// <copyright file="BudgetRadar.razor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using ApexCharts;

using BudgetExperiment.Client.Components.Charts.Models;
using BudgetExperiment.Client.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// ApexCharts radar chart for comparing budget vs actual spend across category axes.
/// </summary>
public partial class BudgetRadar
{
    private ApexChartOptions<RadarDataPoint> _options = new();

    private Dictionary<string, List<RadarDataPoint>> _radarPoints = new();

    /// <summary>
    /// Gets or sets the named data series to plot on the radar axes.
    /// </summary>
    [Parameter]
    public IReadOnlyList<RadarDataSeries>? Series
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the category axis labels (one per radar spoke).
    /// </summary>
    [Parameter]
    public IReadOnlyList<string>? Categories
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the accessibility label for the chart container.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Budget spend radar chart";

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        if (Series == null || Series.Count == 0 || Categories == null || Categories.Count == 0)
        {
            return;
        }

        var themeService = Services.GetService<IChartThemeService>();
        var colorProvider = Services.GetService<IChartColorProvider>();
        var themeMode = themeService?.GetApexChartsThemeMode() ?? "light";
        var palette = colorProvider?.GetDefaultPalette().ToList() ?? new List<string>();

        _options = new ApexChartOptions<RadarDataPoint>
        {
            Chart = new Chart { Type = ChartType.Radar, Background = "transparent" },
            Theme = new Theme { Mode = themeMode == "dark" ? Mode.Dark : Mode.Light },
            Colors = palette,
            Xaxis = new XAxis { Categories = Categories!.ToList() },
        };

        _radarPoints = Series
            .ToDictionary(
                s => s.Label,
                s => s.Values.Select((v, i) => new RadarDataPoint(Categories![i], v)).ToList());
    }

    private record RadarDataPoint(string Category, decimal Value);
}
