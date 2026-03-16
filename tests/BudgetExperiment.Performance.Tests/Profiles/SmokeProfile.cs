// Copyright (c) BecauseImClever. All rights reserved.

using NBomber.Contracts;
using NBomber.CSharp;

namespace BudgetExperiment.Performance.Tests.Profiles;

/// <summary>
/// Smoke test profile: 1 virtual user, 10 seconds. Verifies the system works under minimal load.
/// </summary>
public static class SmokeProfile
{
    /// <summary>
    /// Gets the load simulation for a smoke test: 1 request/second for 10 seconds.
    /// </summary>
    /// <returns>A load simulation array.</returns>
    public static LoadSimulation[] Simulations()
    {
        return
        [
            Simulation.Inject(
                rate: 1,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(10)),
        ];
    }
}
