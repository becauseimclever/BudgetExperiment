// <copyright file="ExportControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Export API endpoints.
/// </summary>
public sealed class ExportControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportControllerTests"/> class.
    /// </summary>
    /// <param name="factory">Test factory.</param>
    public ExportControllerTests(CustomWebApplicationFactory factory)
    {
        this._client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/exports/categories/monthly returns CSV for empty data.
    /// </summary>
    [Fact]
    public async Task ExportMonthlyCategoryReport_Returns_Csv()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/exports/categories/monthly?year=2030&month=12");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Category,Amount,Currency,Percentage,Transactions", content);
    }

    /// <summary>
    /// GET /api/v1/exports/categories/range returns CSV for date range.
    /// </summary>
    [Fact]
    public async Task ExportCategoryReportByRange_Returns_Csv()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/exports/categories/range?startDate=2030-01-01&endDate=2030-01-31");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Category,Amount,Currency,Percentage,Transactions", content);
    }

    /// <summary>
    /// GET /api/v1/exports/trends returns CSV for monthly trends.
    /// </summary>
    [Fact]
    public async Task ExportSpendingTrends_Returns_Csv()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/exports/trends?months=6");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Month,Income,Spending,Net,Transactions", content);
    }

    /// <summary>
    /// GET /api/v1/exports/budget-comparison returns CSV for budget comparison.
    /// </summary>
    [Fact]
    public async Task ExportBudgetComparison_Returns_Csv()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/exports/budget-comparison?year=2030&month=12");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Category,Budgeted,Spent,Remaining,PercentUsed,Status,Transactions", content);
    }
}
