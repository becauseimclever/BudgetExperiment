// <copyright file="StubFeatureFlagClientService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;

namespace BudgetExperiment.Client.Tests.TestHelpers;

/// <summary>
/// Stub implementation of <see cref="IFeatureFlagClientService"/> for bUnit tests.
/// All flags default to <c>false</c>; override <see cref="Flags"/> to enable specific flags.
/// </summary>
internal sealed class StubFeatureFlagClientService : IFeatureFlagClientService
{
    /// <inheritdoc/>
    public Dictionary<string, bool> Flags { get; } = new();

    /// <inheritdoc/>
    public bool IsEnabled(string flagName) => Flags.TryGetValue(flagName, out var v) && v;

    /// <inheritdoc/>
    public Task LoadFlagsAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task RefreshAsync() => Task.CompletedTask;
}
