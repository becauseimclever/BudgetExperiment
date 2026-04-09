// <copyright file="AccountsControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using BudgetExperiment.Application.Accounts;
using BudgetExperiment.Contracts.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace BudgetExperiment.Api.Tests.Accounts;

public sealed class AccountsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private readonly Mock<IAccountService> _accountServiceMock;

    public AccountsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _accountServiceMock = new Mock<IAccountService>();
        _factory.OverrideServices(services =>
        {
            services.AddSingleton(_accountServiceMock.Object);
        });
        _client = _factory.CreateApiClient();
    }

    [Fact]
    public async Task GetAll_Returns200WithList()
    {
        // Arrange
        var accounts = new List<AccountDto>
        {
            new() { Id = Guid.NewGuid(), Name = "A", Type = "Checking" },
            new() { Id = Guid.NewGuid(), Name = "B", Type = "Savings" }
        };
        _accountServiceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(accounts);

        // Act
        var response = await _client.GetAsync("/api/v1/accounts");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<AccountDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetById_ExistingId_Returns200WithDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new AccountDto { Id = id, Name = "A", Type = "Checking" };
        _accountServiceMock.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        // Act
        var response = await _client.GetAsync($"/api/v1/accounts/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        // Arrange
        var id = Guid.NewGuid();
        _accountServiceMock.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((AccountDto?)null);

        // Act
        var response = await _client.GetAsync($"/api/v1/accounts/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201WithLocation()
    {
        // Arrange
        var createDto = new AccountCreateDto { Name = "A", Type = "Checking", InitialBalance = 100, InitialBalanceCurrency = "USD", InitialBalanceDate = new DateOnly(2026, 1, 1) };
        var created = new AccountDto { Id = Guid.NewGuid(), Name = "A", Type = "Checking" };
        _accountServiceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/accounts", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var result = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
    }
}
