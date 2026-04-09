// <copyright file="TransferDeletionControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Application.FeatureFlags;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Api.Tests.Transfers;

/// <summary>
/// Integration tests for the feature-gated <c>DELETE /api/v1/transfers/{transferId}</c> endpoint
/// introduced in Feature 146 (atomic transfer deletion with orphan protection).
/// Feature flag: <c>feature-transfer-atomic-deletion</c>.
/// </summary>
[Collection("ApiDb")]
public sealed class TransferDeletionControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string FeatureFlagName = "feature-transfer-atomic-deletion";

    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransferDeletionControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory backed by a PostgreSQL Testcontainer.</param>
    public TransferDeletionControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
        _client = factory.CreateApiClient();
    }

    // ===== Feature Flag =====

    /// <summary>
    /// Feature flag <c>feature-transfer-atomic-deletion</c> disabled → endpoint returns 403 Forbidden.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteTransfer_FeatureFlagDisabled_Returns403()
    {
        // Arrange
        EnsureFeatureFlag(FeatureFlagName, isEnabled: false);

        // Act
        var response = await _client.DeleteAsync($"/api/v1/transfers/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ===== Happy Path =====

    /// <summary>
    /// Feature flag enabled and a known transfer ID → endpoint atomically removes both legs
    /// and returns 204 No Content.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteTransfer_ValidTransferId_Returns204()
    {
        // Arrange
        EnsureFeatureFlag(FeatureFlagName, isEnabled: true);

        var sourceAccount = await CreateAccountAsync("Delete Source Account");
        var destAccount = await CreateAccountAsync("Delete Dest Account");
        var transfer = await CreateTransferAsync(sourceAccount.Id, destAccount.Id, 100m);

        // Act
        var response = await _client.DeleteAsync($"/api/v1/transfers/{transfer.TransferId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ===== Not Found =====

    /// <summary>
    /// Feature flag enabled, unknown transfer ID → endpoint returns 404 Not Found.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteTransfer_UnknownTransferId_Returns404()
    {
        // Arrange
        EnsureFeatureFlag(FeatureFlagName, isEnabled: true);

        // Act
        var response = await _client.DeleteAsync($"/api/v1/transfers/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ===== Validation =====

    /// <summary>
    /// Non-GUID path segment → the <c>:guid</c> route constraint does not match,
    /// so the endpoint returns 404 Not Found.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteTransfer_InvalidGuid_Returns404()
    {
        // Arrange
        EnsureFeatureFlag(FeatureFlagName, isEnabled: true);

        // Act
        var response = await _client.DeleteAsync("/api/v1/transfers/not-a-valid-guid");

        // Assert — the :guid route constraint returns 404 when the segment is not a valid GUID
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ===== Helpers =====
    private void EnsureFeatureFlag(string name, bool isEnabled)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();

#pragma warning disable EF1002 // name is a fixed string constant, not user input
        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO "FeatureFlags" ("Name", "IsEnabled", "UpdatedAtUtc")
            VALUES ({0}, {1}, {2})
            ON CONFLICT ("Name") DO UPDATE SET "IsEnabled" = {1}, "UpdatedAtUtc" = {2}
            """,
            name,
            isEnabled,
            DateTime.UtcNow);
#pragma warning restore EF1002

        // Invalidate the cached flags so the next request reads fresh values from DB
        var featureFlagService = scope.ServiceProvider.GetRequiredService<IFeatureFlagService>();
        featureFlagService.SetFlagAsync(name, isEnabled).GetAwaiter().GetResult();
    }

    private async Task<AccountDto> CreateAccountAsync(string name)
    {
        var dto = new AccountCreateDto
        {
            Name = name,
            Type = "Checking",
            InitialBalance = 1000m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2060, 1, 1),
        };
        var response = await _client.PostAsJsonAsync("/api/v1/accounts", dto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AccountDto>())!;
    }

    private async Task<TransferResponse> CreateTransferAsync(Guid sourceId, Guid destId, decimal amount)
    {
        var dto = new CreateTransferRequest
        {
            SourceAccountId = sourceId,
            DestinationAccountId = destId,
            Amount = amount,
            Currency = "USD",
            Date = new DateOnly(2060, 3, 1),
            Description = "Test transfer for deletion",
        };
        var response = await _client.PostAsJsonAsync("/api/v1/transfers", dto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TransferResponse>())!;
    }
}
