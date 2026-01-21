// <copyright file="TransactionServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

using BudgetExperiment.Domain;
using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for TransactionService.
/// </summary>
public class TransactionServiceTests
{
    [Fact]
    public async Task CreateAsync_Creates_Transaction()
    {
        // Arrange
        var account = Account.Create("Test", AccountType.Checking);
        var accountRepo = new Mock<IAccountRepository>();
        accountRepo.Setup(r => r.GetByIdAsync(account.Id, default)).ReturnsAsync(account);
        var transactionRepo = new Mock<ITransactionRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var categorizationEngine = new Mock<ICategorizationEngine>();
        var service = new TransactionService(transactionRepo.Object, accountRepo.Object, uow.Object, categorizationEngine.Object);
        var dto = new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = new MoneyDto { Currency = "USD", Amount = 10m },
            Date = new DateOnly(2026, 1, 9),
            Description = "Test Transaction"
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.Equal(account.Id, result.AccountId);
        Assert.Equal(10m, result.Amount.Amount);
        Assert.Equal("Test Transaction", result.Description);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Throws_If_Account_Not_Found()
    {
        // Arrange
        var accountRepo = new Mock<IAccountRepository>();
        accountRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Account?)null);
        var transactionRepo = new Mock<ITransactionRepository>();
        var uow = new Mock<IUnitOfWork>();
        var categorizationEngine = new Mock<ICategorizationEngine>();
        var service = new TransactionService(transactionRepo.Object, accountRepo.Object, uow.Object, categorizationEngine.Object);
        var dto = new TransactionCreateDto
        {
            AccountId = Guid.NewGuid(),
            Amount = new MoneyDto { Currency = "USD", Amount = 10m },
            Date = new DateOnly(2026, 1, 9),
            Description = "Test Transaction"
        };

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(dto));
        uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_AutoCategorizes_When_No_Manual_Category_Provided()
    {
        // Arrange
        var account = Account.Create("Test", AccountType.Checking);
        var categoryId = Guid.NewGuid();
        var accountRepo = new Mock<IAccountRepository>();
        accountRepo.Setup(r => r.GetByIdAsync(account.Id, default)).ReturnsAsync(account);
        var transactionRepo = new Mock<ITransactionRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var categorizationEngine = new Mock<ICategorizationEngine>();
        categorizationEngine.Setup(e => e.FindMatchingCategoryAsync("WALMART STORE #123", default))
            .ReturnsAsync(categoryId);
        var service = new TransactionService(transactionRepo.Object, accountRepo.Object, uow.Object, categorizationEngine.Object);
        var dto = new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = new MoneyDto { Currency = "USD", Amount = -50m },
            Date = new DateOnly(2026, 1, 15),
            Description = "WALMART STORE #123",
            CategoryId = null, // No manual category
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.Equal(categoryId, result.CategoryId);
        categorizationEngine.Verify(e => e.FindMatchingCategoryAsync("WALMART STORE #123", default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Does_Not_Overwrite_Manual_Category()
    {
        // Arrange
        var account = Account.Create("Test", AccountType.Checking);
        var manualCategoryId = Guid.NewGuid();
        var ruleCategoryId = Guid.NewGuid();
        var accountRepo = new Mock<IAccountRepository>();
        accountRepo.Setup(r => r.GetByIdAsync(account.Id, default)).ReturnsAsync(account);
        var transactionRepo = new Mock<ITransactionRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var categorizationEngine = new Mock<ICategorizationEngine>();
        categorizationEngine.Setup(e => e.FindMatchingCategoryAsync(It.IsAny<string>(), default))
            .ReturnsAsync(ruleCategoryId);
        var service = new TransactionService(transactionRepo.Object, accountRepo.Object, uow.Object, categorizationEngine.Object);
        var dto = new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = new MoneyDto { Currency = "USD", Amount = -50m },
            Date = new DateOnly(2026, 1, 15),
            Description = "WALMART STORE #123",
            CategoryId = manualCategoryId, // Manual category provided
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.Equal(manualCategoryId, result.CategoryId);
        categorizationEngine.Verify(e => e.FindMatchingCategoryAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_Sets_No_Category_When_No_Rule_Matches()
    {
        // Arrange
        var account = Account.Create("Test", AccountType.Checking);
        var accountRepo = new Mock<IAccountRepository>();
        accountRepo.Setup(r => r.GetByIdAsync(account.Id, default)).ReturnsAsync(account);
        var transactionRepo = new Mock<ITransactionRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var categorizationEngine = new Mock<ICategorizationEngine>();
        categorizationEngine.Setup(e => e.FindMatchingCategoryAsync(It.IsAny<string>(), default))
            .ReturnsAsync((Guid?)null); // No matching rule
        var service = new TransactionService(transactionRepo.Object, accountRepo.Object, uow.Object, categorizationEngine.Object);
        var dto = new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = new MoneyDto { Currency = "USD", Amount = -50m },
            Date = new DateOnly(2026, 1, 15),
            Description = "RANDOM STORE",
            CategoryId = null,
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.Null(result.CategoryId);
        categorizationEngine.Verify(e => e.FindMatchingCategoryAsync("RANDOM STORE", default), Times.Once);
    }
}
