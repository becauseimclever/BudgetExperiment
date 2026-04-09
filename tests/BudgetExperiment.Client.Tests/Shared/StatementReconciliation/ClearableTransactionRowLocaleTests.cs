// <copyright file="ClearableTransactionRowLocaleTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Shared.StatementReconciliation;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Shared.StatementReconciliation;

/// <summary>
/// bUnit locale tests for the <see cref="ClearableTransactionRow"/> component.
/// Verifies that the transaction amount is formatted using <see cref="CultureService.CurrentCulture"/>
/// rather than the thread culture.
/// </summary>
public class ClearableTransactionRowLocaleTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClearableTransactionRowLocaleTests"/> class.
    /// </summary>
    public ClearableTransactionRowLocaleTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that the transaction amount is formatted with the Euro symbol and
    /// European decimal/thousands separators when CultureService is set to de-DE.
    /// Thread culture is intentionally kept at en-US to prove the component uses
    /// CultureService, not the thread culture.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task ClearableTransactionRow_Render_WithDeDeLocale_FormatsAmountCorrectly()
    {
        // Arrange — thread stays en-US, CultureService is de-DE
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

        var cultureService = await TestCultureServiceFactory.CreateAsync("de-DE");
        this.Services.AddSingleton(cultureService);

        var transaction = new TransactionDto
        {
            Id = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Date = new DateOnly(2025, 6, 15),
            Description = "Test Transaction",
            Amount = new MoneyDto { Amount = 1234.56m, Currency = "EUR" },
            ReconciliationRecordId = null,
            IsCleared = false,
        };

        // Act
        var cut = Render<ClearableTransactionRow>(p => p
            .Add(x => x.Transaction, transaction)
            .Add(x => x.IsSelected, false)
            .Add(x => x.IsProcessing, false)
            .Add(x => x.StatementDate, new DateOnly(2025, 6, 30)));

        // Assert — de-DE: comma as decimal separator, period as thousands, € symbol
        cut.Markup.ShouldContain("1.234,56");
        cut.Markup.ShouldContain("€");
        cut.Markup.ShouldNotContain("$1,234.56");
    }

    /// <summary>
    /// Verifies that the transaction amount is formatted with the dollar sign when
    /// CultureService is set to en-US (regression test — existing behavior must not break).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task ClearableTransactionRow_Render_WithEnUsLocale_FormatsAmountCorrectly()
    {
        // Arrange
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

        var cultureService = await TestCultureServiceFactory.CreateAsync("en-US");
        this.Services.AddSingleton(cultureService);

        var transaction = new TransactionDto
        {
            Id = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Date = new DateOnly(2025, 6, 15),
            Description = "Test Transaction",
            Amount = new MoneyDto { Amount = 1234.56m, Currency = "USD" },
            ReconciliationRecordId = null,
            IsCleared = false,
        };

        // Act
        var cut = Render<ClearableTransactionRow>(p => p
            .Add(x => x.Transaction, transaction)
            .Add(x => x.IsSelected, false)
            .Add(x => x.IsProcessing, false)
            .Add(x => x.StatementDate, new DateOnly(2025, 6, 30)));

        // Assert — en-US: dollar prefix, comma as thousands, period as decimal
        cut.Markup.ShouldContain("$1,234.56");
        cut.Markup.ShouldNotContain("€");
    }
}
