// <copyright file="ChatActionTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for ChatAction types.
/// </summary>
public class ChatActionTests
{
    [Fact]
    public void CreateTransactionAction_Has_Correct_Type()
    {
        // Arrange
        var action = new CreateTransactionAction
        {
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
            Amount = 50.00m,
            Date = new DateOnly(2026, 1, 19),
            Description = "Groceries at Walmart",
        };

        // Assert
        Assert.Equal(ChatActionType.CreateTransaction, action.Type);
    }

    [Fact]
    public void CreateTransactionAction_GetPreviewSummary_Returns_Formatted_String()
    {
        // Arrange
        var action = new CreateTransactionAction
        {
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
            Amount = 50.00m,
            Date = new DateOnly(2026, 1, 19),
            Description = "Groceries at Walmart",
        };

        // Act
        var summary = action.GetPreviewSummary();

        // Assert - check for amount value without currency symbol (culture-invariant)
        Assert.Contains("50.00", summary);
        Assert.Contains("Groceries at Walmart", summary);
        Assert.Contains("Checking", summary);
    }

    [Fact]
    public void CreateTransferAction_Has_Correct_Type()
    {
        // Arrange
        var action = new CreateTransferAction
        {
            FromAccountId = Guid.NewGuid(),
            FromAccountName = "Checking",
            ToAccountId = Guid.NewGuid(),
            ToAccountName = "Savings",
            Amount = 500.00m,
            Date = new DateOnly(2026, 1, 19),
        };

        // Assert
        Assert.Equal(ChatActionType.CreateTransfer, action.Type);
    }

    [Fact]
    public void CreateTransferAction_GetPreviewSummary_Returns_Formatted_String()
    {
        // Arrange
        var action = new CreateTransferAction
        {
            FromAccountId = Guid.NewGuid(),
            FromAccountName = "Checking",
            ToAccountId = Guid.NewGuid(),
            ToAccountName = "Savings",
            Amount = 500.00m,
            Date = new DateOnly(2026, 1, 19),
        };

        // Act
        var summary = action.GetPreviewSummary();

        // Assert - check for amount value without currency symbol (culture-invariant)
        Assert.Contains("500.00", summary);
        Assert.Contains("Checking", summary);
        Assert.Contains("Savings", summary);
        Assert.Contains("Transfer", summary);
    }

    [Fact]
    public void CreateRecurringTransactionAction_Has_Correct_Type()
    {
        // Arrange
        var action = new CreateRecurringTransactionAction
        {
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
            Amount = 1800.00m,
            Description = "Rent",
            Recurrence = RecurrencePattern.CreateMonthly(1, 1),
            StartDate = new DateOnly(2026, 2, 1),
        };

        // Assert
        Assert.Equal(ChatActionType.CreateRecurringTransaction, action.Type);
    }

    [Fact]
    public void CreateRecurringTransactionAction_GetPreviewSummary_Includes_Recurrence()
    {
        // Arrange
        var action = new CreateRecurringTransactionAction
        {
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
            Amount = 1800.00m,
            Description = "Rent",
            Recurrence = RecurrencePattern.CreateMonthly(1, 1),
            StartDate = new DateOnly(2026, 2, 1),
        };

        // Act
        var summary = action.GetPreviewSummary();

        // Assert - check for amount value without currency symbol (culture-invariant)
        Assert.Contains("1,800.00", summary);
        Assert.Contains("Rent", summary);
        Assert.Contains("Recurring", summary);
    }

    [Fact]
    public void CreateRecurringTransferAction_Has_Correct_Type()
    {
        // Arrange
        var action = new CreateRecurringTransferAction
        {
            FromAccountId = Guid.NewGuid(),
            FromAccountName = "Checking",
            ToAccountId = Guid.NewGuid(),
            ToAccountName = "Savings",
            Amount = 200.00m,
            Recurrence = RecurrencePattern.CreateWeekly(1, DayOfWeek.Friday),
            StartDate = new DateOnly(2026, 1, 24),
        };

        // Assert
        Assert.Equal(ChatActionType.CreateRecurringTransfer, action.Type);
    }

    [Fact]
    public void CreateRecurringTransferAction_GetPreviewSummary_Includes_Accounts()
    {
        // Arrange
        var action = new CreateRecurringTransferAction
        {
            FromAccountId = Guid.NewGuid(),
            FromAccountName = "Checking",
            ToAccountId = Guid.NewGuid(),
            ToAccountName = "Savings",
            Amount = 200.00m,
            Recurrence = RecurrencePattern.CreateWeekly(1, DayOfWeek.Friday),
            StartDate = new DateOnly(2026, 1, 24),
        };

        // Act
        var summary = action.GetPreviewSummary();

        // Assert - check for amount value without currency symbol (culture-invariant)
        Assert.Contains("200.00", summary);
        Assert.Contains("Checking", summary);
        Assert.Contains("Savings", summary);
        Assert.Contains("Recurring", summary);
    }

    [Fact]
    public void ClarificationNeededAction_Has_Correct_Type()
    {
        // Arrange
        var action = new ClarificationNeededAction
        {
            Question = "Which account should I use?",
            FieldName = "AccountId",
            Options = new List<ClarificationOption>
            {
                new ClarificationOption { Label = "Checking", Value = "checking", EntityId = Guid.NewGuid() },
                new ClarificationOption { Label = "Savings", Value = "savings", EntityId = Guid.NewGuid() },
            },
        };

        // Assert
        Assert.Equal(ChatActionType.ClarificationNeeded, action.Type);
    }

    [Fact]
    public void ClarificationNeededAction_GetPreviewSummary_Returns_Question()
    {
        // Arrange
        var action = new ClarificationNeededAction
        {
            Question = "Which account should I use?",
            FieldName = "AccountId",
            Options = new List<ClarificationOption>(),
        };

        // Act
        var summary = action.GetPreviewSummary();

        // Assert
        Assert.Equal("Which account should I use?", summary);
    }

    [Fact]
    public void ClarificationOption_Stores_EntityId()
    {
        // Arrange
        var entityId = Guid.NewGuid();

        // Act
        var option = new ClarificationOption
        {
            Label = "Checking Account",
            Value = "checking",
            EntityId = entityId,
        };

        // Assert
        Assert.Equal(entityId, option.EntityId);
        Assert.Equal("Checking Account", option.Label);
        Assert.Equal("checking", option.Value);
    }
}
