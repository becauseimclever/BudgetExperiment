// <copyright file="ToleranceSettingsPanelTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Reconciliation;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.Reconciliation;

/// <summary>
/// Unit tests for the <see cref="ToleranceSettingsPanel"/> component.
/// </summary>
public sealed class ToleranceSettingsPanelTests : BunitContext, IAsyncLifetime
{
    private readonly StubReconciliationApiService _reconciliationApi;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToleranceSettingsPanelTests"/> class.
    /// </summary>
    public ToleranceSettingsPanelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
        _reconciliationApi = new StubReconciliationApiService();
        Services.AddSingleton<IReconciliationApiService>(_reconciliationApi);
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies the panel renders the container element initially.
    /// </summary>
    [Fact]
    public void ToleranceSettingsPanel_RendersContainer()
    {
        var cut = Render<ToleranceSettingsPanel>();

        Assert.Contains("tolerance-settings-panel", cut.Markup);
    }

    /// <summary>
    /// Verifies the panel shows the title.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToleranceSettingsPanel_ShowsTitle()
    {
        _reconciliationApi.TolerancesResult = new MatchingTolerancesDto();

        var cut = Render<ToleranceSettingsPanel>();
        await Task.Delay(50);
        cut.Render();

        Assert.Contains("Matching Tolerances", cut.Markup);
    }

    /// <summary>
    /// Verifies the panel shows preset buttons after loading.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToleranceSettingsPanel_ShowsPresetButtons()
    {
        _reconciliationApi.TolerancesResult = new MatchingTolerancesDto();

        var cut = Render<ToleranceSettingsPanel>();
        await Task.Delay(50);
        cut.Render();

        Assert.Contains("Strict", cut.Markup);
        Assert.Contains("Moderate", cut.Markup);
        Assert.Contains("Loose", cut.Markup);
    }

    /// <summary>
    /// Verifies the panel shows date tolerance setting.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToleranceSettingsPanel_ShowsDateToleranceSetting()
    {
        _reconciliationApi.TolerancesResult = new MatchingTolerancesDto();

        var cut = Render<ToleranceSettingsPanel>();
        await Task.Delay(50);
        cut.Render();

        Assert.Contains("Date Tolerance", cut.Markup);
    }

    /// <summary>
    /// Verifies the panel shows amount tolerance percent setting.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToleranceSettingsPanel_ShowsAmountTolerancePercentSetting()
    {
        _reconciliationApi.TolerancesResult = new MatchingTolerancesDto();

        var cut = Render<ToleranceSettingsPanel>();
        await Task.Delay(50);
        cut.Render();

        Assert.Contains("Amount Tolerance (%)", cut.Markup);
    }

    /// <summary>
    /// Verifies the panel shows description similarity setting.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToleranceSettingsPanel_ShowsDescriptionSimilaritySetting()
    {
        _reconciliationApi.TolerancesResult = new MatchingTolerancesDto();

        var cut = Render<ToleranceSettingsPanel>();
        await Task.Delay(50);
        cut.Render();

        Assert.Contains("Description Similarity", cut.Markup);
    }

    /// <summary>
    /// Verifies the panel shows auto-match threshold setting.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToleranceSettingsPanel_ShowsAutoMatchThresholdSetting()
    {
        _reconciliationApi.TolerancesResult = new MatchingTolerancesDto();

        var cut = Render<ToleranceSettingsPanel>();
        await Task.Delay(50);
        cut.Render();

        Assert.Contains("Auto-Match Threshold", cut.Markup);
    }

    /// <summary>
    /// Verifies the panel shows help text.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToleranceSettingsPanel_ShowsHelpText()
    {
        _reconciliationApi.TolerancesResult = new MatchingTolerancesDto();

        var cut = Render<ToleranceSettingsPanel>();
        await Task.Delay(50);
        cut.Render();

        Assert.Contains("Configure how strictly", cut.Markup);
    }

    /// <summary>
    /// Verifies the panel renders five setting rows.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ToleranceSettingsPanel_RendersFiveSettingRows()
    {
        _reconciliationApi.TolerancesResult = new MatchingTolerancesDto();

        var cut = Render<ToleranceSettingsPanel>();
        await Task.Delay(50);
        cut.Render();

        var rows = cut.FindAll(".setting-row");
        Assert.Equal(5, rows.Count);
    }

    private sealed class StubReconciliationApiService : IReconciliationApiService
    {
        /// <summary>Gets or sets the tolerances result.</summary>
        public MatchingTolerancesDto? TolerancesResult { get; set; }

        /// <summary>Gets or sets a value indicating whether update succeeds.</summary>
        public bool UpdateTolerancesSuccess { get; set; } = true;

        /// <inheritdoc/>
        public Task<MatchingTolerancesDto?> GetTolerancesAsync() => Task.FromResult(this.TolerancesResult);

        /// <inheritdoc/>
        public Task<bool> UpdateTolerancesAsync(MatchingTolerancesDto tolerances) => Task.FromResult(this.UpdateTolerancesSuccess);

        /// <inheritdoc/>
        public Task<ReconciliationStatusDto?> GetStatusAsync(int year, int month, Guid? accountId = null) => Task.FromResult<ReconciliationStatusDto?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<ReconciliationMatchDto>> GetPendingMatchesAsync(Guid? accountId = null) => Task.FromResult<IReadOnlyList<ReconciliationMatchDto>>([]);

        /// <inheritdoc/>
        public Task<FindMatchesResult?> FindMatchesAsync(FindMatchesRequest request) => Task.FromResult<FindMatchesResult?>(null);

        /// <inheritdoc/>
        public Task<bool> AcceptMatchAsync(Guid matchId) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<bool> RejectMatchAsync(Guid matchId) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<int> BulkAcceptMatchesAsync(IReadOnlyList<Guid> matchIds) => Task.FromResult(0);

        /// <inheritdoc/>
        public Task<ReconciliationMatchDto?> CreateManualMatchAsync(ManualMatchRequest request) => Task.FromResult<ReconciliationMatchDto?>(null);

        /// <inheritdoc/>
        public Task<bool> UnlinkMatchAsync(Guid matchId) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<IReadOnlyList<LinkableInstanceDto>> GetLinkableInstancesAsync(Guid transactionId) => Task.FromResult<IReadOnlyList<LinkableInstanceDto>>([]);
    }
}
