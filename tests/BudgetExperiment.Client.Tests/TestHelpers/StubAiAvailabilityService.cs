// <copyright file="StubAiAvailabilityService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;

namespace BudgetExperiment.Client.Tests.TestHelpers;

/// <summary>
/// Shared stub implementation of <see cref="IAiAvailabilityService"/> for tests.
/// </summary>
internal sealed class StubAiAvailabilityService : IAiAvailabilityService
{
    /// <inheritdoc/>
    public event Action? StatusChanged;

    /// <inheritdoc/>
    public AiAvailabilityState State { get; set; } = AiAvailabilityState.Available;

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc/>
    public bool IsAvailable { get; set; } = true;

    /// <inheritdoc/>
    public bool IsFullyOperational { get; set; } = true;

    /// <inheritdoc/>
    public string? ErrorMessage { get; set; }

    /// <inheritdoc/>
    public Task RefreshAsync() => Task.CompletedTask;

    /// <summary>
    /// Raises the <see cref="StatusChanged"/> event.
    /// </summary>
    internal void OnStatusChanged() => this.StatusChanged?.Invoke();
}
