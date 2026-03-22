// <copyright file="BudgetApiServiceAccountTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for <see cref="BudgetApiService"/> account operations.
/// </summary>
public class BudgetApiServiceAccountTests
{
    /// <summary>
    /// Tests that GetAccountsAsync returns accounts on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetAccountsAsync_ReturnsAccounts_OnSuccess()
    {
        // Arrange
        var accounts = new List<AccountDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Checking" },
            new() { Id = Guid.NewGuid(), Name = "Savings" },
        };
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(accounts),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetAccountsAsync();

        // Assert
        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("Checking");
        result[1].Name.ShouldBe("Savings");
    }

    /// <summary>
    /// Tests that GetAccountsAsync returns empty list when response is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetAccountsAsync_ReturnsEmptyList_WhenResponseIsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create<List<AccountDto>?>(null),
            }));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetAccountsAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that GetAccountAsync returns account on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetAccountAsync_ReturnsAccount_OnSuccess()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.RequestUri!.PathAndQuery.ShouldContain($"api/v1/accounts/{accountId}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new AccountDto { Id = accountId, Name = "Checking" }),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetAccountAsync(accountId);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(accountId);
        result.Name.ShouldBe("Checking");
    }

    /// <summary>
    /// Tests that GetAccountAsync returns null on HTTP error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetAccountAsync_ReturnsNull_OnHttpError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Not found"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.GetAccountAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that CreateAccountAsync returns account on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateAccountAsync_ReturnsAccount_OnSuccess()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Post);
            request.RequestUri!.PathAndQuery.ShouldBe("/api/v1/accounts");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(new AccountDto { Id = expectedId, Name = "New Account" }),
            });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.CreateAccountAsync(new AccountCreateDto { Name = "New Account" });

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(expectedId);
        result.Name.ShouldBe("New Account");
    }

    /// <summary>
    /// Tests that CreateAccountAsync returns null on failure status.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateAccountAsync_ReturnsNull_OnFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.CreateAccountAsync(new AccountCreateDto { Name = "Test" });

        // Assert
        result.ShouldBeNull();
    }

    /// <summary>
    /// Tests that DeleteAccountAsync returns true on success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteAccountAsync_ReturnsTrue_OnSuccess()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Delete);
            request.RequestUri!.PathAndQuery.ShouldBe($"/api/v1/accounts/{accountId}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.DeleteAccountAsync(accountId);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that DeleteAccountAsync returns false on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteAccountAsync_ReturnsFalse_OnFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new BudgetApiService(httpClient);

        // Act
        var result = await service.DeleteAccountAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeFalse();
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }
}
