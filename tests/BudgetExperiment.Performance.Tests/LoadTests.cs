// Copyright (c) BecauseImClever. All rights reserved.

using BudgetExperiment.Performance.Tests.Infrastructure;
using BudgetExperiment.Performance.Tests.Profiles;
using BudgetExperiment.Performance.Tests.Scenarios;

using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;

namespace BudgetExperiment.Performance.Tests;

/// <summary>
/// Load-level performance tests that establish baseline latency and throughput metrics
/// for P0 read endpoints under sustained concurrent traffic.
/// </summary>
[Trait("Category", "Performance")]
[Trait("Category", "Load")]
public sealed class LoadTests : IClassFixture<PerformanceWebApplicationFactory>, IAsyncLifetime
{
    private readonly PerformanceWebApplicationFactory _factory;
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadTests"/> class.
    /// </summary>
    /// <param name="factory">The web application factory.</param>
    public LoadTests(PerformanceWebApplicationFactory factory)
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
    /// Accounts endpoint under load — p95 under 500ms, p99 under 1000ms, error rate under 1%.
    /// </summary>
    [Fact]
    public void Accounts_LoadTest()
    {
        var scenario = AccountsScenario.Create(this._client, LoadProfile.Simulations())
            .WithThresholds(
                Threshold.Create(stats => stats.Ok.Latency.Percent95 < 500),
                Threshold.Create(stats => stats.Ok.Latency.Percent99 < 1000),
                Threshold.Create(stats => stats.Fail.Request.Percent < 1));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(ReportPath())
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .WithReportFileName("load_accounts")
            .Run();

        AssertNoThresholdFailures(result);
    }

    /// <summary>
    /// Transactions endpoint under load — p95 under 500ms, p99 under 1000ms, error rate under 1%.
    /// </summary>
    [Fact]
    public void Transactions_LoadTest()
    {
        var scenario = TransactionsScenario.Create(this._client, LoadProfile.Simulations())
            .WithThresholds(
                Threshold.Create(stats => stats.Ok.Latency.Percent95 < 500),
                Threshold.Create(stats => stats.Ok.Latency.Percent99 < 1000),
                Threshold.Create(stats => stats.Fail.Request.Percent < 1));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(ReportPath())
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .WithReportFileName("load_transactions")
            .Run();

        AssertNoThresholdFailures(result);
    }

    /// <summary>
    /// Calendar endpoint under load — this is the most complex read endpoint (9 sequential DB queries).
    /// Thresholds are relaxed compared to simpler endpoints.
    /// </summary>
    [Fact]
    public void Calendar_LoadTest()
    {
        var scenario = CalendarScenario.Create(this._client, LoadProfile.Simulations())
            .WithThresholds(
                Threshold.Create(stats => stats.Ok.Latency.Percent95 < 2000),
                Threshold.Create(stats => stats.Ok.Latency.Percent99 < 3000),
                Threshold.Create(stats => stats.Fail.Request.Percent < 1));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(ReportPath())
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .WithReportFileName("load_calendar")
            .Run();

        AssertNoThresholdFailures(result);
    }

    /// <summary>
    /// Budgets endpoint under load — p95 under 500ms, p99 under 1000ms, error rate under 1%.
    /// </summary>
    [Fact]
    public void Budgets_LoadTest()
    {
        var scenario = BudgetsScenario.Create(this._client, LoadProfile.Simulations())
            .WithThresholds(
                Threshold.Create(stats => stats.Ok.Latency.Percent95 < 500),
                Threshold.Create(stats => stats.Ok.Latency.Percent99 < 1000),
                Threshold.Create(stats => stats.Fail.Request.Percent < 1));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(ReportPath())
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .WithReportFileName("load_budgets")
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
