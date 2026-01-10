// <copyright file="TransactionServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Dtos;
using BudgetExperiment.Application.Services;
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
        var service = new TransactionService(transactionRepo.Object, accountRepo.Object, uow.Object);
        var dto = new TransactionCreateDto
        {
            AccountId = account.Id,
            Amount = MoneyValue.Create("USD", 10m),
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
        var service = new TransactionService(transactionRepo.Object, accountRepo.Object, uow.Object);
        var dto = new TransactionCreateDto
        {
            AccountId = Guid.NewGuid(),
            Amount = MoneyValue.Create("USD", 10m),
            Date = new DateOnly(2026, 1, 9),
            Description = "Test Transaction"
        };

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(dto));
        uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }
}
