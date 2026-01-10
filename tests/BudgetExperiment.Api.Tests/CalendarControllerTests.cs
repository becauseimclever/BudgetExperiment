// <copyright file="CalendarControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Application.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Calendar API endpoints.
/// </summary>
public sealed class CalendarControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public CalendarControllerTests(CustomWebApplicationFactory factory)
    {
        this._client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/calendar/summary with valid year/month returns 200 OK.
    /// </summary>
    [Fact]
    public async Task GetMonthlySummary_Returns_200_WithEmptyList()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/calendar/summary?year=2026&month=1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<List<DailyTotalDto>>();
        Assert.NotNull(summary);
        Assert.Empty(summary);
    }

    /// <summary>
    /// GET /api/v1/calendar/summary returns 400 for invalid month.
    /// </summary>
    [Fact]
    public async Task GetMonthlySummary_Returns_400_ForInvalidMonth()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/calendar/summary?year=2026&month=13");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/calendar/summary returns 400 for month 0.
    /// </summary>
    [Fact]
    public async Task GetMonthlySummary_Returns_400_ForMonthZero()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/calendar/summary?year=2026&month=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
