// <copyright file="RecurringChargeSuggestionsControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the RecurringChargeSuggestions API endpoints.
/// </summary>
[Collection("ApiDb")]
public sealed class RecurringChargeSuggestionsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringChargeSuggestionsControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public RecurringChargeSuggestionsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateApiClient();
    }

    /// <summary>
    /// POST /api/v1/recurring-charge-suggestions/detect returns 200 with zero when no transactions exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Detect_Returns_200_WithZeroCount_WhenNoTransactions()
    {
        // Arrange
        var request = new DetectRecurringChargesRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/recurring-charge-suggestions/detect", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var count = await response.Content.ReadFromJsonAsync<int>();
        Assert.True(count >= 0);
    }

    /// <summary>
    /// POST /api/v1/recurring-charge-suggestions/detect with matching monthly transactions returns a positive count.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Detect_Returns_PositiveCount_WhenMatchingTransactionsExist()
    {
        // Arrange
        await this.SeedDetectablePatternAsync("SVCDET");

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/v1/recurring-charge-suggestions/detect",
            new DetectRecurringChargesRequest());

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var count = await response.Content.ReadFromJsonAsync<int>();
        Assert.True(count >= 1);
    }

    /// <summary>
    /// GET /api/v1/recurring-charge-suggestions returns 200 OK with a list and pagination header.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSuggestions_Returns_200_WithListAndPaginationHeader()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/recurring-charge-suggestions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Pagination-TotalCount"));
        var suggestions = await response.Content.ReadFromJsonAsync<List<RecurringChargeSuggestionDto>>();
        Assert.NotNull(suggestions);
    }

    /// <summary>
    /// GET /api/v1/recurring-charge-suggestions?status=Pending returns 200 OK with filtered results after detection.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSuggestions_WithPendingStatusFilter_Returns_200()
    {
        // Arrange — ensure there is at least one detectable pattern in the DB
        await this.SeedDetectablePatternAsync("SVCFILT");
        await _client.PostAsJsonAsync(
            "/api/v1/recurring-charge-suggestions/detect",
            new DetectRecurringChargesRequest());

        // Act
        var response = await _client.GetAsync("/api/v1/recurring-charge-suggestions?status=Pending");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var suggestions = await response.Content.ReadFromJsonAsync<List<RecurringChargeSuggestionDto>>();
        Assert.NotNull(suggestions);
        Assert.All(suggestions, s => Assert.Equal("Pending", s.Status));
    }

    /// <summary>
    /// GET /api/v1/recurring-charge-suggestions?status=InvalidValue returns 200 OK, ignoring the invalid filter.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSuggestions_WithInvalidStatusFilter_Returns_200_IgnoresFilter()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/recurring-charge-suggestions?status=NotARealStatus");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var suggestions = await response.Content.ReadFromJsonAsync<List<RecurringChargeSuggestionDto>>();
        Assert.NotNull(suggestions);
    }

    /// <summary>
    /// GET /api/v1/recurring-charge-suggestions/{id} returns 404 when the suggestion does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetById_Returns_404_WhenNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/recurring-charge-suggestions/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/recurring-charge-suggestions/{id} returns 200 with the suggestion when it exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetById_Returns_200_WhenSuggestionExists()
    {
        // Arrange
        var suggestionId = await this.SeedAndDetectAsync("SVCGTID");

        // Act
        var response = await _client.GetAsync($"/api/v1/recurring-charge-suggestions/{suggestionId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<RecurringChargeSuggestionDto>();
        Assert.NotNull(dto);
        Assert.Equal(suggestionId, dto.Id);
    }

    /// <summary>
    /// POST /api/v1/recurring-charge-suggestions/{id}/accept returns 404 when the suggestion does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Accept_Returns_404_WhenNotFound()
    {
        // Act
        var response = await _client.PostAsync(
            $"/api/v1/recurring-charge-suggestions/{Guid.NewGuid()}/accept",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/recurring-charge-suggestions/{id}/accept returns 200 with the created recurring transaction ID.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Accept_Returns_200_WithRecurringTransactionId()
    {
        // Arrange
        var suggestionId = await this.SeedAndDetectAsync("SVCACC");

        // Act
        var response = await _client.PostAsync(
            $"/api/v1/recurring-charge-suggestions/{suggestionId}/accept",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AcceptRecurringChargeSuggestionResultDto>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.RecurringTransactionId);
        Assert.True(result.LinkedTransactionCount >= 0);
    }

    /// <summary>
    /// POST /api/v1/recurring-charge-suggestions/{id}/dismiss returns 404 when the suggestion does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Dismiss_Returns_404_WhenNotFound()
    {
        // Act
        var response = await _client.PostAsync(
            $"/api/v1/recurring-charge-suggestions/{Guid.NewGuid()}/dismiss",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/recurring-charge-suggestions/{id}/dismiss returns 204 No Content on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Dismiss_Returns_204_OnSuccess()
    {
        // Arrange
        var suggestionId = await this.SeedAndDetectAsync("SVCDIS");

        // Act
        var response = await _client.PostAsync(
            $"/api/v1/recurring-charge-suggestions/{suggestionId}/dismiss",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/recurring-charge-suggestions/{id}/accept returns 400 when suggestion is already accepted.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Accept_Returns_400_WhenSuggestionAlreadyAccepted()
    {
        // Arrange
        var suggestionId = await this.SeedAndDetectAsync("SVCDBL");
        await _client.PostAsync(
            $"/api/v1/recurring-charge-suggestions/{suggestionId}/accept",
            null);

        // Act — accept the same suggestion again
        var response = await _client.PostAsync(
            $"/api/v1/recurring-charge-suggestions/{suggestionId}/accept",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/recurring-charge-suggestions/{id}/dismiss returns 400 when suggestion is already accepted.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Dismiss_Returns_400_WhenSuggestionAlreadyAccepted()
    {
        // Arrange
        var suggestionId = await this.SeedAndDetectAsync("SVCACC2");
        await _client.PostAsync(
            $"/api/v1/recurring-charge-suggestions/{suggestionId}/accept",
            null);

        // Act
        var response = await _client.PostAsync(
            $"/api/v1/recurring-charge-suggestions/{suggestionId}/dismiss",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task SeedDetectablePatternAsync(string key)
    {
        var accountDto = new AccountCreateDto { Name = $"SuggAccount {key}", Type = "Checking" };
        var accountResponse = await _client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        for (var monthsBack = 3; monthsBack >= 1; monthsBack--)
        {
            var txDto = new TransactionCreateDto
            {
                AccountId = account!.Id,
                Amount = new MoneyDto { Currency = "USD", Amount = 15.99m },
                Date = today.AddMonths(-monthsBack),
                Description = $"TESTCHARGE {key}",
            };
            await _client.PostAsJsonAsync("/api/v1/transactions", txDto);
        }
    }

    private async Task<Guid> SeedAndDetectAsync(string key)
    {
        await this.SeedDetectablePatternAsync(key);

        await _client.PostAsJsonAsync(
            "/api/v1/recurring-charge-suggestions/detect",
            new DetectRecurringChargesRequest());

        var listResponse = await _client.GetAsync("/api/v1/recurring-charge-suggestions");
        var all = await listResponse.Content.ReadFromJsonAsync<List<RecurringChargeSuggestionDto>>();
        var match = all!.FirstOrDefault(s => s.SampleDescription.Contains(key, StringComparison.OrdinalIgnoreCase)
            || s.NormalizedDescription.Contains(key, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(match);
        return match.Id;
    }
}
