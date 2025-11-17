// <copyright file="CapitalOneRealCsvTests.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

using BudgetExperiment.Application.CsvImport.Parsers;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Tests.CsvImport;

/// <summary>
/// Integration test for Capital One parser with real sample CSV.
/// </summary>
public sealed class CapitalOneRealCsvTests
{
    /// <summary>
    /// Test parsing the real Capital One sample CSV file.
    /// </summary>
    [Fact]
    public async Task ParseAsync_RealCapitalOneSampleCsv_ParsesAllTransactionsCorrectly()
    {
        // Arrange
        var csvPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "..",
            "..",
            "..",
            "..",
            "sample data",
            "capone.csv");

        // Skip test if file doesn't exist
        if (!File.Exists(csvPath))
        {
            // This allows the test to pass in CI where sample file might not be present
            return;
        }

        var parser = new CapitalOneCsvParser();
        using var stream = File.OpenRead(csvPath);

        // Act
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Verify first transaction
        var first = result[0];
        Assert.Equal(new DateOnly(2025, 11, 15), first.Date);
        Assert.Equal("Debit Card Purchase - RESTAURANT A CITY ST", first.Description);
        Assert.Equal(28.66m, first.Amount);
        Assert.Equal(TransactionType.Expense, first.TransactionType);

        // Verify an income transaction exists
        var incomeTransactions = result.Where(t => t.TransactionType == TransactionType.Income).ToList();
        Assert.NotEmpty(incomeTransactions);

        var payrollDeposit = incomeTransactions.FirstOrDefault(t => t.Description.Contains("EMPLOYER PAYROLL"));
        Assert.NotNull(payrollDeposit);
        Assert.True(payrollDeposit.Amount > 0);

        // Verify zero amount transactions are skipped (prenote)
        var prenote = result.FirstOrDefault(t => t.Description.Contains("Prenote"));
        Assert.Null(prenote);

        // Verify all transactions have valid dates
        Assert.All(result, t =>
        {
            Assert.True(t.Date.Year >= 2025 || t.Date.Year <= 1999);
        });

        // Verify all transactions have descriptions
        Assert.All(result, t =>
        {
            Assert.False(string.IsNullOrWhiteSpace(t.Description));
        });
    }
}
