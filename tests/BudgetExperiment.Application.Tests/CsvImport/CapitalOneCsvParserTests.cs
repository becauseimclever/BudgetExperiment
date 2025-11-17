// <copyright file="CapitalOneCsvParserTests.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

using System.Text;

using BudgetExperiment.Application.CsvImport;
using BudgetExperiment.Application.CsvImport.Parsers;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Tests.CsvImport;

/// <summary>
/// Unit tests for Capital One CSV parser.
/// </summary>
public sealed class CapitalOneCsvParserTests
{
    /// <summary>
    /// Parses valid Capital One CSV and returns correct transactions.
    /// </summary>
    [Fact]
    public async Task ParseAsync_ValidCapitalOneCsv_ReturnsCorrectTransactions()
    {
        // Arrange
        var csv = @"Account Number,Transaction Description,Transaction Date,Transaction Type,Transaction Amount,Balance
1234,Debit Card Purchase - RESTAURANT A CITY ST,11/15/25,Debit,28.66,168.05
1234,Deposit from EMPLOYER PAYROLL,11/13/25,Credit,664.5,956.16
1234,Debit Card Purchase - GROCERY STORE CITY ST,11/12/25,Debit,58.35,291.66";

        var parser = new CapitalOneCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);

        // First transaction - expense (Debit)
        var first = result[0];
        Assert.Equal(new DateOnly(2025, 11, 15), first.Date);
        Assert.Equal("Debit Card Purchase - RESTAURANT A CITY ST", first.Description);
        Assert.Equal(28.66m, first.Amount);
        Assert.Equal(TransactionType.Expense, first.TransactionType);

        // Second transaction - income (Credit)
        var second = result[1];
        Assert.Equal(new DateOnly(2025, 11, 13), second.Date);
        Assert.Equal("Deposit from EMPLOYER PAYROLL", second.Description);
        Assert.Equal(664.5m, second.Amount);
        Assert.Equal(TransactionType.Income, second.TransactionType);

        // Third transaction - expense (Debit)
        var third = result[2];
        Assert.Equal(new DateOnly(2025, 11, 12), third.Date);
        Assert.Equal("Debit Card Purchase - GROCERY STORE CITY ST", third.Description);
        Assert.Equal(58.35m, third.Amount);
        Assert.Equal(TransactionType.Expense, third.TransactionType);
    }

    /// <summary>
    /// Parser correctly maps Debit/Credit column to transaction types.
    /// </summary>
    [Fact]
    public async Task ParseAsync_DebitCreditColumn_CorrectlyMapsToTransactionType()
    {
        // Arrange
        var csv = @"Account Number,Transaction Description,Transaction Date,Transaction Type,Transaction Amount,Balance
1234,Purchase,11/15/25,Debit,100.00,500.00
1234,Refund,11/14/25,Credit,50.00,600.00
1234,Withdrawal,11/13/25,Debit,25.00,550.00";

        var parser = new CapitalOneCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(TransactionType.Expense, result[0].TransactionType);
        Assert.Equal(TransactionType.Income, result[1].TransactionType);
        Assert.Equal(TransactionType.Expense, result[2].TransactionType);
    }

    /// <summary>
    /// Parser throws exception for invalid date format.
    /// </summary>
    [Fact]
    public async Task ParseAsync_InvalidDateFormat_ThrowsDomainException()
    {
        // Arrange
        var csv = @"Account Number,Transaction Description,Transaction Date,Transaction Type,Transaction Amount,Balance
1234,Test Transaction,99/99/99,Debit,100.00,100.00";

        var parser = new CapitalOneCsvParser();
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
        var csv = @"Account Number,Transaction Description,Transaction Date,Transaction Type,Transaction Amount,Balance
1234,Test Transaction,11/15/25,Debit,not-a-number,100.00";

        var parser = new CapitalOneCsvParser();
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
        var csv = @"Account Number,Transaction Description,Transaction Date,Transaction Type,Transaction Amount,Balance";

        var parser = new CapitalOneCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// Parser skips transactions with zero amount (like prenotes).
    /// </summary>
    [Fact]
    public async Task ParseAsync_ZeroAmountTransaction_SkipsTransaction()
    {
        // Arrange
        var csv = @"Account Number,Transaction Description,Transaction Date,Transaction Type,Transaction Amount,Balance
1234,Prenote INSURANCE COMPANY PAYMENT NOV 12,11/13/25,Debit,0,291.66
1234,Deposit from EMPLOYER PAYROLL,11/13/25,Credit,664.5,956.16";

        var parser = new CapitalOneCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Deposit from EMPLOYER PAYROLL", result[0].Description);
    }

    /// <summary>
    /// Parser correctly handles quoted strings with commas in descriptions.
    /// </summary>
    [Fact]
    public async Task ParseAsync_HandlesQuotedStrings_CorrectlyParsesDescription()
    {
        // Arrange
        var csv = @"Account Number,Transaction Description,Transaction Date,Transaction Type,Transaction Amount,Balance
1234,""Debit Card Purchase - COSTCO GAS CITY, ST US"",11/14/25,Debit,28.39,415.65";

        var parser = new CapitalOneCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Debit Card Purchase - COSTCO GAS CITY, ST US", result[0].Description);
    }

    /// <summary>
    /// Parser correctly handles two-digit years (follows .NET default interpretation).
    /// </summary>
    [Fact]
    public async Task ParseAsync_TwoDigitYear_ParsesCorrectly()
    {
        // Arrange
        var csv = @"Account Number,Transaction Description,Transaction Date,Transaction Type,Transaction Amount,Balance
1234,Test Transaction,11/15/25,Debit,100.00,100.00
1234,Test Transaction 2,01/01/99,Debit,50.00,50.00";

        var parser = new CapitalOneCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(new DateOnly(2025, 11, 15), result[0].Date);
        Assert.Equal(new DateOnly(1999, 1, 1), result[1].Date); // 99 maps to 1999 per .NET default
    }

    /// <summary>
    /// Parser throws exception for unrecognized transaction type.
    /// </summary>
    [Fact]
    public async Task ParseAsync_InvalidTransactionType_ThrowsDomainException()
    {
        // Arrange
        var csv = @"Account Number,Transaction Description,Transaction Date,Transaction Type,Transaction Amount,Balance
1234,Test Transaction,11/15/25,Unknown,100.00,100.00";

        var parser = new CapitalOneCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => parser.ParseAsync(stream, CancellationToken.None));
    }

    /// <summary>
    /// Parser correctly identifies CapitalOne as its bank type.
    /// </summary>
    [Fact]
    public void BankType_Returns_CapitalOne()
    {
        // Arrange & Act
        var parser = new CapitalOneCsvParser();

        // Assert
        Assert.Equal(BankType.CapitalOne, parser.BankType);
    }
}
