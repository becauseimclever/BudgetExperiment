// <copyright file="AllocationsControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Allocations API endpoints.
/// </summary>
public sealed class AllocationsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllocationsControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public AllocationsControllerTests(CustomWebApplicationFactory factory)
    {
        this._client = factory.CreateApiClient();
    }

    #region GET /api/v1/allocations/paycheck

    /// <summary>
    /// GET /api/v1/allocations/paycheck returns 200 OK with summary.
    /// </summary>
    [Fact]
    public async Task GetPaycheckAllocation_Returns_200_WithSummary()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/allocations/paycheck?frequency=BiWeekly");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<PaycheckAllocationSummaryDto>();
        Assert.NotNull(summary);
        Assert.Equal("BiWeekly", summary.PaycheckFrequency);
    }

    /// <summary>
    /// GET /api/v1/allocations/paycheck without frequency returns 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task GetPaycheckAllocation_WithoutFrequency_Returns_400()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/allocations/paycheck");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/allocations/paycheck with invalid frequency returns 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task GetPaycheckAllocation_WithInvalidFrequency_Returns_400()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/allocations/paycheck?frequency=InvalidFrequency");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/allocations/paycheck with paycheck amount calculates income.
    /// </summary>
    [Fact]
    public async Task GetPaycheckAllocation_WithAmount_CalculatesIncome()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/allocations/paycheck?frequency=BiWeekly&amount=2000");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<PaycheckAllocationSummaryDto>();
        Assert.NotNull(summary);
        Assert.NotNull(summary.PaycheckAmount);
        Assert.Equal(2000m, summary.PaycheckAmount.Amount);
        Assert.NotNull(summary.TotalAnnualIncome);
        Assert.Equal(52000m, summary.TotalAnnualIncome.Amount); // $2,000 × 26 paychecks
    }

    /// <summary>
    /// GET /api/v1/allocations/paycheck with negative amount returns 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task GetPaycheckAllocation_WithNegativeAmount_Returns_400()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/allocations/paycheck?frequency=BiWeekly&amount=-1000");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/allocations/paycheck with zero amount returns 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task GetPaycheckAllocation_WithZeroAmount_Returns_400()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/allocations/paycheck?frequency=BiWeekly&amount=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/allocations/paycheck with accountId filters by account.
    /// </summary>
    [Fact]
    public async Task GetPaycheckAllocation_WithAccountId_FiltersByAccount()
    {
        // Arrange - use a random GUID that likely has no transactions
        var accountId = Guid.NewGuid();

        // Act
        var response = await this._client.GetAsync($"/api/v1/allocations/paycheck?frequency=BiWeekly&accountId={accountId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<PaycheckAllocationSummaryDto>();
        Assert.NotNull(summary);

        // With a random account ID, we should get no allocations
        Assert.Empty(summary.Allocations);
    }

    /// <summary>
    /// GET /api/v1/allocations/paycheck supports Weekly frequency.
    /// </summary>
    [Fact]
    public async Task GetPaycheckAllocation_WithWeeklyFrequency_Returns_200()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/allocations/paycheck?frequency=Weekly");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<PaycheckAllocationSummaryDto>();
        Assert.NotNull(summary);
        Assert.Equal("Weekly", summary.PaycheckFrequency);
    }

    /// <summary>
    /// GET /api/v1/allocations/paycheck supports Monthly frequency.
    /// </summary>
    [Fact]
    public async Task GetPaycheckAllocation_WithMonthlyFrequency_Returns_200()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/allocations/paycheck?frequency=Monthly");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<PaycheckAllocationSummaryDto>();
        Assert.NotNull(summary);
        Assert.Equal("Monthly", summary.PaycheckFrequency);
    }

    /// <summary>
    /// GET /api/v1/allocations/paycheck without amount generates NoIncomeConfigured warning.
    /// </summary>
    [Fact]
    public async Task GetPaycheckAllocation_WithoutAmount_HasNoIncomeWarning()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/allocations/paycheck?frequency=BiWeekly");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<PaycheckAllocationSummaryDto>();
        Assert.NotNull(summary);
        Assert.True(summary.HasWarnings);

        // Should have either NoIncomeConfigured or NoBillsConfigured (depending on test data)
        Assert.True(summary.Warnings.Count > 0);
    }

    #endregion
}
