// <copyright file="TransactionsControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Transactions API endpoints.
/// </summary>
[Collection("ApiDb")]
public sealed class TransactionsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionsControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public TransactionsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/transactions with valid date range returns 200 OK.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetByDateRange_Returns_200_WithValidDateRange()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/transactions?startDate=2026-01-01&endDate=2026-01-31");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionDto>>();
        Assert.NotNull(transactions);

        // The list may contain seed data or be empty - just ensure it's a valid response
    }

    /// <summary>
    /// GET /api/v1/transactions returns 400 when startDate is after endDate.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetByDateRange_Returns_400_WhenStartDateAfterEndDate()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/transactions?startDate=2026-01-31&endDate=2026-01-01");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/transactions/{id} returns 404 for non-existent transaction.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetById_Returns_404_WhenNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/transactions/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// DELETE /api/v1/transactions/{id} returns 404 for non-existent transaction.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Delete_Returns_404_WhenNotFound()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/v1/transactions/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// DELETE /api/v1/transactions/{id} returns 204 when transaction exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Delete_Returns_204_WhenTransactionExists()
    {
        // Arrange — create an account first, then a transaction
        var accountDto = new AccountCreateDto { Name = "DeleteTest", Type = "Checking" };
        var accountResponse = await _client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(account);

        var transactionDto = new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = new MoneyDto { Currency = "USD", Amount = 42m },
            Date = new DateOnly(2026, 2, 19),
            Description = "To Be Deleted",
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", transactionDto);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(created);

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/v1/transactions/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify transaction is gone
        var getResponse = await _client.GetAsync($"/api/v1/transactions/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/transactions/uncategorized returns 200 OK with paged response.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetUncategorized_Returns_200_WithPagedResponse()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/transactions/uncategorized");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UncategorizedTransactionPageDto>();
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 0);
        Assert.Equal(1, result.Page);
        Assert.Equal(50, result.PageSize);
    }

    /// <summary>
    /// GET /api/v1/transactions/uncategorized returns pagination header.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetUncategorized_Returns_PaginationHeader()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/transactions/uncategorized");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Pagination-TotalCount"));
        var headerValue = response.Headers.GetValues("X-Pagination-TotalCount").FirstOrDefault();
        Assert.NotNull(headerValue);
        Assert.True(int.TryParse(headerValue, out _));
    }

    /// <summary>
    /// GET /api/v1/transactions/uncategorized accepts filter parameters.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetUncategorized_AcceptsFilterParameters()
    {
        // Act
        var response = await _client.GetAsync(
            "/api/v1/transactions/uncategorized?" +
            "startDate=2026-01-01&endDate=2026-01-31&minAmount=10&maxAmount=100&" +
            "descriptionContains=test&sortBy=Amount&sortDescending=false&page=2&pageSize=25");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UncategorizedTransactionPageDto>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Page);
        Assert.Equal(25, result.PageSize);
    }

    /// <summary>
    /// POST /api/v1/transactions/bulk-categorize returns 400 when CategoryId is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BulkCategorize_Returns_400_WhenCategoryIdEmpty()
    {
        // Arrange
        var request = new BulkCategorizeRequest
        {
            TransactionIds = [Guid.NewGuid()],
            CategoryId = Guid.Empty,
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions/bulk-categorize", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/transactions/bulk-categorize returns 400 when TransactionIds is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BulkCategorize_Returns_400_WhenTransactionIdsEmpty()
    {
        // Arrange
        var request = new BulkCategorizeRequest
        {
            TransactionIds = [],
            CategoryId = Guid.NewGuid(),
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions/bulk-categorize", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/transactions/bulk-categorize returns 200 with response when category not found.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BulkCategorize_Returns_200_WithErrorWhenCategoryNotFound()
    {
        // Arrange
        var request = new BulkCategorizeRequest
        {
            TransactionIds = [Guid.NewGuid()],
            CategoryId = Guid.NewGuid(), // Non-existent category
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions/bulk-categorize", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BulkCategorizeResponse>();
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalRequested);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.NotEmpty(result.Errors);
    }

    /// <summary>
    /// PATCH /api/v1/transactions/{id}/location returns 200 with populated location.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PatchLocation_ValidCity_Returns200WithLocation()
    {
        // Arrange — create account + transaction
        var accountDto = new AccountCreateDto { Name = "LocationTest", Type = "Checking" };
        var accountResponse = await _client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(account);

        var transactionDto = new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = new MoneyDto { Currency = "USD", Amount = -25.00m },
            Date = new DateOnly(2026, 2, 20),
            Description = "Coffee Shop",
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", transactionDto);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(created);

        var locationUpdate = new TransactionLocationUpdateDto
        {
            City = "Seattle",
            StateOrRegion = "WA",
            Country = "US",
            PostalCode = "98101",
        };

        // Act
        var patchResponse = await _client.PatchAsJsonAsync(
            $"/api/v1/transactions/{created.Id}/location",
            locationUpdate);

        // Assert
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        var result = await patchResponse.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(result);
        Assert.NotNull(result.Location);
        Assert.Equal("Seattle", result.Location.City);
        Assert.Equal("WA", result.Location.StateOrRegion);
        Assert.Equal("US", result.Location.Country);
        Assert.Equal("98101", result.Location.PostalCode);
        Assert.Equal("Manual", result.Location.Source);
    }

    /// <summary>
    /// PATCH /api/v1/transactions/{id}/location returns 404 for non-existent transaction.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PatchLocation_InvalidTransactionId_Returns404()
    {
        // Arrange
        var locationUpdate = new TransactionLocationUpdateDto { City = "Seattle" };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/transactions/{Guid.NewGuid()}/location",
            locationUpdate);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// DELETE /api/v1/transactions/{id}/location returns 204 when transaction has location.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteLocation_ExistingTransaction_Returns204()
    {
        // Arrange — create account + transaction + set location
        var accountDto = new AccountCreateDto { Name = "LocationDeleteTest", Type = "Checking" };
        var accountResponse = await _client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(account);

        var transactionDto = new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = new MoneyDto { Currency = "USD", Amount = -15.00m },
            Date = new DateOnly(2026, 2, 20),
            Description = "Lunch",
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", transactionDto);
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(created);

        var locationUpdate = new TransactionLocationUpdateDto { City = "Portland", StateOrRegion = "OR", Country = "US" };
        await _client.PatchAsJsonAsync($"/api/v1/transactions/{created.Id}/location", locationUpdate);

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/v1/transactions/{created.Id}/location");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify location is cleared
        var getResponse = await _client.GetAsync($"/api/v1/transactions/{created.Id}");
        var transaction = await getResponse.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(transaction);
        Assert.Null(transaction.Location);
    }

    /// <summary>
    /// DELETE /api/v1/transactions/{id}/location returns 404 for non-existent transaction.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteLocation_NonExistentTransaction_Returns404()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/v1/transactions/{Guid.NewGuid()}/location");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/transactions/{id} includes location data when set.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetTransaction_WithLocation_IncludesLocationDto()
    {
        // Arrange — create account + transaction + set location with coordinates
        var accountDto = new AccountCreateDto { Name = "LocationGetTest", Type = "Checking" };
        var accountResponse = await _client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(account);

        var transactionDto = new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = new MoneyDto { Currency = "USD", Amount = -50.00m },
            Date = new DateOnly(2026, 2, 20),
            Description = "Dinner",
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", transactionDto);
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(created);

        var locationUpdate = new TransactionLocationUpdateDto
        {
            Latitude = 47.6062m,
            Longitude = -122.3321m,
            City = "Seattle",
            StateOrRegion = "WA",
            Country = "US",
        };
        await _client.PatchAsJsonAsync($"/api/v1/transactions/{created.Id}/location", locationUpdate);

        // Act
        var getResponse = await _client.GetAsync($"/api/v1/transactions/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var transaction = await getResponse.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(transaction);
        Assert.NotNull(transaction.Location);
        Assert.Equal(47.6062m, transaction.Location.Latitude);
        Assert.Equal(-122.3321m, transaction.Location.Longitude);
        Assert.Equal("Seattle", transaction.Location.City);
    }

    /// <summary>
    /// GET /api/v1/transactions/{id} returns null location when not set.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetTransaction_WithoutLocation_LocationIsNull()
    {
        // Arrange — create account + transaction (no location)
        var accountDto = new AccountCreateDto { Name = "NoLocationTest", Type = "Checking" };
        var accountResponse = await _client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(account);

        var transactionDto = new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = new MoneyDto { Currency = "USD", Amount = -5.00m },
            Date = new DateOnly(2026, 2, 20),
            Description = "Snack",
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", transactionDto);
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(created);

        // Act
        var getResponse = await _client.GetAsync($"/api/v1/transactions/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var transaction = await getResponse.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(transaction);
        Assert.Null(transaction.Location);
    }

    /// <summary>
    /// GET /api/v1/transactions/{id} returns an ETag header for concurrency control.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetById_Returns_ETag_Header()
    {
        // Arrange
        var accountDto = new AccountCreateDto { Name = "TxnETagTest", Type = "Checking" };
        var accountResponse = await _client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(account);

        var transactionDto = new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = new MoneyDto { Currency = "USD", Amount = -10m },
            Date = new DateOnly(2026, 3, 1),
            Description = "ETag Test Txn",
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", transactionDto);
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(created);

        // Act
        var response = await _client.GetAsync($"/api/v1/transactions/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        Assert.False(string.IsNullOrEmpty(response.Headers.ETag.Tag));
    }

    /// <summary>
    /// PUT /api/v1/transactions/{id} succeeds when a valid If-Match header is provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Update_With_Valid_IfMatch_Succeeds()
    {
        // Arrange
        var accountDto = new AccountCreateDto { Name = "TxnIfMatchValid", Type = "Checking" };
        var accountResponse = await _client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(account);

        var transactionDto = new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = new MoneyDto { Currency = "USD", Amount = -20m },
            Date = new DateOnly(2026, 3, 1),
            Description = "IfMatch Valid Txn",
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", transactionDto);
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(created);

        var getResponse = await _client.GetAsync($"/api/v1/transactions/{created.Id}");
        var etag = getResponse.Headers.ETag;

        var updateDto = new TransactionUpdateDto
        {
            Amount = new MoneyDto { Currency = "USD", Amount = -25m },
            Date = new DateOnly(2026, 3, 2),
            Description = "Updated With ETag",
        };
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/transactions/{created.Id}")
        {
            Content = JsonContent.Create(updateDto),
        };
        request.Headers.IfMatch.Add(etag!);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        var updated = await response.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.Equal("Updated With ETag", updated!.Description);
    }

    /// <summary>
    /// PUT /api/v1/transactions/{id} returns 409 Conflict when If-Match does not match current version.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Update_With_Stale_IfMatch_Returns_409()
    {
        // Arrange
        var accountDto = new AccountCreateDto { Name = "TxnIfMatchStale", Type = "Checking" };
        var accountResponse = await _client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(account);

        var transactionDto = new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = new MoneyDto { Currency = "USD", Amount = -30m },
            Date = new DateOnly(2026, 3, 1),
            Description = "Stale IfMatch Txn",
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", transactionDto);
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(created);

        var staleETag = new EntityTagHeaderValue("\"99999999\"");
        var updateDto = new TransactionUpdateDto
        {
            Amount = new MoneyDto { Currency = "USD", Amount = -35m },
            Date = new DateOnly(2026, 3, 1),
            Description = "Should Fail",
        };
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/transactions/{created.Id}")
        {
            Content = JsonContent.Create(updateDto),
        };
        request.Headers.IfMatch.Add(staleETag);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>
    /// PUT /api/v1/transactions/{id} succeeds without If-Match header for backward compatibility.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Update_Without_IfMatch_Succeeds_BackwardCompatible()
    {
        // Arrange
        var accountDto = new AccountCreateDto { Name = "TxnNoIfMatch", Type = "Checking" };
        var accountResponse = await _client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(account);

        var transactionDto = new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = new MoneyDto { Currency = "USD", Amount = -40m },
            Date = new DateOnly(2026, 3, 1),
            Description = "No IfMatch Txn",
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", transactionDto);
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(created);

        var updateDto = new TransactionUpdateDto
        {
            Amount = new MoneyDto { Currency = "USD", Amount = -45m },
            Date = new DateOnly(2026, 3, 1),
            Description = "Updated Without ETag",
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/transactions/{created.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.Equal("Updated Without ETag", updated!.Description);
    }

    /// <summary>
    /// PATCH /api/v1/transactions/{id}/location returns ETag and respects If-Match.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PatchLocation_With_Valid_IfMatch_Succeeds()
    {
        // Arrange
        var accountDto = new AccountCreateDto { Name = "TxnLocIfMatch", Type = "Checking" };
        var accountResponse = await _client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(account);

        var transactionDto = new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = new MoneyDto { Currency = "USD", Amount = -15m },
            Date = new DateOnly(2026, 3, 1),
            Description = "Location IfMatch Txn",
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", transactionDto);
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(created);

        var getResponse = await _client.GetAsync($"/api/v1/transactions/{created.Id}");
        var etag = getResponse.Headers.ETag;

        var locationUpdate = new TransactionLocationUpdateDto
        {
            City = "Denver",
            StateOrRegion = "CO",
            Country = "US",
        };
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/transactions/{created.Id}/location")
        {
            Content = JsonContent.Create(locationUpdate),
        };
        request.Headers.IfMatch.Add(etag!);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        var result = await response.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.Equal("Denver", result!.Location!.City);
    }

    /// <summary>
    /// PATCH /api/v1/transactions/{id}/location returns 409 with stale If-Match.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PatchLocation_With_Stale_IfMatch_Returns_409()
    {
        // Arrange
        var accountDto = new AccountCreateDto { Name = "TxnLocStale", Type = "Checking" };
        var accountResponse = await _client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(account);

        var transactionDto = new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = new MoneyDto { Currency = "USD", Amount = -12m },
            Date = new DateOnly(2026, 3, 1),
            Description = "Location Stale Txn",
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", transactionDto);
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(created);

        var staleETag = new EntityTagHeaderValue("\"99999999\"");
        var locationUpdate = new TransactionLocationUpdateDto
        {
            City = "Boston",
            StateOrRegion = "MA",
            Country = "US",
        };
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/transactions/{created.Id}/location")
        {
            Content = JsonContent.Create(locationUpdate),
        };
        request.Headers.IfMatch.Add(staleETag);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
