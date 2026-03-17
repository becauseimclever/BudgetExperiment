// <copyright file="CurrencyFormattingExtensionsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Client.Services;

using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="CurrencyFormattingExtensions"/> class.
/// </summary>
public sealed class CurrencyFormattingExtensionsTests
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CurrencyFormattingExtensionsTests"/> class.
    /// </summary>
    public CurrencyFormattingExtensionsTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    /// <summary>
    /// Verifies that FormatCurrency produces the expected format for en-US.
    /// </summary>
    [Fact]
    public void FormatCurrency_EnUS_ProducesDollarFormat()
    {
        var culture = CultureInfo.GetCultureInfo("en-US");

        var result = 42.50m.FormatCurrency(culture);

        Assert.Equal("$42.50", result);
    }

    /// <summary>
    /// Verifies that FormatCurrency produces the expected format for fr-FR.
    /// </summary>
    [Fact]
    public void FormatCurrency_FrFR_ProducesEuroFormat()
    {
        var culture = CultureInfo.GetCultureInfo("fr-FR");

        var result = 42.50m.FormatCurrency(culture);

        // fr-FR uses Euro with non-breaking space separator
        Assert.Contains("42,50", result);
        Assert.Contains("€", result);
    }

    /// <summary>
    /// Verifies that FormatCurrency handles negative amounts.
    /// </summary>
    [Fact]
    public void FormatCurrency_NegativeAmount_FormatsCorrectly()
    {
        var culture = CultureInfo.GetCultureInfo("en-US");

        var result = (-99.99m).FormatCurrency(culture);

        Assert.Contains("$99.99", result);
    }

    /// <summary>
    /// Verifies that FormatCurrency handles zero.
    /// </summary>
    [Fact]
    public void FormatCurrency_Zero_FormatsCorrectly()
    {
        var culture = CultureInfo.GetCultureInfo("en-US");

        var result = 0m.FormatCurrency(culture);

        Assert.Equal("$0.00", result);
    }

    /// <summary>
    /// Verifies that nullable decimal FormatCurrency returns empty for null.
    /// </summary>
    [Fact]
    public void FormatCurrency_NullDecimal_ReturnsEmptyString()
    {
        var culture = CultureInfo.GetCultureInfo("en-US");
        decimal? value = null;

        var result = value.FormatCurrency(culture);

        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    /// Verifies that nullable decimal FormatCurrency formats non-null values.
    /// </summary>
    [Fact]
    public void FormatCurrency_NullableWithValue_FormatsCorrectly()
    {
        var culture = CultureInfo.GetCultureInfo("en-US");
        decimal? value = 100.00m;

        var result = value.FormatCurrency(culture);

        Assert.Equal("$100.00", result);
    }

    /// <summary>
    /// Stub JavaScript runtime for CultureService creation.
    /// </summary>
    private sealed class StubJSRuntime : IJSRuntime
    {
        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return new ValueTask<TValue>(default(TValue)!);
        }

        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return new ValueTask<TValue>(default(TValue)!);
        }
    }
}
