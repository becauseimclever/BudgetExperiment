// Copyright (c) BecauseImClever. All rights reserved.

using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace BudgetExperiment.Performance.Tests.Scenarios;

/// <summary>
/// NBomber scenario for the <c>GET /api/v1/accounts</c> endpoint.
/// </summary>
public static class AccountsScenario
{
    /// <summary>The scenario name used in reports and threshold references.</summary>
    public const string Name = "get_accounts";

    /// <summary>
    /// Creates the accounts GET scenario with the specified load simulations.
    /// </summary>
    /// <param name="client">An authenticated <see cref="HttpClient"/>.</param>
    /// <param name="loadSimulations">The load simulations to apply.</param>
    /// <returns>A configured <see cref="ScenarioProps"/>.</returns>
    public static ScenarioProps Create(HttpClient client, params LoadSimulation[] loadSimulations)
    {
        return Scenario.Create(Name, async context =>
        {
            var request = Http.CreateRequest("GET", "/api/v1/accounts");
            var response = await Http.Send(client, request);
            return response;
        })
        .WithoutWarmUp()
        .WithLoadSimulations(loadSimulations);
    }
}
