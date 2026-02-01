// <copyright file="ReportsControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Reports API endpoints.
/// </summary>
public sealed class ReportsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportsControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public ReportsControllerTests(CustomWebApplicationFactory factory)
    {
        this._client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/reports/categories/monthly returns 200 OK with empty report when no transactions exist in that month.
    /// </summary>
    [Fact]
    public async Task GetMonthlyCategoryReport_Returns_200_WithEmptyReport()
    {
        // Act - use December 2030 which should have no seed data
        var response = await this._client.GetAsync("/api/v1/reports/categories/monthly?year=2030&month=12");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<MonthlyCategoryReportDto>();
        Assert.NotNull(report);
        Assert.Equal(2030, report.Year);
        Assert.Equal(12, report.Month);
        Assert.Equal(0m, report.TotalSpending.Amount);
        Assert.Equal(0m, report.TotalIncome.Amount);
        Assert.Empty(report.Categories);
    }

    /// <summary>
    /// GET /api/v1/reports/categories/monthly returns 400 for invalid month.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public async Task GetMonthlyCategoryReport_Returns_400_ForInvalidMonth(int month)
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/reports/categories/monthly?year=2026&month={month}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/reports/categories/monthly returns 400 for invalid year.
    /// </summary>
    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public async Task GetMonthlyCategoryReport_Returns_400_ForInvalidYear(int year)
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/reports/categories/monthly?year={year}&month=1");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/reports/categories/monthly returns spending data for transactions in the month.
    /// </summary>
    [Fact]
    public async Task GetMonthlyCategoryReport_Returns_SpendingData_ForTransactions()
    {
        // Arrange - create account, category, and transactions
        // Use July 2027 which should have no seed data
        var accountDto = new AccountCreateDto
        {
            Name = "Report Test Account",
            Type = "Checking",
            InitialBalance = 1000m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2027, 7, 1),
        };
        var accountResponse = await this._client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();

        var categoryDto = new BudgetCategoryCreateDto { Name = "Report Test Category", Type = "Expense" };
        var categoryResponse = await this._client.PostAsJsonAsync("/api/v1/categories", categoryDto);
        var category = await categoryResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        // Create expense transactions in July 2027
        var transaction1 = new TransactionCreateDto
        {
            AccountId = account!.Id,
            Amount = new MoneyDto { Amount = -50m, Currency = "USD" },
            Date = new DateOnly(2027, 7, 5),
            Description = "Test expense 1",
            CategoryId = category!.Id,
        };
        await this._client.PostAsJsonAsync("/api/v1/transactions", transaction1);

        var transaction2 = new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = new MoneyDto { Amount = -75m, Currency = "USD" },
            Date = new DateOnly(2027, 7, 15),
            Description = "Test expense 2",
            CategoryId = category.Id,
        };
        await this._client.PostAsJsonAsync("/api/v1/transactions", transaction2);

        // Act
        var response = await this._client.GetAsync("/api/v1/reports/categories/monthly?year=2027&month=7");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<MonthlyCategoryReportDto>();
        Assert.NotNull(report);
        Assert.Equal(125m, report.TotalSpending.Amount);

        var categorySpending = report.Categories.FirstOrDefault(c => c.CategoryId == category.Id);
        Assert.NotNull(categorySpending);
        Assert.Equal("Report Test Category", categorySpending.CategoryName);
        Assert.Equal(125m, categorySpending.Amount.Amount);
        Assert.Equal(2, categorySpending.TransactionCount);
        Assert.Equal(100m, categorySpending.Percentage);
    }

    /// <summary>
    /// GET /api/v1/reports/categories/monthly separates income from expenses.
    /// </summary>
    [Fact]
    public async Task GetMonthlyCategoryReport_Separates_Income_FromExpenses()
    {
        // Arrange - create account and categories
        // Use a future date (December 2028) to avoid conflicts with seed data
        var accountDto = new AccountCreateDto
        {
            Name = "Income Test Account",
            Type = "Checking",
            InitialBalance = 0m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2028, 12, 1),
        };
        var accountResponse = await this._client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();

        var incomeCategoryDto = new BudgetCategoryCreateDto { Name = "Salary Category", Type = "Income" };
        var incomeCategoryResponse = await this._client.PostAsJsonAsync("/api/v1/categories", incomeCategoryDto);
        var incomeCategory = await incomeCategoryResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        var expenseCategoryDto = new BudgetCategoryCreateDto { Name = "Expense Category", Type = "Expense" };
        var expenseCategoryResponse = await this._client.PostAsJsonAsync("/api/v1/categories", expenseCategoryDto);
        var expenseCategory = await expenseCategoryResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        // Create income and expense transactions in December 2028
        var incomeTransaction = new TransactionCreateDto
        {
            AccountId = account!.Id,
            Amount = new MoneyDto { Amount = 3000m, Currency = "USD" },
            Date = new DateOnly(2028, 12, 1),
            Description = "Paycheck",
            CategoryId = incomeCategory!.Id,
        };
        await this._client.PostAsJsonAsync("/api/v1/transactions", incomeTransaction);

        var expenseTransaction = new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = new MoneyDto { Amount = -200m, Currency = "USD" },
            Date = new DateOnly(2028, 12, 10),
            Description = "Bills",
            CategoryId = expenseCategory!.Id,
        };
        await this._client.PostAsJsonAsync("/api/v1/transactions", expenseTransaction);

        // Act
        var response = await this._client.GetAsync("/api/v1/reports/categories/monthly?year=2028&month=12");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<MonthlyCategoryReportDto>();
        Assert.NotNull(report);
        Assert.Equal(3000m, report.TotalIncome.Amount);
        Assert.Equal(200m, report.TotalSpending.Amount);

        // Categories should only include expense categories
        Assert.Single(report.Categories);
        Assert.Equal("Expense Category", report.Categories.First().CategoryName);
    }
}
