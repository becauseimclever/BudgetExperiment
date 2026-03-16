// Copyright (c) BecauseImClever. All rights reserved.

using NBomber.Contracts;
using NBomber.CSharp;

namespace BudgetExperiment.Performance.Tests.Profiles;

/// <summary>
/// Spike test profile: maintains a baseline of 5 req/s, spikes to 50 req/s
/// for 10 seconds, then recovers back to 5 req/s to test resilience.
/// </summary>
public static class SpikeProfile
{
    /// <summary>
    /// Gets the spike load simulation: baseline → spike → recovery.
    /// </summary>
    /// <returns>A load simulation array.</returns>
    public static LoadSimulation[] Simulations()
    {
        return
        [
            Simulation.Inject(
                rate: 5,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(10)),
            Simulation.Inject(
                rate: 50,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(10)),
            Simulation.Inject(
                rate: 5,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(10)),
        ];
    }
}
