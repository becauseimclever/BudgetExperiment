// <copyright file="SoftDeleteMethodsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;
using BudgetExperiment.Domain.Accounts;
using BudgetExperiment.Domain.Budgeting;
using BudgetExperiment.Domain.Common;
using BudgetExperiment.Domain.Recurring;
using Shouldly;
using Xunit;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Tests for soft-delete methods across domain entities.
/// </summary>
public sealed class SoftDeleteMethodsTests : IDisposable
{
    private readonly CultureInfo _originalCulture;

    public SoftDeleteMethodsTests()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
    }

    [Fact]
    public void Transaction_SoftDelete_SetsDeletedAtUtcToNow()
    {
        // Arrange
        var account = Account.Create("Test Account", AccountType.Checking);
        var amount = MoneyValue.Create("USD", 100m);
        var transaction = account.AddTransaction(amount, DateOnly.FromDateTime(DateTime.UtcNow), "Test transaction");
        var beforeDelete = DateTime.UtcNow;

        // Act
        transaction.SoftDelete();

        // Assert
        transaction.DeletedAtUtc.ShouldNotBeNull();
        transaction.DeletedAtUtc.Value.ShouldBeGreaterThanOrEqualTo(beforeDelete);
        transaction.DeletedAtUtc.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void Transaction_Restore_ClearsDeletedAtUtc()
    {
        // Arrange
        var account = Account.Create("Test Account", AccountType.Checking);
        var amount = MoneyValue.Create("USD", 100m);
        var transaction = account.AddTransaction(amount, DateOnly.FromDateTime(DateTime.UtcNow), "Test transaction");
        transaction.SoftDelete();

        // Act
        transaction.Restore();

        // Assert
        transaction.DeletedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Account_SoftDelete_SetsDeletedAtUtcToNow()
    {
        // Arrange
        var account = Account.Create("Test Account", AccountType.Checking);
        var beforeDelete = DateTime.UtcNow;

        // Act
        account.SoftDelete();

        // Assert
        account.DeletedAtUtc.ShouldNotBeNull();
        account.DeletedAtUtc.Value.ShouldBeGreaterThanOrEqualTo(beforeDelete);
        account.DeletedAtUtc.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void Account_Restore_ClearsDeletedAtUtc()
    {
        // Arrange
        var account = Account.Create("Test Account", AccountType.Checking);
        account.SoftDelete();

        // Act
        account.Restore();

        // Assert
        account.DeletedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void BudgetCategory_SoftDelete_SetsDeletedAtUtcToNow()
    {
        // Arrange
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        var beforeDelete = DateTime.UtcNow;

        // Act
        category.SoftDelete();

        // Assert
        category.DeletedAtUtc.ShouldNotBeNull();
        category.DeletedAtUtc.Value.ShouldBeGreaterThanOrEqualTo(beforeDelete);
        category.DeletedAtUtc.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void BudgetCategory_Restore_ClearsDeletedAtUtc()
    {
        // Arrange
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        category.SoftDelete();

        // Act
        category.Restore();

        // Assert
        category.DeletedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void BudgetGoal_SoftDelete_SetsDeletedAtUtcToNow()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var targetAmount = MoneyValue.Create("USD", 500m);
        var goal = BudgetGoal.Create(categoryId, 2026, 1, targetAmount);
        var beforeDelete = DateTime.UtcNow;

        // Act
        goal.SoftDelete();

        // Assert
        goal.DeletedAtUtc.ShouldNotBeNull();
        goal.DeletedAtUtc.Value.ShouldBeGreaterThanOrEqualTo(beforeDelete);
        goal.DeletedAtUtc.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void BudgetGoal_Restore_ClearsDeletedAtUtc()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var targetAmount = MoneyValue.Create("USD", 500m);
        var goal = BudgetGoal.Create(categoryId, 2026, 1, targetAmount);
        goal.SoftDelete();

        // Act
        goal.Restore();

        // Assert
        goal.DeletedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void RecurringTransaction_SoftDelete_SetsDeletedAtUtcToNow()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = MoneyValue.Create("USD", 100m);
        var pattern = RecurrencePatternValue.CreateMonthly(1, 1);
        var recurring = RecurringTransaction.Create(
            accountId,
            "Monthly subscription",
            amount,
            pattern,
            DateOnly.FromDateTime(DateTime.UtcNow));
        var beforeDelete = DateTime.UtcNow;

        // Act
        recurring.SoftDelete();

        // Assert
        recurring.DeletedAtUtc.ShouldNotBeNull();
        recurring.DeletedAtUtc.Value.ShouldBeGreaterThanOrEqualTo(beforeDelete);
        recurring.DeletedAtUtc.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void RecurringTransaction_Restore_ClearsDeletedAtUtc()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = MoneyValue.Create("USD", 100m);
        var pattern = RecurrencePatternValue.CreateMonthly(1, 1);
        var recurring = RecurringTransaction.Create(
            accountId,
            "Monthly subscription",
            amount,
            pattern,
            DateOnly.FromDateTime(DateTime.UtcNow));
        recurring.SoftDelete();

        // Act
        recurring.Restore();

        // Assert
        recurring.DeletedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void RecurringTransfer_SoftDelete_SetsDeletedAtUtcToNow()
    {
        // Arrange
        var sourceAccountId = Guid.NewGuid();
        var destAccountId = Guid.NewGuid();
        var amount = MoneyValue.Create("USD", 1000m);
        var pattern = RecurrencePatternValue.CreateMonthly(1, 1);
        var transfer = RecurringTransfer.Create(
            sourceAccountId,
            destAccountId,
            "Monthly transfer",
            amount,
            pattern,
            DateOnly.FromDateTime(DateTime.UtcNow));
        var beforeDelete = DateTime.UtcNow;

        // Act
        transfer.SoftDelete();

        // Assert
        transfer.DeletedAtUtc.ShouldNotBeNull();
        transfer.DeletedAtUtc.Value.ShouldBeGreaterThanOrEqualTo(beforeDelete);
        transfer.DeletedAtUtc.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void RecurringTransfer_Restore_ClearsDeletedAtUtc()
    {
        // Arrange
        var sourceAccountId = Guid.NewGuid();
        var destAccountId = Guid.NewGuid();
        var amount = MoneyValue.Create("USD", 1000m);
        var pattern = RecurrencePatternValue.CreateMonthly(1, 1);
        var transfer = RecurringTransfer.Create(
            sourceAccountId,
            destAccountId,
            "Monthly transfer",
            amount,
            pattern,
            DateOnly.FromDateTime(DateTime.UtcNow));
        transfer.SoftDelete();

        // Act
        transfer.Restore();

        // Assert
        transfer.DeletedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Restore_CalledMultipleTimes_IsIdempotent()
    {
        // Arrange
        var account = Account.Create("Test Account", AccountType.Checking);
        account.SoftDelete();
        account.Restore();

        // Act - restore again
        account.Restore();

        // Assert
        account.DeletedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void SoftDelete_CalledOnAlreadyDeletedEntity_IsIdempotent()
    {
        // Arrange
        var account = Account.Create("Test Account", AccountType.Checking);
        account.SoftDelete();
        var firstDeleteTime = account.DeletedAtUtc;
        System.Threading.Thread.Sleep(10); // Small delay to ensure different timestamp

        // Act - soft-delete again
        account.SoftDelete();

        // Assert - timestamp should be updated
        account.DeletedAtUtc.ShouldNotBeNull();
        account.DeletedAtUtc.Value.ShouldBeGreaterThan(firstDeleteTime!.Value);
    }
}
