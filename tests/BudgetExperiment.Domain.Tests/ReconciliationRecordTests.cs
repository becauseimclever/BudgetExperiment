// <copyright file="ReconciliationRecordTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the <see cref="ReconciliationRecord"/> aggregate.
/// </summary>
public class ReconciliationRecordTests
{
    // AC-125b-04
    [Fact]
    public void Create_WhenBalancesDoNotMatch_ThrowsDomainException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var statementBalance = MoneyValue.Create("USD", 1000.00m);
        var clearedBalance = MoneyValue.Create("USD", 950.00m);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            ReconciliationRecord.Create(
                accountId,
                new DateOnly(2026, 1, 31),
                statementBalance,
                clearedBalance,
                5,
                Guid.NewGuid(),
                null));

        Assert.Equal(DomainExceptionType.Validation, ex.ExceptionType);
    }

    // AC-125b-05
    [Fact]
    public void Create_WhenBalancesMatch_SetsAllFieldsCorrectly()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var statementDate = new DateOnly(2026, 1, 31);
        var balance = MoneyValue.Create("USD", 1000.00m);

        // Act
        var record = ReconciliationRecord.Create(
            accountId,
            statementDate,
            balance,
            balance,
            10,
            userId,
            null);

        // Assert
        Assert.NotEqual(Guid.Empty, record.Id);
        Assert.Equal(accountId, record.AccountId);
        Assert.Equal(statementDate, record.StatementDate);
        Assert.Equal(balance, record.StatementBalance);
        Assert.Equal(balance, record.ClearedBalance);
        Assert.Equal(10, record.TransactionCount);
        Assert.Equal(userId, record.CompletedByUserId);
        Assert.Null(record.OwnerUserId);
        Assert.True(record.CompletedAtUtc <= DateTime.UtcNow);
    }
}
