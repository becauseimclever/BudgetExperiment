// Copyright (c) BecauseImClever. All rights reserved.

using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace BudgetExperiment.Performance.Tests.Scenarios;

/// <summary>
/// NBomber scenario for the <c>GET /api/v1/calendar/grid</c> endpoint.
/// </summary>
public static class CalendarScenario
{
    /// <summary>The scenario name used in reports and threshold references.</summary>
    public const string Name = "get_calendar";

    /// <summary>
    /// Creates the calendar grid GET scenario with the specified load simulations.
    /// </summary>
    /// <param name="client">An authenticated <see cref="HttpClient"/>.</param>
    /// <param name="loadSimulations">The load simulations to apply.</param>
    /// <returns>A configured <see cref="ScenarioProps"/>.</returns>
    public static ScenarioProps Create(HttpClient client, params LoadSimulation[] loadSimulations)
    {
        return Scenario.Create(Name, async context =>
        {
            var request = Http.CreateRequest("GET", "/api/v1/calendar/grid?year=2026&month=3");
            var response = await Http.Send(client, request);
            return response;
        })
        .WithoutWarmUp()
        .WithLoadSimulations(loadSimulations);
    }
}
