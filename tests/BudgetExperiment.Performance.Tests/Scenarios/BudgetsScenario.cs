// Copyright (c) BecauseImClever. All rights reserved.

using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace BudgetExperiment.Performance.Tests.Scenarios;

/// <summary>
/// NBomber scenario for the <c>GET /api/v1/budgets</c> endpoint.
/// </summary>
public static class BudgetsScenario
{
    /// <summary>The scenario name used in reports and threshold references.</summary>
    public const string Name = "get_budgets";

    /// <summary>
    /// Creates the budgets GET scenario with the specified load simulations.
    /// The year and month are derived from <see cref="DateTime.UtcNow"/> so the
    /// query always targets the current month's budget regardless of when tests run.
    /// </summary>
    /// <param name="client">An authenticated <see cref="HttpClient"/>.</param>
    /// <param name="loadSimulations">The load simulations to apply.</param>
    /// <returns>A configured <see cref="ScenarioProps"/>.</returns>
    public static ScenarioProps Create(HttpClient client, params LoadSimulation[] loadSimulations)
    {
        var now = DateTime.UtcNow;
        var year = now.Year;
        var month = now.Month;

        return Scenario.Create(Name, async context =>
        {
            var request = Http.CreateRequest("GET", $"/api/v1/budgets?year={year}&month={month}");
            var response = await Http.Send(client, request);
            return response;
        })
        .WithoutWarmUp()
        .WithLoadSimulations(loadSimulations);
    }
}
