// <copyright file="CalendarControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Calendar API endpoints.
/// </summary>
[Collection("ApiDb")]
public sealed class CalendarControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public CalendarControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
        _client = factory.CreateApiClient();
    }

    /// <summary>
    /// API-128-I1: grid day total equals the seeded transaction sum for that date, including days outside current month.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCalendarGrid_I1_DayAmountMatchesSeededTransactions_ForOutOfMonthGridDay()
    {
        // Arrange
        var account = await CreateTestAccountAsync(
            initialBalance: 100m,
            initialBalanceDate: new DateOnly(2025, 12, 1));

        await CreateTransactionAsync(account.Id, new DateOnly(2025, 12, 30), 20m, "Out-of-month paycheck");
        await CreateTransactionAsync(account.Id, new DateOnly(2026, 1, 2), -10m, "January expense");

        // Act
        var response = await _client.GetAsync("/api/v1/calendar/grid?year=2026&month=1&accountId=" + account.Id);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var grid = await response.Content.ReadFromJsonAsync<CalendarGridDto>();
        Assert.NotNull(grid);

        var december30 = grid.Days.Single(d => d.Date == new DateOnly(2025, 12, 30));
        Assert.Equal(20m, december30.ActualTotal.Amount);
        Assert.Equal(20m, december30.CombinedTotal.Amount);
    }

    /// <summary>
    /// API-128-I2: running total continuity matches deterministic seeded day amounts.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCalendarGrid_I2_RunningTotalContinuity_UsesExpectedDeterministicValues()
    {
        // Arrange
        var account = await CreateTestAccountAsync(
            initialBalance: 100m,
            initialBalanceDate: new DateOnly(2025, 12, 1));

        await CreateTransactionAsync(account.Id, new DateOnly(2025, 12, 30), 20m, "Out-of-month paycheck");
        await CreateTransactionAsync(account.Id, new DateOnly(2026, 1, 2), -10m, "January expense");

        // Act
        var response = await _client.GetAsync("/api/v1/calendar/grid?year=2026&month=1&accountId=" + account.Id);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var grid = await response.Content.ReadFromJsonAsync<CalendarGridDto>();
        Assert.NotNull(grid);

        var january1 = grid.Days.Single(d => d.Date == new DateOnly(2026, 1, 1));
        var january2 = grid.Days.Single(d => d.Date == new DateOnly(2026, 1, 2));

        Assert.Equal(120m, january1.EndOfDayBalance.Amount);
        Assert.Equal(-10m, january2.CombinedTotal.Amount);
        Assert.Equal(110m, january2.EndOfDayBalance.Amount);
    }

    /// <summary>
    /// API-128-I3: running total equals initial balance plus prefix sum through the selected day.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCalendarGrid_I3_RunningTotalEqualsInitialBalancePlusPrefixSum()
    {
        // Arrange
        var account = await CreateTestAccountAsync(
            initialBalance: 100m,
            initialBalanceDate: new DateOnly(2025, 12, 1));

        await CreateTransactionAsync(account.Id, new DateOnly(2025, 12, 30), 20m, "Out-of-month paycheck");
        await CreateTransactionAsync(account.Id, new DateOnly(2026, 1, 2), -10m, "January expense");
        await CreateTransactionAsync(account.Id, new DateOnly(2026, 1, 4), 5m, "January refund");

        // Act
        var response = await _client.GetAsync("/api/v1/calendar/grid?year=2026&month=1&accountId=" + account.Id);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var grid = await response.Content.ReadFromJsonAsync<CalendarGridDto>();
        Assert.NotNull(grid);

        var january4 = grid.Days.Single(d => d.Date == new DateOnly(2026, 1, 4));

        Assert.Equal(100m, grid.StartingBalance.Amount);
        Assert.Equal(115m, january4.EndOfDayBalance.Amount);
    }

    /// <summary>
    /// API-128-I6: deleting a transaction updates grid totals and repeated delete attempts do not change results.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCalendarGrid_I6_DeleteIdempotence_RepeatedDeleteLeavesGridUnchanged()
    {
        // Arrange
        var account = await CreateTestAccountAsync(
            initialBalance: 100m,
            initialBalanceDate: new DateOnly(2025, 12, 1));

        var transactionId = await CreateTransactionAsync(
            account.Id,
            new DateOnly(2026, 1, 3),
            25m,
            "Delete idempotence candidate");

        var beforeDeleteResponse = await _client.GetAsync($"/api/v1/calendar/grid?year=2026&month=1&accountId={account.Id}");
        Assert.Equal(HttpStatusCode.OK, beforeDeleteResponse.StatusCode);
        var beforeDeleteGrid = await beforeDeleteResponse.Content.ReadFromJsonAsync<CalendarGridDto>();
        Assert.NotNull(beforeDeleteGrid);

        // Act - first delete
        var firstDeleteResponse = await _client.DeleteAsync($"/api/v1/transactions/{transactionId}");

        var afterFirstDeleteResponse = await _client.GetAsync($"/api/v1/calendar/grid?year=2026&month=1&accountId={account.Id}");
        Assert.Equal(HttpStatusCode.OK, afterFirstDeleteResponse.StatusCode);
        var afterFirstDeleteGrid = await afterFirstDeleteResponse.Content.ReadFromJsonAsync<CalendarGridDto>();
        Assert.NotNull(afterFirstDeleteGrid);

        // Act - repeated delete (resource already deleted)
        var secondDeleteResponse = await _client.DeleteAsync($"/api/v1/transactions/{transactionId}");

        var afterSecondDeleteResponse = await _client.GetAsync($"/api/v1/calendar/grid?year=2026&month=1&accountId={account.Id}");
        Assert.Equal(HttpStatusCode.OK, afterSecondDeleteResponse.StatusCode);
        var afterSecondDeleteGrid = await afterSecondDeleteResponse.Content.ReadFromJsonAsync<CalendarGridDto>();
        Assert.NotNull(afterSecondDeleteGrid);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, firstDeleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, secondDeleteResponse.StatusCode);

        var targetDate = new DateOnly(2026, 1, 3);
        var dayBefore = beforeDeleteGrid.Days.Single(d => d.Date == targetDate);
        var dayAfterFirstDelete = afterFirstDeleteGrid.Days.Single(d => d.Date == targetDate);
        var dayAfterSecondDelete = afterSecondDeleteGrid.Days.Single(d => d.Date == targetDate);

        Assert.Equal(25m, dayBefore.ActualTotal.Amount);
        Assert.Equal(0m, dayAfterFirstDelete.ActualTotal.Amount);
        Assert.Equal(0m, dayAfterSecondDelete.ActualTotal.Amount);

        Assert.Equal(dayAfterFirstDelete.CombinedTotal.Amount, dayAfterSecondDelete.CombinedTotal.Amount);
        Assert.Equal(dayAfterFirstDelete.EndOfDayBalance.Amount, dayAfterSecondDelete.EndOfDayBalance.Amount);
    }

    /// <summary>
    /// GET /api/v1/calendar/summary with valid year/month returns 200 OK.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetMonthlySummary_Returns_200_WithValidYearMonth()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/calendar/summary?year=2026&month=1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<List<DailyTotalDto>>();
        Assert.NotNull(summary);

        // The list may contain seed data or be empty - just ensure it's a valid response
    }

    /// <summary>
    /// GET /api/v1/calendar/summary returns 400 for invalid month.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetMonthlySummary_Returns_400_ForInvalidMonth()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/calendar/summary?year=2026&month=13");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/calendar/summary returns 400 for month 0.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetMonthlySummary_Returns_400_ForMonthZero()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/calendar/summary?year=2026&month=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<AccountDto> CreateTestAccountAsync(decimal initialBalance, DateOnly initialBalanceDate)
    {
        var req = new AccountCreateDto
        {
            Name = $"Calendar Test Account {Guid.NewGuid():N}",
            InitialBalance = initialBalance,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = initialBalanceDate,
            Scope = "Shared",
        };

        var response = await _client.PostAsJsonAsync("/api/v1/accounts", req);
        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(account);
        return account;
    }

    private async Task<Guid> CreateTransactionAsync(Guid accountId, DateOnly date, decimal amount, string description)
    {
        var req = new TransactionCreateDto
        {
            AccountId = accountId,
            Amount = new MoneyDto { Amount = amount, Currency = "USD" },
            Date = date,
            Description = description,
        };

        var response = await _client.PostAsJsonAsync("/api/v1/transactions", req);
        response.EnsureSuccessStatusCode();
        var transaction = await response.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(transaction);
        return transaction.Id;
    }
}
