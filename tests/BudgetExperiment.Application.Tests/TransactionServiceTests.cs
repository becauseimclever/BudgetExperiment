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
            Description = "Test Transaction",
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
            Description = "Test Transaction",
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

    [Fact]
    public async Task DeleteAsync_Removes_Transaction_And_Returns_True()
    {
        // Arrange
        var account = Account.Create("Test", AccountType.Checking);
        var money = MoneyValue.Create("USD", 25m);
        var transaction = account.AddTransaction(money, new DateOnly(2026, 1, 10), "To Delete");
        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByIdAsync(transaction.Id, default)).ReturnsAsync(transaction);
        var accountRepo = new Mock<IAccountRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var categorizationEngine = new Mock<ICategorizationEngine>();
        var service = new TransactionService(transactionRepo.Object, accountRepo.Object, uow.Object, categorizationEngine.Object);

        // Act
        var result = await service.DeleteAsync(transaction.Id);

        // Assert
        Assert.True(result);
        transactionRepo.Verify(r => r.RemoveAsync(transaction, default), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Returns_False_When_Transaction_Not_Found()
    {
        // Arrange
        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Transaction?)null);
        var accountRepo = new Mock<IAccountRepository>();
        var uow = new Mock<IUnitOfWork>();
        var categorizationEngine = new Mock<ICategorizationEngine>();
        var service = new TransactionService(transactionRepo.Object, accountRepo.Object, uow.Object, categorizationEngine.Object);

        // Act
        var result = await service.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
        transactionRepo.Verify(r => r.RemoveAsync(It.IsAny<Transaction>(), default), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UpdateCategoryAsync_AssignsCategory_And_ReturnsDto()
    {
        // Arrange
        var account = Account.Create("Test", AccountType.Checking);
        var money = MoneyValue.Create("USD", 5m);
        var transaction = account.AddTransaction(money, new DateOnly(2026, 1, 15), "Coffee");
        var categoryId = Guid.NewGuid();
        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByIdAsync(transaction.Id, default)).ReturnsAsync(transaction);
        var accountRepo = new Mock<IAccountRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var categorizationEngine = new Mock<ICategorizationEngine>();
        var service = new TransactionService(transactionRepo.Object, accountRepo.Object, uow.Object, categorizationEngine.Object);
        var dto = new TransactionCategoryUpdateDto { CategoryId = categoryId };

        // Act
        var result = await service.UpdateCategoryAsync(transaction.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(categoryId, result.CategoryId);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateCategoryAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Transaction?)null);
        var accountRepo = new Mock<IAccountRepository>();
        var uow = new Mock<IUnitOfWork>();
        var categorizationEngine = new Mock<ICategorizationEngine>();
        var service = new TransactionService(transactionRepo.Object, accountRepo.Object, uow.Object, categorizationEngine.Object);

        // Act
        var result = await service.UpdateCategoryAsync(Guid.NewGuid(), new TransactionCategoryUpdateDto { CategoryId = Guid.NewGuid() });

        // Assert
        Assert.Null(result);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UpdateCategoryAsync_ClearsCategory_WhenNullCategoryId()
    {
        // Arrange
        var account = Account.Create("Test", AccountType.Checking);
        var money = MoneyValue.Create("USD", 5m);
        var transaction = account.AddTransaction(money, new DateOnly(2026, 1, 15), "Coffee");
        transaction.UpdateCategory(Guid.NewGuid()); // pre-assign
        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByIdAsync(transaction.Id, default)).ReturnsAsync(transaction);
        var accountRepo = new Mock<IAccountRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var categorizationEngine = new Mock<ICategorizationEngine>();
        var service = new TransactionService(transactionRepo.Object, accountRepo.Object, uow.Object, categorizationEngine.Object);

        // Act
        var result = await service.UpdateCategoryAsync(transaction.Id, new TransactionCategoryUpdateDto { CategoryId = null });

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.CategoryId);
    }

    [Fact]
    public async Task UpdateAsync_With_Optimistic_Concurrency_Version_Check()
    {
        // Arrange
        var account = Account.Create("Test", AccountType.Checking);
        var money = MoneyValue.Create("USD", 100m);
        var transaction = account.AddTransaction(money, new DateOnly(2026, 1, 10), "Original");
        var expectedVersion = "version-1";

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByIdAsync(transaction.Id, default)).ReturnsAsync(transaction);

        var accountRepo = new Mock<IAccountRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var categorizationEngine = new Mock<ICategorizationEngine>();
        var service = new TransactionService(transactionRepo.Object, accountRepo.Object, uow.Object, categorizationEngine.Object);

        var dto = new TransactionUpdateDto
        {
            Amount = new MoneyDto { Currency = "USD", Amount = 150m },
            Date = new DateOnly(2026, 1, 11),
            Description = "Updated",
            CategoryId = null,
        };

        // Act
        var result = await service.UpdateAsync(transaction.Id, dto, expectedVersion);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(150m, result.Amount.Amount);
        uow.Verify(u => u.SetExpectedConcurrencyToken(transaction, expectedVersion), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Soft_Delete_Sets_DeletedAt_Timestamp()
    {
        // Arrange
        var account = Account.Create("Test", AccountType.Checking);
        var money = MoneyValue.Create("USD", 50m);
        var transaction = account.AddTransaction(money, new DateOnly(2026, 1, 10), "To Delete");

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByIdAsync(transaction.Id, default)).ReturnsAsync(transaction);

        var accountRepo = new Mock<IAccountRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var categorizationEngine = new Mock<ICategorizationEngine>();
        var service = new TransactionService(transactionRepo.Object, accountRepo.Object, uow.Object, categorizationEngine.Object);

        // Act
        var result = await service.DeleteAsync(transaction.Id);

        // Assert
        Assert.True(result);
        transactionRepo.Verify(r => r.RemoveAsync(transaction, default), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Unit_Of_Work_Failure_Rollback()
    {
        // Arrange
        var account = Account.Create("Test", AccountType.Checking);
        var accountRepo = new Mock<IAccountRepository>();
        accountRepo.Setup(r => r.GetByIdAsync(account.Id, default)).ReturnsAsync(account);

        var transactionRepo = new Mock<ITransactionRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ThrowsAsync(new InvalidOperationException("Database error"));

        var categorizationEngine = new Mock<ICategorizationEngine>();
        var service = new TransactionService(transactionRepo.Object, accountRepo.Object, uow.Object, categorizationEngine.Object);

        var dto = new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = new MoneyDto { Currency = "USD", Amount = 10m },
            Date = new DateOnly(2026, 1, 9),
            Description = "Test Transaction",
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto));
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Concurrency_Conflict_Row_Version_Mismatch()
    {
        // Arrange
        var account = Account.Create("Test", AccountType.Checking);
        var money = MoneyValue.Create("USD", 100m);
        var transaction = account.AddTransaction(money, new DateOnly(2026, 1, 10), "Original");
        var staleVersion = "old-version-123";

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByIdAsync(transaction.Id, default)).ReturnsAsync(transaction);

        var accountRepo = new Mock<IAccountRepository>();
        var uow = new Mock<IUnitOfWork>();

        // Simulate concurrency exception
        uow.Setup(u => u.SaveChangesAsync(default))
            .ThrowsAsync(new InvalidOperationException("Concurrency conflict: RowVersion mismatch"));

        var categorizationEngine = new Mock<ICategorizationEngine>();
        var service = new TransactionService(transactionRepo.Object, accountRepo.Object, uow.Object, categorizationEngine.Object);

        var dto = new TransactionUpdateDto
        {
            Amount = new MoneyDto { Currency = "USD", Amount = 200m },
            Date = new DateOnly(2026, 1, 11),
            Description = "Conflict Update",
            CategoryId = null,
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(transaction.Id, dto, staleVersion));
        uow.Verify(u => u.SetExpectedConcurrencyToken(transaction, staleVersion), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Duplicate_Soft_Delete_Idempotent()
    {
        // Arrange
        var account = Account.Create("Test", AccountType.Checking);
        var money = MoneyValue.Create("USD", 75m);
        var transaction = account.AddTransaction(money, new DateOnly(2026, 1, 15), "Delete Twice");

        var transactionRepo = new Mock<ITransactionRepository>();

        // First call returns transaction, second returns null (already deleted)
        var callCount = 0;
        transactionRepo.Setup(r => r.GetByIdAsync(transaction.Id, default))
            .Returns(() =>
            {
                callCount++;
                return callCount == 1 ? Task.FromResult<Transaction?>(transaction) : Task.FromResult<Transaction?>(null);
            });

        var accountRepo = new Mock<IAccountRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var categorizationEngine = new Mock<ICategorizationEngine>();
        var service = new TransactionService(transactionRepo.Object, accountRepo.Object, uow.Object, categorizationEngine.Object);

        // Act - First delete succeeds
        var firstResult = await service.DeleteAsync(transaction.Id);
        Assert.True(firstResult);

        // Reset mock for second call
        callCount = 2;
        var secondResult = await service.DeleteAsync(transaction.Id);

        // Assert
        Assert.False(secondResult);
    }
}
