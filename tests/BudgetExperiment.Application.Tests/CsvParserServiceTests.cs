// <copyright file="CsvParserServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text;
using BudgetExperiment.Application.Services;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for the CsvParserService.
/// </summary>
public class CsvParserServiceTests
{
    private readonly CsvParserService _sut = new();

    [Fact]
    public async Task ParseAsync_With_Standard_Csv_Returns_Success()
    {
        // Arrange
        var csv = "Date,Description,Amount\n01/15/2026,WALMART,45.99\n01/16/2026,AMAZON,29.99";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.Headers.Count);
        Assert.Equal("Date", result.Headers[0]);
        Assert.Equal("Description", result.Headers[1]);
        Assert.Equal("Amount", result.Headers[2]);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(',', result.DetectedDelimiter);
        Assert.True(result.HasHeaderRow);
    }

    [Fact]
    public async Task ParseAsync_Detects_Semicolon_Delimiter()
    {
        // Arrange
        var csv = "Date;Description;Amount\n01/15/2026;WALMART;45.99";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(';', result.DetectedDelimiter);
        Assert.Equal(3, result.Headers.Count);
    }

    [Fact]
    public async Task ParseAsync_Detects_Tab_Delimiter()
    {
        // Arrange
        var csv = "Date\tDescription\tAmount\n01/15/2026\tWALMART\t45.99";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Equal('\t', result.DetectedDelimiter);
        Assert.Equal(3, result.Headers.Count);
    }

    [Fact]
    public async Task ParseAsync_Handles_Quoted_Fields()
    {
        // Arrange
        var csv = "Date,Description,Amount\n01/15/2026,\"WALMART STORE, INC\",45.99";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Rows);
        Assert.Equal("WALMART STORE, INC", result.Rows[0][1]);
    }

    [Fact]
    public async Task ParseAsync_Handles_Quoted_Fields_With_Embedded_Quotes()
    {
        // Arrange
        var csv = "Date,Description,Amount\n01/15/2026,\"He said \"\"Hello\"\"\",45.99";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("He said \"Hello\"", result.Rows[0][1]);
    }

    [Fact]
    public async Task ParseAsync_Handles_Quoted_Fields_With_Newlines()
    {
        // Arrange
        var csv = "Date,Description,Amount\n01/15/2026,\"Line1\nLine2\",45.99";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Rows);
        Assert.Equal("Line1\nLine2", result.Rows[0][1]);
    }

    [Fact]
    public async Task ParseAsync_Handles_Empty_Fields()
    {
        // Arrange
        var csv = "Date,Description,Amount\n01/15/2026,,45.99";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Rows);
        Assert.Equal(string.Empty, result.Rows[0][1]);
    }

    [Fact]
    public async Task ParseAsync_Handles_Windows_Line_Endings()
    {
        // Arrange
        var csv = "Date,Description,Amount\r\n01/15/2026,WALMART,45.99\r\n01/16/2026,AMAZON,29.99";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Rows.Count);
    }

    [Fact]
    public async Task ParseAsync_Handles_Mac_Line_Endings()
    {
        // Arrange
        var csv = "Date,Description,Amount\r01/15/2026,WALMART,45.99\r01/16/2026,AMAZON,29.99";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Rows.Count);
    }

    [Fact]
    public async Task ParseAsync_Trims_Whitespace_From_Values()
    {
        // Arrange
        var csv = "Date,Description,Amount\n 01/15/2026 , WALMART , 45.99 ";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("01/15/2026", result.Rows[0][0]);
        Assert.Equal("WALMART", result.Rows[0][1]);
        Assert.Equal("45.99", result.Rows[0][2]);
    }

    [Fact]
    public async Task ParseAsync_Trims_Whitespace_From_Headers()
    {
        // Arrange
        var csv = " Date , Description , Amount \n01/15/2026,WALMART,45.99";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Date", result.Headers[0]);
        Assert.Equal("Description", result.Headers[1]);
        Assert.Equal("Amount", result.Headers[2]);
    }

    [Fact]
    public async Task ParseAsync_Handles_Empty_Lines()
    {
        // Arrange
        var csv = "Date,Description,Amount\n\n01/15/2026,WALMART,45.99\n\n01/16/2026,AMAZON,29.99\n";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Rows.Count);
    }

    [Fact]
    public async Task ParseAsync_Empty_File_Returns_Failure()
    {
        // Arrange
        var csv = "";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("empty", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ParseAsync_Only_Header_Returns_Success_With_Zero_Rows()
    {
        // Arrange
        var csv = "Date,Description,Amount";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.Headers.Count);
        Assert.Empty(result.Rows);
        Assert.Equal(0, result.RowCount);
    }

    [Fact]
    public async Task ParseAsync_Handles_Varying_Column_Counts()
    {
        // Arrange - Some rows have fewer columns
        var csv = "Date,Description,Amount,Balance\n01/15/2026,WALMART,45.99\n01/16/2026,AMAZON,29.99,1000.00";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(4, result.Headers.Count);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(3, result.Rows[0].Count); // First row has 3 columns
        Assert.Equal(4, result.Rows[1].Count); // Second row has 4 columns
    }

    [Fact]
    public async Task ParseAsync_European_Format_With_Semicolon_And_Decimal_Comma()
    {
        // Arrange - European format: semicolon delimiter, comma decimal
        var csv = "Date;Description;Amount\n15/01/2026;WALMART;45,99\n16/01/2026;AMAZON;29,99";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(';', result.DetectedDelimiter);
        Assert.Equal("45,99", result.Rows[0][2]); // Amount kept as string with comma
    }

    [Fact]
    public async Task ParseAsync_Handles_BOM()
    {
        // Arrange - UTF-8 BOM
        var csv = "\uFEFFDate,Description,Amount\n01/15/2026,WALMART,45.99";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Date", result.Headers[0]); // BOM should be stripped
    }

    [Fact]
    public async Task ParseAsync_Large_File_Parses_Successfully()
    {
        // Arrange - Generate 1000 rows
        var sb = new StringBuilder();
        sb.AppendLine("Date,Description,Amount");
        for (int i = 0; i < 1000; i++)
        {
            sb.AppendLine($"01/{(i % 28) + 1:00}/2026,Transaction {i},{i * 10.50m:F2}");
        }

        using var stream = CreateStream(sb.ToString());

        // Act
        var result = await _sut.ParseAsync(stream, "large.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1000, result.Rows.Count);
        Assert.Equal(1000, result.RowCount);
    }

    [Fact]
    public async Task ParseAsync_RowCount_Matches_Rows_Count()
    {
        // Arrange
        var csv = "Date,Description,Amount\n01/15/2026,WALMART,45.99\n01/16/2026,AMAZON,29.99\n01/17/2026,TARGET,15.00";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.RowCount);
        Assert.Equal(result.Rows.Count, result.RowCount);
    }

    [Fact]
    public async Task ParseAsync_Respects_Cancellation()
    {
        // Arrange
        var csv = "Date,Description,Amount\n01/15/2026,WALMART,45.99";
        using var stream = CreateStream(csv);
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _sut.ParseAsync(stream, "test.csv", cts.Token));
    }

    [Fact]
    public async Task ParseAsync_Handles_Pipe_Delimiter()
    {
        // Arrange
        var csv = "Date|Description|Amount\n01/15/2026|WALMART|45.99";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Equal('|', result.DetectedDelimiter);
        Assert.Equal(3, result.Headers.Count);
    }

    [Fact]
    public async Task ParseAsync_Single_Column_File()
    {
        // Arrange
        var csv = "Description\nWALMART\nAMAZON\nTARGET";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Headers);
        Assert.Equal("Description", result.Headers[0]);
        Assert.Equal(3, result.Rows.Count);
    }

    [Fact]
    public async Task ParseAsync_Negative_Amounts_Preserved()
    {
        // Arrange
        var csv = "Date,Description,Amount\n01/15/2026,WALMART,-45.99\n01/16/2026,Refund,+29.99";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("-45.99", result.Rows[0][2]);
        Assert.Equal("+29.99", result.Rows[1][2]);
    }

    [Fact]
    public async Task ParseAsync_Currency_Symbols_Preserved()
    {
        // Arrange
        var csv = "Date,Description,Amount\n01/15/2026,WALMART,$45.99\n01/16/2026,AMAZON,€29.99";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("$45.99", result.Rows[0][2]);
        Assert.Equal("€29.99", result.Rows[1][2]);
    }

    [Fact]
    public async Task ParseAsync_Parentheses_For_Negatives_Preserved()
    {
        // Arrange
        var csv = "Date,Description,Amount\n01/15/2026,WALMART,(45.99)";
        using var stream = CreateStream(csv);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("(45.99)", result.Rows[0][2]);
    }

    private static MemoryStream CreateStream(string content)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }
}

/// <summary>
/// Unit tests for the CsvParseResult record.
/// </summary>
public class CsvParseResultTests
{
    [Fact]
    public void CreateSuccess_Returns_Successful_Result()
    {
        // Arrange
        var headers = new List<string> { "Date", "Description", "Amount" };
        var rows = new List<IReadOnlyList<string>>
        {
            new List<string> { "01/15/2026", "WALMART", "45.99" },
        };

        // Act
        var result = CsvParseResult.CreateSuccess(headers, rows, ',', hasHeaderRow: true);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(headers, result.Headers);
        Assert.Equal(rows, result.Rows);
        Assert.Equal(',', result.DetectedDelimiter);
        Assert.True(result.HasHeaderRow);
        Assert.Equal(1, result.RowCount);
    }

    [Fact]
    public void CreateFailure_Returns_Failed_Result()
    {
        // Act
        var result = CsvParseResult.CreateFailure("File is empty");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("File is empty", result.ErrorMessage);
        Assert.Empty(result.Headers);
        Assert.Empty(result.Rows);
        Assert.Equal(0, result.RowCount);
    }
}
