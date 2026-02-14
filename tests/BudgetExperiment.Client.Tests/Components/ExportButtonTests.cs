// <copyright file="ExportButtonTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Threading;

using Bunit;
using BudgetExperiment.Client.Components.Export;
using BudgetExperiment.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the ExportButton component.
/// </summary>
public class ExportButtonTests : BunitContext
{
    [Fact]
    public void ExportButton_Renders_Label()
    {
        // Arrange
        var options = new List<ExportOption>
        {
            new() { Label = "CSV", Href = "/export.csv" },
        };

        Services.AddSingleton<IExportDownloadService>(new TestExportDownloadService());
        Services.AddSingleton<IToastService>(new ToastService());

        // Act
        var cut = Render<ExportButton>(parameters => parameters
            .Add(p => p.Label, "Export")
            .Add(p => p.Options, options));

        // Assert
        var button = cut.Find("button.export-trigger");
        Assert.Contains("Export", button.TextContent);
    }

    [Fact]
    public void ExportButton_Shows_Menu_On_Click()
    {
        // Arrange
        var options = new List<ExportOption>
        {
            new() { Label = "CSV", Href = "/export.csv" },
        };

        Services.AddSingleton<IExportDownloadService>(new TestExportDownloadService());
        Services.AddSingleton<IToastService>(new ToastService());

        var cut = Render<ExportButton>(parameters => parameters
            .Add(p => p.Options, options));

        // Act
        cut.Find("button.export-trigger").Click();

        // Assert
        var items = cut.FindAll(".export-item");
        Assert.Single(items);
    }

    [Fact]
    public void ExportButton_Invokes_Download_Service()
    {
        // Arrange
        var options = new List<ExportOption>
        {
            new() { Label = "CSV", Href = "/export.csv" },
        };

        var downloadService = new TestExportDownloadService();
        Services.AddSingleton<IExportDownloadService>(downloadService);
        Services.AddSingleton<IToastService>(new ToastService());

        var cut = Render<ExportButton>(parameters => parameters
            .Add(p => p.Options, options));

        // Act
        cut.Find("button.export-trigger").Click();
        cut.Find("button.export-item").Click();

        // Assert
        Assert.Equal("/export.csv", downloadService.LastUrl);
    }

    private sealed class TestExportDownloadService : IExportDownloadService
    {
        public string? LastUrl { get; private set; }

        public Task<ExportDownloadResult> DownloadAsync(string url, CancellationToken cancellationToken = default)
        {
            LastUrl = url;
            return Task.FromResult(ExportDownloadResult.Ok());
        }
    }
}
