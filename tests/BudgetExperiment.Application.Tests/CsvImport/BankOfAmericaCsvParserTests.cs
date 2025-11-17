// <copyright file="BankOfAmericaCsvParserTests.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

using System.Text;

using BudgetExperiment.Application.CsvImport;
using BudgetExperiment.Application.CsvImport.Parsers;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Tests.CsvImport;

/// <summary>
/// Unit tests for Bank of America CSV parser.
/// </summary>
public sealed class BankOfAmericaCsvParserTests
{
    /// <summary>
    /// Parses valid BofA CSV and returns correct transactions.
    /// </summary>
    [Fact]
    public async Task ParseAsync_ValidBofACsv_ReturnsCorrectTransactions()
    {
        // Arrange
        var csv = @"Description,,Summary Amt.
Beginning balance as of 10/01/2025,,""357.05""

Date,Description,Amount,Running Bal.
10/01/2025,Beginning balance as of 10/01/2025,,""357.05""
10/01/2025,""Zelle payment from John Smith Conf# AB8KL2MXC"",""100.00"",""457.05""
10/02/2025,""AMAZON MKTPL*AB3CD5EF0 10/01 PURCHASE Amzn.com/bill WA"",""-30.59"",""410.92""
10/03/2025,""UPSTART NETWORK DES:JEFFERIES ID:9482156"",""-300.00"",""110.92""";

        var parser = new BankOfAmericaCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count); // 3 transactions (excluding beginning balance row)

        // First transaction - income
        var first = result[0];
        Assert.Equal(new DateOnly(2025, 10, 1), first.Date);
        Assert.Equal("Zelle payment from John Smith Conf# AB8KL2MXC", first.Description);
        Assert.Equal(100.00m, first.Amount);
        Assert.Equal(TransactionType.Income, first.TransactionType);

        // Second transaction - expense
        var second = result[1];
        Assert.Equal(new DateOnly(2025, 10, 2), second.Date);
        Assert.Equal("AMAZON MKTPL*AB3CD5EF0 10/01 PURCHASE Amzn.com/bill WA", second.Description);
        Assert.Equal(-30.59m, second.Amount);
        Assert.Equal(TransactionType.Expense, second.TransactionType);

        // Third transaction - expense
        var third = result[2];
        Assert.Equal(new DateOnly(2025, 10, 3), third.Date);
        Assert.Equal("UPSTART NETWORK DES:JEFFERIES ID:9482156", third.Description);
        Assert.Equal(-300.00m, third.Amount);
        Assert.Equal(TransactionType.Expense, third.TransactionType);
    }

    /// <summary>
    /// Parser correctly identifies income and expense types based on amount sign.
    /// </summary>
    [Fact]
    public async Task ParseAsync_MixedIncomeAndExpenses_CorrectlyMapsTransactionTypes()
    {
        // Arrange
        var csv = @"Date,Description,Amount,Running Bal.
10/01/2025,""PAYCHECK DEPOSIT"",""2500.00"",""2500.00""
10/02/2025,""GROCERY STORE"",""-50.00"",""2450.00""
10/03/2025,""REFUND"",""25.00"",""2475.00""";

        var parser = new BankOfAmericaCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(TransactionType.Income, result[0].TransactionType);
        Assert.True(result[0].Amount > 0);
        Assert.Equal(TransactionType.Expense, result[1].TransactionType);
        Assert.True(result[1].Amount < 0);
        Assert.Equal(TransactionType.Income, result[2].TransactionType);
        Assert.True(result[2].Amount > 0);
    }

    /// <summary>
    /// Parser throws exception for invalid date format.
    /// </summary>
    [Fact]
    public async Task ParseAsync_InvalidDateFormat_ThrowsDomainException()
    {
        // Arrange
        var csv = @"Date,Description,Amount,Running Bal.
13/45/2025,""TEST TRANSACTION"",""100.00"",""100.00""";

        var parser = new BankOfAmericaCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => parser.ParseAsync(stream, CancellationToken.None));
    }

    /// <summary>
    /// Parser throws exception for invalid amount format.
    /// </summary>
    [Fact]
    public async Task ParseAsync_InvalidAmountFormat_ThrowsDomainException()
    {
        // Arrange
        var csv = @"Date,Description,Amount,Running Bal.
10/01/2025,""TEST TRANSACTION"",""not-a-number"",""100.00""";

        var parser = new BankOfAmericaCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => parser.ParseAsync(stream, CancellationToken.None));
    }

    /// <summary>
    /// Parser returns empty list for file with only headers.
    /// </summary>
    [Fact]
    public async Task ParseAsync_EmptyFile_ReturnsEmptyList()
    {
        // Arrange
        var csv = @"Date,Description,Amount,Running Bal.";

        var parser = new BankOfAmericaCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// Parser skips rows with empty amounts (like beginning balance).
    /// </summary>
    [Fact]
    public async Task ParseAsync_SkipsRowsWithEmptyAmount()
    {
        // Arrange
        var csv = @"Date,Description,Amount,Running Bal.
10/01/2025,""Beginning balance as of 10/01/2025"",,""357.05""
10/01/2025,""Zelle payment from John Smith"",""100.00"",""457.05""";

        var parser = new BankOfAmericaCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Zelle payment from John Smith", result[0].Description);
    }

    /// <summary>
    /// Parser correctly handles quoted strings with commas in descriptions.
    /// </summary>
    [Fact]
    public async Task ParseAsync_HandlesQuotedStrings_CorrectlyParsesDescription()
    {
        // Arrange
        var csv = @"Date,Description,Amount,Running Bal.
10/01/2025,""STORE NAME, INC 10/01 PURCHASE"",""100.00"",""100.00""";

        var parser = new BankOfAmericaCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("STORE NAME, INC 10/01 PURCHASE", result[0].Description);
    }

    /// <summary>
    /// Parser correctly identifies BankOfAmerica as its bank type.
    /// </summary>
    [Fact]
    public void BankType_Returns_BankOfAmerica()
    {
        // Arrange & Act
        var parser = new BankOfAmericaCsvParser();

        // Assert
        Assert.Equal(BankType.BankOfAmerica, parser.BankType);
    }
}
