// <copyright file="CsvImportControllerTests.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

using BudgetExperiment.Application.CsvImport.Models;

using Microsoft.AspNetCore.Mvc.Testing;

namespace BudgetExperiment.Api.Tests.CsvImport;

/// <summary>
/// Integration tests for CSV import controller.
/// </summary>
public sealed class CsvImportControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvImportControllerTests"/> class.
    /// </summary>
    /// <param name="factory">Web application factory.</param>
    public CsvImportControllerTests(WebApplicationFactory<Program> factory)
    {
        this._factory = factory;
    }

    /// <summary>
    /// POST /api/v1/csv-import with valid BofA CSV returns 200 with results.
    /// </summary>
    [Fact]
    public async Task ImportCsv_ValidBofAFile_Returns200WithResults()
    {
        // Arrange
        var client = this._factory.CreateClient();
        var csv = @"Date,Description,Amount,Running Bal.
10/01/2025,""Income Transaction"",""100.00"",""100.00""
10/02/2025,""Expense Transaction"",""-50.00"",""50.00""";

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/csv");
        content.Add(fileContent, "file", "transactions.csv");
        content.Add(new StringContent("BankOfAmerica"), "bankType");

        // Act
        var response = await client.PostAsync("/api/v1/csv-import", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.SuccessfulImports);
        Assert.Equal(0, result.FailedImports);
    }

    /// <summary>
    /// POST /api/v1/csv-import with invalid bank type returns 400.
    /// </summary>
    [Fact]
    public async Task ImportCsv_InvalidBankType_Returns400()
    {
        // Arrange
        var client = this._factory.CreateClient();
        var csv = @"Date,Description,Amount,Running Bal.";

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/csv");
        content.Add(fileContent, "file", "transactions.csv");
        content.Add(new StringContent("InvalidBank"), "bankType");

        // Act
        var response = await client.PostAsync("/api/v1/csv-import", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/csv-import with non-CSV file returns 400.
    /// </summary>
    [Fact]
    public async Task ImportCsv_NonCsvFile_Returns400()
    {
        // Arrange
        var client = this._factory.CreateClient();
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("not a csv"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(fileContent, "file", "document.txt");
        content.Add(new StringContent("BankOfAmerica"), "bankType");

        // Act
        var response = await client.PostAsync("/api/v1/csv-import", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/csv-import with empty file returns 200 with zero imports.
    /// </summary>
    [Fact]
    public async Task ImportCsv_EmptyFile_Returns200WithZeroImports()
    {
        // Arrange
        var client = this._factory.CreateClient();
        var csv = @"Date,Description,Amount,Running Bal.";

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/csv");
        content.Add(fileContent, "file", "empty.csv");
        content.Add(new StringContent("BankOfAmerica"), "bankType");

        // Act
        var response = await client.PostAsync("/api/v1/csv-import", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResult>();
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.SuccessfulImports);
    }

    /// <summary>
    /// POST /api/v1/csv-import with malformed CSV returns 400 or 422.
    /// </summary>
    [Fact]
    public async Task ImportCsv_MalformedCsv_Returns400Or422()
    {
        // Arrange
        var client = this._factory.CreateClient();
        var csv = @"Date,Description,Amount,Running Bal.
99/99/9999,""Invalid Date"",""50.00"",""150.00""";

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/csv");
        content.Add(fileContent, "file", "malformed.csv");
        content.Add(new StringContent("BankOfAmerica"), "bankType");

        // Act
        var response = await client.PostAsync("/api/v1/csv-import", content);

        // Assert - Accept either 400 or 422 for malformed data
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == (HttpStatusCode)422);
    }
}
