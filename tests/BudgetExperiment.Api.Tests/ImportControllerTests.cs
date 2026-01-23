// <copyright file="ImportControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using System.Text;

using BudgetExperiment.Api.Controllers;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Import API endpoints.
/// </summary>
public sealed class ImportControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public ImportControllerTests(CustomWebApplicationFactory factory)
    {
        this._client = factory.CreateApiClient();
    }

    /// <summary>
    /// POST /api/v1/import/parse with no file returns 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task Parse_WithoutFile_Returns_400()
    {
        // Arrange
        using var content = new MultipartFormDataContent();

        // Act
        var response = await this._client.PostAsync("/api/v1/import/parse", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/import/parse with valid CSV returns 200 with parsed data.
    /// </summary>
    [Fact]
    public async Task Parse_WithValidCsv_Returns_200()
    {
        // Arrange
        var csv = "Date,Description,Amount\n2025-01-15,Coffee Shop,12.50\n2025-01-16,Grocery Store,45.00";
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "transactions.csv");

        // Act
        var response = await this._client.PostAsync("/api/v1/import/parse", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CsvParseResultDto>();
        Assert.NotNull(result);
        Assert.Equal(3, result.Headers.Count);
        Assert.Contains("Date", result.Headers);
        Assert.Contains("Description", result.Headers);
        Assert.Contains("Amount", result.Headers);
        Assert.Equal(2, result.Rows.Count);
    }

    /// <summary>
    /// GET /api/v1/import/mappings returns 200 with empty list when no mappings exist.
    /// </summary>
    [Fact]
    public async Task GetMappings_Returns_200_WithEmptyList()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/import/mappings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var mappings = await response.Content.ReadFromJsonAsync<List<ImportMappingDto>>();
        Assert.NotNull(mappings);
    }

    /// <summary>
    /// GET /api/v1/import/mappings/{id} returns 404 when mapping not found.
    /// </summary>
    [Fact]
    public async Task GetMappingById_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/import/mappings/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/import/mappings creates a mapping and returns 201.
    /// </summary>
    [Fact]
    public async Task CreateMapping_Returns_201_WithCreatedMapping()
    {
        // Arrange
        var request = new CreateImportMappingRequest
        {
            Name = "Test Bank Mapping",
            DateFormat = "yyyy-MM-dd",
            ColumnMappings = new List<ColumnMappingDto>
            {
                new ColumnMappingDto { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, ColumnHeader = "Description", TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, ColumnHeader = "Amount", TargetField = ImportField.Amount },
            },
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/import/mappings", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ImportMappingDto>();
        Assert.NotNull(created);
        Assert.Equal("Test Bank Mapping", created.Name);
        Assert.NotEqual(Guid.Empty, created.Id);
    }

    /// <summary>
    /// PUT /api/v1/import/mappings/{id} updates mapping and returns 200.
    /// </summary>
    [Fact]
    public async Task UpdateMapping_Returns_200_WhenFound()
    {
        // Arrange - create first
        var createRequest = new CreateImportMappingRequest
        {
            Name = "Original Mapping",
            ColumnMappings = new List<ColumnMappingDto>
            {
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
            },
        };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/import/mappings", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ImportMappingDto>();

        var updateRequest = new UpdateImportMappingRequest
        {
            Name = "Updated Mapping",
            DateFormat = "MM/dd/yyyy",
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/import/mappings/{created!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ImportMappingDto>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Mapping", updated.Name);
    }

    /// <summary>
    /// PUT /api/v1/import/mappings/{id} returns 404 when mapping not found.
    /// </summary>
    [Fact]
    public async Task UpdateMapping_Returns_404_WhenNotFound()
    {
        // Arrange
        var updateRequest = new UpdateImportMappingRequest
        {
            Name = "Updated Mapping",
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/import/mappings/{Guid.NewGuid()}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// DELETE /api/v1/import/mappings/{id} returns 204 when found.
    /// </summary>
    [Fact]
    public async Task DeleteMapping_Returns_204_WhenFound()
    {
        // Arrange - create first
        var createRequest = new CreateImportMappingRequest
        {
            Name = "Mapping To Delete",
            ColumnMappings = new List<ColumnMappingDto>
            {
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
            },
        };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/import/mappings", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ImportMappingDto>();

        // Act
        var response = await this._client.DeleteAsync($"/api/v1/import/mappings/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>
    /// DELETE /api/v1/import/mappings/{id} returns 404 when not found.
    /// </summary>
    [Fact]
    public async Task DeleteMapping_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.DeleteAsync($"/api/v1/import/mappings/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/import/mappings/suggest returns 204 when no matching mapping found.
    /// </summary>
    [Fact]
    public async Task SuggestMapping_Returns_204_WhenNoMatch()
    {
        // Arrange
        var request = new SuggestMappingRequest
        {
            Headers = new List<string> { "UnknownColumn1", "UnknownColumn2" },
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/import/mappings/suggest", request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/import/preview with valid request returns 200.
    /// </summary>
    [Fact]
    public async Task Preview_WithValidRequest_Returns_200()
    {
        // Arrange - need an account first
        var accountRequest = new AccountCreateDto { Name = "Import Account", Type = "Checking" };
        var accountResponse = await this._client.PostAsJsonAsync("/api/v1/accounts", accountRequest);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();

        var request = new ImportPreviewRequest
        {
            AccountId = account!.Id,
            Mappings = new List<ColumnMappingDto>
            {
                new ColumnMappingDto { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, ColumnHeader = "Description", TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, ColumnHeader = "Amount", TargetField = ImportField.Amount },
            },
            Rows = new List<IReadOnlyList<string>>
            {
                new List<string> { "01/15/2025", "Coffee Shop", "12.50" },
            },
            DateFormat = "MM/dd/yyyy",
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/import/preview", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportPreviewResult>();
        Assert.NotNull(result);
    }

    /// <summary>
    /// GET /api/v1/import/history returns 200 with list.
    /// </summary>
    [Fact]
    public async Task GetHistory_Returns_200_WithList()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/import/history");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var batches = await response.Content.ReadFromJsonAsync<List<ImportBatchDto>>();
        Assert.NotNull(batches);
    }

    /// <summary>
    /// GET /api/v1/import/batches/{id} returns 404 when batch not found.
    /// </summary>
    [Fact]
    public async Task GetBatchById_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/import/batches/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// DELETE /api/v1/import/batches/{id} returns 404 when batch not found.
    /// </summary>
    [Fact]
    public async Task DeleteBatch_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.DeleteAsync($"/api/v1/import/batches/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Full import flow: parse CSV, preview, execute, verify history.
    /// </summary>
    [Fact]
    public async Task FullImportFlow_Success()
    {
        // Arrange - create account
        var accountRequest = new AccountCreateDto { Name = "Flow Test Account", Type = "Checking" };
        var accountResponse = await this._client.PostAsJsonAsync("/api/v1/accounts", accountRequest);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();

        // Step 1: Parse CSV
        var csv = "Date,Description,Amount\n01/15/2025,Coffee Shop,-12.50\n01/16/2025,Salary,1500.00";
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "test.csv");

        var parseResponse = await this._client.PostAsync("/api/v1/import/parse", content);
        Assert.Equal(HttpStatusCode.OK, parseResponse.StatusCode);
        var parseResult = await parseResponse.Content.ReadFromJsonAsync<CsvParseResultDto>();
        Assert.NotNull(parseResult);

        // Step 2: Preview
        var previewRequest = new ImportPreviewRequest
        {
            AccountId = account!.Id,
            Mappings = new List<ColumnMappingDto>
            {
                new ColumnMappingDto { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, ColumnHeader = "Description", TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, ColumnHeader = "Amount", TargetField = ImportField.Amount },
            },
            Rows = parseResult.Rows,
            DateFormat = "MM/dd/yyyy",
        };

        var previewResponse = await this._client.PostAsJsonAsync("/api/v1/import/preview", previewRequest);
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var previewResult = await previewResponse.Content.ReadFromJsonAsync<ImportPreviewResult>();
        Assert.NotNull(previewResult);
        Assert.Equal(2, previewResult.ValidCount);

        // Step 3: Execute
        var executeRequest = new ImportExecuteRequest
        {
            AccountId = account.Id,
            FileName = "test.csv",
            Transactions = previewResult.Rows
                .Where(r => r.Status == ImportRowStatus.Valid)
                .Select(r => new ImportTransactionData
                {
                    Date = r.Date ?? DateOnly.MinValue,
                    Description = r.Description ?? string.Empty,
                    Amount = r.Amount ?? 0,
                    CategoryId = r.CategoryId,
                    CategorySource = r.CategorySource,
                }).ToList(),
        };

        var executeResponse = await this._client.PostAsJsonAsync("/api/v1/import/execute", executeRequest);
        Assert.Equal(HttpStatusCode.Created, executeResponse.StatusCode);
        var importResult = await executeResponse.Content.ReadFromJsonAsync<ImportResult>();
        Assert.NotNull(importResult);
        Assert.Equal(2, importResult.ImportedCount);

        // Step 4: Verify History
        var historyResponse = await this._client.GetAsync("/api/v1/import/history");
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        var batches = await historyResponse.Content.ReadFromJsonAsync<List<ImportBatchDto>>();
        Assert.NotNull(batches);
        Assert.Contains(batches, b => b.Id == importResult.BatchId);
    }

    /// <summary>
    /// POST /api/v1/import/parse accepts rowsToSkip query parameter and skips metadata rows.
    /// </summary>
    [Fact]
    public async Task Parse_WithRowsToSkip_SkipsMetadataRows()
    {
        // Arrange - CSV with 2 metadata rows before actual header
        var csv = """
            Bank of America Statement
            Account: ****1234
            Date,Description,Amount
            2025-01-15,Coffee Shop,12.50
            2025-01-16,Grocery Store,45.00
            """;
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "bank_statement.csv");

        // Act - skip 2 metadata rows
        var response = await this._client.PostAsync("/api/v1/import/parse?rowsToSkip=2", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CsvParseResultDto>();
        Assert.NotNull(result);
        Assert.Equal(3, result.Headers.Count);
        Assert.Contains("Date", result.Headers);
        Assert.Contains("Description", result.Headers);
        Assert.Contains("Amount", result.Headers);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(2, result.RowsSkipped);
    }

    /// <summary>
    /// POST /api/v1/import/parse with zero rowsToSkip behaves like default.
    /// </summary>
    [Fact]
    public async Task Parse_WithZeroRowsToSkip_ReturnsAllRows()
    {
        // Arrange
        var csv = "Date,Description,Amount\n2025-01-15,Coffee Shop,12.50";
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "transactions.csv");

        // Act
        var response = await this._client.PostAsync("/api/v1/import/parse?rowsToSkip=0", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CsvParseResultDto>();
        Assert.NotNull(result);
        Assert.Single(result.Rows);
        Assert.Equal(0, result.RowsSkipped);
    }

    /// <summary>
    /// POST /api/v1/import/parse with negative rowsToSkip returns 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task Parse_WithNegativeRowsToSkip_Returns400()
    {
        // Arrange
        var csv = "Date,Description,Amount\n2025-01-15,Coffee Shop,12.50";
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "transactions.csv");

        // Act
        var response = await this._client.PostAsync("/api/v1/import/parse?rowsToSkip=-1", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/import/parse with rowsToSkip exceeding file rows returns parse failure.
    /// </summary>
    [Fact]
    public async Task Parse_WithRowsToSkipExceedingFileRows_Returns400()
    {
        // Arrange - only 2 lines total
        var csv = "Date,Description,Amount\n2025-01-15,Coffee,12.50";
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "transactions.csv");

        // Act - try to skip more than available
        var response = await this._client.PostAsync("/api/v1/import/parse?rowsToSkip=10", content);

        // Assert - parser returns failure, controller returns 400
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/import/parse RowsSkipped property reflects actual rows skipped.
    /// </summary>
    [Fact]
    public async Task Parse_RowsSkipped_ReflectsActualSkipCount()
    {
        // Arrange - CSV with metadata including an empty line
        var csv = """
            Bank Statement
            
            Date,Description,Amount
            2025-01-15,Coffee,12.50
            """;
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "bank.csv");

        // Act - skip 2 rows (metadata + empty line)
        var response = await this._client.PostAsync("/api/v1/import/parse?rowsToSkip=2", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CsvParseResultDto>();
        Assert.NotNull(result);
        Assert.Equal(2, result.RowsSkipped);
        Assert.Contains("Date", result.Headers);
    }
}
