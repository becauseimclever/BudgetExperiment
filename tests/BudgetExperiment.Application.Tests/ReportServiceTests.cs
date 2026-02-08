// <copyright file="ReportServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for ReportService.
/// </summary>
public class ReportServiceTests
{
    [Fact]
    public async Task GetMonthlyCategoryReportAsync_Returns_Empty_Report_When_No_Transactions()
    {
        // Arrange
        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            null,
            default)).ReturnsAsync(new List<Transaction>());
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetMonthlyCategoryReportAsync(2026, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2026, result.Year);
        Assert.Equal(1, result.Month);
        Assert.Equal(0m, result.TotalSpending.Amount);
        Assert.Equal(0m, result.TotalIncome.Amount);
        Assert.Empty(result.Categories);
    }

    [Fact]
    public async Task GetMonthlyCategoryReportAsync_Aggregates_Spending_By_Category()
    {
        // Arrange
        var groceriesCategory = BudgetCategory.Create("Groceries", CategoryType.Expense, color: "#10B981");
        var entertainmentCategory = BudgetCategory.Create("Entertainment", CategoryType.Expense, color: "#8B5CF6");

        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateOnly(2026, 1, 5), -50m, groceriesCategory.Id),
            CreateTransaction(new DateOnly(2026, 1, 10), -30m, groceriesCategory.Id),
            CreateTransaction(new DateOnly(2026, 1, 15), -100m, entertainmentCategory.Id),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();

        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            null,
            default)).ReturnsAsync(transactions);

        categoryRepo.Setup(r => r.GetByIdAsync(groceriesCategory.Id, default)).ReturnsAsync(groceriesCategory);
        categoryRepo.Setup(r => r.GetByIdAsync(entertainmentCategory.Id, default)).ReturnsAsync(entertainmentCategory);

        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetMonthlyCategoryReportAsync(2026, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2026, result.Year);
        Assert.Equal(1, result.Month);
        Assert.Equal(180m, result.TotalSpending.Amount);
        Assert.Equal(2, result.Categories.Count);

        var groceriesSpending = result.Categories.FirstOrDefault(c => c.CategoryId == groceriesCategory.Id);
        Assert.NotNull(groceriesSpending);
        Assert.Equal("Groceries", groceriesSpending.CategoryName);
        Assert.Equal(80m, groceriesSpending.Amount.Amount);
        Assert.Equal(2, groceriesSpending.TransactionCount);
        Assert.Equal("#10B981", groceriesSpending.CategoryColor);

        var entertainmentSpending = result.Categories.FirstOrDefault(c => c.CategoryId == entertainmentCategory.Id);
        Assert.NotNull(entertainmentSpending);
        Assert.Equal("Entertainment", entertainmentSpending.CategoryName);
        Assert.Equal(100m, entertainmentSpending.Amount.Amount);
        Assert.Equal(1, entertainmentSpending.TransactionCount);
    }

    [Fact]
    public async Task GetMonthlyCategoryReportAsync_Calculates_Percentages_Correctly()
    {
        // Arrange
        var category1 = BudgetCategory.Create("Category1", CategoryType.Expense);
        var category2 = BudgetCategory.Create("Category2", CategoryType.Expense);

        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateOnly(2026, 1, 5), -25m, category1.Id),
            CreateTransaction(new DateOnly(2026, 1, 10), -75m, category2.Id),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();

        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            null,
            default)).ReturnsAsync(transactions);

        categoryRepo.Setup(r => r.GetByIdAsync(category1.Id, default)).ReturnsAsync(category1);
        categoryRepo.Setup(r => r.GetByIdAsync(category2.Id, default)).ReturnsAsync(category2);

        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetMonthlyCategoryReportAsync(2026, 1);

        // Assert
        Assert.Equal(100m, result.TotalSpending.Amount);

        var cat1Spending = result.Categories.First(c => c.CategoryId == category1.Id);
        Assert.Equal(25m, cat1Spending.Percentage);

        var cat2Spending = result.Categories.First(c => c.CategoryId == category2.Id);
        Assert.Equal(75m, cat2Spending.Percentage);
    }

    [Fact]
    public async Task GetMonthlyCategoryReportAsync_Groups_Uncategorized_Transactions()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateOnly(2026, 1, 5), -50m, null),
            CreateTransaction(new DateOnly(2026, 1, 10), -30m, null),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();

        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            null,
            default)).ReturnsAsync(transactions);

        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetMonthlyCategoryReportAsync(2026, 1);

        // Assert
        Assert.Equal(80m, result.TotalSpending.Amount);
        Assert.Single(result.Categories);

        var uncategorized = result.Categories.First();
        Assert.Null(uncategorized.CategoryId);
        Assert.Equal("Uncategorized", uncategorized.CategoryName);
        Assert.Equal(80m, uncategorized.Amount.Amount);
        Assert.Equal(2, uncategorized.TransactionCount);
        Assert.Equal(100m, uncategorized.Percentage);
    }

    [Fact]
    public async Task GetMonthlyCategoryReportAsync_Separates_Income_From_Expenses()
    {
        // Arrange
        var incomeCategory = BudgetCategory.Create("Salary", CategoryType.Income);
        var expenseCategory = BudgetCategory.Create("Groceries", CategoryType.Expense);

        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateOnly(2026, 1, 1), 3000m, incomeCategory.Id),
            CreateTransaction(new DateOnly(2026, 1, 5), -100m, expenseCategory.Id),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();

        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            null,
            default)).ReturnsAsync(transactions);

        categoryRepo.Setup(r => r.GetByIdAsync(incomeCategory.Id, default)).ReturnsAsync(incomeCategory);
        categoryRepo.Setup(r => r.GetByIdAsync(expenseCategory.Id, default)).ReturnsAsync(expenseCategory);

        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetMonthlyCategoryReportAsync(2026, 1);

        // Assert
        Assert.Equal(100m, result.TotalSpending.Amount);
        Assert.Equal(3000m, result.TotalIncome.Amount);

        // Categories should only include expense categories by default
        Assert.Single(result.Categories);
        Assert.Equal("Groceries", result.Categories.First().CategoryName);
    }

    [Fact]
    public async Task GetMonthlyCategoryReportAsync_Excludes_Transfers()
    {
        // Arrange
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        var transferId = Guid.NewGuid();

        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateOnly(2026, 1, 5), -100m, category.Id),
            CreateTransfer(new DateOnly(2026, 1, 10), -500m, transferId),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();

        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            null,
            default)).ReturnsAsync(transactions);

        categoryRepo.Setup(r => r.GetByIdAsync(category.Id, default)).ReturnsAsync(category);

        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetMonthlyCategoryReportAsync(2026, 1);

        // Assert
        Assert.Equal(100m, result.TotalSpending.Amount);
        Assert.Single(result.Categories);
    }

    [Fact]
    public async Task GetMonthlyCategoryReportAsync_Orders_Categories_By_Amount_Descending()
    {
        // Arrange
        var smallCategory = BudgetCategory.Create("Small", CategoryType.Expense);
        var largeCategory = BudgetCategory.Create("Large", CategoryType.Expense);
        var mediumCategory = BudgetCategory.Create("Medium", CategoryType.Expense);

        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateOnly(2026, 1, 5), -10m, smallCategory.Id),
            CreateTransaction(new DateOnly(2026, 1, 10), -100m, largeCategory.Id),
            CreateTransaction(new DateOnly(2026, 1, 15), -50m, mediumCategory.Id),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();

        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            null,
            default)).ReturnsAsync(transactions);

        categoryRepo.Setup(r => r.GetByIdAsync(smallCategory.Id, default)).ReturnsAsync(smallCategory);
        categoryRepo.Setup(r => r.GetByIdAsync(largeCategory.Id, default)).ReturnsAsync(largeCategory);
        categoryRepo.Setup(r => r.GetByIdAsync(mediumCategory.Id, default)).ReturnsAsync(mediumCategory);

        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetMonthlyCategoryReportAsync(2026, 1);

        // Assert
        Assert.Equal(3, result.Categories.Count);
        Assert.Equal("Large", result.Categories[0].CategoryName);
        Assert.Equal("Medium", result.Categories[1].CategoryName);
        Assert.Equal("Small", result.Categories[2].CategoryName);
    }

    [Fact]
    public async Task GetMonthlyCategoryReportAsync_Handles_February_Correctly()
    {
        // Arrange - February 2026 has 28 days
        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();

        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 2, 28),
            null,
            default)).ReturnsAsync(new List<Transaction>());

        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetMonthlyCategoryReportAsync(2026, 2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Month);

        // Verify the correct date range was used
        transactionRepo.Verify(r => r.GetByDateRangeAsync(
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 2, 28),
            null,
            default), Times.Once);
    }

    // ===== GetCategoryReportByRangeAsync Tests =====

    [Fact]
    public async Task GetCategoryReportByRangeAsync_Returns_Empty_Report_When_No_Transactions()
    {
        // Arrange
        var startDate = new DateOnly(2026, 1, 15);
        var endDate = new DateOnly(2026, 2, 15);
        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(startDate, endDate, null, default))
            .ReturnsAsync(new List<Transaction>());
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetCategoryReportByRangeAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(startDate, result.StartDate);
        Assert.Equal(endDate, result.EndDate);
        Assert.Equal(0m, result.TotalSpending.Amount);
        Assert.Equal(0m, result.TotalIncome.Amount);
        Assert.Empty(result.Categories);
    }

    [Fact]
    public async Task GetCategoryReportByRangeAsync_Aggregates_Spending_By_Category()
    {
        // Arrange
        var startDate = new DateOnly(2026, 1, 15);
        var endDate = new DateOnly(2026, 2, 15);
        var groceries = BudgetCategory.Create("Groceries", CategoryType.Expense, color: "#10B981");
        var dining = BudgetCategory.Create("Dining", CategoryType.Expense, color: "#EF4444");

        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateOnly(2026, 1, 20), -60m, groceries.Id),
            CreateTransaction(new DateOnly(2026, 2, 1), -40m, groceries.Id),
            CreateTransaction(new DateOnly(2026, 2, 10), -100m, dining.Id),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(startDate, endDate, null, default))
            .ReturnsAsync(transactions);
        categoryRepo.Setup(r => r.GetByIdAsync(groceries.Id, default)).ReturnsAsync(groceries);
        categoryRepo.Setup(r => r.GetByIdAsync(dining.Id, default)).ReturnsAsync(dining);

        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetCategoryReportByRangeAsync(startDate, endDate);

        // Assert
        Assert.Equal(200m, result.TotalSpending.Amount);
        Assert.Equal(2, result.Categories.Count);

        // Both categories: Dining=100, Groceries=100. Order is stable but may vary when equal.
        var diningSpending = result.Categories.First(c => c.CategoryName == "Dining");
        Assert.Equal(100m, diningSpending.Amount.Amount);
        var groceriesSpending = result.Categories.First(c => c.CategoryName == "Groceries");
        Assert.Equal(100m, groceriesSpending.Amount.Amount);
    }

    [Fact]
    public async Task GetCategoryReportByRangeAsync_Filters_By_AccountId()
    {
        // Arrange
        var startDate = new DateOnly(2026, 3, 1);
        var endDate = new DateOnly(2026, 3, 31);
        var accountId = Guid.NewGuid();
        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(startDate, endDate, accountId, default))
            .ReturnsAsync(new List<Transaction>());
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetCategoryReportByRangeAsync(startDate, endDate, accountId);

        // Assert
        Assert.NotNull(result);
        transactionRepo.Verify(r => r.GetByDateRangeAsync(startDate, endDate, accountId, default), Times.Once);
    }

    [Fact]
    public async Task GetCategoryReportByRangeAsync_Excludes_Transfers()
    {
        // Arrange
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 1, 31);
        var category = BudgetCategory.Create("Food", CategoryType.Expense);

        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateOnly(2026, 1, 5), -50m, category.Id),
            CreateTransfer(new DateOnly(2026, 1, 10), -200m, Guid.NewGuid()),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(startDate, endDate, null, default))
            .ReturnsAsync(transactions);
        categoryRepo.Setup(r => r.GetByIdAsync(category.Id, default)).ReturnsAsync(category);
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetCategoryReportByRangeAsync(startDate, endDate);

        // Assert
        Assert.Equal(50m, result.TotalSpending.Amount);
        Assert.Single(result.Categories);
    }

    // ===== GetSpendingTrendsAsync Tests =====

    [Fact]
    public async Task GetSpendingTrendsAsync_Returns_Empty_Data_When_No_Transactions()
    {
        // Arrange
        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), null, default))
            .ReturnsAsync(new List<Transaction>());
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetSpendingTrendsAsync(6, 2026, 6);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(6, result.MonthlyData.Count);
        Assert.All(result.MonthlyData, m => Assert.Equal(0m, m.TotalSpending.Amount));
        Assert.Equal(0m, result.AverageMonthlySpending.Amount);
        Assert.Equal("stable", result.TrendDirection);
    }

    [Fact]
    public async Task GetSpendingTrendsAsync_Aggregates_Monthly_Totals()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateOnly(2026, 1, 5), -100m, null),
            CreateTransaction(new DateOnly(2026, 1, 20), -50m, null),
            CreateTransaction(new DateOnly(2026, 1, 10), 3000m, null), // income
            CreateTransaction(new DateOnly(2026, 2, 5), -200m, null),
            CreateTransaction(new DateOnly(2026, 2, 15), 3500m, null), // income
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), null, default))
            .ReturnsAsync(transactions);
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetSpendingTrendsAsync(2, 2026, 2);

        // Assert
        Assert.Equal(2, result.MonthlyData.Count);

        var jan = result.MonthlyData.First(m => m.Month == 1);
        Assert.Equal(150m, jan.TotalSpending.Amount);
        Assert.Equal(3000m, jan.TotalIncome.Amount);
        Assert.Equal(2850m, jan.NetAmount.Amount);
        Assert.Equal(3, jan.TransactionCount);

        var feb = result.MonthlyData.First(m => m.Month == 2);
        Assert.Equal(200m, feb.TotalSpending.Amount);
        Assert.Equal(3500m, feb.TotalIncome.Amount);
        Assert.Equal(3300m, feb.NetAmount.Amount);
        Assert.Equal(2, feb.TransactionCount);
    }

    [Fact]
    public async Task GetSpendingTrendsAsync_Filters_By_Category()
    {
        // Arrange
        var groceries = BudgetCategory.Create("Groceries", CategoryType.Expense);
        var dining = BudgetCategory.Create("Dining", CategoryType.Expense);

        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateOnly(2026, 1, 5), -100m, groceries.Id),
            CreateTransaction(new DateOnly(2026, 1, 10), -50m, dining.Id),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), null, default))
            .ReturnsAsync(transactions);
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetSpendingTrendsAsync(1, 2026, 1, groceries.Id);

        // Assert
        var jan = result.MonthlyData.First(m => m.Month == 1);
        Assert.Equal(100m, jan.TotalSpending.Amount);
        Assert.Equal(1, jan.TransactionCount); // Only groceries
    }

    [Fact]
    public async Task GetSpendingTrendsAsync_Clamps_Months_To_24_Max()
    {
        // Arrange
        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), null, default))
            .ReturnsAsync(new List<Transaction>());
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetSpendingTrendsAsync(100, 2026, 6);

        // Assert - should be clamped to 24
        Assert.Equal(24, result.MonthlyData.Count);
    }

    [Fact]
    public async Task GetSpendingTrendsAsync_Detects_Increasing_Trend()
    {
        // Arrange - spending increasing over months
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateOnly(2026, 1, 5), -100m, null),
            CreateTransaction(new DateOnly(2026, 2, 5), -120m, null),
            CreateTransaction(new DateOnly(2026, 3, 5), -200m, null),
            CreateTransaction(new DateOnly(2026, 4, 5), -250m, null),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), null, default))
            .ReturnsAsync(transactions);
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetSpendingTrendsAsync(4, 2026, 4);

        // Assert
        Assert.Equal("increasing", result.TrendDirection);
        Assert.True(result.TrendPercentage > 0);
    }

    [Fact]
    public async Task GetSpendingTrendsAsync_Detects_Decreasing_Trend()
    {
        // Arrange - spending decreasing over months
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateOnly(2026, 1, 5), -300m, null),
            CreateTransaction(new DateOnly(2026, 2, 5), -250m, null),
            CreateTransaction(new DateOnly(2026, 3, 5), -100m, null),
            CreateTransaction(new DateOnly(2026, 4, 5), -80m, null),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), null, default))
            .ReturnsAsync(transactions);
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetSpendingTrendsAsync(4, 2026, 4);

        // Assert
        Assert.Equal("decreasing", result.TrendDirection);
        Assert.True(result.TrendPercentage < 0);
    }

    [Fact]
    public async Task GetSpendingTrendsAsync_Excludes_Transfers()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateOnly(2026, 1, 5), -100m, null),
            CreateTransfer(new DateOnly(2026, 1, 10), -500m, Guid.NewGuid()),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), null, default))
            .ReturnsAsync(transactions);
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetSpendingTrendsAsync(1, 2026, 1);

        // Assert
        var jan = result.MonthlyData.First(m => m.Month == 1);
        Assert.Equal(100m, jan.TotalSpending.Amount);
        Assert.Equal(1, jan.TransactionCount);
    }

    [Fact]
    public async Task GetSpendingTrendsAsync_Handles_Gap_Months()
    {
        // Arrange - transactions only in Jan and Mar, Feb should be zero
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateOnly(2026, 1, 5), -100m, null),
            CreateTransaction(new DateOnly(2026, 3, 5), -200m, null),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), null, default))
            .ReturnsAsync(transactions);
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetSpendingTrendsAsync(3, 2026, 3);

        // Assert
        Assert.Equal(3, result.MonthlyData.Count);
        var feb = result.MonthlyData.First(m => m.Month == 2);
        Assert.Equal(0m, feb.TotalSpending.Amount);
        Assert.Equal(0, feb.TransactionCount);
    }

    [Fact]
    public async Task GetSpendingTrendsAsync_Spans_Year_Boundary()
    {
        // Arrange - range from Nov 2025 to Feb 2026
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateOnly(2025, 11, 5), -100m, null),
            CreateTransaction(new DateOnly(2025, 12, 5), -150m, null),
            CreateTransaction(new DateOnly(2026, 1, 5), -200m, null),
            CreateTransaction(new DateOnly(2026, 2, 5), -250m, null),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), null, default))
            .ReturnsAsync(transactions);
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetSpendingTrendsAsync(4, 2026, 2);

        // Assert
        Assert.Equal(4, result.MonthlyData.Count);
        Assert.Equal(2025, result.MonthlyData[0].Year);
        Assert.Equal(11, result.MonthlyData[0].Month);
        Assert.Equal(2026, result.MonthlyData[3].Year);
        Assert.Equal(2, result.MonthlyData[3].Month);
    }

    // ===== GetDaySummaryAsync Tests =====

    [Fact]
    public async Task GetDaySummaryAsync_Returns_Empty_Summary_When_No_Transactions()
    {
        // Arrange
        var date = new DateOnly(2026, 2, 5);
        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(date, date, null, default))
            .ReturnsAsync(new List<Transaction>());
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetDaySummaryAsync(date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(date, result.Date);
        Assert.Equal(0m, result.TotalSpending.Amount);
        Assert.Equal(0m, result.TotalIncome.Amount);
        Assert.Equal(0m, result.NetAmount.Amount);
        Assert.Equal(0, result.TransactionCount);
        Assert.Empty(result.TopCategories);
    }

    [Fact]
    public async Task GetDaySummaryAsync_Calculates_Spending_Income_Net()
    {
        // Arrange
        var date = new DateOnly(2026, 2, 5);
        var incomeCategory = BudgetCategory.Create("Salary", CategoryType.Income);
        var expenseCategory = BudgetCategory.Create("Groceries", CategoryType.Expense);

        var transactions = new List<Transaction>
        {
            CreateTransaction(date, 3000m, incomeCategory.Id),
            CreateTransaction(date, -85m, expenseCategory.Id),
            CreateTransaction(date, -15m, expenseCategory.Id),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(date, date, null, default))
            .ReturnsAsync(transactions);
        categoryRepo.Setup(r => r.GetByIdAsync(expenseCategory.Id, default)).ReturnsAsync(expenseCategory);
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetDaySummaryAsync(date);

        // Assert
        Assert.Equal(100m, result.TotalSpending.Amount);
        Assert.Equal(3000m, result.TotalIncome.Amount);
        Assert.Equal(2900m, result.NetAmount.Amount);
        Assert.Equal(3, result.TransactionCount);
    }

    [Fact]
    public async Task GetDaySummaryAsync_Returns_Top_3_Categories()
    {
        // Arrange
        var date = new DateOnly(2026, 2, 5);
        var cat1 = BudgetCategory.Create("Groceries", CategoryType.Expense);
        var cat2 = BudgetCategory.Create("Dining", CategoryType.Expense);
        var cat3 = BudgetCategory.Create("Transport", CategoryType.Expense);
        var cat4 = BudgetCategory.Create("Shopping", CategoryType.Expense);

        var transactions = new List<Transaction>
        {
            CreateTransaction(date, -50m, cat1.Id),
            CreateTransaction(date, -100m, cat2.Id),
            CreateTransaction(date, -25m, cat3.Id),
            CreateTransaction(date, -10m, cat4.Id),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(date, date, null, default))
            .ReturnsAsync(transactions);
        categoryRepo.Setup(r => r.GetByIdAsync(cat1.Id, default)).ReturnsAsync(cat1);
        categoryRepo.Setup(r => r.GetByIdAsync(cat2.Id, default)).ReturnsAsync(cat2);
        categoryRepo.Setup(r => r.GetByIdAsync(cat3.Id, default)).ReturnsAsync(cat3);
        categoryRepo.Setup(r => r.GetByIdAsync(cat4.Id, default)).ReturnsAsync(cat4);
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetDaySummaryAsync(date);

        // Assert
        Assert.Equal(3, result.TopCategories.Count);
        Assert.Equal("Dining", result.TopCategories[0].CategoryName); // 100 - largest
        Assert.Equal("Groceries", result.TopCategories[1].CategoryName); // 50
        Assert.Equal("Transport", result.TopCategories[2].CategoryName); // 25
    }

    [Fact]
    public async Task GetDaySummaryAsync_Excludes_Transfers()
    {
        // Arrange
        var date = new DateOnly(2026, 2, 5);
        var category = BudgetCategory.Create("Food", CategoryType.Expense);

        var transactions = new List<Transaction>
        {
            CreateTransaction(date, -50m, category.Id),
            CreateTransfer(date, -500m, Guid.NewGuid()),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(date, date, null, default))
            .ReturnsAsync(transactions);
        categoryRepo.Setup(r => r.GetByIdAsync(category.Id, default)).ReturnsAsync(category);
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetDaySummaryAsync(date);

        // Assert
        Assert.Equal(50m, result.TotalSpending.Amount);
        Assert.Equal(1, result.TransactionCount);
    }

    [Fact]
    public async Task GetDaySummaryAsync_Filters_By_AccountId()
    {
        // Arrange
        var date = new DateOnly(2026, 2, 5);
        var accountId = Guid.NewGuid();
        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(date, date, accountId, default))
            .ReturnsAsync(new List<Transaction>());
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetDaySummaryAsync(date, accountId);

        // Assert
        Assert.NotNull(result);
        transactionRepo.Verify(r => r.GetByDateRangeAsync(date, date, accountId, default), Times.Once);
    }

    [Fact]
    public async Task GetDaySummaryAsync_Handles_Uncategorized_Transactions()
    {
        // Arrange
        var date = new DateOnly(2026, 2, 5);

        var transactions = new List<Transaction>
        {
            CreateTransaction(date, -75m, null),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(date, date, null, default))
            .ReturnsAsync(transactions);
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object);

        // Act
        var result = await service.GetDaySummaryAsync(date);

        // Assert
        Assert.Single(result.TopCategories);
        Assert.Equal("Uncategorized", result.TopCategories[0].CategoryName);
        Assert.Equal(75m, result.TopCategories[0].Amount.Amount);
    }

    private static Transaction CreateTransaction(DateOnly date, decimal amount, Guid? categoryId)
    {
        var accountId = Guid.NewGuid();
        return Transaction.Create(
            accountId,
            MoneyValue.Create("USD", amount),
            date,
            "Test transaction",
            categoryId);
    }

    private static Transaction CreateTransfer(DateOnly date, decimal amount, Guid transferId)
    {
        var accountId = Guid.NewGuid();
        return Transaction.CreateTransfer(
            accountId,
            MoneyValue.Create("USD", amount),
            date,
            "Transfer",
            transferId,
            TransferDirection.Source);
    }
}
