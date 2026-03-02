// <copyright file="ImportFieldTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the ImportField enum.
/// </summary>
public class ImportFieldTests
{
    [Theory]
    [InlineData(ImportField.Ignore, 0)]
    [InlineData(ImportField.Date, 1)]
    [InlineData(ImportField.Description, 2)]
    [InlineData(ImportField.Amount, 3)]
    [InlineData(ImportField.DebitAmount, 4)]
    [InlineData(ImportField.CreditAmount, 5)]
    [InlineData(ImportField.Category, 6)]
    [InlineData(ImportField.Reference, 7)]
    [InlineData(ImportField.DebitCreditIndicator, 8)]
    public void ImportField_Has_Expected_Values(ImportField field, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)field);
    }
}
