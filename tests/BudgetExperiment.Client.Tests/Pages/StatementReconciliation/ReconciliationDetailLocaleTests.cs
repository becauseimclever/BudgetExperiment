// <copyright file="ReconciliationDetailLocaleTests.cs" company="BecauseImClever">
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
/// bUnit locale tests for the <see cref="ReconciliationDetail"/> page component.
/// Verifies that the locked transaction amount column is formatted using
/// <see cref="CultureService.CurrentCulture"/> rather than the thread culture.
/// </summary>
public class ReconciliationDetailLocaleTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();
    private readonly Guid _recordId = Guid.NewGuid();

    /// <summary>
    /// Initializes a new instance of the <see cref="ReconciliationDetailLocaleTests"/> class.
    /// </summary>
    public ReconciliationDetailLocaleTests()
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
    /// Verifies that the locked transaction amount is formatted with the Euro symbol and
    /// European decimal/thousands separators when CultureService is set to de-DE.
    /// Thread culture is intentionally kept at en-US to prove the component uses
    /// CultureService, not the thread culture.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task ReconciliationDetail_Render_WithDeDeLocale_FormatsAmountCorrectly()
    {
        // Arrange — thread stays en-US, CultureService is de-DE
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

        var cultureService = await TestCultureServiceFactory.CreateAsync("de-DE");
        this.Services.AddSingleton(cultureService);

        _apiService.ReconciliationTransactions.Add(new TransactionDto
        {
            Id = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Date = new DateOnly(2025, 6, 15),
            Description = "Locked Transaction",
            Amount = new MoneyDto { Amount = 1234.56m, Currency = "EUR" },
            ReconciliationRecordId = _recordId,
            IsCleared = true,
        });

        // Act — render page, the route parameter Id triggers LoadAsync(_recordId)
        var cut = Render<ReconciliationDetail>(p => p.Add(x => x.Id, _recordId));

        // Assert — de-DE: comma as decimal separator, period as thousands, € symbol
        cut.Markup.ShouldContain("1.234,56");
        cut.Markup.ShouldContain("€");
        cut.Markup.ShouldNotContain("$1,234.56");
    }
}
