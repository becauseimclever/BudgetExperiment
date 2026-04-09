// <copyright file="CalendarControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using BudgetExperiment.Application.Calendar;
using BudgetExperiment.Contracts.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace BudgetExperiment.Api.Tests.Calendar;

public sealed class CalendarControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private readonly Mock<ICalendarService> _calendarServiceMock;

    public CalendarControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _calendarServiceMock = new Mock<ICalendarService>();
        _factory.OverrideServices(services =>
        {
            services.AddSingleton(_calendarServiceMock.Object);
        });
        _client = _factory.CreateApiClient();
    }

    [Fact]
    public async Task GetMonth_ValidYearMonth_Returns200WithDto()
    {
        // Arrange
        var year = 2026;
        var month = 5;
        var expected = new List<DailyTotalDto> { new() { Date = new DateOnly(2026, 5, 1), Total = 100m } };
        _calendarServiceMock.Setup(s => s.GetMonthlySummaryAsync(year, month, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await _client.GetAsync($"/api/v1/calendar/summary?year={year}&month={month}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<DailyTotalDto>>();
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(expected[0].Date, result[0].Date);
        Assert.Equal(expected[0].Total, result[0].Total);
    }

    [Fact]
    public async Task GetMonth_InvalidMonth_Returns400()
    {
        // Arrange
        var year = 2026;
        var month = 13;

        // Act
        var response = await _client.GetAsync($"/api/v1/calendar/summary?year={year}&month={month}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
