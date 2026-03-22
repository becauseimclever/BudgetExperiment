// <copyright file="SavedMappingSelectorTests.cs" company="BecauseImClever">
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
/// Unit tests for the <see cref="SavedMappingSelector"/> component.
/// </summary>
public sealed class SavedMappingSelectorTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SavedMappingSelectorTests"/> class.
    /// </summary>
    public SavedMappingSelectorTests()
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
    /// Verifies the label is rendered.
    /// </summary>
    [Fact]
    public void SavedMappingSelector_ShowsLabel()
    {
        var cut = Render<SavedMappingSelector>();

        Assert.Contains("Use Saved Mapping", cut.Markup);
    }

    /// <summary>
    /// Verifies the dropdown shows "Start fresh" default option.
    /// </summary>
    [Fact]
    public void SavedMappingSelector_ShowsStartFreshOption()
    {
        var cut = Render<SavedMappingSelector>();

        Assert.Contains("(Start fresh)", cut.Markup);
    }

    /// <summary>
    /// Verifies mapping names appear in dropdown.
    /// </summary>
    [Fact]
    public void SavedMappingSelector_ShowsMappingNames()
    {
        var mappings = new List<ImportMappingDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Bank of America" },
            new() { Id = Guid.NewGuid(), Name = "Chase CSV" },
        };

        var cut = Render<SavedMappingSelector>(parameters => parameters
            .Add(p => p.Mappings, mappings));

        Assert.Contains("Bank of America", cut.Markup);
        Assert.Contains("Chase CSV", cut.Markup);
    }

    /// <summary>
    /// Verifies clear button is shown when a mapping is selected.
    /// </summary>
    [Fact]
    public void SavedMappingSelector_ShowsClearButton_WhenSelected()
    {
        var mappingId = Guid.NewGuid();
        var mappings = new List<ImportMappingDto>
        {
            new() { Id = mappingId, Name = "Test Mapping", ColumnMappings = [new ColumnMappingDto { ColumnIndex = 0 }], CreatedAtUtc = DateTime.UtcNow },
        };

        var cut = Render<SavedMappingSelector>(parameters => parameters
            .Add(p => p.Mappings, mappings)
            .Add(p => p.SelectedMappingId, mappingId));

        var clearBtn = cut.Find("button.btn-outline-secondary");
        Assert.NotNull(clearBtn);
    }

    /// <summary>
    /// Verifies clear button is hidden when no mapping is selected.
    /// </summary>
    [Fact]
    public void SavedMappingSelector_HidesClearButton_WhenNotSelected()
    {
        var cut = Render<SavedMappingSelector>();

        var buttons = cut.FindAll("button.btn-outline-secondary");
        Assert.Empty(buttons);
    }

    /// <summary>
    /// Verifies selected mapping details are shown.
    /// </summary>
    [Fact]
    public void SavedMappingSelector_ShowsMappingDetails_WhenSelected()
    {
        var mappingId = Guid.NewGuid();
        var col1 = new ColumnMappingDto { ColumnIndex = 0 };
        var col2 = new ColumnMappingDto { ColumnIndex = 1 };
        var mappings = new List<ImportMappingDto>
        {
            new()
            {
                Id = mappingId,
                Name = "Test Mapping",
                ColumnMappings = [col1, col2],
                CreatedAtUtc = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            },
        };

        var cut = Render<SavedMappingSelector>(parameters => parameters
            .Add(p => p.Mappings, mappings)
            .Add(p => p.SelectedMappingId, mappingId));

        Assert.Contains("2 columns mapped", cut.Markup);
    }

    /// <summary>
    /// Verifies clear button fires OnMappingSelected with null.
    /// </summary>
    [Fact]
    public void SavedMappingSelector_ClearButton_FiresNullSelection()
    {
        Guid? selectedId = Guid.NewGuid();
        var mappings = new List<ImportMappingDto>
        {
            new() { Id = selectedId.Value, Name = "Test", ColumnMappings = [new ColumnMappingDto()], CreatedAtUtc = DateTime.UtcNow },
        };

        var cut = Render<SavedMappingSelector>(parameters => parameters
            .Add(p => p.Mappings, mappings)
            .Add(p => p.SelectedMappingId, selectedId)
            .Add(p => p.OnMappingSelected, (Guid? id) => { selectedId = id; }));

        cut.Find("button.btn-outline-secondary").Click();

        Assert.Null(selectedId);
    }
}
