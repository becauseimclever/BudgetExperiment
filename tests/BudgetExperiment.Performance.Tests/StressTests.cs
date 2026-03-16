// Copyright (c) BecauseImClever. All rights reserved.

using BudgetExperiment.Performance.Tests.Infrastructure;
using BudgetExperiment.Performance.Tests.Profiles;
using BudgetExperiment.Performance.Tests.Scenarios;

using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;

namespace BudgetExperiment.Performance.Tests;

/// <summary>
/// Stress and spike tests that push the system beyond normal load
/// to identify breaking points and validate recovery behaviour.
/// </summary>
[Trait("Category", "Performance")]
[Trait("Category", "Stress")]
public sealed class StressTests : IClassFixture<PerformanceWebApplicationFactory>, IAsyncLifetime
{
    private readonly PerformanceWebApplicationFactory _factory;
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="StressTests"/> class.
    /// </summary>
    /// <param name="factory">The web application factory.</param>
    public StressTests(PerformanceWebApplicationFactory factory)
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
    /// Transactions POST under load — validates write path handles sustained traffic.
    /// </summary>
    [Fact]
    public void TransactionsWrite_LoadTest()
    {
        var scenario = TransactionsWriteScenario.Create(
            this._client,
            TestDataSeeder.FirstAccountId,
            LoadProfile.Simulations())
            .WithThresholds(
                Threshold.Create(stats => stats.Ok.Latency.Percent95 < 1000),
                Threshold.Create(stats => stats.Fail.Request.Percent < 1));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(ReportPath())
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .WithReportFileName("load_transactions_write")
            .Run();

        AssertNoThresholdFailures(result);
    }

    /// <summary>
    /// Recurring transactions GET under load — P1 endpoint baseline.
    /// </summary>
    [Fact]
    public void RecurringTransactions_LoadTest()
    {
        var scenario = RecurringTransactionsScenario.Create(this._client, LoadProfile.Simulations())
            .WithThresholds(
                Threshold.Create(stats => stats.Ok.Latency.Percent95 < 500),
                Threshold.Create(stats => stats.Ok.Latency.Percent99 < 1000),
                Threshold.Create(stats => stats.Fail.Request.Percent < 1));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(ReportPath())
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .WithReportFileName("load_recurring_transactions")
            .Run();

        AssertNoThresholdFailures(result);
    }

    /// <summary>
    /// Transactions read under stress — ramp to 100 req/s to find degradation thresholds.
    /// Per the feature doc, stress tests observe degradation without hard latency pass/fail.
    /// Latency metrics are captured in the HTML/CSV report for analysis.
    /// </summary>
    [Fact]
    public void Transactions_StressTest()
    {
        var scenario = TransactionsScenario.Create(this._client, StressProfile.Simulations())
            .WithThresholds(
                Threshold.Create(stats => stats.Fail.Request.Percent < 5));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(ReportPath())
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .WithReportFileName("stress_transactions")
            .Run();

        AssertNoThresholdFailures(result);
    }

    /// <summary>
    /// Calendar endpoint under stress — the most complex read endpoint (9 sequential DB queries).
    /// Stress tests observe degradation without hard latency thresholds.
    /// </summary>
    [Fact]
    public void Calendar_StressTest()
    {
        var scenario = CalendarScenario.Create(this._client, StressProfile.Simulations())
            .WithThresholds(
                Threshold.Create(stats => stats.Fail.Request.Percent < 5));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(ReportPath())
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .WithReportFileName("stress_calendar")
            .Run();

        AssertNoThresholdFailures(result);
    }

    /// <summary>
    /// Spike test — sudden burst of traffic followed by recovery.
    /// Validates the system recovers gracefully and error rate stays under 5% during spike.
    /// </summary>
    [Fact]
    public void Transactions_SpikeTest()
    {
        var scenario = TransactionsScenario.Create(this._client, SpikeProfile.Simulations())
            .WithThresholds(
                Threshold.Create(stats => stats.Fail.Request.Percent < 5));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(ReportPath())
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .WithReportFileName("spike_transactions")
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
