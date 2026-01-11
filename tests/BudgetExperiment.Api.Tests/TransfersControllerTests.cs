// <copyright file="TransfersControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Application.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Transfers API endpoints.
/// </summary>
public sealed class TransfersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransfersControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public TransfersControllerTests(CustomWebApplicationFactory factory)
    {
        this._client = factory.CreateApiClient();
    }

    /// <summary>
    /// POST /api/v1/transfers creates a transfer and returns 201.
    /// </summary>
    [Fact]
    public async Task CreateTransfer_Returns_201_WithTransfer()
    {
        // Arrange - create two accounts first
        var checkingAccount = await this.CreateAccountAsync("Checking", "Checking");
        var savingsAccount = await this.CreateAccountAsync("Savings", "Savings");

        var request = new CreateTransferRequest
        {
            SourceAccountId = checkingAccount.Id,
            DestinationAccountId = savingsAccount.Id,
            Amount = 500m,
            Currency = "USD",
            Date = new DateOnly(2026, 1, 10),
            Description = "Monthly savings",
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/transfers", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var transfer = await response.Content.ReadFromJsonAsync<TransferResponse>();
        Assert.NotNull(transfer);
        Assert.NotEqual(Guid.Empty, transfer.TransferId);
        Assert.Equal(checkingAccount.Id, transfer.SourceAccountId);
        Assert.Equal(savingsAccount.Id, transfer.DestinationAccountId);
        Assert.Equal(500m, transfer.Amount);
        Assert.Equal("USD", transfer.Currency);
        Assert.Equal("Monthly savings", transfer.Description);
        Assert.Contains("transfers", response.Headers.Location?.ToString() ?? string.Empty);
    }

    /// <summary>
    /// POST /api/v1/transfers returns 400 when source and destination are the same.
    /// </summary>
    [Fact]
    public async Task CreateTransfer_Returns_400_WhenSameAccount()
    {
        // Arrange
        var account = await this.CreateAccountAsync("Test Account", "Checking");
        var request = new CreateTransferRequest
        {
            SourceAccountId = account.Id,
            DestinationAccountId = account.Id,
            Amount = 100m,
            Date = new DateOnly(2026, 1, 10),
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/transfers", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/transfers returns 400 when amount is zero.
    /// </summary>
    [Fact]
    public async Task CreateTransfer_Returns_400_WhenAmountZero()
    {
        // Arrange
        var request = new CreateTransferRequest
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 0m,
            Date = new DateOnly(2026, 1, 10),
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/transfers", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/transfers/{id} returns the transfer when found.
    /// </summary>
    [Fact]
    public async Task GetById_Returns_200_WhenFound()
    {
        // Arrange
        var checkingAccount = await this.CreateAccountAsync("Checking 2", "Checking");
        var savingsAccount = await this.CreateAccountAsync("Savings 2", "Savings");

        var createRequest = new CreateTransferRequest
        {
            SourceAccountId = checkingAccount.Id,
            DestinationAccountId = savingsAccount.Id,
            Amount = 250m,
            Currency = "USD",
            Date = new DateOnly(2026, 1, 10),
        };

        var createResponse = await this._client.PostAsJsonAsync("/api/v1/transfers", createRequest);
        var createdTransfer = await createResponse.Content.ReadFromJsonAsync<TransferResponse>();

        // Act
        var response = await this._client.GetAsync($"/api/v1/transfers/{createdTransfer!.TransferId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var transfer = await response.Content.ReadFromJsonAsync<TransferResponse>();
        Assert.NotNull(transfer);
        Assert.Equal(createdTransfer.TransferId, transfer.TransferId);
    }

    /// <summary>
    /// GET /api/v1/transfers/{id} returns 404 for non-existent transfer.
    /// </summary>
    [Fact]
    public async Task GetById_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/transfers/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/transfers returns list of transfers.
    /// </summary>
    [Fact]
    public async Task List_Returns_200_WithTransfers()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/transfers");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var transfers = await response.Content.ReadFromJsonAsync<List<TransferListItemResponse>>();
        Assert.NotNull(transfers);
    }

    /// <summary>
    /// PUT /api/v1/transfers/{id} updates the transfer.
    /// </summary>
    [Fact]
    public async Task Update_Returns_200_WhenSuccessful()
    {
        // Arrange
        var checkingAccount = await this.CreateAccountAsync("Checking 3", "Checking");
        var savingsAccount = await this.CreateAccountAsync("Savings 3", "Savings");

        var createRequest = new CreateTransferRequest
        {
            SourceAccountId = checkingAccount.Id,
            DestinationAccountId = savingsAccount.Id,
            Amount = 100m,
            Currency = "USD",
            Date = new DateOnly(2026, 1, 10),
        };

        var createResponse = await this._client.PostAsJsonAsync("/api/v1/transfers", createRequest);
        var createdTransfer = await createResponse.Content.ReadFromJsonAsync<TransferResponse>();

        var updateRequest = new UpdateTransferRequest
        {
            Amount = 200m,
            Currency = "USD",
            Date = new DateOnly(2026, 1, 15),
            Description = "Updated transfer",
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/transfers/{createdTransfer!.TransferId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updatedTransfer = await response.Content.ReadFromJsonAsync<TransferResponse>();
        Assert.NotNull(updatedTransfer);
        Assert.Equal(200m, updatedTransfer.Amount);
        Assert.Equal(new DateOnly(2026, 1, 15), updatedTransfer.Date);
        Assert.Equal("Updated transfer", updatedTransfer.Description);
    }

    /// <summary>
    /// PUT /api/v1/transfers/{id} returns 404 when transfer not found.
    /// </summary>
    [Fact]
    public async Task Update_Returns_404_WhenNotFound()
    {
        // Arrange
        var updateRequest = new UpdateTransferRequest
        {
            Amount = 100m,
            Currency = "USD",
            Date = new DateOnly(2026, 1, 15),
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/transfers/{Guid.NewGuid()}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// DELETE /api/v1/transfers/{id} deletes the transfer.
    /// </summary>
    [Fact]
    public async Task Delete_Returns_204_WhenSuccessful()
    {
        // Arrange
        var checkingAccount = await this.CreateAccountAsync("Checking 4", "Checking");
        var savingsAccount = await this.CreateAccountAsync("Savings 4", "Savings");

        var createRequest = new CreateTransferRequest
        {
            SourceAccountId = checkingAccount.Id,
            DestinationAccountId = savingsAccount.Id,
            Amount = 100m,
            Currency = "USD",
            Date = new DateOnly(2026, 1, 10),
        };

        var createResponse = await this._client.PostAsJsonAsync("/api/v1/transfers", createRequest);
        var createdTransfer = await createResponse.Content.ReadFromJsonAsync<TransferResponse>();

        // Act
        var response = await this._client.DeleteAsync($"/api/v1/transfers/{createdTransfer!.TransferId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify transfer is gone
        var getResponse = await this._client.GetAsync($"/api/v1/transfers/{createdTransfer.TransferId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    /// <summary>
    /// DELETE /api/v1/transfers/{id} returns 404 when transfer not found.
    /// </summary>
    [Fact]
    public async Task Delete_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.DeleteAsync($"/api/v1/transfers/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<AccountDto> CreateAccountAsync(string name, string type)
    {
        var response = await this._client.PostAsJsonAsync("/api/v1/accounts", new { name, type });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AccountDto>())!;
    }
}
