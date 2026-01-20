// <copyright file="ReportServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Services;
using BudgetExperiment.Domain;

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
