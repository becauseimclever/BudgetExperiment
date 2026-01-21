// <copyright file="AccountServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

using BudgetExperiment.Domain;
using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for AccountService.
/// </summary>
public class AccountServiceTests
{
    private static readonly Guid TestUserId = new("11111111-1111-1111-1111-111111111111");

    private static Mock<IUserContext> CreateMockUserContext()
    {
        var userContext = new Mock<IUserContext>();
        userContext.Setup(u => u.UserIdAsGuid).Returns(TestUserId);
        userContext.Setup(u => u.UserId).Returns(TestUserId.ToString());
        userContext.Setup(u => u.IsAuthenticated).Returns(true);
        return userContext;
    }

    [Fact]
    public async Task CreateAsync_Creates_SharedAccount()
    {
        // Arrange
        var repo = new Mock<IAccountRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Account>(), default)).Returns(Task.CompletedTask);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var userContext = CreateMockUserContext();
        var service = new AccountService(repo.Object, uow.Object, userContext.Object);
        var dto = new AccountCreateDto { Name = "Test", Type = "Checking", Scope = "Shared" };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.Equal("Test", result.Name);
        Assert.Equal("Checking", result.Type);
        Assert.NotEqual(Guid.Empty, result.Id);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Creates_PersonalAccount()
    {
        // Arrange
        var repo = new Mock<IAccountRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Account>(), default)).Returns(Task.CompletedTask);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var userContext = CreateMockUserContext();
        var service = new AccountService(repo.Object, uow.Object, userContext.Object);
        var dto = new AccountCreateDto { Name = "Personal Test", Type = "Savings", Scope = "Personal" };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.Equal("Personal Test", result.Name);
        Assert.Equal("Savings", result.Type);
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
        var userContext = CreateMockUserContext();
        var service = new AccountService(repo.Object, uow.Object, userContext.Object);

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
        var userContext = CreateMockUserContext();
        var service = new AccountService(repo.Object, uow.Object, userContext.Object);

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
        var userContext = CreateMockUserContext();
        var service = new AccountService(repo.Object, uow.Object, userContext.Object);

        // Act
        var result = await service.RemoveAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_With_InitialBalance_Creates_Account()
    {
        // Arrange
        var repo = new Mock<IAccountRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Account>(), default)).Returns(Task.CompletedTask);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var userContext = CreateMockUserContext();
        var service = new AccountService(repo.Object, uow.Object, userContext.Object);
        var dto = new AccountCreateDto
        {
            Name = "Checking With Balance",
            Type = "Checking",
            InitialBalance = 1500.00m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2026, 1, 1),
            Scope = "Shared",
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.Equal("Checking With Balance", result.Name);
        Assert.Equal(1500.00m, result.InitialBalance);
        Assert.Equal("USD", result.InitialBalanceCurrency);
        Assert.Equal(new DateOnly(2026, 1, 1), result.InitialBalanceDate);
    }

    [Fact]
    public async Task CreateAsync_Without_InitialBalanceDate_Defaults_To_Today()
    {
        // Arrange
        var repo = new Mock<IAccountRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Account>(), default)).Returns(Task.CompletedTask);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var userContext = CreateMockUserContext();
        var service = new AccountService(repo.Object, uow.Object, userContext.Object);
        var dto = new AccountCreateDto
        {
            Name = "Default Date Test",
            Type = "Savings",
            InitialBalance = 100.00m,
            Scope = "Shared",
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), result.InitialBalanceDate);
    }

    [Fact]
    public async Task UpdateAsync_Updates_Account_Name()
    {
        // Arrange
        var account = Account.Create("Original Name", AccountType.Checking);
        var repo = new Mock<IAccountRepository>();
        repo.Setup(r => r.GetByIdAsync(account.Id, default)).ReturnsAsync(account);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var userContext = CreateMockUserContext();
        var service = new AccountService(repo.Object, uow.Object, userContext.Object);
        var dto = new AccountUpdateDto { Name = "Updated Name" };

        // Act
        var result = await service.UpdateAsync(account.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Updates_InitialBalance()
    {
        // Arrange
        var account = Account.Create("Balance Test", AccountType.Savings);
        var repo = new Mock<IAccountRepository>();
        repo.Setup(r => r.GetByIdAsync(account.Id, default)).ReturnsAsync(account);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var userContext = CreateMockUserContext();
        var service = new AccountService(repo.Object, uow.Object, userContext.Object);
        var dto = new AccountUpdateDto
        {
            InitialBalance = 2500.00m,
            InitialBalanceDate = new DateOnly(2026, 1, 15),
        };

        // Act
        var result = await service.UpdateAsync(account.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2500.00m, result.InitialBalance);
        Assert.Equal(new DateOnly(2026, 1, 15), result.InitialBalanceDate);
    }

    [Fact]
    public async Task UpdateAsync_Returns_Null_If_Not_Found()
    {
        // Arrange
        var repo = new Mock<IAccountRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Account?)null);
        var uow = new Mock<IUnitOfWork>();
        var userContext = CreateMockUserContext();
        var service = new AccountService(repo.Object, uow.Object, userContext.Object);
        var dto = new AccountUpdateDto { Name = "New Name" };

        // Act
        var result = await service.UpdateAsync(Guid.NewGuid(), dto);

        // Assert
        Assert.Null(result);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_With_Invalid_Type_Throws()
    {
        // Arrange
        var account = Account.Create("Type Test", AccountType.Checking);
        var repo = new Mock<IAccountRepository>();
        repo.Setup(r => r.GetByIdAsync(account.Id, default)).ReturnsAsync(account);
        var uow = new Mock<IUnitOfWork>();
        var userContext = CreateMockUserContext();
        var service = new AccountService(repo.Object, uow.Object, userContext.Object);
        var dto = new AccountUpdateDto { Type = "InvalidType" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.UpdateAsync(account.Id, dto));
        Assert.Contains("Invalid account type", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_With_Invalid_Scope_Throws()
    {
        // Arrange
        var repo = new Mock<IAccountRepository>();
        var uow = new Mock<IUnitOfWork>();
        var userContext = CreateMockUserContext();
        var service = new AccountService(repo.Object, uow.Object, userContext.Object);
        var dto = new AccountCreateDto { Name = "Test", Type = "Checking", Scope = "InvalidScope" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(dto));
        Assert.Contains("Invalid scope", ex.Message);
    }
}
