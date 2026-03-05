// <copyright file="ImportControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Api.Controllers;
using BudgetExperiment.Api.Models;
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
    /// GET /api/v1/import/mappings returns 200 with empty list when no mappings exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteBatch_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.DeleteAsync($"/api/v1/import/batches/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Full import flow: preview, execute, verify history (parsing is now client-side).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task FullImportFlow_Success()
    {
        // Arrange - create account
        var accountRequest = new AccountCreateDto { Name = "Flow Test Account", Type = "Checking" };
        var accountResponse = await this._client.PostAsJsonAsync("/api/v1/accounts", accountRequest);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();

        // Step 1: Preview (rows provided directly, simulating client-side parsing)
        var rows = new List<IReadOnlyList<string>>
        {
            new List<string> { "01/15/2025", "Coffee Shop", "-12.50" },
            new List<string> { "01/16/2025", "Salary", "1500.00" },
        };

        var previewRequest = new ImportPreviewRequest
        {
            AccountId = account!.Id,
            Mappings = new List<ColumnMappingDto>
            {
                new ColumnMappingDto { ColumnIndex = 0, ColumnHeader = "Date", TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, ColumnHeader = "Description", TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, ColumnHeader = "Amount", TargetField = ImportField.Amount },
            },
            Rows = rows,
            DateFormat = "MM/dd/yyyy",
        };

        var previewResponse = await this._client.PostAsJsonAsync("/api/v1/import/preview", previewRequest);
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var previewResult = await previewResponse.Content.ReadFromJsonAsync<ImportPreviewResult>();
        Assert.NotNull(previewResult);
        Assert.Equal(2, previewResult.ValidCount);

        // Step 2: Execute
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

        // Step 3: Verify History
        var historyResponse = await this._client.GetAsync("/api/v1/import/history");
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        var batches = await historyResponse.Content.ReadFromJsonAsync<List<ImportBatchDto>>();
        Assert.NotNull(batches);
        Assert.Contains(batches, b => b.Id == importResult.BatchId);
    }

    /// <summary>
    /// POST /api/v1/import/parse endpoint has been removed (Slice 4 — server-side parsing eliminated).
    /// Endpoint returns 404 (Not Found) or 405 (Method Not Allowed) depending on routing.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Parse_EndpointRemoved_ReturnsErrorStatus()
    {
        // Arrange
        using var content = new MultipartFormDataContent();

        // Act
        var response = await this._client.PostAsync("/api/v1/import/parse", content);

        // Assert — endpoint no longer exists (404 or 405 depending on routing)
        Assert.True(
            response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed,
            $"Expected 404 or 405 but got {response.StatusCode}");
    }

    /// <summary>
    /// GET /api/v1/import/mappings/{id} returns ETag header.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetMappingById_Returns_ETag_Header()
    {
        // Arrange
        var createRequest = new CreateImportMappingRequest
        {
            Name = "ETag Mapping",
            ColumnMappings = new List<ColumnMappingDto>
            {
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
            },
        };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/import/mappings", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ImportMappingDto>();

        // Act
        var response = await this._client.GetAsync($"/api/v1/import/mappings/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        Assert.False(string.IsNullOrEmpty(response.Headers.ETag.Tag));
    }

    /// <summary>
    /// PUT /api/v1/import/mappings/{id} with valid If-Match succeeds and returns new ETag.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateMapping_With_Valid_IfMatch_Succeeds()
    {
        // Arrange
        var createRequest = new CreateImportMappingRequest
        {
            Name = "IfMatch Valid Mapping",
            ColumnMappings = new List<ColumnMappingDto>
            {
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
            },
        };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/import/mappings", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ImportMappingDto>();

        var getResponse = await this._client.GetAsync($"/api/v1/import/mappings/{created!.Id}");
        var etag = getResponse.Headers.ETag;

        var updateRequest = new UpdateImportMappingRequest { Name = "Updated With ETag" };
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/import/mappings/{created.Id}")
        {
            Content = JsonContent.Create(updateRequest),
        };
        request.Headers.IfMatch.Add(etag!);

        // Act
        var response = await this._client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        var updated = await response.Content.ReadFromJsonAsync<ImportMappingDto>();
        Assert.Equal("Updated With ETag", updated!.Name);
    }

    /// <summary>
    /// PUT /api/v1/import/mappings/{id} with stale If-Match returns 409 Conflict.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateMapping_With_Stale_IfMatch_Returns_409()
    {
        // Arrange
        var createRequest = new CreateImportMappingRequest
        {
            Name = "Stale IfMatch Mapping",
            ColumnMappings = new List<ColumnMappingDto>
            {
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
            },
        };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/import/mappings", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ImportMappingDto>();

        var staleETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"99999999\"");
        var updateRequest = new UpdateImportMappingRequest { Name = "Should Fail" };
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/import/mappings/{created!.Id}")
        {
            Content = JsonContent.Create(updateRequest),
        };
        request.Headers.IfMatch.Add(staleETag);

        // Act
        var response = await this._client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>
    /// PUT /api/v1/import/mappings/{id} without If-Match still succeeds (backward compatible).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateMapping_Without_IfMatch_Succeeds_BackwardCompatible()
    {
        // Arrange
        var createRequest = new CreateImportMappingRequest
        {
            Name = "No IfMatch Mapping",
            ColumnMappings = new List<ColumnMappingDto>
            {
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
            },
        };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/import/mappings", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ImportMappingDto>();

        var updateRequest = new UpdateImportMappingRequest { Name = "Updated Without ETag" };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/import/mappings/{created!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ImportMappingDto>();
        Assert.Equal("Updated Without ETag", updated!.Name);
    }
}
