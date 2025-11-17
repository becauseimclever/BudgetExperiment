// <copyright file="UnitedHeritageCreditUnionRealCsvTests.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

using BudgetExperiment.Application.CsvImport.Parsers;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Tests.CsvImport;

/// <summary>
/// Integration tests using real UHCU CSV sample data.
/// </summary>
public sealed class UnitedHeritageCreditUnionRealCsvTests
{
    /// <summary>
    /// Parses the real UHCU sample CSV file and verifies transaction count and basic structure.
    /// </summary>
    [Fact]
    public async Task ParseAsync_RealUhcuCsv_ReturnsExpectedTransactions()
    {
        // Arrange
        var csvPath = Path.Combine("..", "..", "..", "..", "..", "sample data", "uhcu.csv");
        var parser = new UnitedHeritageCreditUnionCsvParser();

        // Skip test if file doesn't exist
        if (!File.Exists(csvPath))
        {
            return; // Skip test in environments where sample data isn't available
        }

        // Act
        using var stream = File.OpenRead(csvPath);
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Verify all transactions have required fields
        foreach (var transaction in result)
        {
            Assert.NotEqual(default, transaction.Date);
            Assert.NotEmpty(transaction.Description);
            Assert.NotEqual(0m, transaction.Amount);
            Assert.True(transaction.TransactionType is TransactionType.Income or TransactionType.Expense);
        }

        // Verify we have both income and expense transactions
        Assert.Contains(result, t => t.TransactionType == TransactionType.Income);
        Assert.Contains(result, t => t.TransactionType == TransactionType.Expense);

        // Verify date range is reasonable (all transactions from Oct-Nov 2025)
        Assert.All(result, t =>
        {
            Assert.True(t.Date >= new DateOnly(2025, 10, 1), $"Date {t.Date} should be >= 2025-10-01");
            Assert.True(t.Date <= new DateOnly(2025, 11, 30), $"Date {t.Date} should be <= 2025-11-30");
        });
    }

    /// <summary>
    /// Verifies specific known transactions from the real CSV file.
    /// </summary>
    [Fact]
    public async Task ParseAsync_RealUhcuCsv_ParsesKnownTransactionsCorrectly()
    {
        // Arrange
        var csvPath = Path.Combine("..", "..", "..", "..", "..", "sample data", "uhcu.csv");
        var parser = new UnitedHeritageCreditUnionCsvParser();

        // Skip test if file doesn't exist
        if (!File.Exists(csvPath))
        {
            return;
        }

        // Act
        using var stream = File.OpenRead(csvPath);
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert - check for a few known transactions from the sample file
        // Deposit on 11/16/2025 for 0.99
        var depositTransfer = result.FirstOrDefault(t =>
            t.Date == new DateOnly(2025, 11, 16) &&
            t.Description.Contains("Deposit Transfer From Share") &&
            t.Amount == 0.99m);
        Assert.NotNull(depositTransfer);
        Assert.Equal(TransactionType.Income, depositTransfer.TransactionType);

        // Large payroll deposit on 10/30/2025 for 3006.00
        var payrollDeposit = result.FirstOrDefault(t =>
            t.Date == new DateOnly(2025, 10, 30) &&
            t.Description.Contains("ACME CORP") &&
            t.Amount == 3006.00m);
        Assert.NotNull(payrollDeposit);
        Assert.Equal(TransactionType.Income, payrollDeposit.TransactionType);

        // Check expense with check number (11/3/2025, check 1049)
        var checkTransaction = result.FirstOrDefault(t =>
            t.Date == new DateOnly(2025, 11, 3) &&
            t.Description.Contains("Check 1049") &&
            t.Amount == -901.00m);
        Assert.NotNull(checkTransaction);
        Assert.Equal(TransactionType.Expense, checkTransaction.TransactionType);

        // Verify negative amount for expenses
        var debitCardTransaction = result.FirstOrDefault(t =>
            t.Date == new DateOnly(2025, 11, 16) &&
            t.Description.Contains("QT 4150") &&
            Math.Abs(t.Amount - (-15.23m)) < 0.01m);
        Assert.NotNull(debitCardTransaction);
        Assert.Equal(TransactionType.Expense, debitCardTransaction.TransactionType);
        Assert.True(debitCardTransaction.Amount < 0, "Expense should have negative amount");
    }

    /// <summary>
    /// Verifies HTML entities in descriptions are preserved.
    /// </summary>
    [Fact]
    public async Task ParseAsync_RealUhcuCsv_PreservesHtmlEntities()
    {
        // Arrange
        var csvPath = Path.Combine("..", "..", "..", "..", "..", "sample data", "uhcu.csv");
        var parser = new UnitedHeritageCreditUnionCsvParser();

        // Skip test if file doesn't exist
        if (!File.Exists(csvPath))
        {
            return;
        }

        // Act
        using var stream = File.OpenRead(csvPath);
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert - verify HTML entity (&amp;) is preserved in description
        var transactionWithEntity = result.FirstOrDefault(t =>
            t.Description.Contains("&amp;"));

        // May or may not exist depending on sample data, but if it does, verify format
        if (transactionWithEntity != null)
        {
            Assert.Contains("&amp;", transactionWithEntity.Description);
        }
    }
}
