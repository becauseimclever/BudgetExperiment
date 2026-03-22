// Copyright (c) BecauseImClever. All rights reserved.

using NBomber.Contracts;
using NBomber.CSharp;

namespace BudgetExperiment.Performance.Tests.Profiles;

/// <summary>
/// Reduced stress profile for the calendar endpoint, which performs 9 sequential
/// DB queries per request and degrades rapidly at high concurrency.
/// Uses 25 req/s (vs 100 for the standard stress profile) to identify degradation
/// without creating an unbounded request backlog.
/// </summary>
public static class CalendarStressProfile
{
    /// <summary>
    /// Gets the calendar stress simulation: ramp to 25 req/s over 30 seconds,
    /// then sustain 25 req/s for 30 seconds.
    /// </summary>
    /// <returns>A load simulation array.</returns>
    public static LoadSimulation[] Simulations()
    {
        return
        [
            Simulation.RampingInject(
                rate: 25,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(
                rate: 25,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(30)),
        ];
    }
}
