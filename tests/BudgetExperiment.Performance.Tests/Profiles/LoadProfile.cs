// Copyright (c) BecauseImClever. All rights reserved.

using NBomber.Contracts;
using NBomber.CSharp;

namespace BudgetExperiment.Performance.Tests.Profiles;

/// <summary>
/// Standard load test profile: simulates expected production traffic for baseline measurement.
/// </summary>
public static class LoadProfile
{
    /// <summary>
    /// Gets the load simulation: ramp to 10 concurrent users over 5 seconds, sustain for 30 seconds.
    /// </summary>
    /// <returns>A load simulation array.</returns>
    public static LoadSimulation[] Simulations()
    {
        return
        [
            Simulation.RampingInject(
                rate: 10,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(5)),
            Simulation.Inject(
                rate: 10,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(30)),
        ];
    }
}
