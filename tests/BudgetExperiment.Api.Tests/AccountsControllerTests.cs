// <copyright file="AccountsControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Accounts API endpoints.
/// </summary>
public sealed class AccountsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountsControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public AccountsControllerTests(CustomWebApplicationFactory factory)
    {
        this._client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/accounts returns 200 OK with empty list when no accounts exist.
    /// </summary>
    [Fact]
    public async Task GetAll_Returns_EmptyList_WhenNoAccounts()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/accounts");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var accounts = await response.Content.ReadFromJsonAsync<List<AccountDto>>();
        Assert.NotNull(accounts);
        Assert.Empty(accounts);
    }

    /// <summary>
    /// POST /api/v1/accounts creates an account and returns 201 Created.
    /// </summary>
    [Fact]
    public async Task Create_Returns_201_WithCreatedAccount()
    {
        // Arrange
        var createDto = new AccountCreateDto { Name = "Test Checking", Type = "Checking" };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/accounts", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(created);
        Assert.Equal("Test Checking", created.Name);
        Assert.Equal("Checking", created.Type);
        Assert.NotEqual(Guid.Empty, created.Id);
    }

    /// <summary>
    /// GET /api/v1/accounts/{id} returns 404 for non-existent account.
    /// </summary>
    [Fact]
    public async Task GetById_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/accounts/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// DELETE /api/v1/accounts/{id} returns 404 for non-existent account.
    /// </summary>
    [Fact]
    public async Task Delete_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.DeleteAsync($"/api/v1/accounts/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/accounts creates account with initial balance.
    /// </summary>
    [Fact]
    public async Task Create_With_InitialBalance_Returns_201()
    {
        // Arrange
        var createDto = new AccountCreateDto
        {
            Name = "Checking With Balance",
            Type = "Checking",
            InitialBalance = 1500.00m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2026, 1, 1),
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/accounts", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(created);
        Assert.Equal("Checking With Balance", created.Name);
        Assert.Equal(1500.00m, created.InitialBalance);
        Assert.Equal("USD", created.InitialBalanceCurrency);
        Assert.Equal(new DateOnly(2026, 1, 1), created.InitialBalanceDate);
    }

    /// <summary>
    /// PUT /api/v1/accounts/{id} updates account and returns 200.
    /// </summary>
    [Fact]
    public async Task Update_Returns_200_WithUpdatedAccount()
    {
        // Arrange - create account first
        var createDto = new AccountCreateDto { Name = "Original Name", Type = "Savings" };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/accounts", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<AccountDto>();

        var updateDto = new AccountUpdateDto
        {
            Name = "Updated Name",
            InitialBalance = 2500.00m,
            InitialBalanceDate = new DateOnly(2026, 1, 15),
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/accounts/{created!.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal(2500.00m, updated.InitialBalance);
        Assert.Equal(new DateOnly(2026, 1, 15), updated.InitialBalanceDate);
    }

    /// <summary>
    /// PUT /api/v1/accounts/{id} returns 404 for non-existent account.
    /// </summary>
    [Fact]
    public async Task Update_Returns_404_WhenNotFound()
    {
        // Arrange
        var updateDto = new AccountUpdateDto { Name = "New Name" };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/accounts/{Guid.NewGuid()}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/accounts/{id} returns account with initial balance fields.
    /// </summary>
    [Fact]
    public async Task GetById_Returns_Account_With_InitialBalance()
    {
        // Arrange - create account with balance
        var createDto = new AccountCreateDto
        {
            Name = "Get Test Account",
            Type = "Checking",
            InitialBalance = 3000.00m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2026, 1, 5),
        };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/accounts", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<AccountDto>();

        // Act
        var response = await this._client.GetAsync($"/api/v1/accounts/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var account = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(account);
        Assert.Equal(3000.00m, account.InitialBalance);
        Assert.Equal("USD", account.InitialBalanceCurrency);
        Assert.Equal(new DateOnly(2026, 1, 5), account.InitialBalanceDate);
    }
}
