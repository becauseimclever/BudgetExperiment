// <copyright file="ImportPreviewTableLocationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Import;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Import;

/// <summary>
/// Unit tests for location enrichment display in <see cref="ImportPreviewTable"/>.
/// </summary>
public class ImportPreviewTableLocationTests : BunitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImportPreviewTableLocationTests"/> class.
    /// </summary>
    public ImportPreviewTableLocationTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
    }

    /// <summary>
    /// Verifies the location column header is rendered.
    /// </summary>
    [Fact]
    public void Render_LocationColumnHeader_IsPresent()
    {
        // Arrange
        var preview = CreatePreviewResult();

        // Act
        var cut = RenderTable(preview);

        // Assert
        var headers = cut.FindAll("thead th");
        Assert.Contains(headers, th => th.TextContent.Contains("Location"));
    }

    /// <summary>
    /// When a row has a parsed location, the location preview with city and state is displayed.
    /// </summary>
    [Fact]
    public void Render_RowWithLocation_ShowsCityAndState()
    {
        // Arrange
        var preview = CreatePreviewResult(new ImportLocationPreview
        {
            City = "Seattle",
            StateOrRegion = "WA",
            Confidence = 0.95m,
            IsAccepted = true,
        });

        // Act
        var cut = RenderTable(preview);

        // Assert
        var locationCell = cut.Find("td.location-col .location-preview");
        Assert.Contains("Seattle", locationCell.TextContent);
        Assert.Contains("WA", locationCell.TextContent);
    }

    /// <summary>
    /// When a row has no parsed location, a dash placeholder is shown.
    /// </summary>
    [Fact]
    public void Render_RowWithoutLocation_ShowsDash()
    {
        // Arrange
        var preview = CreatePreviewResult(parsedLocation: null);

        // Act
        var cut = RenderTable(preview);

        // Assert
        var locationCell = cut.Find("td.location-col");
        Assert.Contains("\u2014", locationCell.TextContent);
    }

    /// <summary>
    /// High confidence locations display a green confidence dot.
    /// </summary>
    [Fact]
    public void Render_HighConfidence_ShowsGreenDot()
    {
        // Arrange
        var preview = CreatePreviewResult(new ImportLocationPreview
        {
            City = "Portland",
            StateOrRegion = "OR",
            Confidence = 0.95m,
            IsAccepted = true,
        });

        // Act
        var cut = RenderTable(preview);

        // Assert
        var dot = cut.Find(".confidence-dot");
        Assert.Contains("confidence-high", dot.ClassList);
    }

    /// <summary>
    /// Medium confidence locations display the medium confidence dot.
    /// </summary>
    [Fact]
    public void Render_MediumConfidence_ShowsMediumDot()
    {
        // Arrange
        var preview = CreatePreviewResult(new ImportLocationPreview
        {
            City = "Portland",
            StateOrRegion = "OR",
            Confidence = 0.85m,
            IsAccepted = true,
        });

        // Act
        var cut = RenderTable(preview);

        // Assert
        var dot = cut.Find(".confidence-dot");
        Assert.Contains("confidence-medium", dot.ClassList);
    }

    /// <summary>
    /// Low confidence locations display the low confidence dot.
    /// </summary>
    [Fact]
    public void Render_LowConfidence_ShowsLowDot()
    {
        // Arrange
        var preview = CreatePreviewResult(new ImportLocationPreview
        {
            City = "Portland",
            StateOrRegion = "OR",
            Confidence = 0.70m,
            IsAccepted = true,
        });

        // Act
        var cut = RenderTable(preview);

        // Assert
        var dot = cut.Find(".confidence-dot");
        Assert.Contains("confidence-low", dot.ClassList);
    }

    /// <summary>
    /// When location enrichment count is positive, the footer shows the enrichment stat.
    /// </summary>
    [Fact]
    public void Render_WithLocationEnrichedCount_ShowsFooterStat()
    {
        // Arrange
        var preview = CreatePreviewResult(
            new ImportLocationPreview
            {
                City = "Seattle",
                StateOrRegion = "WA",
                Confidence = 0.95m,
                IsAccepted = true,
            },
            locationEnrichedCount: 1);

        // Act
        var cut = RenderTable(preview);

        // Assert
        Assert.Contains("enriched with location", cut.Markup);
    }

    /// <summary>
    /// When location enrichment count is zero, the footer does not show location stats.
    /// </summary>
    [Fact]
    public void Render_WithZeroLocationEnriched_HidesFooterStat()
    {
        // Arrange
        var preview = CreatePreviewResult(parsedLocation: null, locationEnrichedCount: 0);

        // Act
        var cut = RenderTable(preview);

        // Assert
        Assert.DoesNotContain("enriched with location", cut.Markup);
    }

    /// <summary>
    /// When a location row has IsAccepted true, a map-pin icon toggle button is shown.
    /// </summary>
    [Fact]
    public void Render_AcceptedLocation_ShowsMapPinToggle()
    {
        // Arrange
        var preview = CreatePreviewResult(new ImportLocationPreview
        {
            City = "Seattle",
            StateOrRegion = "WA",
            Confidence = 0.95m,
            IsAccepted = true,
        });

        // Act
        var cut = RenderTable(preview);

        // Assert
        var toggleBtn = cut.Find(".location-toggle-btn");
        Assert.Contains("Reject location", toggleBtn.GetAttribute("title") ?? string.Empty);
    }

    /// <summary>
    /// When a location row has IsAccepted false, the location is shown with rejected styling.
    /// </summary>
    [Fact]
    public void Render_RejectedLocation_ShowsRejectedStyling()
    {
        // Arrange
        var preview = CreatePreviewResult(new ImportLocationPreview
        {
            City = "Seattle",
            StateOrRegion = "WA",
            Confidence = 0.95m,
            IsAccepted = false,
        });

        // Act
        var cut = RenderTable(preview);

        // Assert
        var locationDiv = cut.Find(".location-preview");
        Assert.Contains("location-rejected", locationDiv.ClassList);

        var toggleBtn = cut.Find(".location-toggle-btn");
        Assert.Contains("Accept location", toggleBtn.GetAttribute("title") ?? string.Empty);
    }

    /// <summary>
    /// Clicking the toggle button fires the OnLocationToggle callback with the row index.
    /// </summary>
    [Fact]
    public void Click_LocationToggle_FiresCallback()
    {
        // Arrange
        int? toggledRow = null;
        var preview = CreatePreviewResult(new ImportLocationPreview
        {
            City = "Seattle",
            StateOrRegion = "WA",
            Confidence = 0.95m,
            IsAccepted = true,
        });

        var callback = EventCallback.Factory.Create<int>(this, row => toggledRow = row);
        var cut = RenderTable(preview, callback);

        // Act
        cut.Find(".location-toggle-btn").Click();

        // Assert
        Assert.Equal(1, toggledRow);
    }

    /// <summary>
    /// Rejected locations do not show confidence dots.
    /// </summary>
    [Fact]
    public void Render_RejectedLocation_HidesConfidenceDot()
    {
        // Arrange
        var preview = CreatePreviewResult(new ImportLocationPreview
        {
            City = "Seattle",
            StateOrRegion = "WA",
            Confidence = 0.95m,
            IsAccepted = false,
        });

        // Act
        var cut = RenderTable(preview);

        // Assert
        var dots = cut.FindAll(".confidence-dot");
        Assert.Empty(dots);
    }

    private static ImportPreviewResult CreatePreviewResult(
        ImportLocationPreview? parsedLocation = null,
        int? locationEnrichedCount = null)
    {
        return new ImportPreviewResult
        {
            Rows =
            [
                new ImportPreviewRow
                {
                    RowIndex = 1,
                    Date = new DateOnly(2025, 1, 15),
                    Description = "STARBUCKS SEATTLE WA",
                    Amount = -5.50m,
                    Status = ImportRowStatus.Valid,
                    ParsedLocation = parsedLocation,
                },
            ],
            ValidCount = 1,
            LocationEnrichedCount = locationEnrichedCount ?? (parsedLocation != null ? 1 : 0),
        };
    }

    private IRenderedComponent<ImportPreviewTable> RenderTable(
        ImportPreviewResult preview,
        EventCallback<int>? onLocationToggle = null)
    {
        return Render<ImportPreviewTable>(parameters =>
        {
            parameters
                .Add(p => p.PreviewResult, preview)
                .Add(p => p.SelectedIndices, new HashSet<int> { 1 });

            if (onLocationToggle.HasValue)
            {
                parameters.Add(p => p.OnLocationToggle, onLocationToggle.Value);
            }
        });
    }
}
