// <copyright file="SavedMappingsManagerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Import;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.Import;

/// <summary>
/// Unit tests for the <see cref="SavedMappingsManager"/> component.
/// </summary>
public sealed class SavedMappingsManagerTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SavedMappingsManagerTests"/> class.
    /// </summary>
    public SavedMappingsManagerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
        Services.AddSingleton<CultureService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies the title is rendered.
    /// </summary>
    [Fact]
    public void SavedMappingsManager_ShowsTitle()
    {
        var cut = Render<SavedMappingsManager>();

        Assert.Contains("Saved Column Mappings", cut.Markup);
    }

    /// <summary>
    /// Verifies empty state when no mappings exist.
    /// </summary>
    [Fact]
    public void SavedMappingsManager_ShowsEmptyState_WhenNoMappings()
    {
        var cut = Render<SavedMappingsManager>();

        Assert.Contains("No saved mappings yet.", cut.Markup);
        Assert.Contains("empty-state", cut.Markup);
    }

    /// <summary>
    /// Verifies loading state shows spinner.
    /// </summary>
    [Fact]
    public void SavedMappingsManager_ShowsLoadingSpinner_WhenIsLoading()
    {
        var cut = Render<SavedMappingsManager>(parameters => parameters
            .Add(p => p.IsLoading, true));

        Assert.Contains("Loading saved mappings...", cut.Markup);
        Assert.Contains("spinner-border", cut.Markup);
    }

    /// <summary>
    /// Verifies mapping cards are rendered with names.
    /// </summary>
    [Fact]
    public void SavedMappingsManager_RendersMappingCards()
    {
        var mappings = new List<ImportMappingDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "BOA CSV",
                ColumnMappings = [new ColumnMappingDto { ColumnIndex = 0 }, new ColumnMappingDto { ColumnIndex = 1 }],
                CreatedAtUtc = DateTime.UtcNow,
            },
        };

        var cut = Render<SavedMappingsManager>(parameters => parameters
            .Add(p => p.Mappings, mappings));

        Assert.Contains("BOA CSV", cut.Markup);
        Assert.Contains("2 columns", cut.Markup);
    }

    /// <summary>
    /// Verifies date format is shown when present.
    /// </summary>
    [Fact]
    public void SavedMappingsManager_ShowsDateFormat_WhenPresent()
    {
        var mappings = new List<ImportMappingDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Test",
                DateFormat = "MM/dd/yyyy",
                ColumnMappings = [],
                CreatedAtUtc = DateTime.UtcNow,
            },
        };

        var cut = Render<SavedMappingsManager>(parameters => parameters
            .Add(p => p.Mappings, mappings));

        Assert.Contains("MM/dd/yyyy", cut.Markup);
    }

    /// <summary>
    /// Verifies refresh button invokes callback.
    /// </summary>
    [Fact]
    public void SavedMappingsManager_RefreshButton_InvokesCallback()
    {
        var refreshCalled = false;
        var mappings = new List<ImportMappingDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Test", ColumnMappings = [], CreatedAtUtc = DateTime.UtcNow },
        };

        var cut = Render<SavedMappingsManager>(parameters => parameters
            .Add(p => p.Mappings, mappings)
            .Add(p => p.OnRefresh, () => { refreshCalled = true; }));

        var refreshBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Refresh"));
        refreshBtn.Click();

        Assert.True(refreshCalled);
    }

    /// <summary>
    /// Verifies expand/collapse toggles mapping details.
    /// </summary>
    [Fact]
    public void SavedMappingsManager_ToggleExpand_ShowsAndHidesDetails()
    {
        var mappings = new List<ImportMappingDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Test",
                ColumnMappings =
                [
                    new ColumnMappingDto { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
                ],
                CreatedAtUtc = DateTime.UtcNow,
            },
        };

        var cut = Render<SavedMappingsManager>(parameters => parameters
            .Add(p => p.Mappings, mappings));

        // Initially collapsed
        Assert.Contains("Show details", cut.Markup);
        Assert.DoesNotContain("mapping-details", cut.Markup);

        // Click to expand
        var toggleBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Show details"));
        toggleBtn.Click();

        Assert.Contains("Hide details", cut.Markup);
        Assert.Contains("mapping-details", cut.Markup);
    }

    /// <summary>
    /// Verifies clicking delete shows confirmation.
    /// </summary>
    [Fact]
    public void SavedMappingsManager_DeleteButton_ShowsConfirmation()
    {
        var mappings = new List<ImportMappingDto>
        {
            new() { Id = Guid.NewGuid(), Name = "BOA CSV", ColumnMappings = [], CreatedAtUtc = DateTime.UtcNow },
        };

        var cut = Render<SavedMappingsManager>(parameters => parameters
            .Add(p => p.Mappings, mappings));

        var deleteBtn = cut.Find(".btn-outline-danger");
        deleteBtn.Click();

        Assert.Contains("BOA CSV", cut.Markup);
        Assert.Contains("will not affect any previously imported transactions", cut.Markup);
    }

    /// <summary>
    /// Verifies clicking edit opens modal with mapping name.
    /// </summary>
    [Fact]
    public void SavedMappingsManager_EditButton_OpensEditModal()
    {
        var mappings = new List<ImportMappingDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Original Name", ColumnMappings = [], CreatedAtUtc = DateTime.UtcNow },
        };

        var cut = Render<SavedMappingsManager>(parameters => parameters
            .Add(p => p.Mappings, mappings));

        var editBtn = cut.Find(".btn-outline-secondary[title='Edit mapping name']");
        editBtn.Click();

        Assert.Contains("Mapping Name", cut.Markup);
    }

    /// <summary>
    /// Verifies the refresh button is hidden when no mappings exist.
    /// </summary>
    [Fact]
    public void SavedMappingsManager_HidesRefreshButton_WhenNoMappings()
    {
        var cut = Render<SavedMappingsManager>();

        var buttons = cut.FindAll("button");
        Assert.DoesNotContain(buttons, b => b.TextContent.Contains("Refresh"));
    }
}
