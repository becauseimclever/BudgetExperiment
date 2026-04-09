// <copyright file="ReconciliationBalanceBarLocaleTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Shared.StatementReconciliation;
using BudgetExperiment.Client.Tests.TestHelpers;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Shared.StatementReconciliation;

/// <summary>
/// bUnit locale tests for the <see cref="ReconciliationBalanceBar"/> component.
/// Verifies that currency values are formatted using <see cref="CultureService.CurrentCulture"/>
/// rather than the thread culture, covering both de-DE and en-US locales.
/// </summary>
public class ReconciliationBalanceBarLocaleTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReconciliationBalanceBarLocaleTests"/> class.
    /// </summary>
    public ReconciliationBalanceBarLocaleTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that all three monetary values are formatted with the Euro symbol and
    /// European decimal/thousands separators when CultureService is set to de-DE.
    /// Thread culture is intentionally kept at en-US to prove the component uses
    /// CultureService, not the thread culture.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task ReconciliationBalanceBar_Render_WithDeDeLocale_FormatsThreeValuesCorrectly()
    {
        // Arrange — thread stays en-US, CultureService is de-DE
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

        var cultureService = await BuildCultureServiceAsync("de-DE");
        this.Services.AddSingleton(cultureService);

        // Act
        var cut = Render<ReconciliationBalanceBar>(p => p
            .Add(x => x.ClearedBalance, 1234.56m)
            .Add(x => x.StatementBalance, 2000.00m)
            .Add(x => x.Difference, 765.44m)
            .Add(x => x.IsBalanced, false));

        // Assert — de-DE: comma as decimal separator, period as thousands, € symbol
        cut.Markup.ShouldContain("1.234,56");
        cut.Markup.ShouldContain("2.000,00");
        cut.Markup.ShouldContain("765,44");
        cut.Markup.ShouldContain("€");
        cut.Markup.ShouldNotContain("$");
    }

    /// <summary>
    /// Verifies that all three monetary values are formatted with the dollar sign and
    /// US decimal/thousands separators when CultureService is set to en-US.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task ReconciliationBalanceBar_Render_WithEnUsLocale_FormatsThreeValuesCorrectly()
    {
        // Arrange
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

        var cultureService = await BuildCultureServiceAsync("en-US");
        this.Services.AddSingleton(cultureService);

        // Act
        var cut = Render<ReconciliationBalanceBar>(p => p
            .Add(x => x.ClearedBalance, 1234.56m)
            .Add(x => x.StatementBalance, 2000.00m)
            .Add(x => x.Difference, 765.44m)
            .Add(x => x.IsBalanced, false));

        // Assert — en-US: dollar prefix, comma as thousands, period as decimal
        cut.Markup.ShouldContain("$1,234.56");
        cut.Markup.ShouldContain("$2,000.00");
        cut.Markup.ShouldContain("$765.44");
        cut.Markup.ShouldNotContain("€");
    }

    private static async Task<CultureService> BuildCultureServiceAsync(string language)
        => await TestCultureServiceFactory.CreateAsync(language);
}
