// <copyright file="ImportSummaryCardLocationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Import;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Import;

/// <summary>
/// Unit tests for location enrichment display in <see cref="ImportSummaryCard"/>.
/// </summary>
public class ImportSummaryCardLocationTests : BunitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImportSummaryCardLocationTests"/> class.
    /// </summary>
    public ImportSummaryCardLocationTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
    }

    /// <summary>
    /// When location enrichment count is positive, the summary card shows the location section.
    /// </summary>
    [Fact]
    public void Render_WithLocationEnriched_ShowsLocationSection()
    {
        // Arrange
        var result = new ImportResult
        {
            ImportedCount = 10,
            LocationEnrichedCount = 7,
        };

        // Act
        var cut = RenderCard(result);

        // Assert
        var locationSection = cut.Find(".location-breakdown");
        Assert.Contains("7", locationSection.TextContent);
        Assert.Contains("10", locationSection.TextContent);
        Assert.Contains("enriched with location", locationSection.TextContent);
    }

    /// <summary>
    /// When location enrichment count is zero, the location section is not rendered.
    /// </summary>
    [Fact]
    public void Render_WithZeroLocationEnriched_HidesLocationSection()
    {
        // Arrange
        var result = new ImportResult
        {
            ImportedCount = 10,
            LocationEnrichedCount = 0,
        };

        // Act
        var cut = RenderCard(result);

        // Assert
        var sections = cut.FindAll(".location-breakdown");
        Assert.Empty(sections);
    }

    /// <summary>
    /// When there are no imported transactions, the location section is not shown even if count is set.
    /// </summary>
    [Fact]
    public void Render_WithZeroImported_HidesLocationSection()
    {
        // Arrange
        var result = new ImportResult
        {
            ImportedCount = 0,
            LocationEnrichedCount = 0,
        };

        // Act
        var cut = RenderCard(result);

        // Assert
        var sections = cut.FindAll(".location-breakdown");
        Assert.Empty(sections);
    }

    private IRenderedComponent<ImportSummaryCard> RenderCard(ImportResult result)
    {
        return Render<ImportSummaryCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.AccountId, Guid.NewGuid()));
    }
}
