// <copyright file="BulkActionBarTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Display;
using BudgetExperiment.Client.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the <see cref="BulkActionBar"/> component.
/// </summary>
public sealed class BulkActionBarTests : BunitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkActionBarTests"/> class.
    /// </summary>
    public BulkActionBarTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
    }

    /// <summary>
    /// Verifies the bar is hidden when no items are selected.
    /// </summary>
    [Fact]
    public void HiddenWhenSelectedCountIsZero()
    {
        var cut = Render<BulkActionBar>(p => p.Add(x => x.SelectedCount, 0));

        cut.Markup.Trim().ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies the bar is visible when items are selected.
    /// </summary>
    [Fact]
    public void VisibleWhenSelectedCountIsPositive()
    {
        var cut = Render<BulkActionBar>(p => p.Add(x => x.SelectedCount, 3));

        cut.Markup.ShouldContain("3 rule(s) selected");
        cut.Find(".bulk-action-bar").ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies the delete button invokes OnDelete.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteButton_InvokesOnDelete()
    {
        var deleteCalled = false;
        var cut = Render<BulkActionBar>(p => p
            .Add(x => x.SelectedCount, 1)
            .Add(x => x.OnDelete, () =>
            {
                deleteCalled = true;
                return Task.CompletedTask;
            }));

        var deleteBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Delete"));
        await deleteBtn.ClickAsync();

        deleteCalled.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies the activate button invokes OnActivate.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ActivateButton_InvokesOnActivate()
    {
        var activateCalled = false;
        var cut = Render<BulkActionBar>(p => p
            .Add(x => x.SelectedCount, 1)
            .Add(x => x.OnActivate, () =>
            {
                activateCalled = true;
                return Task.CompletedTask;
            }));

        var activateBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Activate"));
        await activateBtn.ClickAsync();

        activateCalled.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies the deactivate button invokes OnDeactivate.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeactivateButton_InvokesOnDeactivate()
    {
        var deactivateCalled = false;
        var cut = Render<BulkActionBar>(p => p
            .Add(x => x.SelectedCount, 1)
            .Add(x => x.OnDeactivate, () =>
            {
                deactivateCalled = true;
                return Task.CompletedTask;
            }));

        var deactivateBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Deactivate"));
        await deactivateBtn.ClickAsync();

        deactivateCalled.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies the clear button invokes OnClearSelection.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ClearButton_InvokesOnClearSelection()
    {
        var clearCalled = false;
        var cut = Render<BulkActionBar>(p => p
            .Add(x => x.SelectedCount, 2)
            .Add(x => x.OnClearSelection, () =>
            {
                clearCalled = true;
                return Task.CompletedTask;
            }));

        var clearBtn = cut.Find(".bulk-action-clear");
        await clearBtn.ClickAsync();

        clearCalled.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies the selected count updates in display.
    /// </summary>
    [Fact]
    public void DisplaysCorrectCount()
    {
        var cut = Render<BulkActionBar>(p => p.Add(x => x.SelectedCount, 7));

        cut.Markup.ShouldContain("7 rule(s) selected");
    }
}
