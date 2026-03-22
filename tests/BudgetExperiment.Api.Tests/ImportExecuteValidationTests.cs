// <copyright file="ImportExecuteValidationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using BudgetExperiment.Application.Import;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for import execute endpoint validation (security hardening).
/// Verifies that oversized, malformed, or abusive import requests are rejected
/// with appropriate HTTP status codes and ProblemDetails responses.
/// </summary>
[Collection("ApiDb")]
public sealed class ImportExecuteValidationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportExecuteValidationTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public ImportExecuteValidationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateApiClient();
    }

    /// <summary>
    /// POST /api/v1/import/execute with more than 5000 transactions returns 400.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Execute_WithTooManyTransactions_Returns_400()
    {
        // Arrange
        var accountId = await this.CreateTestAccountAsync();
        var transactions = Enumerable.Range(0, ImportValidationConstants.MaxTransactionsPerImport + 1)
            .Select(i => new ImportTransactionData
            {
                Date = new DateOnly(2026, 1, 15),
                Description = $"Transaction {i}",
                Amount = -10m,
            })
            .ToList();

        var request = new ImportExecuteRequest
        {
            AccountId = accountId,
            FileName = "big.csv",
            Transactions = transactions,
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/import/execute", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await this.ReadProblemDetailsAsync(response);
        Assert.Contains("5000", problem.Detail);
    }

    /// <summary>
    /// POST /api/v1/import/execute with 0 transactions returns 400.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Execute_WithZeroTransactions_Returns_400()
    {
        // Arrange
        var accountId = await this.CreateTestAccountAsync();
        var request = new ImportExecuteRequest
        {
            AccountId = accountId,
            FileName = "empty.csv",
            Transactions = [],
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/import/execute", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await this.ReadProblemDetailsAsync(response);
        Assert.Contains("No transactions", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// POST /api/v1/import/execute with description exceeding 500 characters returns 422.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Execute_WithDescriptionTooLong_Returns_422()
    {
        // Arrange
        var accountId = await this.CreateTestAccountAsync();
        var request = new ImportExecuteRequest
        {
            AccountId = accountId,
            FileName = "test.csv",
            Transactions =
            [
                new ImportTransactionData
                {
                    Date = new DateOnly(2026, 1, 15),
                    Description = new string('A', ImportValidationConstants.MaxDescriptionLength + 1),
                    Amount = -50m,
                },
            ],
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/import/execute", request);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await this.ReadProblemDetailsAsync(response);
        Assert.Contains("Description", problem.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("row 1", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// POST /api/v1/import/execute with date too far in the future returns 422.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Execute_WithDateTooFarInFuture_Returns_422()
    {
        // Arrange
        var accountId = await this.CreateTestAccountAsync();
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(ImportValidationConstants.MaxFutureDateDays + 1);
        var request = new ImportExecuteRequest
        {
            AccountId = accountId,
            FileName = "test.csv",
            Transactions =
            [
                new ImportTransactionData
                {
                    Date = futureDate,
                    Description = "Future transaction",
                    Amount = -50m,
                },
            ],
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/import/execute", request);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await this.ReadProblemDetailsAsync(response);
        Assert.Contains("future", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// POST /api/v1/import/execute with amount exceeding maximum returns 422.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Execute_WithAmountOutOfRange_Returns_422()
    {
        // Arrange
        var accountId = await this.CreateTestAccountAsync();
        var request = new ImportExecuteRequest
        {
            AccountId = accountId,
            FileName = "test.csv",
            Transactions =
            [
                new ImportTransactionData
                {
                    Date = new DateOnly(2026, 1, 15),
                    Description = "Big purchase",
                    Amount = ImportValidationConstants.MaxAmountAbsoluteValue + 1m,
                },
            ],
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/import/execute", request);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await this.ReadProblemDetailsAsync(response);
        Assert.Contains("Amount out of range", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// POST /api/v1/import/execute with all valid data returns 201.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Execute_WithValidRequest_Returns_201()
    {
        // Arrange
        var accountId = await this.CreateTestAccountAsync();
        var request = new ImportExecuteRequest
        {
            AccountId = accountId,
            FileName = "test.csv",
            Transactions =
            [
                new ImportTransactionData
                {
                    Date = new DateOnly(2026, 1, 15),
                    Description = "Coffee Shop",
                    Amount = -12.50m,
                },
            ],
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/import/execute", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// ProblemDetails responses include traceId.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Execute_ValidationError_IncludesTraceId()
    {
        // Arrange
        var accountId = await this.CreateTestAccountAsync();
        var request = new ImportExecuteRequest
        {
            AccountId = accountId,
            FileName = "test.csv",
            Transactions = [],
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/import/execute", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("traceId", out _), "ProblemDetails should include traceId");
    }

    private async Task<Guid> CreateTestAccountAsync()
    {
        var accountRequest = new AccountCreateDto { Name = $"Validation Test {Guid.NewGuid():N}", Type = "Checking" };
        var response = await _client.PostAsJsonAsync("/api/v1/accounts", accountRequest);
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
        public string? Type
        {
            get; set;
        }

        /// <summary>Gets or sets the short title.</summary>
        public string? Title
        {
            get; set;
        }

        /// <summary>Gets or sets the HTTP status code.</summary>
        public int? Status
        {
            get; set;
        }

        /// <summary>Gets or sets the detailed error description.</summary>
        public string? Detail
        {
            get; set;
        }

        /// <summary>Gets or sets the trace ID for correlation.</summary>
        public string? TraceId
        {
            get; set;
        }
    }
}
