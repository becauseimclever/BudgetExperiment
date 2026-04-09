// <copyright file="ReconciliationHistoryLocaleTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Client.Pages.StatementReconciliation;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Pages.StatementReconciliation;

/// <summary>
/// bUnit locale tests for the <see cref="ReconciliationHistory"/> page component.
/// Verifies that statement and cleared balance columns are formatted using
/// <see cref="CultureService.CurrentCulture"/> rather than the thread culture.
/// </summary>
public class ReconciliationHistoryLocaleTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();
    private readonly Guid _accountId = Guid.NewGuid();

    /// <summary>
    /// Initializes a new instance of the <see cref="ReconciliationHistoryLocaleTests"/> class.
    /// </summary>
    public ReconciliationHistoryLocaleTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(_apiService);
        this.Services.AddSingleton<IToastService>(new ToastService());
        this.Services.AddSingleton<IExportDownloadService>(new StubExportDownloadService());
        this.Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that the statement balance and cleared balance columns are formatted with
    /// the Euro symbol and European separators when CultureService is set to de-DE.
    /// Thread culture is intentionally kept at en-US to prove the component uses
    /// CultureService, not the thread culture.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task ReconciliationHistory_Render_WithDeDeLocale_FormatsLineAmountsCorrectly()
    {
        // Arrange — thread stays en-US, CultureService is de-DE
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

        var cultureService = await TestCultureServiceFactory.CreateAsync("de-DE");
        this.Services.AddSingleton(cultureService);

        _apiService.Accounts.Add(new AccountDto
        {
            Id = _accountId,
            Name = "Test Account",
            Type = "Checking",
            InitialBalance = 0m,
            InitialBalanceCurrency = "EUR",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        _apiService.ReconciliationHistory.Add(new ReconciliationRecordDto
        {
            Id = Guid.NewGuid(),
            AccountId = _accountId,
            StatementDate = new DateOnly(2025, 6, 30),
            StatementBalance = 1234.56m,
            ClearedBalance = 1200.00m,
            TransactionCount = 5,
            CompletedAtUtc = new DateTime(2025, 7, 1, 12, 0, 0, DateTimeKind.Utc),
        });

        // Act — render page and select the account to load the history table
        var cut = Render<ReconciliationHistory>();

        var select = cut.Find("select");
        await select.ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = _accountId.ToString() });

        // Assert — de-DE: comma as decimal separator, period as thousands, € symbol
        cut.Markup.ShouldContain("1.234,56");
        cut.Markup.ShouldContain("1.200,00");
        cut.Markup.ShouldContain("€");
        cut.Markup.ShouldNotContain("$1,234.56");
        cut.Markup.ShouldNotContain("$1,200.00");
    }
}
