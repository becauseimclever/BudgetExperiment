// <copyright file="KakeiboReportControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Api.Tests.Reports;

/// <summary>
/// Integration tests for the <c>GET /api/v1/reports/kakeibo</c> endpoint.
/// Uses WebApplicationFactory backed by a real PostgreSQL Testcontainer.
/// Feature flag: <c>Kakeibo:DateRangeReports</c>.
/// </summary>
[Collection("ApiDb")]
public sealed class KakeiboReportControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="KakeiboReportControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory backed by a PostgreSQL Testcontainer.</param>
    public KakeiboReportControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
        _client = factory.CreateApiClient();
    }

    // ===== Feature Flag =====
    [Fact]
    public async Task GetKakeibo_FeatureFlagDisabled_Returns404()
    {
        // Arrange — ensure feature flag is OFF
        EnsureFeatureFlag("Kakeibo:DateRangeReports", isEnabled: false);

        // Act
        var response = await _client.GetAsync("/api/v1/reports/kakeibo?from=2026-03-01&to=2026-03-31");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ===== Happy Path =====
    [Fact]
    public async Task GetKakeibo_ValidDateRange_Returns200WithSummary()
    {
        // Arrange — enable flag, create account + category + transactions
        EnsureFeatureFlag("Kakeibo:DateRangeReports", isEnabled: true);

        var account = await CreateAccountAsync("Kakeibo Test Account");
        var category = await CreateExpenseCategoryAsync("Groceries", kakeiboCategory: "Essentials");

        await CreateTransactionAsync(account.Id, -50m, new DateOnly(2030, 1, 10), "Supermarket", category.Id);
        await CreateTransactionAsync(account.Id, -30m, new DateOnly(2030, 1, 20), "Pharmacy", category.Id);

        // Act
        var response = await _client.GetAsync("/api/v1/reports/kakeibo?from=2030-01-01&to=2030-01-31");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<KakeiboSummary>();
        Assert.NotNull(summary);
        Assert.Equal(new DateOnly(2030, 1, 1), summary.DateRange.From);
        Assert.Equal(new DateOnly(2030, 1, 31), summary.DateRange.To);
        Assert.True(summary.MonthlyTotals.ContainsKey(KakeiboCategory.Essentials));
        Assert.True(summary.MonthlyTotals.ContainsKey(KakeiboCategory.Wants));
        Assert.True(summary.MonthlyTotals.ContainsKey(KakeiboCategory.Culture));
        Assert.True(summary.MonthlyTotals.ContainsKey(KakeiboCategory.Unexpected));
    }

    // ===== Validation =====
    [Fact]
    public async Task GetKakeibo_FromGreaterThanTo_Returns400()
    {
        // Arrange — enable flag so we reach validation logic
        EnsureFeatureFlag("Kakeibo:DateRangeReports", isEnabled: true);

        // Act
        var response = await _client.GetAsync("/api/v1/reports/kakeibo?from=2026-03-31&to=2026-03-01");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetKakeibo_MissingFromParameter_Returns400()
    {
        // Arrange
        EnsureFeatureFlag("Kakeibo:DateRangeReports", isEnabled: true);

        // Act
        var response = await _client.GetAsync("/api/v1/reports/kakeibo?to=2026-03-31");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetKakeibo_MissingToParameter_Returns400()
    {
        // Arrange
        EnsureFeatureFlag("Kakeibo:DateRangeReports", isEnabled: true);

        // Act
        var response = await _client.GetAsync("/api/v1/reports/kakeibo?from=2026-03-01");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ===== Account Filter =====
    [Fact]
    public async Task GetKakeibo_WithValidAccountId_Returns200()
    {
        // Arrange
        EnsureFeatureFlag("Kakeibo:DateRangeReports", isEnabled: true);

        var account = await CreateAccountAsync("Kakeibo Account Filter Test");

        // Act
        var response = await _client.GetAsync(
            $"/api/v1/reports/kakeibo?from=2031-01-01&to=2031-01-31&accountId={account.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<KakeiboSummary>();
        Assert.NotNull(summary);
    }

    // ===== Helpers =====
    private void EnsureFeatureFlag(string name, bool isEnabled)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();

        // Upsert — ON CONFLICT updates to desired state
#pragma warning disable EF1002 // name is a fixed string, not user input
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
    }

    private async Task<AccountDto> CreateAccountAsync(string name)
    {
        var dto = new AccountCreateDto
        {
            Name = name,
            Type = "Checking",
            InitialBalance = 1000m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2030, 1, 1),
        };
        var response = await _client.PostAsJsonAsync("/api/v1/accounts", dto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AccountDto>())!;
    }

    private async Task<BudgetCategoryDto> CreateExpenseCategoryAsync(string name, string? kakeiboCategory = null)
    {
        var dto = new BudgetCategoryCreateDto
        {
            Name = name,
            Type = "Expense",
            KakeiboCategory = kakeiboCategory,
        };
        var response = await _client.PostAsJsonAsync("/api/v1/categories", dto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BudgetCategoryDto>())!;
    }

    private async Task CreateTransactionAsync(
        Guid accountId,
        decimal amount,
        DateOnly date,
        string description,
        Guid? categoryId)
    {
        var dto = new TransactionCreateDto
        {
            AccountId = accountId,
            Amount = new MoneyDto { Amount = amount, Currency = "USD" },
            Date = date,
            Description = description,
            CategoryId = categoryId,
        };
        var response = await _client.PostAsJsonAsync("/api/v1/transactions", dto);
        response.EnsureSuccessStatusCode();
    }
}
