// <copyright file="AccountsControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Moq;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Accounts API endpoints.
/// </summary>
[Collection("ApiDb")]
public sealed class AccountsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountsControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public AccountsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/accounts returns 200 OK with empty list when no accounts exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetAll_Returns_200_WithAccountList()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/accounts");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var accounts = await response.Content.ReadFromJsonAsync<List<AccountDto>>();
        Assert.NotNull(accounts);

        // The list may contain seed data or be empty - just ensure it's a valid response
    }

    /// <summary>
    /// POST /api/v1/accounts creates an account and returns 201 Created.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Create_Returns_201_WithCreatedAccount()
    {
        // Arrange
        var createDto = new AccountCreateDto { Name = "Test Checking", Type = "Checking" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/accounts", createDto);

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
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetById_Returns_404_WhenNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/accounts/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// DELETE /api/v1/accounts/{id} returns 404 for non-existent account.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Delete_Returns_404_WhenNotFound()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/v1/accounts/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/accounts creates account with initial balance.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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
        var response = await _client.PostAsJsonAsync("/api/v1/accounts", createDto);

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
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Update_Returns_200_WithUpdatedAccount()
    {
        // Arrange - create account first
        var createDto = new AccountCreateDto { Name = "Original Name", Type = "Savings" };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/accounts", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<AccountDto>();

        var updateDto = new AccountUpdateDto
        {
            Name = "Updated Name",
            InitialBalance = 2500.00m,
            InitialBalanceDate = new DateOnly(2026, 1, 15),
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/accounts/{created!.Id}", updateDto);

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
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Update_Returns_404_WhenNotFound()
    {
        // Arrange
        var updateDto = new AccountUpdateDto { Name = "New Name" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/accounts/{Guid.NewGuid()}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/accounts/{id} returns account with initial balance fields.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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
        var createResponse = await _client.PostAsJsonAsync("/api/v1/accounts", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<AccountDto>();

        // Act
        var response = await _client.GetAsync($"/api/v1/accounts/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var account = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(account);
        Assert.Equal(3000.00m, account.InitialBalance);
        Assert.Equal("USD", account.InitialBalanceCurrency);
        Assert.Equal(new DateOnly(2026, 1, 5), account.InitialBalanceDate);
    }

    /// <summary>
    /// GET /api/v1/accounts/{id} returns an ETag header for concurrency control.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetById_Returns_ETag_Header()
    {
        // Arrange
        var createDto = new AccountCreateDto { Name = "ETag Test", Type = "Checking" };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/accounts", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<AccountDto>();

        // Act
        var response = await _client.GetAsync($"/api/v1/accounts/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        Assert.False(string.IsNullOrEmpty(response.Headers.ETag.Tag));
    }

    /// <summary>
    /// PUT /api/v1/accounts/{id} succeeds when a valid If-Match header is provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Update_With_Valid_IfMatch_Succeeds()
    {
        // Arrange - create account and get ETag
        var createDto = new AccountCreateDto { Name = "IfMatch Valid Test", Type = "Checking" };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/accounts", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<AccountDto>();

        var getResponse = await _client.GetAsync($"/api/v1/accounts/{created!.Id}");
        var etag = getResponse.Headers.ETag;

        var updateDto = new AccountUpdateDto { Name = "Updated With ETag" };
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/accounts/{created.Id}")
        {
            Content = JsonContent.Create(updateDto),
        };
        request.Headers.IfMatch.Add(etag!);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        var updated = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.Equal("Updated With ETag", updated!.Name);
    }

    /// <summary>
    /// PUT /api/v1/accounts/{id} returns 409 Conflict when If-Match does not match current version.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Update_With_Stale_IfMatch_Returns_409()
    {
        // Arrange - create account
        var createDto = new AccountCreateDto { Name = "Stale IfMatch Test", Type = "Checking" };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/accounts", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<AccountDto>();

        // Use a deliberately wrong ETag to simulate stale version
        var staleETag = new EntityTagHeaderValue("\"99999999\"");
        var updateDto = new AccountUpdateDto { Name = "Should Fail" };
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/accounts/{created!.Id}")
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
    /// PUT /api/v1/accounts/{id} succeeds without If-Match header for backward compatibility.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Update_Without_IfMatch_Succeeds_BackwardCompatible()
    {
        // Arrange
        var createDto = new AccountCreateDto { Name = "No IfMatch Test", Type = "Checking" };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/accounts", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<AccountDto>();

        var updateDto = new AccountUpdateDto { Name = "Updated Without ETag" };

        // Act - PUT without If-Match header
        var response = await _client.PutAsJsonAsync($"/api/v1/accounts/{created!.Id}", updateDto);

        // Assert - should still succeed
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.Equal("Updated Without ETag", updated!.Name);
    }

    /// <summary>
    /// IAccountService mock returns two accounts → GET /api/v1/accounts returns 200 with two items.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AccountsController_GetAll_Returns200WithList()
    {
        // Arrange
        var stubAccounts = new List<AccountDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Checking", Type = "Checking" },
            new() { Id = Guid.NewGuid(), Name = "Savings", Type = "Savings" },
        };

        var mockAccountService = new Mock<IAccountService>();
        mockAccountService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stubAccounts);

        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAccountService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddScoped<IAccountService>(_ => mockAccountService.Object);
            }));

        using var client = CreateAuthenticatedClient(factory);

        // Act
        var response = await client.GetAsync("/api/v1/accounts");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<AccountDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.Name == "Checking");
        Assert.Contains(result, a => a.Name == "Savings");
    }

    /// <summary>
    /// IAccountService mock returns a known account → GET /api/v1/accounts/{id} returns 200 with that DTO.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AccountsController_GetById_ValidId_Returns200WithDto()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var stubAccount = new AccountDto
        {
            Id = accountId,
            Name = "Test Account",
            Type = "Checking",
            Version = "1",
        };

        var mockAccountService = new Mock<IAccountService>();
        mockAccountService
            .Setup(s => s.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stubAccount);

        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAccountService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddScoped<IAccountService>(_ => mockAccountService.Object);
            }));

        using var client = CreateAuthenticatedClient(factory);

        // Act
        var response = await client.GetAsync($"/api/v1/accounts/{accountId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(result);
        Assert.Equal(accountId, result.Id);
        Assert.Equal("Test Account", result.Name);
        Assert.NotNull(response.Headers.ETag);
    }

    /// <summary>
    /// IAccountService mock returns null → GET /api/v1/accounts/{unknownId} returns 404.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AccountsController_GetById_NotFoundId_Returns404()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        var mockAccountService = new Mock<IAccountService>();
        mockAccountService
            .Setup(s => s.GetByIdAsync(unknownId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountDto?)null);

        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAccountService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddScoped<IAccountService>(_ => mockAccountService.Object);
            }));

        using var client = CreateAuthenticatedClient(factory);

        // Act
        var response = await client.GetAsync($"/api/v1/accounts/{unknownId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// IAccountService mock returns created DTO → POST /api/v1/accounts returns 201 with Location header.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AccountsController_Create_ValidRequest_Returns201WithLocation()
    {
        // Arrange
        var newId = Guid.NewGuid();
        var createDto = new AccountCreateDto { Name = "New Savings", Type = "Savings" };
        var createdAccount = new AccountDto { Id = newId, Name = "New Savings", Type = "Savings" };

        var mockAccountService = new Mock<IAccountService>();
        mockAccountService
            .Setup(s => s.CreateAsync(It.IsAny<AccountCreateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdAccount);

        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAccountService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddScoped<IAccountService>(_ => mockAccountService.Object);
            }));

        using var client = CreateAuthenticatedClient(factory);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/accounts", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var result = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(result);
        Assert.Equal(newId, result.Id);
        Assert.Equal("New Savings", result.Name);
    }

    private static HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestAuto", "authenticated");
        return client;
    }
}
