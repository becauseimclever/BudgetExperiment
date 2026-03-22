// <copyright file="ScopeBadgeTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Display;
using BudgetExperiment.Client.Services;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Display;

/// <summary>
/// Unit tests for the <see cref="ScopeBadge"/> component.
/// </summary>
public class ScopeBadgeTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScopeBadgeTests"/> class.
    /// </summary>
    public ScopeBadgeTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies nothing renders when scope is null.
    /// </summary>
    [Fact]
    public void NullScope_RendersNothing()
    {
        var cut = Render<ScopeBadge>(p => p
            .Add(x => x.Scope, (string?)null));

        Assert.Empty(cut.Markup.Trim());
    }

    /// <summary>
    /// Verifies nothing renders when scope is empty.
    /// </summary>
    [Fact]
    public void EmptyScope_RendersNothing()
    {
        var cut = Render<ScopeBadge>(p => p
            .Add(x => x.Scope, string.Empty));

        Assert.Empty(cut.Markup.Trim());
    }

    /// <summary>
    /// Verifies Shared scope applies correct CSS class.
    /// </summary>
    [Fact]
    public void SharedScope_HasSharedClass()
    {
        var cut = Render<ScopeBadge>(p => p
            .Add(x => x.Scope, "Shared"));

        Assert.Contains("scope-shared", cut.Markup);
    }

    /// <summary>
    /// Verifies Personal scope applies correct CSS class.
    /// </summary>
    [Fact]
    public void PersonalScope_HasPersonalClass()
    {
        var cut = Render<ScopeBadge>(p => p
            .Add(x => x.Scope, "Personal"));

        Assert.Contains("scope-personal", cut.Markup);
    }

    /// <summary>
    /// Verifies label is shown by default.
    /// </summary>
    [Fact]
    public void ShowLabel_Default_DisplaysLabel()
    {
        var cut = Render<ScopeBadge>(p => p
            .Add(x => x.Scope, "Shared"));

        Assert.Contains("Shared", cut.Markup);
    }

    /// <summary>
    /// Verifies label is hidden when ShowLabel is false.
    /// </summary>
    [Fact]
    public void ShowLabelFalse_HidesLabelText()
    {
        var cut = Render<ScopeBadge>(p => p
            .Add(x => x.Scope, "Shared")
            .Add(x => x.ShowLabel, false));

        // Badge should still render (for icon) but the text should not appear as a span
        Assert.Contains("scope-badge", cut.Markup);
        var spans = cut.FindAll(".scope-badge span");

        // Only the icon component renders, no label span
        Assert.DoesNotContain(spans, s => s.TextContent.Trim() == "Shared");
    }

    /// <summary>
    /// Verifies Shared scope has correct tooltip.
    /// </summary>
    [Fact]
    public void SharedScope_HasCorrectTooltip()
    {
        var cut = Render<ScopeBadge>(p => p
            .Add(x => x.Scope, "Shared"));

        var badge = cut.Find(".scope-badge");
        Assert.Equal("Shared with family members", badge.GetAttribute("title"));
    }

    /// <summary>
    /// Verifies Personal scope has correct tooltip.
    /// </summary>
    [Fact]
    public void PersonalScope_HasCorrectTooltip()
    {
        var cut = Render<ScopeBadge>(p => p
            .Add(x => x.Scope, "Personal"));

        var badge = cut.Find(".scope-badge");
        Assert.Equal("Private to you", badge.GetAttribute("title"));
    }
}
