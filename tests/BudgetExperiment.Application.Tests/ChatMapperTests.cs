// <copyright file="ChatMapperTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Shouldly;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for <see cref="ChatMapper"/> polymorphic action mapping.
/// </summary>
public class ChatMapperTests
{
    [Fact]
    public void ToDto_CreateTransactionAction_MapsTransactionFields()
    {
        var accountId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var action = new CreateTransactionAction
        {
            AccountId = accountId,
            AccountName = "Checking",
            Amount = -45.99m,
            Date = new DateOnly(2026, 3, 1),
            Description = "Grocery Store",
            Category = "Food",
            CategoryId = categoryId,
        };

        var dto = ChatMapper.ToDto(action);

        dto.Type.ShouldBe(ChatActionType.CreateTransaction);
        dto.AccountId.ShouldBe(accountId);
        dto.AccountName.ShouldBe("Checking");
        dto.Amount.ShouldBe(-45.99m);
        dto.Date.ShouldBe(new DateOnly(2026, 3, 1));
        dto.Description.ShouldBe("Grocery Store");
        dto.CategoryId.ShouldBe(categoryId);
        dto.CategoryName.ShouldBe("Food");
    }

    [Fact]
    public void ToDto_CreateTransferAction_MapsTransferFields()
    {
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        var action = new CreateTransferAction
        {
            FromAccountId = fromId,
            FromAccountName = "Checking",
            ToAccountId = toId,
            ToAccountName = "Savings",
            Amount = 500m,
            Date = new DateOnly(2026, 3, 15),
            Description = "Monthly Savings",
        };

        var dto = ChatMapper.ToDto(action);

        dto.Type.ShouldBe(ChatActionType.CreateTransfer);
        dto.FromAccountId.ShouldBe(fromId);
        dto.FromAccountName.ShouldBe("Checking");
        dto.ToAccountId.ShouldBe(toId);
        dto.ToAccountName.ShouldBe("Savings");
        dto.Amount.ShouldBe(500m);
        dto.Date.ShouldBe(new DateOnly(2026, 3, 15));
        dto.Description.ShouldBe("Monthly Savings");
    }

    [Fact]
    public void ToDto_CreateRecurringTransactionAction_MapsRecurringFields()
    {
        var accountId = Guid.NewGuid();
        var action = new CreateRecurringTransactionAction
        {
            AccountId = accountId,
            AccountName = "Checking",
            Amount = -15.99m,
            StartDate = new DateOnly(2026, 4, 1),
            Description = "Netflix",
            Category = "Entertainment",
            Recurrence = RecurrencePatternValue.CreateMonthly(1, 1),
        };

        var dto = ChatMapper.ToDto(action);

        dto.Type.ShouldBe(ChatActionType.CreateRecurringTransaction);
        dto.AccountId.ShouldBe(accountId);
        dto.Amount.ShouldBe(-15.99m);
        dto.Date.ShouldBe(new DateOnly(2026, 4, 1));
        dto.Description.ShouldBe("Netflix");
        dto.CategoryName.ShouldBe("Entertainment");
    }

    [Fact]
    public void ToDto_CreateRecurringTransferAction_MapsDualAccountFields()
    {
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        var action = new CreateRecurringTransferAction
        {
            FromAccountId = fromId,
            FromAccountName = "Checking",
            ToAccountId = toId,
            ToAccountName = "Savings",
            Amount = 1000m,
            StartDate = new DateOnly(2026, 5, 1),
            Description = "Bi-weekly savings",
            Recurrence = RecurrencePatternValue.CreateBiWeekly(DayOfWeek.Friday),
        };

        var dto = ChatMapper.ToDto(action);

        dto.Type.ShouldBe(ChatActionType.CreateRecurringTransfer);
        dto.FromAccountId.ShouldBe(fromId);
        dto.ToAccountId.ShouldBe(toId);
        dto.Amount.ShouldBe(1000m);
        dto.Date.ShouldBe(new DateOnly(2026, 5, 1));
        dto.Description.ShouldBe("Bi-weekly savings");
    }

    [Fact]
    public void ToDto_ClarificationNeededAction_MapsQuestionAndOptions()
    {
        var entityId = Guid.NewGuid();
        var action = new ClarificationNeededAction
        {
            Question = "Which account?",
            FieldName = "accountId",
            Options = new List<ClarificationOption>
            {
                new ClarificationOption { Label = "Checking", Value = "checking", EntityId = entityId },
                new ClarificationOption { Label = "Savings", Value = "savings", EntityId = null },
            },
        };

        var dto = ChatMapper.ToDto(action);

        dto.Type.ShouldBe(ChatActionType.ClarificationNeeded);
        dto.ClarificationQuestion.ShouldBe("Which account?");
        dto.ClarificationFieldName.ShouldBe("accountId");
        dto.Options.ShouldNotBeNull();
        dto.Options!.Count.ShouldBe(2);
        dto.Options[0].Label.ShouldBe("Checking");
        dto.Options[0].EntityId.ShouldBe(entityId);
        dto.Options[1].EntityId.ShouldBeNull();
    }

    [Fact]
    public void ToDto_MessageWithNullAction_MapsActionAsNull()
    {
        var session = ChatSession.Create();
        var message = ChatMessage.CreateUserMessage(session.Id, "Hello");

        var dto = ChatMapper.ToDto(message);

        dto.Action.ShouldBeNull();
        dto.Content.ShouldBe("Hello");
    }
}
