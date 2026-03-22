// Copyright (c) BecauseImClever. All rights reserved.

using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace BudgetExperiment.Performance.Tests.Scenarios;

/// <summary>
/// NBomber scenario for the <c>GET /api/v1/transactions</c> endpoint.
/// </summary>
public static class TransactionsScenario
{
    /// <summary>The scenario name used in reports and threshold references.</summary>
    public const string Name = "get_transactions";

    /// <summary>
    /// Creates the transactions GET scenario with the specified load simulations.
    /// The date range is computed relative to <see cref="DateTime.UtcNow"/> so the
    /// query always spans the last 6 months regardless of when the tests run.
    /// </summary>
    /// <param name="client">An authenticated <see cref="HttpClient"/>.</param>
    /// <param name="loadSimulations">The load simulations to apply.</param>
    /// <returns>A configured <see cref="ScenarioProps"/>.</returns>
    public static ScenarioProps Create(HttpClient client, params LoadSimulation[] loadSimulations)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = today.AddMonths(-6).ToString("yyyy-MM-dd");
        var endDate = today.ToString("yyyy-MM-dd");

        return Scenario.Create(Name, async context =>
        {
            var request = Http.CreateRequest("GET", $"/api/v1/transactions?startDate={startDate}&endDate={endDate}");
            var response = await Http.Send(client, request);
            return response;
        })
        .WithoutWarmUp()
        .WithLoadSimulations(loadSimulations);
    }
}
