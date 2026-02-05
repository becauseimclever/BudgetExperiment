// <copyright file="MobileFabTests.cs" company="Jason Awbrey">
// Copyright (c) Jason Awbrey. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Tests.Components;

using Bunit;
using BudgetExperiment.Client.Components;
using BudgetExperiment.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Unit tests for the <see cref="MobileFab"/> component.
/// </summary>
public sealed class MobileFabTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MobileFabTests"/> class.
    /// Sets up JSInterop mocks and required services.
    /// </summary>
    public MobileFabTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc />
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc />
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that the FAB renders in collapsed state by default.
    /// </summary>
    [Fact]
    public void Render_DefaultState_IsCollapsed()
    {
        // Act
        var cut = Render<MobileFab>();

        // Assert
        var container = cut.Find(".fab-container");
        Assert.DoesNotContain("is-expanded", container.ClassList);
    }

    /// <summary>
    /// Verifies that clicking the primary FAB expands the speed dial.
    /// </summary>
    [Fact]
    public void ClickPrimaryFab_WhenCollapsed_ExpandsMenu()
    {
        // Arrange
        var cut = Render<MobileFab>();
        var primaryButton = cut.Find(".fab-primary");

        // Act
        primaryButton.Click();

        // Assert
        var container = cut.Find(".fab-container");
        Assert.Contains("is-expanded", container.ClassList);
    }

    /// <summary>
    /// Verifies that clicking the primary FAB when expanded collapses the menu.
    /// </summary>
    [Fact]
    public void ClickPrimaryFab_WhenExpanded_CollapsesMenu()
    {
        // Arrange
        var cut = Render<MobileFab>(p => p
            .Add(x => x.IsExpanded, true));
        var primaryButton = cut.Find(".fab-primary");

        // Act
        primaryButton.Click();

        // Assert
        var container = cut.Find(".fab-container");
        Assert.DoesNotContain("is-expanded", container.ClassList);
    }

    /// <summary>
    /// Verifies that the primary button shows expanded state styling.
    /// </summary>
    [Fact]
    public void PrimaryButton_WhenExpanded_HasExpandedClass()
    {
        // Arrange
        var cut = Render<MobileFab>(p => p
            .Add(x => x.IsExpanded, true));

        // Act
        var primaryButton = cut.Find(".fab-primary");

        // Assert
        Assert.Contains("is-expanded", primaryButton.ClassList);
    }

    /// <summary>
    /// Verifies that the primary button has correct aria-expanded attribute.
    /// </summary>
    [Fact]
    public void PrimaryButton_AriaExpanded_ReflectsState()
    {
        // Arrange - collapsed
        var cut = Render<MobileFab>();
        var primaryButton = cut.Find(".fab-primary");

        // Assert collapsed state
        Assert.Equal("false", primaryButton.GetAttribute("aria-expanded"));

        // Act - expand
        primaryButton.Click();

        // Assert expanded state
        primaryButton = cut.Find(".fab-primary");
        Assert.Equal("true", primaryButton.GetAttribute("aria-expanded"));
    }

    /// <summary>
    /// Verifies that the Quick Add button triggers the callback.
    /// </summary>
    [Fact]
    public void ClickQuickAddButton_InvokesCallback()
    {
        // Arrange
        var callbackInvoked = false;
        var cut = Render<MobileFab>(p => p
            .Add(x => x.IsExpanded, true)
            .Add(x => x.OnQuickAddClick, () => callbackInvoked = true));
        var quickAddButton = cut.Find(".fab-secondary:not(.fab-ai)");

        // Act
        quickAddButton.Click();

        // Assert
        Assert.True(callbackInvoked);
    }

    /// <summary>
    /// Verifies that clicking Quick Add also collapses the menu.
    /// </summary>
    [Fact]
    public void ClickQuickAddButton_CollapsesMenu()
    {
        // Arrange
        var cut = Render<MobileFab>(p => p
            .Add(x => x.IsExpanded, true));
        var quickAddButton = cut.Find(".fab-secondary:not(.fab-ai)");

        // Act
        quickAddButton.Click();

        // Assert
        var container = cut.Find(".fab-container");
        Assert.DoesNotContain("is-expanded", container.ClassList);
    }

    /// <summary>
    /// Verifies that the AI button is shown by default.
    /// </summary>
    [Fact]
    public void Render_DefaultShowAiButton_AiButtonIsPresent()
    {
        // Act
        var cut = Render<MobileFab>(p => p
            .Add(x => x.IsExpanded, true));

        // Assert
        var aiButton = cut.Find(".fab-ai");
        Assert.NotNull(aiButton);
    }

    /// <summary>
    /// Verifies that the AI button is hidden when ShowAiButton is false.
    /// </summary>
    [Fact]
    public void Render_ShowAiButtonFalse_AiButtonNotPresent()
    {
        // Act
        var cut = Render<MobileFab>(p => p
            .Add(x => x.ShowAiButton, false)
            .Add(x => x.IsExpanded, true));

        // Assert
        var aiButtons = cut.FindAll(".fab-ai");
        Assert.Empty(aiButtons);
    }

    /// <summary>
    /// Verifies that clicking the AI button triggers the callback.
    /// </summary>
    [Fact]
    public void ClickAiButton_InvokesCallback()
    {
        // Arrange
        var callbackInvoked = false;
        var cut = Render<MobileFab>(p => p
            .Add(x => x.IsExpanded, true)
            .Add(x => x.OnAiClick, () => callbackInvoked = true));
        var aiButton = cut.Find(".fab-ai");

        // Act
        aiButton.Click();

        // Assert
        Assert.True(callbackInvoked);
    }

    /// <summary>
    /// Verifies that the AI button shows unavailable state when marked.
    /// </summary>
    [Fact]
    public void Render_AiUnavailable_HasUnavailableClass()
    {
        // Act
        var cut = Render<MobileFab>(p => p
            .Add(x => x.IsExpanded, true)
            .Add(x => x.IsAiUnavailable, true));

        // Assert
        var aiButton = cut.Find(".fab-ai");
        Assert.Contains("is-unavailable", aiButton.ClassList);
    }

    /// <summary>
    /// Verifies that the AI button does not invoke callback when unavailable.
    /// </summary>
    [Fact]
    public void ClickAiButton_WhenUnavailable_DoesNotInvokeCallback()
    {
        // Arrange
        var callbackInvoked = false;
        var cut = Render<MobileFab>(p => p
            .Add(x => x.IsExpanded, true)
            .Add(x => x.IsAiUnavailable, true)
            .Add(x => x.OnAiClick, () => callbackInvoked = true));
        var aiButton = cut.Find(".fab-ai");

        // Act
        aiButton.Click();

        // Assert
        Assert.False(callbackInvoked);
    }

    /// <summary>
    /// Verifies that the backdrop is shown when expanded and ShowBackdrop is true.
    /// </summary>
    [Fact]
    public void Render_ExpandedWithBackdrop_BackdropIsVisible()
    {
        // Act
        var cut = Render<MobileFab>(p => p
            .Add(x => x.IsExpanded, true)
            .Add(x => x.ShowBackdrop, true));

        // Assert
        var backdrop = cut.Find(".fab-backdrop");
        Assert.Contains("is-visible", backdrop.ClassList);
    }

    /// <summary>
    /// Verifies that the backdrop is not rendered when ShowBackdrop is false.
    /// </summary>
    [Fact]
    public void Render_ShowBackdropFalse_NoBackdrop()
    {
        // Act
        var cut = Render<MobileFab>(p => p
            .Add(x => x.IsExpanded, true)
            .Add(x => x.ShowBackdrop, false));

        // Assert
        var backdrops = cut.FindAll(".fab-backdrop");
        Assert.Empty(backdrops);
    }

    /// <summary>
    /// Verifies that clicking the backdrop collapses the menu.
    /// </summary>
    [Fact]
    public void ClickBackdrop_CollapsesMenu()
    {
        // Arrange
        var cut = Render<MobileFab>(p => p
            .Add(x => x.IsExpanded, true)
            .Add(x => x.ShowBackdrop, true));
        var backdrop = cut.Find(".fab-backdrop");

        // Act
        backdrop.Click();

        // Assert
        var container = cut.Find(".fab-container");
        Assert.DoesNotContain("is-expanded", container.ClassList);
    }

    /// <summary>
    /// Verifies that the component renders hidden state when IsHidden is true.
    /// </summary>
    [Fact]
    public void Render_IsHiddenTrue_HasHiddenClass()
    {
        // Act
        var cut = Render<MobileFab>(p => p
            .Add(x => x.IsHidden, true));

        // Assert
        var container = cut.Find(".fab-container");
        Assert.Contains("is-hidden", container.ClassList);
    }

    /// <summary>
    /// Verifies that custom labels are applied to the buttons.
    /// </summary>
    [Fact]
    public void Render_CustomLabels_LabelsAreApplied()
    {
        // Act
        var cut = Render<MobileFab>(p => p
            .Add(x => x.IsExpanded, true)
            .Add(x => x.QuickAddLabel, "Add Transaction")
            .Add(x => x.AiButtonLabel, "Chat with AI"));

        // Assert
        var quickAddLabel = cut.Find("#fab-quickadd-label");
        var aiLabel = cut.Find("#fab-ai-label");
        Assert.Equal("Add Transaction", quickAddLabel.TextContent);
        Assert.Equal("Chat with AI", aiLabel.TextContent);
    }

    /// <summary>
    /// Verifies that two-way binding works for IsExpanded.
    /// </summary>
    [Fact]
    public void IsExpandedChanged_WhenToggled_InvokesCallback()
    {
        // Arrange
        var receivedValue = false;
        var cut = Render<MobileFab>(p => p
            .Add(x => x.IsExpandedChanged, (bool val) => receivedValue = val));
        var primaryButton = cut.Find(".fab-primary");

        // Act
        primaryButton.Click();

        // Assert
        Assert.True(receivedValue);
    }
}
