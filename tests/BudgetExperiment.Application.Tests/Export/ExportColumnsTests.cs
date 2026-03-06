// <copyright file="ExportColumnsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Export;

namespace BudgetExperiment.Application.Tests.Export;

/// <summary>
/// Regression guard tests for <see cref="ExportColumns"/> values.
/// Ensures export column header strings are never accidentally changed.
/// </summary>
public sealed class ExportColumnsTests
{
    /// <summary>
    /// Each column constant must equal its expected string value.
    /// </summary>
    /// <param name="fieldName">The constant field name.</param>
    /// <param name="expectedValue">The expected string value.</param>
    [Theory]
    [InlineData(nameof(ExportColumns.Category), "Category")]
    [InlineData(nameof(ExportColumns.Transactions), "Transactions")]
    [InlineData(nameof(ExportColumns.Amount), "Amount")]
    [InlineData(nameof(ExportColumns.Currency), "Currency")]
    [InlineData(nameof(ExportColumns.Percentage), "Percentage")]
    [InlineData(nameof(ExportColumns.Month), "Month")]
    [InlineData(nameof(ExportColumns.Income), "Income")]
    [InlineData(nameof(ExportColumns.Spending), "Spending")]
    [InlineData(nameof(ExportColumns.Net), "Net")]
    [InlineData(nameof(ExportColumns.Budgeted), "Budgeted")]
    [InlineData(nameof(ExportColumns.Spent), "Spent")]
    [InlineData(nameof(ExportColumns.Remaining), "Remaining")]
    [InlineData(nameof(ExportColumns.PercentUsed), "PercentUsed")]
    [InlineData(nameof(ExportColumns.Status), "Status")]
    public void Column_EqualsExpectedValue(string fieldName, string expectedValue)
    {
        var field = typeof(ExportColumns).GetField(fieldName);
        Assert.NotNull(field);
        Assert.Equal(expectedValue, field.GetValue(null));
    }
}
