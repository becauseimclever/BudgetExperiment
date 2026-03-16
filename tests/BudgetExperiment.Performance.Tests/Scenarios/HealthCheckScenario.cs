// Copyright (c) BecauseImClever. All rights reserved.

using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace BudgetExperiment.Performance.Tests.Scenarios;

/// <summary>
/// NBomber scenario for the <c>GET /health</c> endpoint.
/// </summary>
public static class HealthCheckScenario
{
    /// <summary>The scenario name used in reports and threshold references.</summary>
    public const string Name = "health_check";

    /// <summary>
    /// Creates the health check scenario with the specified load simulations.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> to use.</param>
    /// <param name="loadSimulations">The load simulations to apply.</param>
    /// <returns>A configured <see cref="ScenarioProps"/>.</returns>
    public static ScenarioProps Create(HttpClient client, params LoadSimulation[] loadSimulations)
    {
        return Scenario.Create(Name, async context =>
        {
            var request = Http.CreateRequest("GET", "/health");
            var response = await Http.Send(client, request);
            return response;
        })
        .WithoutWarmUp()
        .WithLoadSimulations(loadSimulations);
    }
}
