// <copyright file="AccountTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the Account aggregate root.
/// </summary>
public class AccountTests
{
    [Fact]
    public void Create_With_Valid_Data_Creates_Account()
    {
        // Arrange
        var name = "Checking Account";
        var type = AccountType.Checking;

        // Act
        var account = Account.Create(name, type);

        // Assert
        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.Equal(name, account.Name);
        Assert.Equal(type, account.Type);
        Assert.NotEqual(default, account.CreatedAt);
        Assert.NotEqual(default, account.UpdatedAt);
        Assert.Empty(account.Transactions);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Empty_Name_Throws(string? name)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => Account.Create(name!, AccountType.Checking));
        Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_Trims_Name()
    {
        // Arrange
        var name = "  My Account  ";

        // Act
        var account = Account.Create(name, AccountType.Savings);

        // Assert
        Assert.Equal("My Account", account.Name);
    }

    [Fact]
    public void UpdateName_Changes_Name_And_UpdatedAt()
    {
        // Arrange
        var account = Account.Create("Original", AccountType.Checking);
        var originalUpdatedAt = account.UpdatedAt;

        // Act
        account.UpdateName("New Name");

        // Assert
        Assert.Equal("New Name", account.Name);
        Assert.True(account.UpdatedAt >= originalUpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateName_With_Empty_Name_Throws(string? name)
    {
        // Arrange
        var account = Account.Create("Original", AccountType.Checking);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => account.UpdateName(name!));
        Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpdateType_Changes_Type_And_UpdatedAt()
    {
        // Arrange
        var account = Account.Create("Account", AccountType.Checking);
        var originalUpdatedAt = account.UpdatedAt;

        // Act
        account.UpdateType(AccountType.Savings);

        // Assert
        Assert.Equal(AccountType.Savings, account.Type);
        Assert.True(account.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void AddTransaction_Creates_Transaction_With_AccountId()
    {
        // Arrange
        var account = Account.Create("Checking", AccountType.Checking);
        var amount = MoneyValue.Create("USD", 100.00m);
        var date = new DateOnly(2026, 1, 9);
        var description = "Test Transaction";

        // Act
        var transaction = account.AddTransaction(amount, date, description);

        // Assert
        Assert.NotEqual(Guid.Empty, transaction.Id);
        Assert.Equal(account.Id, transaction.AccountId);
        Assert.Equal(amount, transaction.Amount);
        Assert.Equal(date, transaction.Date);
        Assert.Equal(description, transaction.Description);
        Assert.Single(account.Transactions);
        Assert.Contains(transaction, account.Transactions);
    }

    [Fact]
    public void AddTransaction_With_Category_Sets_Category()
    {
        // Arrange
        var account = Account.Create("Checking", AccountType.Checking);
        var amount = MoneyValue.Create("USD", 50.00m);
        var date = new DateOnly(2026, 1, 9);

        // Act
        var transaction = account.AddTransaction(amount, date, "Groceries", category: "Food");

        // Assert
        Assert.Equal("Food", transaction.Category);
    }

    [Fact]
    public void AddTransaction_With_Empty_Description_Throws()
    {
        // Arrange
        var account = Account.Create("Checking", AccountType.Checking);
        var amount = MoneyValue.Create("USD", 100.00m);
        var date = new DateOnly(2026, 1, 9);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => account.AddTransaction(amount, date, ""));
        Assert.Contains("description", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddTransaction_With_Null_Amount_Throws()
    {
        // Arrange
        var account = Account.Create("Checking", AccountType.Checking);
        var date = new DateOnly(2026, 1, 9);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => account.AddTransaction(null!, date, "Test"));
        Assert.Contains("amount", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RemoveTransaction_Removes_From_Collection()
    {
        // Arrange
        var account = Account.Create("Checking", AccountType.Checking);
        var transaction = account.AddTransaction(
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 9),
            "Test");

        // Act
        var removed = account.RemoveTransaction(transaction.Id);

        // Assert
        Assert.True(removed);
        Assert.Empty(account.Transactions);
    }

    [Fact]
    public void RemoveTransaction_Returns_False_When_Not_Found()
    {
        // Arrange
        var account = Account.Create("Checking", AccountType.Checking);

        // Act
        var removed = account.RemoveTransaction(Guid.NewGuid());

        // Assert
        Assert.False(removed);
    }

    [Theory]
    [InlineData(AccountType.Checking)]
    [InlineData(AccountType.Savings)]
    [InlineData(AccountType.CreditCard)]
    [InlineData(AccountType.Cash)]
    [InlineData(AccountType.Other)]
    public void Create_With_All_AccountTypes_Succeeds(AccountType type)
    {
        // Act
        var account = Account.Create("Test Account", type);

        // Assert
        Assert.Equal(type, account.Type);
    }

    [Fact]
    public void Create_With_Default_InitialBalance_Returns_Zero()
    {
        // Act
        var account = Account.Create("Checking", AccountType.Checking);

        // Assert
        Assert.NotNull(account.InitialBalance);
        Assert.Equal(0m, account.InitialBalance.Amount);
        Assert.Equal("USD", account.InitialBalance.Currency);
    }

    [Fact]
    public void Create_With_Default_InitialBalanceDate_Returns_Today()
    {
        // Act
        var account = Account.Create("Checking", AccountType.Checking);

        // Assert
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), account.InitialBalanceDate);
    }

    [Fact]
    public void Create_With_InitialBalance_Sets_Balance()
    {
        // Arrange
        var initialBalance = MoneyValue.Create("USD", 1500.00m);
        var initialBalanceDate = new DateOnly(2026, 1, 1);

        // Act
        var account = Account.Create("Checking", AccountType.Checking, initialBalance, initialBalanceDate);

        // Assert
        Assert.Equal(initialBalance, account.InitialBalance);
        Assert.Equal(initialBalanceDate, account.InitialBalanceDate);
    }

    [Fact]
    public void Create_With_Negative_InitialBalance_Succeeds()
    {
        // Arrange - Credit card with existing debt
        var initialBalance = MoneyValue.Create("USD", -2500.00m);
        var initialBalanceDate = new DateOnly(2026, 1, 1);

        // Act
        var account = Account.Create("Credit Card", AccountType.CreditCard, initialBalance, initialBalanceDate);

        // Assert
        Assert.Equal(-2500.00m, account.InitialBalance.Amount);
    }

    [Fact]
    public void UpdateInitialBalance_Changes_Balance_And_UpdatedAt()
    {
        // Arrange
        var account = Account.Create("Checking", AccountType.Checking);
        var originalUpdatedAt = account.UpdatedAt;
        var newBalance = MoneyValue.Create("USD", 2000.00m);
        var newDate = new DateOnly(2026, 1, 15);

        // Act
        account.UpdateInitialBalance(newBalance, newDate);

        // Assert
        Assert.Equal(newBalance, account.InitialBalance);
        Assert.Equal(newDate, account.InitialBalanceDate);
        Assert.True(account.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void UpdateInitialBalance_With_Null_Balance_Throws()
    {
        // Arrange
        var account = Account.Create("Checking", AccountType.Checking);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => account.UpdateInitialBalance(null!, new DateOnly(2026, 1, 1)));
        Assert.Contains("balance", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
