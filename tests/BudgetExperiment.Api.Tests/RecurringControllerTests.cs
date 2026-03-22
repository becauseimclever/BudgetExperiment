// <copyright file="RecurringControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Recurring controller (past-due and batch realize endpoints).
/// </summary>
[Collection("ApiDb")]
public sealed class RecurringControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public RecurringControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/recurring/past-due returns 200 OK with a summary when no items are past due.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPastDue_Returns_200_WithSummary()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/recurring/past-due");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<PastDueSummaryDto>();
        Assert.NotNull(summary);
        Assert.True(summary.TotalCount >= 0);
    }

    /// <summary>
    /// GET /api/v1/recurring/past-due?accountId={guid} returns 200 OK when filtering by an account that has no recurring items.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPastDue_WithAccountFilter_Returns_200()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/recurring/past-due?accountId={accountId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<PastDueSummaryDto>();
        Assert.NotNull(summary);
        Assert.Equal(0, summary.TotalCount);
    }

    /// <summary>
    /// GET /api/v1/recurring/past-due returns items when a recurring transaction is past due.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetPastDue_Returns_Items_WhenRecurringTransactionPastDue()
    {
        // Arrange
        var (recurring, _) = await this.SeedPastDueRecurringTransactionAsync("PDA");

        // Act
        var response = await _client.GetAsync("/api/v1/recurring/past-due");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<PastDueSummaryDto>();
        Assert.NotNull(summary);
        Assert.True(summary.TotalCount > 0);
        Assert.Contains(summary.Items, item => item.Id == recurring.Id && item.Type == "recurring-transaction");
    }

    /// <summary>
    /// POST /api/v1/recurring/realize-batch returns 400 when the items list is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RealizeBatch_Returns_400_WhenItemsListIsEmpty()
    {
        // Arrange
        var request = new BatchRealizeRequest { Items = [] };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/recurring/realize-batch", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/recurring/realize-batch returns 400 when no body is provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RealizeBatch_Returns_400_WhenBodyIsAbsent()
    {
        // Act
        var response = await _client.PostAsync("/api/v1/recurring/realize-batch", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/recurring/realize-batch returns 200 with a failure entry when the item type is unknown.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RealizeBatch_WithUnknownItemType_Returns_200_WithFailure()
    {
        // Arrange
        var request = new BatchRealizeRequest
        {
            Items =
            [
                new BatchRealizeItemRequest
                {
                    Id = Guid.NewGuid(),
                    Type = "unknown-type",
                    InstanceDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-5),
                },
            ],
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/recurring/realize-batch", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BatchRealizeResultDto>();
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Single(result.Failures);
    }

    /// <summary>
    /// POST /api/v1/recurring/realize-batch returns 200 with a failure entry when the recurring-transaction ID does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RealizeBatch_WithNonExistentRecurringTransaction_Returns_200_WithFailure()
    {
        // Arrange
        var request = new BatchRealizeRequest
        {
            Items =
            [
                new BatchRealizeItemRequest
                {
                    Id = Guid.NewGuid(),
                    Type = "recurring-transaction",
                    InstanceDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3),
                },
            ],
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/recurring/realize-batch", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BatchRealizeResultDto>();
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Single(result.Failures);
    }

    /// <summary>
    /// POST /api/v1/recurring/realize-batch returns 200 with a failure entry when the recurring-transfer ID does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RealizeBatch_WithNonExistentRecurringTransfer_Returns_200_WithFailure()
    {
        // Arrange
        var request = new BatchRealizeRequest
        {
            Items =
            [
                new BatchRealizeItemRequest
                {
                    Id = Guid.NewGuid(),
                    Type = "recurring-transfer",
                    InstanceDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7),
                },
            ],
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/recurring/realize-batch", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BatchRealizeResultDto>();
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
    }

    /// <summary>
    /// POST /api/v1/recurring/realize-batch returns 200 with success when a recurring transaction instance is realized.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RealizeBatch_WithRecurringTransaction_Returns_200_WithSuccess()
    {
        // Arrange
        var (recurring, instanceDate) = await this.SeedPastDueRecurringTransactionAsync("RBOK");
        var request = new BatchRealizeRequest
        {
            Items =
            [
                new BatchRealizeItemRequest
                {
                    Id = recurring.Id,
                    Type = "recurring-transaction",
                    InstanceDate = instanceDate,
                },
            ],
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/recurring/realize-batch", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BatchRealizeResultDto>();
        Assert.NotNull(result);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.Empty(result.Failures);
    }

    private async Task<(RecurringTransactionDto Recurring, DateOnly InstanceDate)> SeedPastDueRecurringTransactionAsync(string key)
    {
        var accountDto = new AccountCreateDto { Name = $"PastDue {key}", Type = "Checking" };
        var accountResponse = await _client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = today.AddDays(-21);
        var instanceDate = today.AddDays(-7);
        var createDto = new RecurringTransactionCreateDto
        {
            AccountId = account!.Id,
            Description = $"Past Due {key}",
            Amount = new MoneyDto { Currency = "USD", Amount = 12.34m },
            Frequency = "Weekly",
            DayOfWeek = startDate.DayOfWeek.ToString(),
            StartDate = startDate,
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/recurring-transactions", createDto);
        var recurring = await createResponse.Content.ReadFromJsonAsync<RecurringTransactionDto>();
        return (recurring!, instanceDate);
    }
}
