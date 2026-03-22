// <copyright file="RecurringTransfersControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the RecurringTransfers API endpoints.
/// </summary>
[Collection("ApiDb")]
public sealed class RecurringTransfersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransfersControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public RecurringTransfersControllerTests(CustomWebApplicationFactory factory)
    {
        this._factory = factory;
        this._client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/recurring-transfers returns 200 OK with empty list when no recurring transfers exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetAll_Returns_200_WithEmptyList()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/recurring-transfers");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var transfers = await response.Content.ReadFromJsonAsync<List<RecurringTransferDto>>();
        Assert.NotNull(transfers);
    }

    /// <summary>
    /// GET /api/v1/recurring-transfers with isActive=true returns 200 OK.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetAll_WithActiveFilter_Returns_200()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/recurring-transfers?isActive=true");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var transfers = await response.Content.ReadFromJsonAsync<List<RecurringTransferDto>>();
        Assert.NotNull(transfers);
    }

    /// <summary>
    /// GET /api/v1/recurring-transfers with accountId filter returns 200 OK.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetAll_WithAccountIdFilter_Returns_200()
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/recurring-transfers?accountId={Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var transfers = await response.Content.ReadFromJsonAsync<List<RecurringTransferDto>>();
        Assert.NotNull(transfers);
    }

    /// <summary>
    /// GET /api/v1/recurring-transfers/{id} returns 404 for non-existent recurring transfer.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetById_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/recurring-transfers/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/recurring-transfers returns 404 when source account does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Create_Returns_404_WhenSourceAccountNotFound()
    {
        // Arrange
        var dto = new RecurringTransferCreateDto
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Description = "Test Recurring Transfer",
            Amount = new MoneyDto { Currency = "USD", Amount = 100m },
            Frequency = "Monthly",
            StartDate = new DateOnly(2026, 1, 1),
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/recurring-transfers", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// DELETE /api/v1/recurring-transfers/{id} returns 404 when recurring transfer does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Delete_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.DeleteAsync($"/api/v1/recurring-transfers/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// PUT /api/v1/recurring-transfers/{id} returns 404 when recurring transfer does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Update_Returns_404_WhenNotFound()
    {
        // Arrange
        var dto = new RecurringTransferUpdateDto
        {
            Description = "Updated",
            Amount = new MoneyDto { Currency = "USD", Amount = 200m },
            Frequency = "Monthly",
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/recurring-transfers/{Guid.NewGuid()}", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/recurring-transfers/{id}/pause returns 404 when recurring transfer does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Pause_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.PostAsync($"/api/v1/recurring-transfers/{Guid.NewGuid()}/pause", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/recurring-transfers/{id}/resume returns 404 when recurring transfer does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Resume_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.PostAsync($"/api/v1/recurring-transfers/{Guid.NewGuid()}/resume", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/recurring-transfers/{id}/skip returns 404 when recurring transfer does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SkipNext_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.PostAsync($"/api/v1/recurring-transfers/{Guid.NewGuid()}/skip", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/recurring-transfers/projected returns 200 OK with valid date range.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetProjected_Returns_200_WithValidRange()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/recurring-transfers/projected?from=2026-01-01&to=2026-12-31");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var instances = await response.Content.ReadFromJsonAsync<List<RecurringTransferInstanceDto>>();
        Assert.NotNull(instances);
    }

    /// <summary>
    /// GET /api/v1/recurring-transfers/projected returns 400 when from is after to.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetProjected_Returns_400_WhenFromAfterTo()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/recurring-transfers/projected?from=2026-12-31&to=2026-01-01");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/recurring-transfers/projected with accountId filter returns 200 OK.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetProjected_WithAccountIdFilter_Returns_200()
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/recurring-transfers/projected?from=2026-01-01&to=2026-12-31&accountId={Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/recurring-transfers/{id}/instances returns 404 when recurring transfer does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetInstances_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/recurring-transfers/{Guid.NewGuid()}/instances?from=2026-01-01&to=2026-12-31");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/recurring-transfers/{id}/instances returns 400 when from is after to.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetInstances_Returns_400_WhenFromAfterTo()
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/recurring-transfers/{Guid.NewGuid()}/instances?from=2026-12-31&to=2026-01-01");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// PUT /api/v1/recurring-transfers/{id}/instances/{date} returns 404 when recurring transfer does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ModifyInstance_Returns_404_WhenNotFound()
    {
        // Arrange
        var dto = new RecurringTransferInstanceModifyDto
        {
            Amount = new MoneyDto { Currency = "USD", Amount = 150m },
            Description = "Modified",
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/recurring-transfers/{Guid.NewGuid()}/instances/2026-01-15", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// DELETE /api/v1/recurring-transfers/{id}/instances/{date} returns 404 when recurring transfer does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SkipInstance_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.DeleteAsync($"/api/v1/recurring-transfers/{Guid.NewGuid()}/instances/2026-01-15");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// PUT /api/v1/recurring-transfers/{id}/instances/{date}/future returns 404 when recurring transfer does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateFuture_Returns_404_WhenNotFound()
    {
        // Arrange
        var dto = new RecurringTransferUpdateDto
        {
            Description = "Updated Future",
            Amount = new MoneyDto { Currency = "USD", Amount = 300m },
            Frequency = "Monthly",
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/recurring-transfers/{Guid.NewGuid()}/instances/2026-01-15/future", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/recurring-transfers/{id} returns an ETag header for concurrency control.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetById_Returns_ETag_Header()
    {
        // Arrange
        var created = await this.CreateRecurringTransferAsync();

        // Act
        var response = await this._client.GetAsync($"/api/v1/recurring-transfers/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        Assert.False(string.IsNullOrEmpty(response.Headers.ETag.Tag));
    }

    /// <summary>
    /// PUT /api/v1/recurring-transfers/{id} succeeds when a valid If-Match header is provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Update_With_Valid_IfMatch_Succeeds()
    {
        // Arrange
        var created = await this.CreateRecurringTransferAsync();
        var getResponse = await this._client.GetAsync($"/api/v1/recurring-transfers/{created.Id}");
        var etag = getResponse.Headers.ETag;

        var updateDto = new RecurringTransferUpdateDto
        {
            Description = "Updated With ETag",
            Amount = new MoneyDto { Currency = "USD", Amount = 150m },
            Frequency = "Monthly",
        };
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/recurring-transfers/{created.Id}")
        {
            Content = JsonContent.Create(updateDto),
        };
        request.Headers.IfMatch.Add(etag!);

        // Act
        var response = await this._client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        var updated = await response.Content.ReadFromJsonAsync<RecurringTransferDto>();
        Assert.Equal("Updated With ETag", updated!.Description);
    }

    /// <summary>
    /// PUT /api/v1/recurring-transfers/{id} returns 409 Conflict when If-Match does not match current version.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Update_With_Stale_IfMatch_Returns_409()
    {
        // Arrange
        var created = await this.CreateRecurringTransferAsync();

        var staleETag = new EntityTagHeaderValue("\"99999999\"");
        var updateDto = new RecurringTransferUpdateDto
        {
            Description = "Should Fail",
            Amount = new MoneyDto { Currency = "USD", Amount = 200m },
            Frequency = "Monthly",
        };
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/recurring-transfers/{created.Id}")
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
    /// PUT /api/v1/recurring-transfers/{id} succeeds without If-Match header for backward compatibility.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Update_Without_IfMatch_Succeeds_BackwardCompatible()
    {
        // Arrange
        var created = await this.CreateRecurringTransferAsync();
        var updateDto = new RecurringTransferUpdateDto
        {
            Description = "Updated Without ETag",
            Amount = new MoneyDto { Currency = "USD", Amount = 200m },
            Frequency = "Monthly",
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/recurring-transfers/{created.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<RecurringTransferDto>();
        Assert.Equal("Updated Without ETag", updated!.Description);
    }

    /// <summary>
    /// PUT /api/v1/recurring-transfers/{id}/instances/{date} returns 409 Conflict with stale If-Match.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ModifyInstance_With_Stale_IfMatch_Returns_409()
    {
        // Arrange
        var created = await this.CreateRecurringTransferAsync(dayOfMonth: 15, startDate: new DateOnly(2026, 1, 15));

        var staleETag = new EntityTagHeaderValue("\"99999999\"");
        var modifyDto = new RecurringTransferInstanceModifyDto
        {
            Amount = new MoneyDto { Currency = "USD", Amount = 100m },
            Description = "Should Fail",
        };
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/recurring-transfers/{created.Id}/instances/2026-01-15")
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
    /// PUT /api/v1/recurring-transfers/{id}/instances/{date}/future returns 409 Conflict with stale If-Match.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateFuture_With_Stale_IfMatch_Returns_409()
    {
        // Arrange
        var created = await this.CreateRecurringTransferAsync(dayOfMonth: 1, startDate: new DateOnly(2026, 1, 1));

        var staleETag = new EntityTagHeaderValue("\"99999999\"");
        var updateDto = new RecurringTransferUpdateDto
        {
            Description = "Should Fail",
            Amount = new MoneyDto { Currency = "USD", Amount = 200m },
            Frequency = "Monthly",
        };
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/recurring-transfers/{created.Id}/instances/2026-02-01/future")
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
    /// PUT /api/v1/recurring-transfers/{id}/instances/{date}/future succeeds with valid If-Match and returns ETag.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateFuture_With_Valid_IfMatch_Succeeds()
    {
        // Arrange
        var created = await this.CreateRecurringTransferAsync(dayOfMonth: 1, startDate: new DateOnly(2026, 1, 1));
        var getResponse = await this._client.GetAsync($"/api/v1/recurring-transfers/{created.Id}");
        var etag = getResponse.Headers.ETag;

        var updateDto = new RecurringTransferUpdateDto
        {
            Description = "Updated Future",
            Amount = new MoneyDto { Currency = "USD", Amount = 75m },
            Frequency = "Monthly",
        };
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/recurring-transfers/{created.Id}/instances/2026-02-01/future")
        {
            Content = JsonContent.Create(updateDto),
        };
        request.Headers.IfMatch.Add(etag!);

        // Act
        var response = await this._client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        var updated = await response.Content.ReadFromJsonAsync<RecurringTransferDto>();
        Assert.Equal("Updated Future", updated!.Description);
    }

    private async Task<RecurringTransferDto> CreateRecurringTransferAsync(
        int? dayOfMonth = null,
        DateOnly? startDate = null)
    {
        var sourceAccountDto = new AccountCreateDto { Name = $"RT Source {Guid.NewGuid():N}", Type = "Checking" };
        var sourceResponse = await this._client.PostAsJsonAsync("/api/v1/accounts", sourceAccountDto);
        var sourceAccount = await sourceResponse.Content.ReadFromJsonAsync<AccountDto>();

        var destAccountDto = new AccountCreateDto { Name = $"RT Dest {Guid.NewGuid():N}", Type = "Savings" };
        var destResponse = await this._client.PostAsJsonAsync("/api/v1/accounts", destAccountDto);
        var destAccount = await destResponse.Content.ReadFromJsonAsync<AccountDto>();

        var createDto = new RecurringTransferCreateDto
        {
            SourceAccountId = sourceAccount!.Id,
            DestinationAccountId = destAccount!.Id,
            Description = "ETag Test Transfer",
            Amount = new MoneyDto { Currency = "USD", Amount = 100m },
            Frequency = "Monthly",
            DayOfMonth = dayOfMonth,
            StartDate = startDate ?? new DateOnly(2026, 1, 1),
        };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/recurring-transfers", createDto);
        createResponse.EnsureSuccessStatusCode();
        return (await createResponse.Content.ReadFromJsonAsync<RecurringTransferDto>())!;
    }
}
