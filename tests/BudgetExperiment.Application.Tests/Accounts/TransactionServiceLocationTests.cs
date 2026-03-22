// <copyright file="TransactionServiceLocationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

using Moq;

namespace BudgetExperiment.Application.Tests.Accounts;

/// <summary>
/// Unit tests for TransactionService bulk location operations.
/// </summary>
public class TransactionServiceLocationTests
{
    [Fact]
    public async Task ClearAllLocationDataAsync_SetsAllLocationsToNull()
    {
        // Arrange
        var account = Account.Create("Test", AccountType.Checking);
        var t1 = account.AddTransaction(MoneyValue.Create("USD", -10m), new DateOnly(2026, 1, 1), "Store A");
        var t2 = account.AddTransaction(MoneyValue.Create("USD", -20m), new DateOnly(2026, 1, 2), "Store B");
        var t3 = account.AddTransaction(MoneyValue.Create("USD", -30m), new DateOnly(2026, 1, 3), "Store C");

        t1.SetLocation(TransactionLocationValue.CreateFromParsed("Seattle", "WA"));
        t2.SetLocation(TransactionLocationValue.CreateFromParsed("Portland", "OR"));

        // t3 has no location — should remain unaffected
        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetAllWithLocationAsync(default))
            .ReturnsAsync(new List<Transaction> { t1, t2 });
        var accountRepo = new Mock<IAccountRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var categorizationEngine = new Mock<ICategorizationEngine>();

        var service = new TransactionService(transactionRepo.Object, accountRepo.Object, uow.Object, categorizationEngine.Object);

        // Act
        var result = await service.ClearAllLocationDataAsync();

        // Assert
        Assert.Equal(2, result);
        Assert.Null(t1.Location);
        Assert.Null(t2.Location);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ClearAllLocationDataAsync_ReturnsAffectedCount()
    {
        // Arrange
        var account = Account.Create("Test", AccountType.Checking);
        var t1 = account.AddTransaction(MoneyValue.Create("USD", -10m), new DateOnly(2026, 2, 1), "Gas Station");
        t1.SetLocation(TransactionLocationValue.CreateFromParsed("Denver", "CO"));

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetAllWithLocationAsync(default))
            .ReturnsAsync(new List<Transaction> { t1 });
        var accountRepo = new Mock<IAccountRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var categorizationEngine = new Mock<ICategorizationEngine>();

        var service = new TransactionService(transactionRepo.Object, accountRepo.Object, uow.Object, categorizationEngine.Object);

        // Act
        var result = await service.ClearAllLocationDataAsync();

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ClearAllLocationDataAsync_NoLocations_ReturnsZero()
    {
        // Arrange
        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetAllWithLocationAsync(default))
            .ReturnsAsync(new List<Transaction>());
        var accountRepo = new Mock<IAccountRepository>();
        var uow = new Mock<IUnitOfWork>();
        var categorizationEngine = new Mock<ICategorizationEngine>();

        var service = new TransactionService(transactionRepo.Object, accountRepo.Object, uow.Object, categorizationEngine.Object);

        // Act
        var result = await service.ClearAllLocationDataAsync();

        // Assert
        Assert.Equal(0, result);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }
}
