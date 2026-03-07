// <copyright file="AiStatusBadgeTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.AI;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.AI;

/// <summary>
/// Unit tests for the <see cref="AiStatusBadge"/> component.
/// </summary>
public sealed class AiStatusBadgeTests : BunitContext, IAsyncLifetime
{
    private readonly FakeAiApiService _aiService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiStatusBadgeTests"/> class.
    /// </summary>
    public AiStatusBadgeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
        _aiService = new FakeAiApiService();
        Services.AddSingleton<IAiApiService>(_aiService);
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies the badge shows available status when AI is available.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AiStatusBadge_ShowsAvailable_WhenServiceIsAvailable()
    {
        _aiService.StatusResult = new AiStatusDto { IsAvailable = true, IsEnabled = true, CurrentModel = "llama3.2" };

        var cut = Render<AiStatusBadge>();
        await Task.Delay(50);
        cut.Render();

        Assert.Contains("status-available", cut.Markup);
    }

    /// <summary>
    /// Verifies the badge shows unavailable status.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AiStatusBadge_ShowsUnavailable_WhenServiceIsNotAvailable()
    {
        _aiService.StatusResult = new AiStatusDto { IsAvailable = false, IsEnabled = true };

        var cut = Render<AiStatusBadge>();
        await Task.Delay(50);
        cut.Render();

        Assert.Contains("status-unavailable", cut.Markup);
    }

    /// <summary>
    /// Verifies the badge shows disabled status.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AiStatusBadge_ShowsDisabled_WhenServiceIsDisabled()
    {
        _aiService.StatusResult = new AiStatusDto { IsAvailable = false, IsEnabled = false };

        var cut = Render<AiStatusBadge>();
        await Task.Delay(50);
        cut.Render();

        Assert.Contains("status-disabled", cut.Markup);
    }

    /// <summary>
    /// Verifies the badge shows unknown status when status is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AiStatusBadge_ShowsUnknown_WhenStatusIsNull()
    {
        _aiService.StatusResult = null;

        var cut = Render<AiStatusBadge>();
        await Task.Delay(50);
        cut.Render();

        Assert.Contains("status-unknown", cut.Markup);
    }
}
