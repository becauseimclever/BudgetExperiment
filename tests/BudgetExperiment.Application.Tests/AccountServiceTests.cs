// <copyright file="AccountServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Dtos;
using BudgetExperiment.Application.Services;
using BudgetExperiment.Domain;
using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for AccountService.
/// </summary>
public class AccountServiceTests
{
    [Fact]
    public async Task CreateAsync_Creates_Account()
    {
        // Arrange
        var repo = new Mock<IAccountRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Account>(), default)).Returns(Task.CompletedTask);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var service = new AccountService(repo.Object, uow.Object);
        var dto = new AccountCreateDto { Name = "Test", Type = AccountType.Checking };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.Equal("Test", result.Name);
        Assert.Equal(AccountType.Checking, result.Type);
        Assert.NotEqual(Guid.Empty, result.Id);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_AccountDto()
    {
        // Arrange
        var account = Account.Create("Test", AccountType.Savings);
        var repo = new Mock<IAccountRepository>();
        repo.Setup(r => r.GetByIdWithTransactionsAsync(account.Id, default)).ReturnsAsync(account);
        var uow = new Mock<IUnitOfWork>();
        var service = new AccountService(repo.Object, uow.Object);

        // Act
        var result = await service.GetByIdAsync(account.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(account.Id, result.Id);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public async Task RemoveAsync_Removes_Account()
    {
        // Arrange
        var account = Account.Create("Test", AccountType.Cash);
        var repo = new Mock<IAccountRepository>();
        repo.Setup(r => r.GetByIdAsync(account.Id, default)).ReturnsAsync(account);
        repo.Setup(r => r.RemoveAsync(account, default)).Returns(Task.CompletedTask);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var service = new AccountService(repo.Object, uow.Object);

        // Act
        var result = await service.RemoveAsync(account.Id);

        // Assert
        Assert.True(result);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_Returns_False_If_Not_Found()
    {
        // Arrange
        var repo = new Mock<IAccountRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Account?)null);
        var uow = new Mock<IUnitOfWork>();
        var service = new AccountService(repo.Object, uow.Object);

        // Act
        var result = await service.RemoveAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }
}
