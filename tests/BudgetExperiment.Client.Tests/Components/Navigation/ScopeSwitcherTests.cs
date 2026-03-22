// <copyright file="ScopeSwitcherTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Navigation;
using BudgetExperiment.Client.Services;

using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.Navigation;

/// <summary>
/// Unit tests for the <see cref="ScopeSwitcher"/> component.
/// </summary>
public sealed class ScopeSwitcherTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScopeSwitcherTests"/> class.
    /// </summary>
    public ScopeSwitcherTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
        Services.AddSingleton<CultureService>();
        Services.AddSingleton<ScopeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies the scope switcher renders a container div.
    /// </summary>
    [Fact]
    public void ScopeSwitcher_RendersContainer()
    {
        var cut = Render<ScopeSwitcher>();

        var div = cut.Find("div.scope-switcher");
        Assert.NotNull(div);
    }

    /// <summary>
    /// Verifies the toggle button is present.
    /// </summary>
    [Fact]
    public void ScopeSwitcher_RendersToggleButton()
    {
        var cut = Render<ScopeSwitcher>();

        var button = cut.Find("button.scope-switcher-button");
        Assert.NotNull(button);
    }

    /// <summary>
    /// Verifies the scope text is visible when not collapsed.
    /// </summary>
    [Fact]
    public void ScopeSwitcher_ShowsScopeText_WhenExpanded()
    {
        var cut = Render<ScopeSwitcher>(parameters => parameters
            .Add(p => p.IsCollapsed, false));

        var scopeTexts = cut.FindAll(".scope-text");
        Assert.NotEmpty(scopeTexts);
    }

    /// <summary>
    /// Verifies the scope text is hidden when collapsed.
    /// </summary>
    [Fact]
    public void ScopeSwitcher_HidesScopeText_WhenCollapsed()
    {
        var cut = Render<ScopeSwitcher>(parameters => parameters
            .Add(p => p.IsCollapsed, true));

        var scopeTexts = cut.FindAll(".scope-text");
        Assert.Empty(scopeTexts);
    }

    /// <summary>
    /// Verifies the dropdown appears after clicking the toggle button.
    /// </summary>
    [Fact]
    public void ScopeSwitcher_ShowsDropdown_OnClick()
    {
        var cut = Render<ScopeSwitcher>();

        var button = cut.Find("button.scope-switcher-button");
        button.Click();

        var dropdown = cut.Find("div.scope-dropdown");
        Assert.NotNull(dropdown);
    }

    /// <summary>
    /// Verifies all three scope options are shown in the dropdown.
    /// </summary>
    [Fact]
    public void ScopeSwitcher_ShowsAllScopeOptions()
    {
        var cut = Render<ScopeSwitcher>();

        var button = cut.Find("button.scope-switcher-button");
        button.Click();

        var options = cut.FindAll("button.scope-option");
        Assert.Equal(3, options.Count);
    }

    /// <summary>
    /// Verifies Shared option is present in dropdown.
    /// </summary>
    [Fact]
    public void ScopeSwitcher_ShowsSharedOption()
    {
        var cut = Render<ScopeSwitcher>();

        cut.Find("button.scope-switcher-button").Click();

        Assert.Contains("Shared", cut.Markup);
    }

    /// <summary>
    /// Verifies Personal option is present in dropdown.
    /// </summary>
    [Fact]
    public void ScopeSwitcher_ShowsPersonalOption()
    {
        var cut = Render<ScopeSwitcher>();

        cut.Find("button.scope-switcher-button").Click();

        Assert.Contains("Personal", cut.Markup);
    }

    /// <summary>
    /// Verifies the collapsed class is applied when IsCollapsed is true.
    /// </summary>
    [Fact]
    public void ScopeSwitcher_HasCollapsedClass_WhenCollapsed()
    {
        var cut = Render<ScopeSwitcher>(parameters => parameters
            .Add(p => p.IsCollapsed, true));

        var div = cut.Find("div.scope-switcher");
        Assert.Contains("collapsed", div.ClassList);
    }

    /// <summary>
    /// Verifies the scope icon is always rendered.
    /// </summary>
    [Fact]
    public void ScopeSwitcher_RendersIcon()
    {
        var cut = Render<ScopeSwitcher>();

        var icons = cut.FindAll(".scope-icon");
        Assert.NotEmpty(icons);
    }

    /// <summary>
    /// Verifies dropdown closes when overlay is clicked.
    /// </summary>
    [Fact]
    public void ScopeSwitcher_ClosesDropdown_OnOverlayClick()
    {
        var cut = Render<ScopeSwitcher>();

        cut.Find("button.scope-switcher-button").Click();
        Assert.NotNull(cut.Find("div.scope-dropdown"));

        var overlay = cut.Find("div.scope-overlay");
        overlay.Click();

        var dropdowns = cut.FindAll("div.scope-dropdown");
        Assert.Empty(dropdowns);
    }
}
