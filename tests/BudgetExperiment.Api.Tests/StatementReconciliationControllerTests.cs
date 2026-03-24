// <copyright file="StatementReconciliationControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Statement Reconciliation API endpoints (Feature 125b).
/// </summary>
[Collection("ApiDb")]
public sealed class StatementReconciliationControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatementReconciliationControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public StatementReconciliationControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
        _client = factory.CreateApiClient();
    }

    /// <summary>
    /// AC-125b-11: POST /complete returns 422 when cleared balance does not match statement balance.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CompleteReconciliation_Returns_422_WhenDifferenceIsNotZero()
    {
        // Arrange
        var account = await CreateTestAccountAsync();
        var txDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));

        // Create two transactions totaling -$50
        var tx1 = await CreateTransactionAsync(account.Id, txDate, -25.00m, "Coffee");
        var tx2 = await CreateTransactionAsync(account.Id, txDate, -25.00m, "Lunch");

        // Set statement balance to $200 (wrong — initial 0 + cleared (-50) = -50 ≠ 200)
        await SetStatementBalanceAsync(account.Id, DateOnly.FromDateTime(DateTime.UtcNow), 200.00m);

        // Mark both transactions as cleared
        var txIds = new List<Guid> { tx1.Id, tx2.Id };
        var bulkClearResp = await _client.PostAsJsonAsync(
            "/api/v1/statement-reconciliation/bulk-clear",
            new BulkMarkClearedRequest { TransactionIds = txIds, ClearedDate = txDate });
        Assert.Equal(HttpStatusCode.OK, bulkClearResp.StatusCode);

        var completeReq = new CompleteReconciliationRequest { AccountId = account.Id };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/statement-reconciliation/complete", completeReq);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    /// <summary>
    /// AC-125b-12: POST /complete returns 201 with ReconciliationRecordDto when balanced.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CompleteReconciliation_Returns_201_WhenBalanced()
    {
        // Arrange
        var account = await CreateTestAccountAsync(initialBalance: 1000m);
        var txDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5));

        var tx = await CreateTransactionAsync(account.Id, txDate, -50.00m, "Groceries");

        // Cleared balance = 1000 + (-50) = 950 — set statement balance to match
        var statementDate = DateOnly.FromDateTime(DateTime.UtcNow);
        await SetStatementBalanceAsync(account.Id, statementDate, 950.00m);

        var bulkClearResp = await _client.PostAsJsonAsync(
            "/api/v1/statement-reconciliation/bulk-clear",
            new BulkMarkClearedRequest { TransactionIds = [tx.Id], ClearedDate = txDate });
        Assert.Equal(HttpStatusCode.OK, bulkClearResp.StatusCode);

        var completeReq = new CompleteReconciliationRequest { AccountId = account.Id };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/statement-reconciliation/complete", completeReq);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var record = await response.Content.ReadFromJsonAsync<ReconciliationRecordDto>();
        Assert.NotNull(record);
        Assert.Equal(account.Id, record.AccountId);
        Assert.Equal(statementDate, record.StatementDate);
        Assert.Equal(950.00m, record.ClearedBalance);
        Assert.Equal(1, record.TransactionCount);
    }

    /// <summary>
    /// AC-125b-13: GET /history?accountId returns paginated records.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetReconciliationHistory_Returns_200_WithRecords()
    {
        // Arrange: create account, complete one reconciliation
        var account = await CreateTestAccountAsync(initialBalance: 500m);
        var txDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3));
        var tx = await CreateTransactionAsync(account.Id, txDate, -100m, "Rent");
        var statementDate = DateOnly.FromDateTime(DateTime.UtcNow);
        await SetStatementBalanceAsync(account.Id, statementDate, 400m);
        await _client.PostAsJsonAsync(
            "/api/v1/statement-reconciliation/bulk-clear",
            new BulkMarkClearedRequest { TransactionIds = [tx.Id], ClearedDate = txDate });
        var completeResp = await _client.PostAsJsonAsync(
            "/api/v1/statement-reconciliation/complete",
            new CompleteReconciliationRequest { AccountId = account.Id });
        Assert.Equal(HttpStatusCode.Created, completeResp.StatusCode);

        // Act
        var response = await _client.GetAsync($"/api/v1/statement-reconciliation/history?accountId={account.Id}&page=1&pageSize=20");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var history = await response.Content.ReadFromJsonAsync<List<ReconciliationRecordDto>>();
        Assert.NotNull(history);
        Assert.NotEmpty(history);
    }

    /// <summary>
    /// AC-125b-17: After completion, bulk-unclear skips reconciled (locked) transactions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BulkUncleared_SkipsLockedTransactions_AfterReconciliation()
    {
        // Arrange
        var account = await CreateTestAccountAsync(initialBalance: 200m);
        var txDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
        var tx = await CreateTransactionAsync(account.Id, txDate, -50m, "Utilities");
        var statementDate = DateOnly.FromDateTime(DateTime.UtcNow);
        await SetStatementBalanceAsync(account.Id, statementDate, 150m);
        await _client.PostAsJsonAsync(
            "/api/v1/statement-reconciliation/bulk-clear",
            new BulkMarkClearedRequest { TransactionIds = [tx.Id], ClearedDate = txDate });
        var completeResp = await _client.PostAsJsonAsync(
            "/api/v1/statement-reconciliation/complete",
            new CompleteReconciliationRequest { AccountId = account.Id });
        Assert.Equal(HttpStatusCode.Created, completeResp.StatusCode);

        // Act: try to bulk-unclear the same (now locked) transaction
        var unclearReq = new BulkMarkUnclearedRequest { TransactionIds = [tx.Id] };
        var unclearResponse = await _client.PostAsJsonAsync("/api/v1/statement-reconciliation/bulk-unclear", unclearReq);

        // Assert: 200 but empty list — locked transactions are skipped
        Assert.Equal(HttpStatusCode.OK, unclearResponse.StatusCode);
        var updated = await unclearResponse.Content.ReadFromJsonAsync<List<TransactionDto>>();
        Assert.NotNull(updated);
        Assert.Empty(updated);
    }

    private async Task<AccountDto> CreateTestAccountAsync(decimal initialBalance = 0m)
    {
        var req = new AccountCreateDto
        {
            Name = $"Test Account {Guid.NewGuid():N}",
            InitialBalance = initialBalance,
            InitialBalanceCurrency = "USD",
            Scope = "Shared",
        };
        var resp = await _client.PostAsJsonAsync("/api/v1/accounts", req);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<AccountDto>())!;
    }

    private async Task<TransactionDto> CreateTransactionAsync(Guid accountId, DateOnly date, decimal amount, string description)
    {
        var req = new TransactionCreateDto
        {
            AccountId = accountId,
            Amount = new MoneyDto { Amount = amount, Currency = "USD" },
            Date = date,
            Description = description,
        };
        var resp = await _client.PostAsJsonAsync("/api/v1/transactions", req);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<TransactionDto>())!;
    }

    private async Task SetStatementBalanceAsync(Guid accountId, DateOnly statementDate, decimal balance)
    {
        var req = new SetStatementBalanceRequest
        {
            AccountId = accountId,
            StatementDate = statementDate,
            Balance = balance,
        };
        var resp = await _client.PostAsJsonAsync("/api/v1/statement-reconciliation/statement-balance", req);
        resp.EnsureSuccessStatusCode();
    }
}
