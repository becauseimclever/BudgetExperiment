// Copyright (c) BecauseImClever. All rights reserved.

using BudgetExperiment.Performance.Tests.Infrastructure;
using BudgetExperiment.Performance.Tests.Profiles;
using BudgetExperiment.Performance.Tests.Scenarios;

using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;

namespace BudgetExperiment.Performance.Tests;

/// <summary>
/// Smoke-level performance tests that validate basic endpoint responsiveness.
/// </summary>
[Trait("Category", "Performance")]
[Trait("Category", "Smoke")]
public sealed class SmokeTests : IClassFixture<PerformanceWebApplicationFactory>, IAsyncLifetime
{
    private readonly PerformanceWebApplicationFactory _factory;
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmokeTests"/> class.
    /// </summary>
    /// <param name="factory">The web application factory.</param>
    public SmokeTests(PerformanceWebApplicationFactory factory)
    {
        this._factory = factory;
        this._client = factory.CreateApiClient();
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await TestDataSeeder.SeedAsync(this._factory);
    }

    /// <inheritdoc/>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Health check endpoint should respond in under 100ms with zero errors.
    /// </summary>
    [Fact]
    public void HealthCheck_SmokeTest()
    {
        var scenario = HealthCheckScenario.Create(this._client, SmokeProfile.Simulations())
            .WithThresholds(
                Threshold.Create(stats => stats.Ok.Latency.Percent99 < 100),
                Threshold.Create(stats => stats.Fail.Request.Count == 0));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(ReportPath())
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .WithReportFileName("smoke_health")
            .Run();

        AssertNoThresholdFailures(result);
    }

    /// <summary>
    /// Accounts endpoint smoke test — verify it responds under load without errors.
    /// </summary>
    [Fact]
    public void Accounts_SmokeTest()
    {
        var scenario = AccountsScenario.Create(this._client, SmokeProfile.Simulations())
            .WithThresholds(
                Threshold.Create(stats => stats.Ok.Latency.Percent99 < 1000),
                Threshold.Create(stats => stats.Fail.Request.Percent < 1));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(ReportPath())
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .WithReportFileName("smoke_accounts")
            .Run();

        AssertNoThresholdFailures(result);
    }

    /// <summary>
    /// Transactions endpoint smoke test — verify it responds under load without errors.
    /// </summary>
    [Fact]
    public void Transactions_SmokeTest()
    {
        var scenario = TransactionsScenario.Create(this._client, SmokeProfile.Simulations())
            .WithThresholds(
                Threshold.Create(stats => stats.Ok.Latency.Percent99 < 1000),
                Threshold.Create(stats => stats.Fail.Request.Percent < 1));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(ReportPath())
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .WithReportFileName("smoke_transactions")
            .Run();

        AssertNoThresholdFailures(result);
    }

    /// <summary>
    /// Calendar endpoint smoke test — verify it responds under load without errors.
    /// </summary>
    [Fact]
    public void Calendar_SmokeTest()
    {
        var scenario = CalendarScenario.Create(this._client, SmokeProfile.Simulations())
            .WithThresholds(
                Threshold.Create(stats => stats.Ok.Latency.Percent99 < 1000),
                Threshold.Create(stats => stats.Fail.Request.Percent < 1));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(ReportPath())
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .WithReportFileName("smoke_calendar")
            .Run();

        AssertNoThresholdFailures(result);
    }

    /// <summary>
    /// Budgets endpoint smoke test — verify it responds under load without errors.
    /// </summary>
    [Fact]
    public void Budgets_SmokeTest()
    {
        var scenario = BudgetsScenario.Create(this._client, SmokeProfile.Simulations())
            .WithThresholds(
                Threshold.Create(stats => stats.Ok.Latency.Percent99 < 1000),
                Threshold.Create(stats => stats.Fail.Request.Percent < 1));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(ReportPath())
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .WithReportFileName("smoke_budgets")
            .Run();

        AssertNoThresholdFailures(result);
    }

    private static string ReportPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "reports");
    }

    private static void AssertNoThresholdFailures(NodeStats result)
    {
        var failures = result.Thresholds.Where(t => t.IsFailed).ToList();
        if (failures.Count > 0)
        {
            var messages = failures.Select(f =>
                $"[{f.ScenarioName}] {f.CheckExpression} — FAILED (errors: {f.ErrorCount})");
            Assert.Fail($"Performance thresholds failed:\n{string.Join("\n", messages)}");
        }
    }
}
