// <copyright file="UncategorizedTransactionServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Categorization;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Budgeting;
using BudgetExperiment.Domain.Repositories;
using Moq;

namespace BudgetExperiment.Application.Tests.Categorization;

/// <summary>
/// Unit tests for UncategorizedTransactionService.
/// </summary>
public class UncategorizedTransactionServiceTests
{
    #region GetPagedAsync Tests

    [Fact]
    public async Task GetPagedAsync_Returns_Paged_Transactions()
    {
        // Arrange
        var account = Account.Create("Test Account", AccountType.Checking);
        var transactions = new List<Transaction>
        {
            account.AddTransaction(MoneyValue.Create("USD", 50m), new DateOnly(2026, 1, 10), "Trans 1"),
            account.AddTransaction(MoneyValue.Create("USD", 75m), new DateOnly(2026, 1, 11), "Trans 2"),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetUncategorizedPagedAsync(
            null, null, null, null, null, null, "Date", true, 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((transactions.AsReadOnly(), 2));

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var service = new UncategorizedTransactionService(transactionRepo.Object, categoryRepo.Object, unitOfWork.Object);
        var filter = new UncategorizedTransactionFilterDto();

        // Act
        var result = await service.GetPagedAsync(filter);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(50, result.PageSize);
    }

    [Fact]
    public async Task GetPagedAsync_Passes_Filter_Parameters_To_Repository()
    {
        // Arrange
        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetUncategorizedPagedAsync(
            It.IsAny<DateOnly?>(),
            It.IsAny<DateOnly?>(),
            It.IsAny<decimal?>(),
            It.IsAny<decimal?>(),
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Transaction>().AsReadOnly(), 0));

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var service = new UncategorizedTransactionService(transactionRepo.Object, categoryRepo.Object, unitOfWork.Object);
        var filter = new UncategorizedTransactionFilterDto
        {
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31),
            MinAmount = 10m,
            MaxAmount = 100m,
            DescriptionContains = "Amazon",
            AccountId = Guid.NewGuid(),
            SortBy = "Amount",
            SortDescending = false,
            Page = 2,
            PageSize = 25,
        };

        // Act
        await service.GetPagedAsync(filter);

        // Assert
        transactionRepo.Verify(r => r.GetUncategorizedPagedAsync(
            filter.StartDate,
            filter.EndDate,
            filter.MinAmount,
            filter.MaxAmount,
            filter.DescriptionContains,
            filter.AccountId,
            filter.SortBy,
            filter.SortDescending,
            25, // skip = (page - 1) * pageSize
            25, // take = pageSize
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPagedAsync_Clamps_PageSize_To_Maximum()
    {
        // Arrange
        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetUncategorizedPagedAsync(
            It.IsAny<DateOnly?>(),
            It.IsAny<DateOnly?>(),
            It.IsAny<decimal?>(),
            It.IsAny<decimal?>(),
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Transaction>().AsReadOnly(), 0));

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var service = new UncategorizedTransactionService(transactionRepo.Object, categoryRepo.Object, unitOfWork.Object);
        var filter = new UncategorizedTransactionFilterDto { PageSize = 500 }; // Exceeds max of 100

        // Act
        await service.GetPagedAsync(filter);

        // Assert - should clamp to 100
        transactionRepo.Verify(r => r.GetUncategorizedPagedAsync(
            null, null, null, null, null, null, "Date", true, 0, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPagedAsync_Defaults_Page_To_1_When_Less_Than_1()
    {
        // Arrange
        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetUncategorizedPagedAsync(
            It.IsAny<DateOnly?>(),
            It.IsAny<DateOnly?>(),
            It.IsAny<decimal?>(),
            It.IsAny<decimal?>(),
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Transaction>().AsReadOnly(), 0));

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var service = new UncategorizedTransactionService(transactionRepo.Object, categoryRepo.Object, unitOfWork.Object);
        var filter = new UncategorizedTransactionFilterDto { Page = 0 };

        // Act
        await service.GetPagedAsync(filter);

        // Assert - should default to page 1, skip 0
        transactionRepo.Verify(r => r.GetUncategorizedPagedAsync(
            null, null, null, null, null, null, "Date", true, 0, 50, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region BulkCategorizeAsync Tests

    [Fact]
    public async Task BulkCategorizeAsync_Updates_All_Transactions()
    {
        // Arrange
        var account = Account.Create("Test Account", AccountType.Checking);
        var trans1 = account.AddTransaction(MoneyValue.Create("USD", 50m), new DateOnly(2026, 1, 10), "Trans 1");
        var trans2 = account.AddTransaction(MoneyValue.Create("USD", 75m), new DateOnly(2026, 1, 11), "Trans 2");

        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByIdAsync(trans1.Id, It.IsAny<CancellationToken>())).ReturnsAsync(trans1);
        transactionRepo.Setup(r => r.GetByIdAsync(trans2.Id, It.IsAny<CancellationToken>())).ReturnsAsync(trans2);

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var service = new UncategorizedTransactionService(transactionRepo.Object, categoryRepo.Object, unitOfWork.Object);
        var request = new BulkCategorizeRequest
        {
            TransactionIds = [trans1.Id, trans2.Id],
            CategoryId = category.Id,
        };

        // Act
        var result = await service.BulkCategorizeAsync(request);

        // Assert
        Assert.Equal(2, result.TotalRequested);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Errors);
        Assert.Equal(category.Id, trans1.CategoryId);
        Assert.Equal(category.Id, trans2.CategoryId);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkCategorizeAsync_Returns_Error_When_Category_Not_Found()
    {
        // Arrange
        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((BudgetCategory?)null);

        var unitOfWork = new Mock<IUnitOfWork>();

        var service = new UncategorizedTransactionService(transactionRepo.Object, categoryRepo.Object, unitOfWork.Object);
        var request = new BulkCategorizeRequest
        {
            TransactionIds = [Guid.NewGuid()],
            CategoryId = Guid.NewGuid(),
        };

        // Act
        var result = await service.BulkCategorizeAsync(request);

        // Assert
        Assert.Equal(1, result.TotalRequested);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Category not found", result.Errors[0]);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkCategorizeAsync_Handles_Missing_Transactions_Gracefully()
    {
        // Arrange
        var account = Account.Create("Test Account", AccountType.Checking);
        var existingTrans = account.AddTransaction(MoneyValue.Create("USD", 50m), new DateOnly(2026, 1, 10), "Existing");
        var missingId = Guid.NewGuid();

        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByIdAsync(existingTrans.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existingTrans);
        transactionRepo.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((Transaction?)null);

        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        categoryRepo.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var service = new UncategorizedTransactionService(transactionRepo.Object, categoryRepo.Object, unitOfWork.Object);
        var request = new BulkCategorizeRequest
        {
            TransactionIds = [existingTrans.Id, missingId],
            CategoryId = category.Id,
        };

        // Act
        var result = await service.BulkCategorizeAsync(request);

        // Assert
        Assert.Equal(2, result.TotalRequested);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Single(result.Errors);
        Assert.Contains(missingId.ToString(), result.Errors[0]);
        Assert.Equal(category.Id, existingTrans.CategoryId);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkCategorizeAsync_Returns_Empty_Result_When_No_TransactionIds_Provided()
    {
        // Arrange
        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var service = new UncategorizedTransactionService(transactionRepo.Object, categoryRepo.Object, unitOfWork.Object);
        var request = new BulkCategorizeRequest
        {
            TransactionIds = [],
            CategoryId = Guid.NewGuid(),
        };

        // Act
        var result = await service.BulkCategorizeAsync(request);

        // Assert
        Assert.Equal(0, result.TotalRequested);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Errors);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
