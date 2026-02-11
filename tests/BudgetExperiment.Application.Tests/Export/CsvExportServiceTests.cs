// <copyright file="CsvExportServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text;

using BudgetExperiment.Application.Export;

using Shouldly;

namespace BudgetExperiment.Application.Tests.Export;

/// <summary>
/// Unit tests for <see cref="CsvExportService"/>.
/// </summary>
public class CsvExportServiceTests
{
    [Fact]
    public async Task ExportTableAsync_Writes_Header_And_Rows()
    {
        // Arrange
        var table = new ExportTable(
            "Test Report",
            ["Name", "Amount"],
            [
                ["Coffee", "3.50"],
                ["Lunch", "12.00"],
            ]);

        var service = new CsvExportService();

        // Act
        var bytes = await service.ExportTableAsync(table, CancellationToken.None);
        var csv = Encoding.UTF8.GetString(bytes);

        // Assert
        csv.ShouldContain("Name,Amount");
        csv.ShouldContain("Coffee,3.50");
        csv.ShouldContain("Lunch,12.00");
    }

    [Fact]
    public async Task ExportTableAsync_Escapes_Values_With_Commas_And_Quotes()
    {
        // Arrange
        var table = new ExportTable(
            "Test Report",
            ["Name", "Note"],
            [
                ["Grocery", "Contains, comma"],
                ["Quote", "He said \"Hello\""],
            ]);

        var service = new CsvExportService();

        // Act
        var bytes = await service.ExportTableAsync(table, CancellationToken.None);
        var csv = Encoding.UTF8.GetString(bytes);

        // Assert
        csv.ShouldContain("\"Contains, comma\"");
        csv.ShouldContain("\"He said \"\"Hello\"\"\"");
    }
}
