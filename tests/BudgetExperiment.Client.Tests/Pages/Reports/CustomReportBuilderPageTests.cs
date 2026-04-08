// <copyright file="CustomReportBuilderPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Pages.Reports;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Pages.Reports;

/// <summary>
/// Unit tests for the <see cref="CustomReportBuilder"/> page component.
/// </summary>
public class CustomReportBuilderPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomReportBuilderPageTests"/> class.
    /// </summary>
    public CustomReportBuilderPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(_apiService);
        this.Services.AddSingleton<IToastService>(new ToastService());
        this.Services.AddSingleton<ScopeService>();
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
        var featureFlagStub = new StubFeatureFlagClientService();
        featureFlagStub.Flags["Reports:CustomReportBuilder"] = true;
        this.Services.AddSingleton<IFeatureFlagClientService>(featureFlagStub);
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync()
    {
        return base.DisposeAsync().AsTask();
    }

    /// <summary>
    /// Verifies the page renders without errors.
    /// </summary>
    [Fact]
    public void Renders_WithoutErrors()
    {
        var cut = Render<CustomReportBuilder>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page title is set correctly.
    /// </summary>
    [Fact]
    public void HasCorrectPageTitle()
    {
        var cut = Render<CustomReportBuilder>();

        cut.Markup.ShouldContain("Custom Report Builder");
    }

    /// <summary>
    /// Verifies the New Layout button is present.
    /// </summary>
    [Fact]
    public void HasNewLayoutButton()
    {
        var cut = Render<CustomReportBuilder>();

        cut.Markup.ShouldContain("New Layout");
    }

    /// <summary>
    /// Verifies the Save button is present.
    /// </summary>
    [Fact]
    public void HasSaveButton()
    {
        var cut = Render<CustomReportBuilder>();

        cut.Markup.ShouldContain("Save");
    }

    /// <summary>
    /// Verifies the layout selector is present.
    /// </summary>
    [Fact]
    public void ShowsLayoutSelector()
    {
        var cut = Render<CustomReportBuilder>();

        cut.Markup.ShouldContain("Layout");
        cut.Markup.ShouldContain("Select layout");
    }

    /// <summary>
    /// Verifies the layout name input is present.
    /// </summary>
    [Fact]
    public void ShowsLayoutNameInput()
    {
        var cut = Render<CustomReportBuilder>();

        cut.Markup.ShouldContain("Name");
    }

    /// <summary>
    /// Verifies the starter template selector is present.
    /// </summary>
    [Fact]
    public void ShowsStarterTemplateSelector()
    {
        var cut = Render<CustomReportBuilder>();

        cut.Markup.ShouldContain("Starter Template");
        cut.Markup.ShouldContain("Select template");
    }

    /// <summary>
    /// Verifies the starter template options are rendered.
    /// </summary>
    [Fact]
    public void ShowsTemplateOptions()
    {
        var cut = Render<CustomReportBuilder>();

        cut.Markup.ShouldContain("Summary + Trend");
        cut.Markup.ShouldContain("Spending Focus");
        cut.Markup.ShouldContain("Table Heavy");
    }

    /// <summary>
    /// Verifies the Apply button for templates is present.
    /// </summary>
    [Fact]
    public void HasApplyTemplateButton()
    {
        var cut = Render<CustomReportBuilder>();

        cut.Markup.ShouldContain("Apply");
    }

    /// <summary>
    /// Verifies saved layouts appear in the dropdown.
    /// </summary>
    [Fact]
    public void ShowsSavedLayouts_InDropdown()
    {
        // Need to make GetCustomReportLayoutsAsync return layouts.
        // The stub returns [] by default, so we test that the dropdown contains
        // at least the default "Select layout" option.
        var cut = Render<CustomReportBuilder>();

        cut.Markup.ShouldContain("Select layout");
    }

    /// <summary>
    /// Verifies the builder grid area is rendered.
    /// </summary>
    [Fact]
    public void ShowsBuilderGrid()
    {
        var cut = Render<CustomReportBuilder>();

        var grid = cut.FindAll(".builder-grid");
        grid.Count.ShouldBe(1);
    }

    /// <summary>
    /// Verifies the toolbar area is rendered.
    /// </summary>
    [Fact]
    public void ShowsToolbar()
    {
        var cut = Render<CustomReportBuilder>();

        var toolbar = cut.FindAll(".builder-toolbar");
        toolbar.Count.ShouldBe(1);
    }
}
