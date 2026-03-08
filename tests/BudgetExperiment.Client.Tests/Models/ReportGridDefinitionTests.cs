// <copyright file="ReportGridDefinitionTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using Shouldly;

namespace BudgetExperiment.Client.Tests.Models;

/// <summary>
/// Unit tests for <see cref="ReportGridDefinition"/>.
/// </summary>
public class ReportGridDefinitionTests
{
    /// <summary>
    /// Verifies CreateDefault returns non-null instance.
    /// </summary>
    [Fact]
    public void CreateDefault_ReturnsInstance()
    {
        var grid = ReportGridDefinition.CreateDefault();

        grid.ShouldNotBeNull();
        grid.RowHeight.ShouldBe(24);
        grid.Gap.ShouldBe(12);
        grid.Version.ShouldBe(1);
    }

    /// <summary>
    /// Verifies CreateDefault has all breakpoints.
    /// </summary>
    [Fact]
    public void CreateDefault_HasAllBreakpoints()
    {
        var grid = ReportGridDefinition.CreateDefault();

        grid.Breakpoints.ContainsKey("lg").ShouldBeTrue();
        grid.Breakpoints.ContainsKey("md").ShouldBeTrue();
        grid.Breakpoints.ContainsKey("sm").ShouldBeTrue();
    }

    /// <summary>
    /// Verifies default column counts.
    /// </summary>
    [Fact]
    public void CreateDefault_HasCorrectColumnCounts()
    {
        var grid = ReportGridDefinition.CreateDefault();

        grid.GetColumns("lg").ShouldBe(12);
        grid.GetColumns("md").ShouldBe(8);
        grid.GetColumns("sm").ShouldBe(4);
    }

    /// <summary>
    /// Verifies GetColumns returns default for unknown breakpoint.
    /// </summary>
    [Fact]
    public void GetColumns_ReturnsDefault_ForUnknownBreakpoint()
    {
        var grid = ReportGridDefinition.CreateDefault();

        grid.GetColumns("xl").ShouldBe(12);
    }

    /// <summary>
    /// Verifies Normalize creates breakpoints if null.
    /// </summary>
    [Fact]
    public void Normalize_CreatesBreakpoints_WhenNull()
    {
        var grid = new ReportGridDefinition { Breakpoints = null! };

        grid.Normalize();

        grid.Breakpoints.ShouldNotBeNull();
        grid.Breakpoints.ContainsKey("lg").ShouldBeTrue();
        grid.Breakpoints.ContainsKey("md").ShouldBeTrue();
        grid.Breakpoints.ContainsKey("sm").ShouldBeTrue();
    }

    /// <summary>
    /// Verifies Normalize fixes zero-column breakpoints.
    /// </summary>
    [Fact]
    public void Normalize_FixesZeroColumnBreakpoints()
    {
        var grid = new ReportGridDefinition();
        grid.Breakpoints["lg"] = new ReportGridBreakpointDefinition { Columns = 0 };

        grid.Normalize();

        grid.GetColumns("lg").ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Verifies GetColumns returns at least 1 for low values.
    /// </summary>
    [Fact]
    public void GetColumns_ReturnsAtLeastOne()
    {
        var grid = new ReportGridDefinition();
        grid.Breakpoints["lg"] = new ReportGridBreakpointDefinition { Columns = -5 };

        grid.GetColumns("lg").ShouldBeGreaterThanOrEqualTo(1);
    }

    /// <summary>
    /// Verifies Normalize preserves existing non-zero breakpoints.
    /// </summary>
    [Fact]
    public void Normalize_PreservesExistingBreakpoints()
    {
        var grid = new ReportGridDefinition();
        grid.Breakpoints["lg"] = new ReportGridBreakpointDefinition { Columns = 6 };

        grid.Normalize();

        grid.GetColumns("lg").ShouldBe(6);
    }

    /// <summary>
    /// Verifies Normalize adds missing breakpoints.
    /// </summary>
    [Fact]
    public void Normalize_AddsMissingBreakpoints()
    {
        var grid = new ReportGridDefinition();
        grid.Breakpoints.Remove("md");

        grid.Normalize();

        grid.Breakpoints.ContainsKey("md").ShouldBeTrue();
        grid.GetColumns("md").ShouldBe(8);
    }
}
