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

    // ===== GET /api/v1/reports/categories/range =====

    /// <summary>
    /// GET categories/range returns 200 with empty report when no transactions exist.
    /// </summary>
    [Fact]
    public async Task GetCategoryReportByRange_Returns_200_WithEmptyReport()
    {
        // Act - use a far-future date range with no data
        var response = await this._client.GetAsync(
            "/api/v1/reports/categories/range?startDate=2035-06-01&endDate=2035-06-30");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<DateRangeCategoryReportDto>();
        Assert.NotNull(report);
        Assert.Equal(new DateOnly(2035, 6, 1), report.StartDate);
        Assert.Equal(new DateOnly(2035, 6, 30), report.EndDate);
        Assert.Equal(0m, report.TotalSpending.Amount);
        Assert.Empty(report.Categories);
    }

    /// <summary>
    /// GET categories/range returns 400 when end date is before start date.
    /// </summary>
    [Fact]
    public async Task GetCategoryReportByRange_Returns_400_WhenEndBeforeStart()
    {
        // Act
        var response = await this._client.GetAsync(
            "/api/v1/reports/categories/range?startDate=2026-02-15&endDate=2026-01-15");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET categories/range returns 400 when range exceeds one year.
    /// </summary>
    [Fact]
    public async Task GetCategoryReportByRange_Returns_400_WhenRangeExceedsOneYear()
    {
        // Act
        var response = await this._client.GetAsync(
            "/api/v1/reports/categories/range?startDate=2025-01-01&endDate=2026-06-01");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET categories/range returns spending data for transactions in range.
    /// </summary>
    [Fact]
    public async Task GetCategoryReportByRange_Returns_SpendingData()
    {
        // Arrange
        var accountDto = new AccountCreateDto
        {
            Name = "Range Report Account",
            Type = "Checking",
            InitialBalance = 5000m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2029, 3, 1),
        };
        var accountResponse = await this._client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();

        var categoryDto = new BudgetCategoryCreateDto { Name = "Range Test Category", Type = "Expense" };
        var categoryResponse = await this._client.PostAsJsonAsync("/api/v1/categories", categoryDto);
        var category = await categoryResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        await this._client.PostAsJsonAsync("/api/v1/transactions", new TransactionCreateDto
        {
            AccountId = account!.Id,
            Amount = new MoneyDto { Amount = -60m, Currency = "USD" },
            Date = new DateOnly(2029, 3, 15),
            Description = "Range expense 1",
            CategoryId = category!.Id,
        });

        await this._client.PostAsJsonAsync("/api/v1/transactions", new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = new MoneyDto { Amount = -40m, Currency = "USD" },
            Date = new DateOnly(2029, 4, 5),
            Description = "Range expense 2",
            CategoryId = category.Id,
        });

        // Act - range spanning two months
        var response = await this._client.GetAsync(
            "/api/v1/reports/categories/range?startDate=2029-03-01&endDate=2029-04-30");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<DateRangeCategoryReportDto>();
        Assert.NotNull(report);
        Assert.Equal(100m, report.TotalSpending.Amount);
        Assert.Single(report.Categories);
        Assert.Equal(category.Id, report.Categories.First().CategoryId);
    }

    // ===== GET /api/v1/reports/trends =====

    /// <summary>
    /// GET trends returns 200 with empty monthly data when no transactions exist.
    /// </summary>
    [Fact]
    public async Task GetSpendingTrends_Returns_200_WithEmptyData()
    {
        // Act
        var response = await this._client.GetAsync(
            "/api/v1/reports/trends?months=3&endYear=2040&endMonth=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<SpendingTrendsReportDto>();
        Assert.NotNull(report);
        Assert.Equal(3, report.MonthlyData.Count);
        Assert.All(report.MonthlyData, m => Assert.Equal(0m, m.TotalSpending.Amount));
        Assert.Equal("stable", report.TrendDirection);
    }

    /// <summary>
    /// GET trends returns 400 for invalid months parameter.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(-1)]
    public async Task GetSpendingTrends_Returns_400_ForInvalidMonths(int months)
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/reports/trends?months={months}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET trends returns 400 for invalid endMonth.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task GetSpendingTrends_Returns_400_ForInvalidEndMonth(int endMonth)
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/reports/trends?months=6&endMonth={endMonth}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ===== GET /api/v1/reports/day-summary/{date} =====

    /// <summary>
    /// GET day-summary returns 200 with zero totals when no transactions.
    /// </summary>
    [Fact]
    public async Task GetDaySummary_Returns_200_WithEmptyDay()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/reports/day-summary/2040-01-15");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<DaySummaryDto>();
        Assert.NotNull(summary);
        Assert.Equal(new DateOnly(2040, 1, 15), summary.Date);
        Assert.Equal(0m, summary.TotalSpending.Amount);
        Assert.Equal(0m, summary.TotalIncome.Amount);
        Assert.Equal(0, summary.TransactionCount);
        Assert.Empty(summary.TopCategories);
    }

    /// <summary>
    /// GET day-summary returns spending/income data for a day with transactions.
    /// </summary>
    [Fact]
    public async Task GetDaySummary_Returns_SpendingData()
    {
        // Arrange
        var accountDto = new AccountCreateDto
        {
            Name = "Day Summary Account",
            Type = "Checking",
            InitialBalance = 5000m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2031, 5, 1),
        };
        var accountResponse = await this._client.PostAsJsonAsync("/api/v1/accounts", accountDto);
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountDto>();

        var categoryDto = new BudgetCategoryCreateDto { Name = "Day Summary Category", Type = "Expense" };
        var categoryResponse = await this._client.PostAsJsonAsync("/api/v1/categories", categoryDto);
        var category = await categoryResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        await this._client.PostAsJsonAsync("/api/v1/transactions", new TransactionCreateDto
        {
            AccountId = account!.Id,
            Amount = new MoneyDto { Amount = -45m, Currency = "USD" },
            Date = new DateOnly(2031, 5, 15),
            Description = "Day expense",
            CategoryId = category!.Id,
        });

        await this._client.PostAsJsonAsync("/api/v1/transactions", new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = new MoneyDto { Amount = 2000m, Currency = "USD" },
            Date = new DateOnly(2031, 5, 15),
            Description = "Day income",
        });

        // Act
        var response = await this._client.GetAsync("/api/v1/reports/day-summary/2031-05-15");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<DaySummaryDto>();
        Assert.NotNull(summary);
        Assert.Equal(45m, summary.TotalSpending.Amount);
        Assert.Equal(2000m, summary.TotalIncome.Amount);
        Assert.Equal(1955m, summary.NetAmount.Amount);
        Assert.Equal(2, summary.TransactionCount);
        Assert.Single(summary.TopCategories);
        Assert.Equal("Day Summary Category", summary.TopCategories.First().CategoryName);
    }

    // ===== GET /api/v1/reports/budget-comparison =====

    /// <summary>
    /// GET budget-comparison returns 200 with summary when no budget goals exist.
    /// </summary>
    [Fact]
    public async Task GetBudgetComparison_Returns_200()
    {
        // Act - month with no goals set
        var response = await this._client.GetAsync(
            "/api/v1/reports/budget-comparison?year=2040&month=1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<BudgetSummaryDto>();
        Assert.NotNull(summary);
        Assert.Equal(2040, summary.Year);
        Assert.Equal(1, summary.Month);
    }

    /// <summary>
    /// GET budget-comparison returns 400 for invalid month.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task GetBudgetComparison_Returns_400_ForInvalidMonth(int month)
    {
        // Act
        var response = await this._client.GetAsync(
            $"/api/v1/reports/budget-comparison?year=2026&month={month}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET budget-comparison returns 400 for invalid year.
    /// </summary>
    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public async Task GetBudgetComparison_Returns_400_ForInvalidYear(int year)
    {
        // Act
        var response = await this._client.GetAsync(
            $"/api/v1/reports/budget-comparison?year={year}&month=1");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
