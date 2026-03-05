// <copyright file="RecurringTransactionsControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the RecurringTransactions API endpoints.
/// </summary>
public sealed class RecurringTransactionsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransactionsControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public RecurringTransactionsControllerTests(CustomWebApplicationFactory factory)
    {
        this._factory = factory;
        this._client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/recurring-transactions returns 200 OK with empty list when no recurring transactions exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetAll_Returns_200_WithEmptyList()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/recurring-transactions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var transactions = await response.Content.ReadFromJsonAsync<List<RecurringTransactionDto>>();
        Assert.NotNull(transactions);
    }

    /// <summary>
    /// GET /api/v1/recurring-transactions/{id} returns 404 for non-existent recurring transaction.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetById_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/recurring-transactions/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/recurring-transactions returns 404 when account does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Create_Returns_404_WhenAccountNotFound()
    {
        // Arrange
        var dto = new RecurringTransactionCreateDto
        {
            AccountId = Guid.NewGuid(),
            Description = "Test Recurring",
            Amount = new MoneyDto { Currency = "USD", Amount = 100m },
            Frequency = "Monthly",
            StartDate = new DateOnly(2026, 1, 1),
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/recurring-transactions", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// DELETE /api/v1/recurring-transactions/{id} returns 404 when recurring transaction does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Delete_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.DeleteAsync($"/api/v1/recurring-transactions/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// PUT /api/v1/recurring-transactions/{id} returns 404 when recurring transaction does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Update_Returns_404_WhenNotFound()
    {
        // Arrange
        var dto = new RecurringTransactionUpdateDto
        {
            Description = "Updated",
            Amount = new MoneyDto { Currency = "USD", Amount = 200m },
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/recurring-transactions/{Guid.NewGuid()}", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/recurring-transactions/{id}/pause returns 404 when recurring transaction does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Pause_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.PostAsync($"/api/v1/recurring-transactions/{Guid.NewGuid()}/pause", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/recurring-transactions/{id}/resume returns 404 when recurring transaction does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Resume_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.PostAsync($"/api/v1/recurring-transactions/{Guid.NewGuid()}/resume", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/recurring-transactions/{id}/skip returns 404 when recurring transaction does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SkipNext_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.PostAsync($"/api/v1/recurring-transactions/{Guid.NewGuid()}/skip", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/recurring-transactions/projected returns 200 OK with valid date range.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetProjected_Returns_200_WithValidRange()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/recurring-transactions/projected?from=2026-01-01&to=2026-12-31");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var instances = await response.Content.ReadFromJsonAsync<List<RecurringInstanceDto>>();
        Assert.NotNull(instances);
    }

    /// <summary>
    /// GET /api/v1/recurring-transactions/projected returns 400 when from is after to.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetProjected_Returns_400_WhenFromAfterTo()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/recurring-transactions/projected?from=2026-12-31&to=2026-01-01");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/recurring-transactions/{id}/instances returns 404 when recurring transaction does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetInstances_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/recurring-transactions/{Guid.NewGuid()}/instances?from=2026-01-01&to=2026-12-31");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// PUT /api/v1/recurring-transactions/{id}/instances/{date} returns 404 when recurring transaction does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ModifyInstance_Returns_404_WhenNotFound()
    {
        // Arrange
        var dto = new RecurringInstanceModifyDto
        {
            Amount = new MoneyDto { Currency = "USD", Amount = 150m },
            Description = "Modified",
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/recurring-transactions/{Guid.NewGuid()}/instances/2026-01-15", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// DELETE /api/v1/recurring-transactions/{id}/instances/{date} returns 404 when recurring transaction does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SkipInstance_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.DeleteAsync($"/api/v1/recurring-transactions/{Guid.NewGuid()}/instances/2026-01-15");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// PUT /api/v1/recurring-transactions/{id}/instances/{date}/future returns 404 when recurring transaction does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateFuture_Returns_404_WhenNotFound()
    {
        // Arrange
        var dto = new RecurringTransactionUpdateDto
        {
            Description = "Updated Future",
            Amount = new MoneyDto { Currency = "USD", Amount = 300m },
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/recurring-transactions/{Guid.NewGuid()}/instances/2026-01-15/future", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/recurring-transactions/{id}/import-patterns returns 404 when recurring transaction does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetImportPatterns_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/recurring-transactions/{Guid.NewGuid()}/import-patterns");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// PUT /api/v1/recurring-transactions/{id}/import-patterns returns 404 when recurring transaction does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateImportPatterns_Returns_404_WhenNotFound()
    {
        // Arrange
        var dto = new ImportPatternsDto
        {
            Patterns = ["*NETFLIX*", "*HULU*"],
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/recurring-transactions/{Guid.NewGuid()}/import-patterns", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/recurring-transactions/{id} returns an ETag header for concurrency control.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetById_Returns_ETag_Header()
    {
        // Arrange - create an account first
        var accountDto = new AccountCreateDto { Name = "ETag Recurring Account", Type = "Checking" };
        var accountResponse = await this._client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();

        var createDto = new RecurringTransactionCreateDto
        {
            AccountId = account!.Id,
            Description = "ETag Test Recurring",
            Amount = new MoneyDto { Currency = "USD", Amount = 50m },
            Frequency = "Monthly",
            StartDate = new DateOnly(2026, 1, 1),
        };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/recurring-transactions", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<RecurringTransactionDto>();

        // Act
        var response = await this._client.GetAsync($"/api/v1/recurring-transactions/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        Assert.False(string.IsNullOrEmpty(response.Headers.ETag.Tag));
    }

    /// <summary>
    /// PUT /api/v1/recurring-transactions/{id} succeeds when a valid If-Match header is provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Update_With_Valid_IfMatch_Succeeds()
    {
        // Arrange
        var accountDto = new AccountCreateDto { Name = "IfMatch Valid Recurring", Type = "Checking" };
        var accountResponse = await this._client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();

        var createDto = new RecurringTransactionCreateDto
        {
            AccountId = account!.Id,
            Description = "IfMatch Valid Test",
            Amount = new MoneyDto { Currency = "USD", Amount = 100m },
            Frequency = "Monthly",
            StartDate = new DateOnly(2026, 1, 1),
        };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/recurring-transactions", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<RecurringTransactionDto>();

        var getResponse = await this._client.GetAsync($"/api/v1/recurring-transactions/{created!.Id}");
        var etag = getResponse.Headers.ETag;

        var updateDto = new RecurringTransactionUpdateDto
        {
            Description = "Updated With ETag",
            Amount = new MoneyDto { Currency = "USD", Amount = 150m },
            Frequency = "Monthly",
        };
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/recurring-transactions/{created.Id}")
        {
            Content = JsonContent.Create(updateDto),
        };
        request.Headers.IfMatch.Add(etag!);

        // Act
        var response = await this._client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        var updated = await response.Content.ReadFromJsonAsync<RecurringTransactionDto>();
        Assert.Equal("Updated With ETag", updated!.Description);
    }

    /// <summary>
    /// PUT /api/v1/recurring-transactions/{id} returns 409 Conflict when If-Match does not match current version.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Update_With_Stale_IfMatch_Returns_409()
    {
        // Arrange
        var accountDto = new AccountCreateDto { Name = "Stale IfMatch Recurring", Type = "Checking" };
        var accountResponse = await this._client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();

        var createDto = new RecurringTransactionCreateDto
        {
            AccountId = account!.Id,
            Description = "Stale IfMatch Test",
            Amount = new MoneyDto { Currency = "USD", Amount = 100m },
            Frequency = "Monthly",
            StartDate = new DateOnly(2026, 1, 1),
        };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/recurring-transactions", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<RecurringTransactionDto>();

        var staleETag = new EntityTagHeaderValue("\"99999999\"");
        var updateDto = new RecurringTransactionUpdateDto
        {
            Description = "Should Fail",
            Amount = new MoneyDto { Currency = "USD", Amount = 200m },
            Frequency = "Monthly",
        };
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/recurring-transactions/{created!.Id}")
        {
            Content = JsonContent.Create(updateDto),
        };
        request.Headers.IfMatch.Add(staleETag);

        // Act
        var response = await this._client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>
    /// PUT /api/v1/recurring-transactions/{id} succeeds without If-Match header for backward compatibility.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Update_Without_IfMatch_Succeeds_BackwardCompatible()
    {
        // Arrange
        var accountDto = new AccountCreateDto { Name = "No IfMatch Recurring", Type = "Checking" };
        var accountResponse = await this._client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();

        var createDto = new RecurringTransactionCreateDto
        {
            AccountId = account!.Id,
            Description = "No IfMatch Test",
            Amount = new MoneyDto { Currency = "USD", Amount = 100m },
            Frequency = "Monthly",
            StartDate = new DateOnly(2026, 1, 1),
        };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/recurring-transactions", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<RecurringTransactionDto>();

        var updateDto = new RecurringTransactionUpdateDto
        {
            Description = "Updated Without ETag",
            Amount = new MoneyDto { Currency = "USD", Amount = 200m },
            Frequency = "Monthly",
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/recurring-transactions/{created!.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<RecurringTransactionDto>();
        Assert.Equal("Updated Without ETag", updated!.Description);
    }

    /// <summary>
    /// PUT /api/v1/recurring-transactions/{id}/instances/{date} returns 409 Conflict with stale If-Match.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ModifyInstance_With_Stale_IfMatch_Returns_409()
    {
        // Arrange
        var accountDto = new AccountCreateDto { Name = "Instance Stale IfMatch", Type = "Checking" };
        var accountResponse = await this._client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();

        var createDto = new RecurringTransactionCreateDto
        {
            AccountId = account!.Id,
            Description = "Instance Stale Test",
            Amount = new MoneyDto { Currency = "USD", Amount = 75m },
            Frequency = "Monthly",
            DayOfMonth = 15,
            StartDate = new DateOnly(2026, 1, 15),
        };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/recurring-transactions", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<RecurringTransactionDto>();

        var staleETag = new EntityTagHeaderValue("\"99999999\"");
        var modifyDto = new RecurringInstanceModifyDto
        {
            Amount = new MoneyDto { Currency = "USD", Amount = 100m },
            Description = "Should Fail",
        };
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/recurring-transactions/{created!.Id}/instances/2026-01-15")
        {
            Content = JsonContent.Create(modifyDto),
        };
        request.Headers.IfMatch.Add(staleETag);

        // Act
        var response = await this._client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>
    /// PUT /api/v1/recurring-transactions/{id}/instances/{date}/future returns 409 Conflict with stale If-Match.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateFuture_With_Stale_IfMatch_Returns_409()
    {
        // Arrange
        var accountDto = new AccountCreateDto { Name = "Future Stale IfMatch", Type = "Checking" };
        var accountResponse = await this._client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();

        var createDto = new RecurringTransactionCreateDto
        {
            AccountId = account!.Id,
            Description = "Future Stale Test",
            Amount = new MoneyDto { Currency = "USD", Amount = 50m },
            Frequency = "Monthly",
            DayOfMonth = 1,
            StartDate = new DateOnly(2026, 1, 1),
        };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/recurring-transactions", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<RecurringTransactionDto>();

        var staleETag = new EntityTagHeaderValue("\"99999999\"");
        var updateDto = new RecurringTransactionUpdateDto
        {
            Description = "Should Fail",
            Amount = new MoneyDto { Currency = "USD", Amount = 200m },
            Frequency = "Monthly",
        };
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/recurring-transactions/{created!.Id}/instances/2026-02-01/future")
        {
            Content = JsonContent.Create(updateDto),
        };
        request.Headers.IfMatch.Add(staleETag);

        // Act
        var response = await this._client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>
    /// PUT /api/v1/recurring-transactions/{id}/instances/{date}/future succeeds with valid If-Match and returns ETag.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateFuture_With_Valid_IfMatch_Succeeds()
    {
        // Arrange
        var accountDto = new AccountCreateDto { Name = "Future Valid IfMatch", Type = "Checking" };
        var accountResponse = await this._client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();

        var createDto = new RecurringTransactionCreateDto
        {
            AccountId = account!.Id,
            Description = "Future Valid Test",
            Amount = new MoneyDto { Currency = "USD", Amount = 50m },
            Frequency = "Monthly",
            DayOfMonth = 1,
            StartDate = new DateOnly(2026, 1, 1),
        };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/recurring-transactions", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<RecurringTransactionDto>();

        var getResponse = await this._client.GetAsync($"/api/v1/recurring-transactions/{created!.Id}");
        var etag = getResponse.Headers.ETag;

        var updateDto = new RecurringTransactionUpdateDto
        {
            Description = "Updated Future",
            Amount = new MoneyDto { Currency = "USD", Amount = 75m },
            Frequency = "Monthly",
        };
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/recurring-transactions/{created.Id}/instances/2026-02-01/future")
        {
            Content = JsonContent.Create(updateDto),
        };
        request.Headers.IfMatch.Add(etag!);

        // Act
        var response = await this._client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        var updated = await response.Content.ReadFromJsonAsync<RecurringTransactionDto>();
        Assert.Equal("Updated Future", updated!.Description);
    }
}
