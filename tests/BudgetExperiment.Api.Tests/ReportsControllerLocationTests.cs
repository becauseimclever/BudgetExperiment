// <copyright file="ReportsControllerLocationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the location spending report endpoint.
/// </summary>
[Collection("ApiDb")]
public sealed class ReportsControllerLocationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportsControllerLocationTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public ReportsControllerLocationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET spending-by-location returns 200 with valid date range.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSpendingByLocation_ValidDateRange_Returns200()
    {
        // Act - far-future date range with no data
        var response = await _client.GetAsync(
            "/api/v1/reports/spending-by-location?startDate=2035-07-01&endDate=2035-07-31");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<LocationSpendingReportDto>();
        Assert.NotNull(report);
        Assert.Equal(new DateOnly(2035, 7, 1), report.StartDate);
        Assert.Equal(new DateOnly(2035, 7, 31), report.EndDate);
        Assert.Equal(0m, report.TotalSpending);
        Assert.Equal(0, report.TotalTransactions);
        Assert.Empty(report.Regions);
    }

    /// <summary>
    /// GET spending-by-location returns 400 when end date is before start date.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSpendingByLocation_InvalidDateRange_Returns400()
    {
        // Act
        var response = await _client.GetAsync(
            "/api/v1/reports/spending-by-location?startDate=2026-02-15&endDate=2026-01-15");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET spending-by-location returns location data for transactions with locations.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSpendingByLocation_WithLocationData_ReturnsRegions()
    {
        // Arrange - create account, transaction, then set location
        var accountDto = new AccountCreateDto
        {
            Name = "Location Report Account",
            Type = "Checking",
            InitialBalance = 5000m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2033, 1, 1),
        };
        var accountResponse = await _client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();

        var transactionDto = new TransactionCreateDto
        {
            AccountId = account!.Id,
            Amount = new MoneyDto { Amount = -80m, Currency = "USD" },
            Date = new DateOnly(2033, 1, 10),
            Description = "Location test purchase",
        };
        var txResponse = await _client.PostAsJsonAsync("/api/v1/transactions", transactionDto);
        var transaction = await txResponse.Content.ReadFromJsonAsync<TransactionDto>();

        // Set location on the transaction
        var locationDto = new TransactionLocationUpdateDto
        {
            City = "Seattle",
            StateOrRegion = "WA",
            Country = "US",
        };
        await _client.PatchAsJsonAsync($"/api/v1/transactions/{transaction!.Id}/location", locationDto);

        // Act
        var response = await _client.GetAsync(
            "/api/v1/reports/spending-by-location?startDate=2033-01-01&endDate=2033-01-31");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<LocationSpendingReportDto>();
        Assert.NotNull(report);
        Assert.Equal(80m, report.TotalSpending);
        Assert.Single(report.Regions);
        Assert.Equal("US-WA", report.Regions[0].RegionCode);
        Assert.Equal(80m, report.Regions[0].TotalSpending);
    }
}
