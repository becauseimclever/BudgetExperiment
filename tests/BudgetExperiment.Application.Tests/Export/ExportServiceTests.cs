// <copyright file="ExportServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Export;

using Shouldly;

namespace BudgetExperiment.Application.Tests.Export;

/// <summary>
/// Unit tests for <see cref="ExportService"/>.
/// </summary>
public class ExportServiceTests
{
    [Fact]
    public async Task ExportTableAsync_Uses_Matching_Formatter()
    {
        // Arrange
        var table = new ExportTable("Report", ["Name"], [["A"]]);
        var formatter = new FakeExportFormatter(ExportFormat.Csv, "text/csv", "csv");
        var service = new ExportService([formatter]);

        // Act
        var result = await service.ExportTableAsync(table, ExportFormat.Csv, "report", CancellationToken.None);

        // Assert
        result.Content.ShouldBe(formatter.Payload);
        result.ContentType.ShouldBe("text/csv");
        result.FileName.ShouldBe("report.csv");
    }

    [Fact]
    public async Task ExportTableAsync_Throws_When_Format_Not_Registered()
    {
        // Arrange
        var table = new ExportTable("Report", ["Name"], [["A"]]);
        var service = new ExportService([]);

        // Act
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            service.ExportTableAsync(table, ExportFormat.Csv, "report", CancellationToken.None));

        // Assert
        exception.Message.ShouldContain("formatter");
    }

    private sealed class FakeExportFormatter : IExportFormatter
    {
        public FakeExportFormatter(ExportFormat format, string contentType, string extension)
        {
            this.Format = format;
            this.ContentType = contentType;
            this.FileExtension = extension;
        }

        public ExportFormat Format { get; }

        public string ContentType { get; }

        public string FileExtension { get; }

        public byte[] Payload { get; } = [1, 2, 3];

        public Task<byte[]> ExportTableAsync(ExportTable table, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.Payload);
        }
    }
}
