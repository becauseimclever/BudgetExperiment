// <copyright file="DataHealthControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Data Health API endpoints (Feature 125a).
/// </summary>
[Collection("ApiDb")]
public sealed class DataHealthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataHealthControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public DataHealthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
        _client = factory.CreateApiClient();
    }

    /// <summary>
    /// AC-125a-10: GET /api/v1/datahealth/report returns 200 with a DataHealthReportDto.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetReport_Returns_200()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/datahealth/report");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<DataHealthReportDto>();
        Assert.NotNull(report);
    }

    /// <summary>
    /// AC-125a-11: GET /api/v1/datahealth/duplicates?accountId={id} returns 200.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetDuplicates_WithAccountId_Returns_200()
    {
        // Arrange
        var account = await CreateTestAccountAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/datahealth/duplicates?accountId={account.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// AC-125a-12: POST /api/v1/datahealth/merge-duplicates with nonexistent primary returns 404.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task MergeDuplicates_WithNonexistentPrimary_Returns_404()
    {
        // Arrange
        var request = new MergeDuplicatesRequest
        {
            PrimaryTransactionId = Guid.NewGuid(),
            DuplicateIds = [Guid.NewGuid()],
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/datahealth/merge-duplicates", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<AccountDto> CreateTestAccountAsync()
    {
        var req = new AccountCreateDto
        {
            Name = $"Test Account {Guid.NewGuid():N}",
            InitialBalance = 0m,
            InitialBalanceCurrency = "USD",
            Scope = "Shared",
        };
        var resp = await _client.PostAsJsonAsync("/api/v1/accounts", req);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<AccountDto>())!;
    }
}
