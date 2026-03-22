// <copyright file="TransactionListItemTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Contracts.Dtos;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for <see cref="TransactionListItem"/> factory methods.
/// </summary>
public sealed class TransactionListItemTests
{
    /// <summary>
    /// Verifies FromTransaction maps all basic properties.
    /// </summary>
    [Fact]
    public void FromTransaction_MapsBasicProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var transaction = new TransactionDto
        {
            Id = id,
            Date = new DateOnly(2026, 3, 1),
            Description = "Grocery Store",
            CategoryId = categoryId,
            CategoryName = "Groceries",
            Amount = new MoneyDto { Amount = -55.00m, Currency = "USD" },
            CreatedAtUtc = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc),
        };

        // Act
        var result = TransactionListItem.FromTransaction(transaction);

        // Assert
        result.Id.ShouldBe(id);
        result.Date.ShouldBe(new DateOnly(2026, 3, 1));
        result.Description.ShouldBe("Grocery Store");
        result.CategoryId.ShouldBe(categoryId);
        result.CategoryName.ShouldBe("Groceries");
        result.Amount.Amount.ShouldBe(-55.00m);
        result.Amount.Currency.ShouldBe("USD");
        result.IsRecurring.ShouldBeFalse();
        result.IsModified.ShouldBeFalse();
        result.CreatedAtUtc.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies FromTransaction maps transfer properties.
    /// </summary>
    [Fact]
    public void FromTransaction_MapsTransferProperties()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        var transaction = new TransactionDto
        {
            Id = Guid.NewGuid(),
            Date = new DateOnly(2026, 3, 5),
            Description = "Transfer to Savings",
            Amount = new MoneyDto { Amount = -500m, Currency = "USD" },
            IsTransfer = true,
            TransferId = transferId,
            TransferDirection = "Source",
        };

        // Act
        var result = TransactionListItem.FromTransaction(transaction);

        // Assert
        result.IsTransfer.ShouldBeTrue();
        result.TransferId.ShouldBe(transferId);
        result.TransferDirection.ShouldBe("Source");
    }

    /// <summary>
    /// Verifies FromRecurringInstance maps all properties.
    /// </summary>
    [Fact]
    public void FromRecurringInstance_MapsAllProperties()
    {
        // Arrange
        var recurringId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var instance = new RecurringInstanceDto
        {
            RecurringTransactionId = recurringId,
            ScheduledDate = new DateOnly(2026, 4, 1),
            Description = "Monthly Bill",
            CategoryId = categoryId,
            CategoryName = "Utilities",
            Amount = new MoneyDto { Amount = -150m, Currency = "USD" },
            IsModified = false,
        };

        // Act
        var result = TransactionListItem.FromRecurringInstance(instance);

        // Assert
        result.Id.ShouldBe(recurringId);
        result.Date.ShouldBe(new DateOnly(2026, 4, 1));
        result.Description.ShouldBe("Monthly Bill");
        result.CategoryId.ShouldBe(categoryId);
        result.CategoryName.ShouldBe("Utilities");
        result.IsRecurring.ShouldBeTrue();
        result.IsModified.ShouldBeFalse();
        result.CreatedAtUtc.ShouldBeNull();
    }

    /// <summary>
    /// Verifies FromRecurringInstance sets IsModified when instance is modified.
    /// </summary>
    [Fact]
    public void FromRecurringInstance_SetsIsModified()
    {
        // Arrange
        var instance = new RecurringInstanceDto
        {
            RecurringTransactionId = Guid.NewGuid(),
            ScheduledDate = new DateOnly(2026, 4, 1),
            Description = "Modified Item",
            Amount = new MoneyDto { Amount = -200m, Currency = "USD" },
            IsModified = true,
        };

        // Act
        var result = TransactionListItem.FromRecurringInstance(instance);

        // Assert
        result.IsModified.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies FromRecurringTransferInstance maps source account properties correctly.
    /// </summary>
    [Fact]
    public void FromRecurringTransferInstance_Source_NegatesAmountAndSetsDescription()
    {
        // Arrange
        var recurringTransferId = Guid.NewGuid();
        var instance = new RecurringTransferInstanceDto
        {
            RecurringTransferId = recurringTransferId,
            EffectiveDate = new DateOnly(2026, 4, 1),
            Description = "Monthly Savings",
            Amount = new MoneyDto { Amount = 500m, Currency = "USD" },
            SourceAccountName = "Checking",
            DestinationAccountName = "Savings",
            IsModified = false,
        };

        // Act
        var result = TransactionListItem.FromRecurringTransferInstance(instance, isSource: true);

        // Assert
        result.Id.ShouldBe(recurringTransferId);
        result.Date.ShouldBe(new DateOnly(2026, 4, 1));
        result.Amount.Amount.ShouldBe(-500m);
        result.Description.ShouldContain("Transfer to Savings");
        result.IsRecurring.ShouldBeTrue();
        result.IsRecurringTransfer.ShouldBeTrue();
        result.IsTransfer.ShouldBeTrue();
        result.TransferDirection.ShouldBe("Source");
        result.RecurringTransferId.ShouldBe(recurringTransferId);
    }

    /// <summary>
    /// Verifies FromRecurringTransferInstance maps destination account properties correctly.
    /// </summary>
    [Fact]
    public void FromRecurringTransferInstance_Destination_KeepsAmountPositive()
    {
        // Arrange
        var instance = new RecurringTransferInstanceDto
        {
            RecurringTransferId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2026, 4, 1),
            Description = "Monthly Savings",
            Amount = new MoneyDto { Amount = 500m, Currency = "USD" },
            SourceAccountName = "Checking",
            DestinationAccountName = "Savings",
            IsModified = false,
        };

        // Act
        var result = TransactionListItem.FromRecurringTransferInstance(instance, isSource: false);

        // Assert
        result.Amount.Amount.ShouldBe(500m);
        result.Description.ShouldContain("Transfer from Checking");
        result.TransferDirection.ShouldBe("Destination");
    }

    /// <summary>
    /// Verifies FromRecurringTransferInstance sets CategoryId to null.
    /// </summary>
    [Fact]
    public void FromRecurringTransferInstance_CategoryIdIsNull()
    {
        // Arrange
        var instance = new RecurringTransferInstanceDto
        {
            RecurringTransferId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2026, 4, 1),
            Description = "Transfer",
            Amount = new MoneyDto { Amount = 100m, Currency = "USD" },
            SourceAccountName = "A",
            DestinationAccountName = "B",
            IsModified = false,
        };

        // Act
        var result = TransactionListItem.FromRecurringTransferInstance(instance, isSource: true);

        // Assert
        result.CategoryId.ShouldBeNull();
    }
}
