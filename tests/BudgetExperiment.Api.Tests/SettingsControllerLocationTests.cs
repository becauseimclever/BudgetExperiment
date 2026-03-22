// <copyright file="SettingsControllerLocationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the DELETE /api/v1/settings/location-data endpoint.
/// </summary>
[Collection("ApiDb")]
public sealed class SettingsControllerLocationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsControllerLocationTests"/> class.
    /// </summary>
    /// <param name="factory">The web application factory.</param>
    public SettingsControllerLocationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateApiClient();
    }

    /// <summary>
    /// DELETE /api/v1/settings/location-data returns 200 with cleared count.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Delete_LocationData_Returns200WithCount()
    {
        // Arrange — create account + transaction + set location
        var accountDto = new AccountCreateDto { Name = "BulkDeleteLocTest", Type = "Checking" };
        var accountResponse = await _client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(account);

        var transactionDto = new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = new MoneyDto { Currency = "USD", Amount = -50.00m },
            Date = new DateOnly(2026, 2, 22),
            Description = "Grocery Store",
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", transactionDto);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(created);

        var locationUpdate = new TransactionLocationUpdateDto
        {
            City = "Austin",
            StateOrRegion = "TX",
            Country = "US",
        };
        var patchResponse = await _client.PatchAsJsonAsync(
            $"/api/v1/transactions/{created.Id}/location",
            locationUpdate);
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        // Act
        var deleteResponse = await _client.DeleteAsync("/api/v1/settings/location-data");

        // Assert
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        var result = await deleteResponse.Content.ReadFromJsonAsync<LocationDataClearedDto>();
        Assert.NotNull(result);
        Assert.True(result.ClearedCount >= 1);
    }

    /// <summary>
    /// DELETE /api/v1/settings/location-data clears persisted location data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Delete_LocationData_ClearsPersistedLocations()
    {
        // Arrange — create account + transaction + set location
        var accountDto = new AccountCreateDto { Name = "BulkClearVerify", Type = "Checking" };
        var accountResponse = await _client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(account);

        var transactionDto = new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = new MoneyDto { Currency = "USD", Amount = -30.00m },
            Date = new DateOnly(2026, 2, 22),
            Description = "Book Store",
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", transactionDto);
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(created);

        var locationUpdate = new TransactionLocationUpdateDto
        {
            City = "Chicago",
            StateOrRegion = "IL",
            Country = "US",
        };
        await _client.PatchAsJsonAsync($"/api/v1/transactions/{created.Id}/location", locationUpdate);

        // Act
        await _client.DeleteAsync("/api/v1/settings/location-data");

        // Assert — verify location is null on the transaction
        var getResponse = await _client.GetAsync($"/api/v1/transactions/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var transaction = await getResponse.Content.ReadFromJsonAsync<TransactionDto>();
        Assert.NotNull(transaction);
        Assert.Null(transaction.Location);
    }
}
