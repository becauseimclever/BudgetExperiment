// <copyright file="ExportChartButton.razor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// A button component that triggers chart export functionality.
/// </summary>
public partial class ExportChartButton
{
    /// <summary>
    /// Gets or sets the title of the chart being exported.
    /// </summary>
    [Parameter]
    public string ChartTitle { get; set; } = "chart";

    /// <summary>
    /// Gets or sets a value indicating whether the export button is visible.
    /// </summary>
    [Parameter]
    public bool Visible { get; set; } = true;

    private Task HandleExportAsync()
    {
        // TODO: wire JS interop for screenshot when browser API available
        return Task.CompletedTask;
    }
}
