// <copyright file="UnitedHeritageCreditUnionCsvParserTests.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

using System.Text;

using BudgetExperiment.Application.CsvImport;
using BudgetExperiment.Application.CsvImport.Parsers;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Tests.CsvImport;

/// <summary>
/// Unit tests for United Heritage Credit Union CSV parser.
/// </summary>
public sealed class UnitedHeritageCreditUnionCsvParserTests
{
    /// <summary>
    /// Parses valid UHCU CSV and returns correct transactions.
    /// </summary>
    [Fact]
    public async Task ParseAsync_ValidUhcuCsv_ReturnsCorrectTransactions()
    {
        // Arrange
        var csv = @"Account Number,Post Date,Check,Description,Debit,Credit,Status,Balance
""5678S90"",11/16/2025,,""Deposit Transfer From Share 00/Funds Transfer via Mobile"",,0.99,Posted,855.35
""5678S90"",11/16/2025,,""Withdrawal DEBIT CARD/QT 4150 INSIDE ANYTOWN TX"",15.23,,Posted,854.36
""5678S90"",11/6/2025,,""Deposit ACH ACME CORP/TYPE: PAYROLL"",,500.00,Posted,1146.82";

        var parser = new UnitedHeritageCreditUnionCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);

        // First transaction - income (Credit)
        var first = result[0];
        Assert.Equal(new DateOnly(2025, 11, 16), first.Date);
        Assert.Equal("Deposit Transfer From Share 00/Funds Transfer via Mobile", first.Description);
        Assert.Equal(0.99m, first.Amount);
        Assert.Equal(TransactionType.Income, first.TransactionType);

        // Second transaction - expense (Debit)
        var second = result[1];
        Assert.Equal(new DateOnly(2025, 11, 16), second.Date);
        Assert.Equal("Withdrawal DEBIT CARD/QT 4150 INSIDE ANYTOWN TX", second.Description);
        Assert.Equal(-15.23m, second.Amount);
        Assert.Equal(TransactionType.Expense, second.TransactionType);

        // Third transaction - income (Credit)
        var third = result[2];
        Assert.Equal(new DateOnly(2025, 11, 6), third.Date);
        Assert.Equal("Deposit ACH ACME CORP/TYPE: PAYROLL", third.Description);
        Assert.Equal(500.00m, third.Amount);
        Assert.Equal(TransactionType.Income, third.TransactionType);
    }

    /// <summary>
    /// Parser correctly maps Debit/Credit columns to transaction types.
    /// </summary>
    [Fact]
    public async Task ParseAsync_DebitCreditColumns_CorrectlyMapsToTransactionType()
    {
        // Arrange
        var csv = @"Account Number,Post Date,Check,Description,Debit,Credit,Status,Balance
""5678S90"",11/15/2025,,""Purchase"",100.00,,Posted,500.00
""5678S90"",11/14/2025,,""Refund"",,50.00,Posted,600.00
""5678S90"",11/13/2025,,""Withdrawal"",25.00,,Posted,550.00";

        var parser = new UnitedHeritageCreditUnionCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(TransactionType.Expense, result[0].TransactionType);
        Assert.Equal(-100.00m, result[0].Amount);
        Assert.Equal(TransactionType.Income, result[1].TransactionType);
        Assert.Equal(50.00m, result[1].Amount);
        Assert.Equal(TransactionType.Expense, result[2].TransactionType);
        Assert.Equal(-25.00m, result[2].Amount);
    }

    /// <summary>
    /// Parser includes check number in description when present.
    /// </summary>
    [Fact]
    public async Task ParseAsync_WithCheckNumber_IncludesInDescription()
    {
        // Arrange
        var csv = @"Account Number,Post Date,Check,Description,Debit,Credit,Status,Balance
""5678S90"",11/3/2025,1049,""Check 1049 Tracer 943900000029324"",901.00,,Posted,674.82
""5678S90"",11/3/2025,1048,""Check 1048 Tracer 943900000029381"",900.00,,Posted,1575.82";

        var parser = new UnitedHeritageCreditUnionCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Check 1049 - Check 1049 Tracer 943900000029324", result[0].Description);
        Assert.Equal("Check 1048 - Check 1048 Tracer 943900000029381", result[1].Description);
    }

    /// <summary>
    /// Parser throws exception for invalid date format.
    /// </summary>
    [Fact]
    public async Task ParseAsync_InvalidDateFormat_ThrowsDomainException()
    {
        // Arrange
        var csv = @"Account Number,Post Date,Check,Description,Debit,Credit,Status,Balance
""5678S90"",99/99/9999,,""Test Transaction"",100.00,,Posted,100.00";

        var parser = new UnitedHeritageCreditUnionCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => parser.ParseAsync(stream, CancellationToken.None));
    }

    /// <summary>
    /// Parser throws exception for invalid debit amount format.
    /// </summary>
    [Fact]
    public async Task ParseAsync_InvalidDebitAmountFormat_ThrowsDomainException()
    {
        // Arrange
        var csv = @"Account Number,Post Date,Check,Description,Debit,Credit,Status,Balance
""5678S90"",11/15/2025,,""Test Transaction"",not-a-number,,Posted,100.00";

        var parser = new UnitedHeritageCreditUnionCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => parser.ParseAsync(stream, CancellationToken.None));
    }

    /// <summary>
    /// Parser throws exception for invalid credit amount format.
    /// </summary>
    [Fact]
    public async Task ParseAsync_InvalidCreditAmountFormat_ThrowsDomainException()
    {
        // Arrange
        var csv = @"Account Number,Post Date,Check,Description,Debit,Credit,Status,Balance
""5678S90"",11/15/2025,,""Test Transaction"",,not-a-number,Posted,100.00";

        var parser = new UnitedHeritageCreditUnionCsvParser();
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
        var csv = @"Account Number,Post Date,Check,Description,Debit,Credit,Status,Balance";

        var parser = new UnitedHeritageCreditUnionCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// Parser skips transactions with zero amount.
    /// </summary>
    [Fact]
    public async Task ParseAsync_ZeroAmountTransaction_SkipsTransaction()
    {
        // Arrange
        var csv = @"Account Number,Post Date,Check,Description,Debit,Credit,Status,Balance
""5678S90"",11/13/2025,,""Prenote"",0,,Posted,291.66
""5678S90"",11/13/2025,,""Deposit from EMPLOYER PAYROLL"",,664.50,Posted,956.16";

        var parser = new UnitedHeritageCreditUnionCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Deposit from EMPLOYER PAYROLL", result[0].Description);
    }

    /// <summary>
    /// Parser skips rows with both empty debit and credit.
    /// </summary>
    [Fact]
    public async Task ParseAsync_EmptyDebitAndCredit_SkipsTransaction()
    {
        // Arrange
        var csv = @"Account Number,Post Date,Check,Description,Debit,Credit,Status,Balance
""5678S90"",11/13/2025,,""Invalid Row"",,,,291.66
""5678S90"",11/13/2025,,""Valid Transaction"",10.00,,Posted,281.66";

        var parser = new UnitedHeritageCreditUnionCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Valid Transaction", result[0].Description);
    }

    /// <summary>
    /// Parser correctly handles quoted strings with commas in descriptions.
    /// </summary>
    [Fact]
    public async Task ParseAsync_HandlesQuotedStrings_CorrectlyParsesDescription()
    {
        // Arrange
        var csv = @"Account Number,Post Date,Check,Description,Debit,Credit,Status,Balance
""5678S90"",11/14/2025,,""Withdrawal DEBIT CARD/RESTAURANT, LLC ANYTOWN TX"",28.39,,Posted,415.65";

        var parser = new UnitedHeritageCreditUnionCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Withdrawal DEBIT CARD/RESTAURANT, LLC ANYTOWN TX", result[0].Description);
    }

    /// <summary>
    /// Parser correctly handles various date formats (single digit month/day).
    /// </summary>
    [Fact]
    public async Task ParseAsync_SingleDigitMonthDay_ParsesCorrectly()
    {
        // Arrange
        var csv = @"Account Number,Post Date,Check,Description,Debit,Credit,Status,Balance
""5678S90"",1/5/2025,,""Test Transaction"",100.00,,Posted,100.00
""5678S90"",10/25/2025,,""Test Transaction 2"",50.00,,Posted,50.00";

        var parser = new UnitedHeritageCreditUnionCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(new DateOnly(2025, 1, 5), result[0].Date);
        Assert.Equal(new DateOnly(2025, 10, 25), result[1].Date);
    }

    /// <summary>
    /// Parser handles transactions with HTML entities in descriptions.
    /// </summary>
    [Fact]
    public async Task ParseAsync_HtmlEntitiesInDescription_ParsesCorrectly()
    {
        // Arrange
        var csv = @"Account Number,Post Date,Check,Description,Debit,Credit,Status,Balance
""5678S90"",11/11/2025,,""Withdrawal DEBIT CARD/ROSAS CAFE &amp; TORTILLA F ANYTOWN TX"",34.37,,Posted,898.03";

        var parser = new UnitedHeritageCreditUnionCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Withdrawal DEBIT CARD/ROSAS CAFE &amp; TORTILLA F ANYTOWN TX", result[0].Description);
    }

    /// <summary>
    /// Parser correctly identifies UnitedHeritageCreditUnion as its bank type.
    /// </summary>
    [Fact]
    public void BankType_Returns_UnitedHeritageCreditUnion()
    {
        // Arrange & Act
        var parser = new UnitedHeritageCreditUnionCsvParser();

        // Assert
        Assert.Equal(BankType.UnitedHeritageCreditUnion, parser.BankType);
    }

    /// <summary>
    /// Parser handles amounts with thousands separators.
    /// </summary>
    [Fact]
    public async Task ParseAsync_AmountsWithThousandsSeparators_ParsesCorrectly()
    {
        // Arrange
        var csv = @"Account Number,Post Date,Check,Description,Debit,Credit,Status,Balance
""5678S90"",10/30/2025,,""Deposit ACH ACME CORP"",,""3,006.00"",Posted,3216.53
""5678S90"",10/29/2025,,""Large Withdrawal"",""1,234.56"",,Posted,1981.97";

        var parser = new UnitedHeritageCreditUnionCsvParser();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await parser.ParseAsync(stream, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(3006.00m, result[0].Amount);
        Assert.Equal(-1234.56m, result[1].Amount);
    }
}
