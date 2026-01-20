// <copyright file="ReconciliationControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Reconciliation API endpoints.
/// </summary>
public sealed class ReconciliationControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReconciliationControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public ReconciliationControllerTests(CustomWebApplicationFactory factory)
    {
        this._factory = factory;
        this._client = factory.CreateApiClient();
    }

    #region GET /api/v1/reconciliation/status Tests

    /// <summary>
    /// GET /api/v1/reconciliation/status returns 200 OK with empty status when no recurring transactions exist.
    /// </summary>
    [Fact]
    public async Task GetStatus_Returns_200_WithEmptyStatus()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/reconciliation/status?year=2026&month=1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var status = await response.Content.ReadFromJsonAsync<ReconciliationStatusDto>();
        Assert.NotNull(status);
        Assert.Equal(2026, status.Year);
        Assert.Equal(1, status.Month);
        Assert.Equal(0, status.TotalExpectedInstances);
    }

    /// <summary>
    /// GET /api/v1/reconciliation/status returns 400 for invalid month.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public async Task GetStatus_Returns_400_ForInvalidMonth(int month)
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/reconciliation/status?year=2026&month={month}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/reconciliation/status returns 400 for invalid year.
    /// </summary>
    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public async Task GetStatus_Returns_400_ForInvalidYear(int year)
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/reconciliation/status?year={year}&month=1");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region GET /api/v1/reconciliation/pending Tests

    /// <summary>
    /// GET /api/v1/reconciliation/pending returns 200 OK with empty list when no pending matches.
    /// </summary>
    [Fact]
    public async Task GetPending_Returns_200_WithEmptyList()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/reconciliation/pending");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var matches = await response.Content.ReadFromJsonAsync<List<ReconciliationMatchDto>>();
        Assert.NotNull(matches);
    }

    #endregion

    #region POST /api/v1/reconciliation/find-matches Tests

    /// <summary>
    /// POST /api/v1/reconciliation/find-matches returns 400 for empty transaction list.
    /// </summary>
    [Fact]
    public async Task FindMatches_Returns_400_ForEmptyTransactionList()
    {
        // Arrange
        var request = new FindMatchesRequest
        {
            TransactionIds = [],
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31),
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/reconciliation/find-matches", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/reconciliation/find-matches returns 400 when start date is after end date.
    /// </summary>
    [Fact]
    public async Task FindMatches_Returns_400_WhenStartAfterEnd()
    {
        // Arrange
        var request = new FindMatchesRequest
        {
            TransactionIds = [Guid.NewGuid()],
            StartDate = new DateOnly(2026, 1, 31),
            EndDate = new DateOnly(2026, 1, 1),
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/reconciliation/find-matches", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/reconciliation/find-matches returns 200 with empty result for non-existent transactions.
    /// </summary>
    [Fact]
    public async Task FindMatches_Returns_200_WithEmptyResult_ForNonExistentTransactions()
    {
        // Arrange
        var request = new FindMatchesRequest
        {
            TransactionIds = [Guid.NewGuid()],
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31),
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/reconciliation/find-matches", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<FindMatchesResult>();
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalMatchesFound);
    }

    #endregion

    #region POST /api/v1/reconciliation/match Tests

    /// <summary>
    /// POST /api/v1/reconciliation/match returns 404 when transaction not found.
    /// </summary>
    [Fact]
    public async Task ManualMatch_Returns_404_WhenTransactionNotFound()
    {
        // Arrange
        var request = new ManualMatchRequest
        {
            TransactionId = Guid.NewGuid(),
            RecurringTransactionId = Guid.NewGuid(),
            InstanceDate = new DateOnly(2026, 1, 15),
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/reconciliation/match", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region POST /api/v1/reconciliation/accept/{matchId} Tests

    /// <summary>
    /// POST /api/v1/reconciliation/accept/{matchId} returns 404 when match not found.
    /// </summary>
    [Fact]
    public async Task AcceptMatch_Returns_404_WhenMatchNotFound()
    {
        // Act
        var response = await this._client.PostAsync($"/api/v1/reconciliation/accept/{Guid.NewGuid()}", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region POST /api/v1/reconciliation/reject/{matchId} Tests

    /// <summary>
    /// POST /api/v1/reconciliation/reject/{matchId} returns 404 when match not found.
    /// </summary>
    [Fact]
    public async Task RejectMatch_Returns_404_WhenMatchNotFound()
    {
        // Act
        var response = await this._client.PostAsync($"/api/v1/reconciliation/reject/{Guid.NewGuid()}", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region POST /api/v1/reconciliation/bulk-accept Tests

    /// <summary>
    /// POST /api/v1/reconciliation/bulk-accept returns 400 for empty match list.
    /// </summary>
    [Fact]
    public async Task BulkAccept_Returns_400_ForEmptyMatchList()
    {
        // Arrange
        var request = new BulkMatchActionRequest { MatchIds = [] };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/reconciliation/bulk-accept", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/reconciliation/bulk-accept returns 200 with empty result for non-existent matches.
    /// </summary>
    [Fact]
    public async Task BulkAccept_Returns_200_WithEmptyResult_ForNonExistentMatches()
    {
        // Arrange
        var request = new BulkMatchActionRequest { MatchIds = [Guid.NewGuid(), Guid.NewGuid()] };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/reconciliation/bulk-accept", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var matches = await response.Content.ReadFromJsonAsync<List<ReconciliationMatchDto>>();
        Assert.NotNull(matches);
        Assert.Empty(matches);
    }

    #endregion

    #region GET /api/v1/reconciliation/recurring/{id} Tests

    /// <summary>
    /// GET /api/v1/reconciliation/recurring/{id} returns 200 OK with empty list when no matches exist.
    /// </summary>
    [Fact]
    public async Task GetMatchesForRecurringTransaction_Returns_200_WithEmptyList()
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/reconciliation/recurring/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var matches = await response.Content.ReadFromJsonAsync<List<ReconciliationMatchDto>>();
        Assert.NotNull(matches);
    }

    #endregion
}
