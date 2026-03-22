// Copyright (c) BecauseImClever. All rights reserved.

using System.Text;
using System.Text.Json;

using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace BudgetExperiment.Performance.Tests.Scenarios;

/// <summary>
/// NBomber scenario for the <c>POST /api/v1/transactions</c> endpoint.
/// </summary>
public static class TransactionsWriteScenario
{
    /// <summary>The scenario name used in reports and threshold references.</summary>
    public const string Name = "post_transactions";

    /// <summary>
    /// Creates the transactions POST scenario with the specified load simulations.
    /// Each iteration creates a new transaction with randomised data.
    /// </summary>
    /// <param name="client">An authenticated <see cref="HttpClient"/>.</param>
    /// <param name="accountId">The target account ID for new transactions.</param>
    /// <param name="loadSimulations">The load simulations to apply.</param>
    /// <returns>A configured <see cref="ScenarioProps"/>.</returns>
    public static ScenarioProps Create(HttpClient client, Guid accountId, params LoadSimulation[] loadSimulations)
    {
        return Scenario.Create(Name, async context =>
        {
            var body = JsonSerializer.Serialize(new
            {
                accountId,
                amount = new
                {
                    currency = "USD",
                    amount = -(context.ScenarioInfo.InstanceNumber + 1) * 5.99m,
                },
                date = "2026-02-15",
                description = $"Perf Test Item {context.InvocationNumber}",
            });

            var request = Http.CreateRequest("POST", "/api/v1/transactions")
                .WithBody(new StringContent(body, Encoding.UTF8, "application/json"));

            var response = await Http.Send(client, request);
            return response;
        })
        .WithoutWarmUp()
        .WithLoadSimulations(loadSimulations);
    }
}
