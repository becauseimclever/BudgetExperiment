// <copyright file="CustomReportLayoutDefinition.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Represents the client-side layout definition for a custom report.
/// </summary>
public sealed class CustomReportLayoutDefinition
{
    /// <summary>
    /// Gets or sets the widgets in the layout.
    /// </summary>
    public List<ReportWidgetDefinition> Widgets { get; set; } = [];
}
