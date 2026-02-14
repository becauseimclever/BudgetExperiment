// <copyright file="ReportGridDefinition.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Represents grid layout defaults for custom report layouts.
/// </summary>
public sealed class ReportGridDefinition
{
    private const int DefaultColumnsLg = 12;
    private const int DefaultColumnsMd = 8;
    private const int DefaultColumnsSm = 4;

    /// <summary>
    /// Gets or sets the grid definition version.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the fixed row height in pixels.
    /// </summary>
    public int RowHeight { get; set; } = 24;

    /// <summary>
    /// Gets or sets the gap size in pixels.
    /// </summary>
    public int Gap { get; set; } = 12;

    /// <summary>
    /// Gets or sets the breakpoint definitions.
    /// </summary>
    public Dictionary<string, ReportGridBreakpointDefinition> Breakpoints { get; set; } =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["lg"] = new ReportGridBreakpointDefinition { Columns = DefaultColumnsLg },
            ["md"] = new ReportGridBreakpointDefinition { Columns = DefaultColumnsMd },
            ["sm"] = new ReportGridBreakpointDefinition { Columns = DefaultColumnsSm },
        };

    /// <summary>
    /// Creates a default grid definition.
    /// </summary>
    /// <returns>A new default grid definition.</returns>
    public static ReportGridDefinition CreateDefault()
    {
        return new ReportGridDefinition();
    }

    /// <summary>
    /// Normalizes breakpoint definitions and ensures defaults exist.
    /// </summary>
    public void Normalize()
    {
        Breakpoints ??= new Dictionary<string, ReportGridBreakpointDefinition>(StringComparer.OrdinalIgnoreCase);

        EnsureBreakpoint("lg", DefaultColumnsLg);
        EnsureBreakpoint("md", DefaultColumnsMd);
        EnsureBreakpoint("sm", DefaultColumnsSm);
    }

    /// <summary>
    /// Gets the column count for a breakpoint.
    /// </summary>
    /// <param name="breakpoint">Breakpoint key.</param>
    /// <returns>Column count.</returns>
    public int GetColumns(string breakpoint)
    {
        if (Breakpoints.TryGetValue(breakpoint, out var definition))
        {
            return Math.Max(1, definition.Columns);
        }

        return breakpoint switch
        {
            "md" => DefaultColumnsMd,
            "sm" => DefaultColumnsSm,
            _ => DefaultColumnsLg,
        };
    }

    private void EnsureBreakpoint(string key, int defaultColumns)
    {
        if (!Breakpoints.TryGetValue(key, out var definition))
        {
            Breakpoints[key] = new ReportGridBreakpointDefinition { Columns = defaultColumns };
            return;
        }

        definition.Columns = Math.Max(1, definition.Columns == 0 ? defaultColumns : definition.Columns);
    }
}
