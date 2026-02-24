// <copyright file="ImportPreviewValidationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using BudgetExperiment.Application.Import;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for import preview endpoint validation (security hardening).
/// Verifies that oversized preview requests are rejected with appropriate
/// HTTP status codes and ProblemDetails responses.
/// </summary>
public sealed class ImportPreviewValidationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportPreviewValidationTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public ImportPreviewValidationTests(CustomWebApplicationFactory factory)
    {
        this._client = factory.CreateApiClient();
    }

    /// <summary>
    /// POST /api/v1/import/preview with more than 10,000 rows returns 400.
    /// </summary>
    [Fact]
    public async Task Preview_WithTooManyRows_Returns_400()
    {
        // Arrange
        var accountId = await this.CreateTestAccountAsync();
        var rows = Enumerable.Range(0, ImportValidationConstants.MaxPreviewRows + 1)
            .Select(i => (IReadOnlyList<string>)new List<string> { "01/15/2026", $"Transaction {i}", "-10.00" })
            .ToList();

        var request = new ImportPreviewRequest
        {
            AccountId = accountId,
            Rows = rows,
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/import/preview", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await this.ReadProblemDetailsAsync(response);
        Assert.Contains("10000", problem.Detail);
    }

    /// <summary>
    /// POST /api/v1/import/preview with exactly 10,000 rows returns 200.
    /// </summary>
    [Fact]
    public async Task Preview_WithExactlyMaxRows_Returns_200()
    {
        // Arrange
        var accountId = await this.CreateTestAccountAsync();
        var rows = Enumerable.Range(0, ImportValidationConstants.MaxPreviewRows)
            .Select(i => (IReadOnlyList<string>)new List<string> { "01/15/2026", $"Transaction {i}", "-10.00" })
            .ToList();

        var request = new ImportPreviewRequest
        {
            AccountId = accountId,
            Rows = rows,
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/import/preview", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/import/preview with a normal number of rows returns 200.
    /// </summary>
    [Fact]
    public async Task Preview_WithNormalRowCount_Returns_200()
    {
        // Arrange
        var accountId = await this.CreateTestAccountAsync();
        var request = new ImportPreviewRequest
        {
            AccountId = accountId,
            Rows =
            [
                (IReadOnlyList<string>)new List<string> { "01/15/2026", "Coffee Shop", "-5.50" },
                (IReadOnlyList<string>)new List<string> { "01/16/2026", "Grocery Store", "-42.00" },
            ],
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/import/preview", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// ProblemDetails responses from preview validation include traceId.
    /// </summary>
    [Fact]
    public async Task Preview_ValidationError_IncludesTraceId()
    {
        // Arrange
        var accountId = await this.CreateTestAccountAsync();
        var rows = Enumerable.Range(0, ImportValidationConstants.MaxPreviewRows + 1)
            .Select(i => (IReadOnlyList<string>)new List<string> { "01/15/2026", $"Tx {i}", "-1.00" })
            .ToList();

        var request = new ImportPreviewRequest
        {
            AccountId = accountId,
            Rows = rows,
            Mappings =
            [
                new ColumnMappingDto { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingDto { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingDto { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/import/preview", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("traceId", out _), "ProblemDetails should include traceId");
    }

    private async Task<Guid> CreateTestAccountAsync()
    {
        var accountRequest = new AccountCreateDto { Name = $"Preview Validation Test {Guid.NewGuid():N}", Type = "Checking" };
        var response = await this._client.PostAsJsonAsync("/api/v1/accounts", accountRequest);
        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<AccountDto>();
        return account!.Id;
    }

    private async Task<ProblemDetailsResponse> ReadProblemDetailsAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ProblemDetailsResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        })!;
    }

    /// <summary>
    /// Simplified ProblemDetails model for deserializing error responses.
    /// </summary>
    private sealed record ProblemDetailsResponse
    {
        /// <summary>Gets or sets the problem type URI.</summary>
        public string? Type { get; set; }

        /// <summary>Gets or sets the short title.</summary>
        public string? Title { get; set; }

        /// <summary>Gets or sets the HTTP status code.</summary>
        public int? Status { get; set; }

        /// <summary>Gets or sets the detailed error description.</summary>
        public string? Detail { get; set; }

        /// <summary>Gets or sets the trace ID for correlation.</summary>
        public string? TraceId { get; set; }
    }
}
