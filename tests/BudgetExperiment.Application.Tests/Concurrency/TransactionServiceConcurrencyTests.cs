// <copyright file="TransactionServiceConcurrencyTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;
using BudgetExperiment.Contracts.Dtos;
using Moq;

namespace BudgetExperiment.Application.Tests.Concurrency;

/// <summary>
/// Concurrency and optimistic locking tests for TransactionService.
/// </summary>
public class TransactionServiceConcurrencyTests
{
    public TransactionServiceConcurrencyTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    [Fact]
    public async Task UpdateAsync_WithRowVersionConflict_ThrowsConcurrencyException()
    {
        // Arrange: Create a transaction and simulate version mismatch
        var account = Account.Create("Test Account", AccountType.Checking);
        var transaction = account.AddTransaction(
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 15),
            "Original Description");

        var mockRepository = new Mock<ITransactionRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(transaction.Id, default))
            .ReturnsAsync(transaction);

        var mockAccountRepository = new Mock<IAccountRepository>();
        var mockUow = new Mock<IUnitOfWork>();

        // Simulate SaveChangesAsync detecting RowVersion mismatch
        mockUow.Setup(u => u.SaveChangesAsync(default))
            .ThrowsAsync(new InvalidOperationException(
                "Concurrency conflict: Database operation expected to affect 1 row(s) but actually affected 0 row(s); " +
                "data may have been modified or deleted since entities were loaded."));

        var mockCategorizationEngine = new Mock<ICategorizationEngine>();
        var service = new TransactionService(
            mockRepository.Object,
            mockAccountRepository.Object,
            mockUow.Object,
            mockCategorizationEngine.Object);

        var updateDto = new TransactionUpdateDto
        {
            Amount = new MoneyDto { Currency = "USD", Amount = 150m },
            Date = new DateOnly(2026, 1, 16),
            Description = "Updated Description",
            CategoryId = null,
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(transaction.Id, updateDto, "stale-version"));

        Assert.Contains("affected 0 row", ex.Message);
        mockUow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithoutVersionConflict_SucceedsAndUpdatesTransaction()
    {
        // Arrange: Create a transaction without version conflict
        var account = Account.Create("Test Account", AccountType.Checking);
        var transaction = account.AddTransaction(
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 15),
            "Original Description");

        var mockRepository = new Mock<ITransactionRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(transaction.Id, default))
            .ReturnsAsync(transaction);

        var mockAccountRepository = new Mock<IAccountRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        mockUow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        mockUow.Setup(u => u.GetConcurrencyToken(It.IsAny<Transaction>()))
            .Returns("current-version");

        var mockCategorizationEngine = new Mock<ICategorizationEngine>();
        var service = new TransactionService(
            mockRepository.Object,
            mockAccountRepository.Object,
            mockUow.Object,
            mockCategorizationEngine.Object);

        var updateDto = new TransactionUpdateDto
        {
            Amount = new MoneyDto { Currency = "USD", Amount = 150m },
            Date = new DateOnly(2026, 1, 16),
            Description = "Updated Description",
            CategoryId = null,
        };

        // Act
        var result = await service.UpdateAsync(transaction.Id, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(150m, result.Amount.Amount);
        Assert.Equal("Updated Description", result.Description);
        mockUow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ConcurrentAccountBalanceUpdate_WithMultipleTransactions_AggregatesCorrectly()
    {
        // Arrange: Create account and multiple transactions concurrently
        var account = Account.Create("Test Account", AccountType.Checking);
        var tx1 = account.AddTransaction(
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 15),
            "Transaction 1");
        var tx2 = account.AddTransaction(
            MoneyValue.Create("USD", 200m),
            new DateOnly(2026, 1, 15),
            "Transaction 2");

        var mockRepository = new Mock<ITransactionRepository>();
        var transactions = new List<Transaction> { tx1, tx2 };
        mockRepository.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                account.Id,
                default))
            .ReturnsAsync(transactions);

        var mockAccountRepository = new Mock<IAccountRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockCategorizationEngine = new Mock<ICategorizationEngine>();

        var service = new TransactionService(
            mockRepository.Object,
            mockAccountRepository.Object,
            mockUow.Object,
            mockCategorizationEngine.Object);

        // Act: Retrieve transactions (simulating concurrent queries)
        var result1 = await service.GetByDateRangeAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            account.Id);

        var result2 = await service.GetByDateRangeAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            account.Id);

        // Assert: Both queries should return consistent results (no race condition)
        Assert.Equal(2, result1.Count);
        Assert.Equal(2, result2.Count);
        Assert.Equal(300m, result1.Sum(t => t.Amount.Amount));
        Assert.Equal(300m, result2.Sum(t => t.Amount.Amount));
    }

    [Fact]
    public async Task ConcurrentTransactionUpdates_OnlyFirstSucceeds_SecondFails()
    {
        // Arrange: Two concurrent updates to same transaction
        var account = Account.Create("Test Account", AccountType.Checking);
        var transaction = account.AddTransaction(
            MoneyValue.Create("USD", 500m),
            new DateOnly(2026, 1, 15),
            "Test Transaction");

        var mockRepository = new Mock<ITransactionRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(transaction.Id, default))
            .ReturnsAsync(transaction);

        var mockAccountRepository = new Mock<IAccountRepository>();
        var callCount = 0;
        var mockUow = new Mock<IUnitOfWork>();
        mockUow.Setup(u => u.SaveChangesAsync(default))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return Task.FromResult(1); // First succeeds
                }
                else
                {
                    // Second fails with concurrency exception
                    throw new InvalidOperationException(
                        "Concurrency conflict: Database operation expected to affect 1 row(s) but actually affected 0 row(s)");
                }
            });

        mockUow.Setup(u => u.GetConcurrencyToken(It.IsAny<Transaction>()))
            .Returns("version-1");

        var mockCategorizationEngine = new Mock<ICategorizationEngine>();
        var service = new TransactionService(
            mockRepository.Object,
            mockAccountRepository.Object,
            mockUow.Object,
            mockCategorizationEngine.Object);

        var updateDto1 = new TransactionUpdateDto
        {
            Amount = new MoneyDto { Currency = "USD", Amount = 600m },
            Date = new DateOnly(2026, 1, 16),
            Description = "First Update",
            CategoryId = null,
        };

        var updateDto2 = new TransactionUpdateDto
        {
            Amount = new MoneyDto { Currency = "USD", Amount = 700m },
            Date = new DateOnly(2026, 1, 17),
            Description = "Second Update",
            CategoryId = null,
        };

        // Act: First update succeeds
        var result1 = await service.UpdateAsync(transaction.Id, updateDto1, "old-version");
        Assert.NotNull(result1);

        // Act & Assert: Second update fails with concurrency exception
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(transaction.Id, updateDto2, "old-version"));

        Assert.Contains("affected 0 row", ex.Message);
    }
}
