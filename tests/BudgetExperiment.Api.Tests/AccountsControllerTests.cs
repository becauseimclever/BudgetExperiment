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
}
