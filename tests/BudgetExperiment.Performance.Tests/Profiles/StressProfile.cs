// Copyright (c) BecauseImClever. All rights reserved.

using NBomber.Contracts;
using NBomber.CSharp;

namespace BudgetExperiment.Performance.Tests.Profiles;

/// <summary>
/// Stress test profile: ramps from 10 to 100 concurrent requests per second
/// over 120 seconds to find the system&apos;s breaking point.
/// </summary>
public static class StressProfile
{
    /// <summary>
    /// Gets the stress load simulation: ramp from 10 to 100 req/s over 60 seconds,
    /// then sustain 100 req/s for another 60 seconds.
    /// </summary>
    /// <returns>A load simulation array.</returns>
    public static LoadSimulation[] Simulations()
    {
        return
        [
            Simulation.RampingInject(
                rate: 100,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(60)),
            Simulation.Inject(
                rate: 100,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(60)),
        ];
    }
}
