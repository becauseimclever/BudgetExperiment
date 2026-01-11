// <copyright file="TransactionTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the Transaction entity.
/// </summary>
public class TransactionTests
{
    [Fact]
    public void Create_With_Valid_Data_Creates_Transaction()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = MoneyValue.Create("USD", 100.50m);
        var date = new DateOnly(2026, 1, 9);
        var description = "Test Transaction";

        // Act
        var transaction = Transaction.Create(accountId, amount, date, description);

        // Assert
        Assert.NotEqual(Guid.Empty, transaction.Id);
        Assert.Equal(accountId, transaction.AccountId);
        Assert.Equal(amount, transaction.Amount);
        Assert.Equal(date, transaction.Date);
        Assert.Equal(description, transaction.Description);
        Assert.Null(transaction.Category);
        Assert.NotEqual(default, transaction.CreatedAt);
        Assert.NotEqual(default, transaction.UpdatedAt);
    }

    [Fact]
    public void Create_With_Category_Sets_Category()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = MoneyValue.Create("USD", 50.00m);
        var date = new DateOnly(2026, 1, 9);

        // Act
        var transaction = Transaction.Create(accountId, amount, date, "Groceries", category: "Food");

        // Assert
        Assert.Equal("Food", transaction.Category);
    }

    [Fact]
    public void Create_Trims_Description()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = MoneyValue.Create("USD", 50.00m);
        var date = new DateOnly(2026, 1, 9);

        // Act
        var transaction = Transaction.Create(accountId, amount, date, "  Groceries  ");

        // Assert
        Assert.Equal("Groceries", transaction.Description);
    }

    [Fact]
    public void Create_Trims_Category()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = MoneyValue.Create("USD", 50.00m);
        var date = new DateOnly(2026, 1, 9);

        // Act
        var transaction = Transaction.Create(accountId, amount, date, "Groceries", "  Food  ");

        // Assert
        Assert.Equal("Food", transaction.Category);
    }

    [Fact]
    public void Create_With_Empty_AccountId_Throws()
    {
        // Arrange
        var amount = MoneyValue.Create("USD", 100.00m);
        var date = new DateOnly(2026, 1, 9);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => Transaction.Create(Guid.Empty, amount, date, "Test"));
        Assert.Contains("account", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_With_Null_Amount_Throws()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var date = new DateOnly(2026, 1, 9);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => Transaction.Create(accountId, null!, date, "Test"));
        Assert.Contains("amount", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Empty_Description_Throws(string? description)
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = MoneyValue.Create("USD", 100.00m);
        var date = new DateOnly(2026, 1, 9);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => Transaction.Create(accountId, amount, date, description!));
        Assert.Contains("description", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_Allows_Negative_Amount()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = MoneyValue.Create("USD", -100.00m);
        var date = new DateOnly(2026, 1, 9);

        // Act
        var transaction = Transaction.Create(accountId, amount, date, "Expense");

        // Assert
        Assert.Equal(-100.00m, transaction.Amount.Amount);
    }

    [Fact]
    public void Create_Allows_Zero_Amount()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = MoneyValue.Create("USD", 0m);
        var date = new DateOnly(2026, 1, 9);

        // Act
        var transaction = Transaction.Create(accountId, amount, date, "Zero balance");

        // Assert
        Assert.Equal(0m, transaction.Amount.Amount);
    }

    [Fact]
    public void UpdateDescription_Changes_Description_And_UpdatedAt()
    {
        // Arrange
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 9),
            "Original");
        var originalUpdatedAt = transaction.UpdatedAt;

        // Act
        transaction.UpdateDescription("New Description");

        // Assert
        Assert.Equal("New Description", transaction.Description);
        Assert.True(transaction.UpdatedAt >= originalUpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDescription_With_Empty_Value_Throws(string? description)
    {
        // Arrange
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 9),
            "Original");

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => transaction.UpdateDescription(description!));
        Assert.Contains("description", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpdateAmount_Changes_Amount_And_UpdatedAt()
    {
        // Arrange
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 9),
            "Test");
        var originalUpdatedAt = transaction.UpdatedAt;
        var newAmount = MoneyValue.Create("USD", 200m);

        // Act
        transaction.UpdateAmount(newAmount);

        // Assert
        Assert.Equal(newAmount, transaction.Amount);
        Assert.True(transaction.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void UpdateAmount_With_Null_Throws()
    {
        // Arrange
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 9),
            "Test");

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => transaction.UpdateAmount(null!));
        Assert.Contains("amount", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpdateDate_Changes_Date_And_UpdatedAt()
    {
        // Arrange
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 9),
            "Test");
        var originalUpdatedAt = transaction.UpdatedAt;
        var newDate = new DateOnly(2026, 2, 15);

        // Act
        transaction.UpdateDate(newDate);

        // Assert
        Assert.Equal(newDate, transaction.Date);
        Assert.True(transaction.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void UpdateCategory_Changes_Category_And_UpdatedAt()
    {
        // Arrange
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 9),
            "Test");
        var originalUpdatedAt = transaction.UpdatedAt;

        // Act
        transaction.UpdateCategory("Groceries");

        // Assert
        Assert.Equal("Groceries", transaction.Category);
        Assert.True(transaction.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void UpdateCategory_With_Null_Clears_Category()
    {
        // Arrange
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 9),
            "Test",
            "OldCategory");

        // Act
        transaction.UpdateCategory(null);

        // Assert
        Assert.Null(transaction.Category);
    }

    [Fact]
    public void UpdateCategory_Trims_Value()
    {
        // Arrange
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 9),
            "Test");

        // Act
        transaction.UpdateCategory("  Groceries  ");

        // Assert
        Assert.Equal("Groceries", transaction.Category);
    }

    [Fact]
    public void Create_Without_RecurringTransaction_Has_Null_RecurringTransactionId()
    {
        // Arrange & Act
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 9),
            "Test");

        // Assert
        Assert.Null(transaction.RecurringTransactionId);
        Assert.Null(transaction.RecurringInstanceDate);
    }

    [Fact]
    public void CreateFromRecurring_Sets_RecurringTransaction_Properties()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var recurringTransactionId = Guid.NewGuid();
        var instanceDate = new DateOnly(2026, 1, 15);
        var amount = MoneyValue.Create("USD", -100m);

        // Act
        var transaction = Transaction.CreateFromRecurring(
            accountId,
            amount,
            instanceDate,
            "Monthly Rent",
            recurringTransactionId,
            instanceDate);

        // Assert
        Assert.Equal(recurringTransactionId, transaction.RecurringTransactionId);
        Assert.Equal(instanceDate, transaction.RecurringInstanceDate);
    }

    [Fact]
    public void CreateFromRecurring_With_Empty_RecurringTransactionId_Throws()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = MoneyValue.Create("USD", -100m);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            Transaction.CreateFromRecurring(
                accountId,
                amount,
                new DateOnly(2026, 1, 15),
                "Test",
                Guid.Empty,
                new DateOnly(2026, 1, 15)));

        Assert.Contains("Recurring transaction ID is required", ex.Message);
    }

    [Fact]
    public void IsFromRecurringTransaction_Returns_True_When_Linked()
    {
        // Arrange
        var transaction = Transaction.CreateFromRecurring(
            Guid.NewGuid(),
            MoneyValue.Create("USD", -100m),
            new DateOnly(2026, 1, 15),
            "Monthly Rent",
            Guid.NewGuid(),
            new DateOnly(2026, 1, 15));

        // Act & Assert
        Assert.True(transaction.IsFromRecurringTransaction);
    }

    [Fact]
    public void IsFromRecurringTransaction_Returns_False_When_Not_Linked()
    {
        // Arrange
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 9),
            "Test");

        // Act & Assert
        Assert.False(transaction.IsFromRecurringTransaction);
    }

    [Fact]
    public void Create_Regular_Transaction_Has_Null_TransferId()
    {
        // Arrange & Act
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 10),
            "Regular transaction");

        // Assert
        Assert.Null(transaction.TransferId);
        Assert.Null(transaction.TransferDirection);
        Assert.False(transaction.IsTransfer);
    }

    [Fact]
    public void CreateTransfer_With_Valid_Data_Creates_Source_Transaction()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var amount = MoneyValue.Create("USD", -500m);
        var date = new DateOnly(2026, 1, 10);
        var description = "Transfer to Savings";

        // Act
        var transaction = Transaction.CreateTransfer(
            accountId,
            amount,
            date,
            description,
            transferId,
            TransferDirection.Source);

        // Assert
        Assert.NotEqual(Guid.Empty, transaction.Id);
        Assert.Equal(accountId, transaction.AccountId);
        Assert.Equal(amount, transaction.Amount);
        Assert.Equal(date, transaction.Date);
        Assert.Equal(description, transaction.Description);
        Assert.Equal(transferId, transaction.TransferId);
        Assert.Equal(TransferDirection.Source, transaction.TransferDirection);
        Assert.True(transaction.IsTransfer);
    }

    [Fact]
    public void CreateTransfer_With_Valid_Data_Creates_Destination_Transaction()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var amount = MoneyValue.Create("USD", 500m);
        var date = new DateOnly(2026, 1, 10);
        var description = "Transfer from Checking";

        // Act
        var transaction = Transaction.CreateTransfer(
            accountId,
            amount,
            date,
            description,
            transferId,
            TransferDirection.Destination);

        // Assert
        Assert.NotEqual(Guid.Empty, transaction.Id);
        Assert.Equal(accountId, transaction.AccountId);
        Assert.Equal(amount, transaction.Amount);
        Assert.Equal(date, transaction.Date);
        Assert.Equal(description, transaction.Description);
        Assert.Equal(transferId, transaction.TransferId);
        Assert.Equal(TransferDirection.Destination, transaction.TransferDirection);
        Assert.True(transaction.IsTransfer);
    }

    [Fact]
    public void CreateTransfer_With_Empty_TransferId_Throws()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            Transaction.CreateTransfer(
                Guid.NewGuid(),
                MoneyValue.Create("USD", 500m),
                new DateOnly(2026, 1, 10),
                "Transfer",
                Guid.Empty,
                TransferDirection.Source));

        Assert.Contains("Transfer ID is required", ex.Message);
    }

    [Fact]
    public void IsTransfer_Returns_True_When_TransferId_Set()
    {
        // Arrange
        var transaction = Transaction.CreateTransfer(
            Guid.NewGuid(),
            MoneyValue.Create("USD", 500m),
            new DateOnly(2026, 1, 10),
            "Transfer",
            Guid.NewGuid(),
            TransferDirection.Destination);

        // Act & Assert
        Assert.True(transaction.IsTransfer);
    }

    [Fact]
    public void IsTransfer_Returns_False_When_TransferId_Null()
    {
        // Arrange
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 10),
            "Regular");

        // Act & Assert
        Assert.False(transaction.IsTransfer);
    }

    [Fact]
    public void CreateFromRecurringTransfer_Sets_All_Properties()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var transferId = Guid.NewGuid();
        var recurringTransferId = Guid.NewGuid();
        var instanceDate = new DateOnly(2026, 2, 1);
        var amount = MoneyValue.Create("USD", -500m);

        // Act
        var transaction = Transaction.CreateFromRecurringTransfer(
            accountId,
            amount,
            instanceDate,
            "Monthly Savings Transfer",
            transferId,
            TransferDirection.Source,
            recurringTransferId,
            instanceDate);

        // Assert
        Assert.NotEqual(Guid.Empty, transaction.Id);
        Assert.Equal(accountId, transaction.AccountId);
        Assert.Equal(amount, transaction.Amount);
        Assert.Equal(instanceDate, transaction.Date);
        Assert.Equal("Monthly Savings Transfer", transaction.Description);
        Assert.Equal(transferId, transaction.TransferId);
        Assert.Equal(TransferDirection.Source, transaction.TransferDirection);
        Assert.Equal(recurringTransferId, transaction.RecurringTransferId);
        Assert.Equal(instanceDate, transaction.RecurringTransferInstanceDate);
        Assert.True(transaction.IsTransfer);
        Assert.True(transaction.IsFromRecurringTransfer);
    }

    [Fact]
    public void CreateFromRecurringTransfer_With_Empty_RecurringTransferId_Throws()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            Transaction.CreateFromRecurringTransfer(
                Guid.NewGuid(),
                MoneyValue.Create("USD", -500m),
                new DateOnly(2026, 2, 1),
                "Monthly Savings Transfer",
                Guid.NewGuid(),
                TransferDirection.Source,
                Guid.Empty,
                new DateOnly(2026, 2, 1)));

        Assert.Contains("Recurring transfer ID is required", ex.Message);
    }

    [Fact]
    public void IsFromRecurringTransfer_Returns_True_When_RecurringTransferId_Set()
    {
        // Arrange
        var transaction = Transaction.CreateFromRecurringTransfer(
            Guid.NewGuid(),
            MoneyValue.Create("USD", 500m),
            new DateOnly(2026, 2, 1),
            "Transfer",
            Guid.NewGuid(),
            TransferDirection.Destination,
            Guid.NewGuid(),
            new DateOnly(2026, 2, 1));

        // Act & Assert
        Assert.True(transaction.IsFromRecurringTransfer);
    }

    [Fact]
    public void Create_Regular_Transaction_Has_Null_RecurringTransferId()
    {
        // Arrange & Act
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 10),
            "Regular transaction");

        // Assert
        Assert.Null(transaction.RecurringTransferId);
        Assert.Null(transaction.RecurringTransferInstanceDate);
        Assert.False(transaction.IsFromRecurringTransfer);
    }
}
