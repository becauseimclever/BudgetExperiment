// <copyright file="CsvParseResultTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for the client-side CsvParseResult record.
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
        Assert.Equal(0, result.RowsSkipped);
    }

    [Fact]
    public void CreateSuccess_With_RowsSkipped_Sets_Property()
    {
        // Arrange
        var headers = new List<string> { "Date", "Amount" };
        var rows = new List<IReadOnlyList<string>>
        {
            new List<string> { "01/15/2026", "45.99" },
        };

        // Act
        var result = CsvParseResult.CreateSuccess(headers, rows, ',', hasHeaderRow: true, rowsSkipped: 5);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(5, result.RowsSkipped);
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
