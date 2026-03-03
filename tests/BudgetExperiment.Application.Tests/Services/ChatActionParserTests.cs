// <copyright file="ChatActionParserTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Chat;
using BudgetExperiment.Domain;
using Shouldly;
using Xunit;

namespace BudgetExperiment.Application.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ChatActionParser"/>.
/// </summary>
public class ChatActionParserTests
{
    private readonly List<AccountInfo> _accounts;
    private readonly List<CategoryInfo> _categories;

    public ChatActionParserTests()
    {
        _accounts = new List<AccountInfo>
        {
            new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Checking", AccountType.Checking),
            new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Savings", AccountType.Savings),
            new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Credit Card", AccountType.CreditCard),
        };

        _categories = new List<CategoryInfo>
        {
            new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Groceries"),
            new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Utilities"),
        };
    }

    [Fact]
    public void ParseResponse_TransactionIntent_ReturnsCreateTransactionAction()
    {
        // Arrange
        var json = """
            {
              "intent": "transaction",
              "confidence": 0.95,
              "response": "I'll add that transaction.",
              "data": {
                "accountName": "Checking",
                "amount": -50.00,
                "date": "2026-03-01",
                "description": "Grocery Store",
                "category": "Groceries"
              }
            }
            """;

        // Act
        var result = ChatActionParser.ParseResponse(json, _accounts, _categories, null);

        // Assert
        result.Success.ShouldBeTrue();
        result.Action.ShouldBeOfType<CreateTransactionAction>();
        var action = (CreateTransactionAction)result.Action!;
        action.AccountId.ShouldBe(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        action.Amount.ShouldBe(-50.00m);
        action.Description.ShouldBe("Grocery Store");
    }

    [Fact]
    public void ParseResponse_TransferIntent_ReturnsCreateTransferAction()
    {
        // Arrange
        var json = """
            {
              "intent": "transfer",
              "confidence": 0.92,
              "response": "Transfer created.",
              "data": {
                "fromAccountName": "Checking",
                "toAccountName": "Savings",
                "amount": 500.00,
                "date": "2026-03-01"
              }
            }
            """;

        // Act
        var result = ChatActionParser.ParseResponse(json, _accounts, _categories, null);

        // Assert
        result.Success.ShouldBeTrue();
        result.Action.ShouldBeOfType<CreateTransferAction>();
        var action = (CreateTransferAction)result.Action!;
        action.FromAccountId.ShouldBe(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        action.ToAccountId.ShouldBe(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        action.Amount.ShouldBe(500.00m);
    }

    [Fact]
    public void ParseResponse_RecurringTransactionIntent_ReturnsCreateRecurringTransactionAction()
    {
        // Arrange
        var json = """
            {
              "intent": "recurring_transaction",
              "confidence": 0.88,
              "response": "Recurring transaction created.",
              "data": {
                "accountName": "Checking",
                "amount": -100.00,
                "description": "Electric Bill",
                "frequency": "monthly",
                "dayOfMonth": 15,
                "startDate": "2026-03-15"
              }
            }
            """;

        // Act
        var result = ChatActionParser.ParseResponse(json, _accounts, _categories, null);

        // Assert
        result.Success.ShouldBeTrue();
        result.Action.ShouldBeOfType<CreateRecurringTransactionAction>();
        var action = (CreateRecurringTransactionAction)result.Action!;
        action.AccountId.ShouldBe(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        action.Amount.ShouldBe(-100.00m);
        action.Recurrence.ShouldNotBeNull();
    }

    [Fact]
    public void ParseResponse_RecurringTransferIntent_ReturnsCreateRecurringTransferAction()
    {
        // Arrange
        var json = """
            {
              "intent": "recurring_transfer",
              "confidence": 0.85,
              "response": "Recurring transfer created.",
              "data": {
                "fromAccountName": "Checking",
                "toAccountName": "Savings",
                "amount": 200.00,
                "frequency": "weekly",
                "dayOfWeek": "Friday",
                "startDate": "2026-03-01"
              }
            }
            """;

        // Act
        var result = ChatActionParser.ParseResponse(json, _accounts, _categories, null);

        // Assert
        result.Success.ShouldBeTrue();
        result.Action.ShouldBeOfType<CreateRecurringTransferAction>();
    }

    [Fact]
    public void ParseResponse_UnknownIntent_ReturnsNoAction()
    {
        // Arrange
        var json = """
            {
              "intent": "unknown",
              "confidence": 0.30,
              "response": "I didn't understand that."
            }
            """;

        // Act
        var result = ChatActionParser.ParseResponse(json, _accounts, _categories, null);

        // Assert
        result.Success.ShouldBeFalse();
        result.Action.ShouldBeNull();
    }

    [Fact]
    public void ParseResponse_ClarificationNeeded_ReturnsClarificationAction()
    {
        // Arrange
        var json = """
            {
              "intent": "transaction",
              "confidence": 0.50,
              "response": "Which account?",
              "clarification": {
                "needed": true,
                "question": "Which account should I use?",
                "field": "account",
                "options": [
                  { "label": "Checking", "value": "Checking" },
                  { "label": "Savings", "value": "Savings" }
                ]
              }
            }
            """;

        // Act
        var result = ChatActionParser.ParseResponse(json, _accounts, _categories, null);

        // Assert
        result.Success.ShouldBeTrue();
        result.Action.ShouldBeOfType<ClarificationNeededAction>();
        var action = (ClarificationNeededAction)result.Action!;
        action.Question.ShouldBe("Which account should I use?");
        action.Options.Count.ShouldBe(2);
    }

    [Fact]
    public void ParseResponse_NoJsonInContent_ReturnsFailure()
    {
        // Arrange
        var content = "Sorry, I can't help with that.";

        // Act
        var result = ChatActionParser.ParseResponse(content, _accounts, _categories, null);

        // Assert
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("No JSON found in AI response");
    }

    [Fact]
    public void ParseResponse_InvalidJson_ReturnsFailure()
    {
        // Arrange
        var content = "{ broken json }";

        // Act
        var result = ChatActionParser.ParseResponse(content, _accounts, _categories, null);

        // Assert
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
    }

    [Fact]
    public void ParseResponse_AccountResolvedByName_MatchesCaseInsensitive()
    {
        // Arrange
        var json = """
            {
              "intent": "transaction",
              "confidence": 0.95,
              "response": "Done.",
              "data": {
                "accountName": "checking",
                "amount": -25.00,
                "description": "Test"
              }
            }
            """;

        // Act
        var result = ChatActionParser.ParseResponse(json, _accounts, _categories, null);

        // Assert
        result.Success.ShouldBeTrue();
        var action = result.Action.ShouldBeOfType<CreateTransactionAction>();
        action.AccountId.ShouldBe(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public void ParseResponse_SingleAccount_DefaultsToOnly()
    {
        // Arrange
        var singleAccount = new List<AccountInfo>
        {
            new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Checking", AccountType.Checking),
        };

        var json = """
            {
              "intent": "transaction",
              "confidence": 0.90,
              "response": "Done.",
              "data": {
                "amount": -10.00,
                "description": "Coffee"
              }
            }
            """;

        // Act
        var result = ChatActionParser.ParseResponse(json, singleAccount, _categories, null);

        // Assert
        result.Success.ShouldBeTrue();
        var action = result.Action.ShouldBeOfType<CreateTransactionAction>();
        action.AccountId.ShouldBe(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public void ParseResponse_TransactionWithContext_UsesContextDate()
    {
        // Arrange
        var context = new ChatContext
        {
            CurrentDate = new DateOnly(2026, 6, 15),
            CurrentAccountName = "Checking",
        };

        var json = """
            {
              "intent": "transaction",
              "confidence": 0.90,
              "response": "Done.",
              "data": {
                "accountName": "Checking",
                "amount": -30.00,
                "description": "Lunch"
              }
            }
            """;

        // Act
        var result = ChatActionParser.ParseResponse(json, _accounts, _categories, context);

        // Assert
        result.Success.ShouldBeTrue();
        var action = result.Action.ShouldBeOfType<CreateTransactionAction>();
        action.Date.ShouldBe(new DateOnly(2026, 6, 15));
    }

    [Fact]
    public void ParseResponse_JsonWrappedInMarkdown_ExtractsCorrectly()
    {
        // Arrange
        var content = """
            Here is the response:
            ```json
            {
              "intent": "transaction",
              "confidence": 0.90,
              "response": "Done.",
              "data": {
                "accountName": "Checking",
                "amount": -40.00,
                "description": "Gas"
              }
            }
            ```
            """;

        // Act
        var result = ChatActionParser.ParseResponse(content, _accounts, _categories, null);

        // Assert
        result.Success.ShouldBeTrue();
        result.Action.ShouldBeOfType<CreateTransactionAction>();
    }

    [Fact]
    public void ParseResponse_MonthlyRecurrence_CreatesCorrectPattern()
    {
        // Arrange
        var json = """
            {
              "intent": "recurring_transaction",
              "confidence": 0.90,
              "response": "Done.",
              "data": {
                "accountName": "Checking",
                "amount": -100.00,
                "description": "Electric Bill",
                "frequency": "monthly",
                "dayOfMonth": 15,
                "startDate": "2026-03-15"
              }
            }
            """;

        // Act
        var result = ChatActionParser.ParseResponse(json, _accounts, _categories, null);

        // Assert
        result.Success.ShouldBeTrue();
        var action = result.Action.ShouldBeOfType<CreateRecurringTransactionAction>();
        action.Recurrence.ShouldNotBeNull();
        action.Recurrence.Frequency.ShouldBe(RecurrenceFrequency.Monthly);
        action.Recurrence.DayOfMonth.ShouldBe(15);
    }

    [Fact]
    public void ParseResponse_WeeklyRecurrence_WithDayOfWeek()
    {
        // Arrange
        var json = """
            {
              "intent": "recurring_transaction",
              "confidence": 0.90,
              "response": "Done.",
              "data": {
                "accountName": "Checking",
                "amount": -50.00,
                "description": "Weekly Payment",
                "frequency": "weekly",
                "dayOfWeek": "Monday",
                "startDate": "2026-03-02"
              }
            }
            """;

        // Act
        var result = ChatActionParser.ParseResponse(json, _accounts, _categories, null);

        // Assert
        result.Success.ShouldBeTrue();
        var action = result.Action.ShouldBeOfType<CreateRecurringTransactionAction>();
        action.Recurrence.ShouldNotBeNull();
        action.Recurrence.Frequency.ShouldBe(RecurrenceFrequency.Weekly);
        action.Recurrence.DayOfWeek.ShouldBe(DayOfWeek.Monday);
    }

    [Fact]
    public void ParseResponse_BiWeeklyRecurrence_DefaultsFriday()
    {
        // Arrange
        var json = """
            {
              "intent": "recurring_transaction",
              "confidence": 0.90,
              "response": "Done.",
              "data": {
                "accountName": "Checking",
                "amount": -75.00,
                "description": "Bi-weekly",
                "frequency": "biweekly",
                "startDate": "2026-03-01"
              }
            }
            """;

        // Act
        var result = ChatActionParser.ParseResponse(json, _accounts, _categories, null);

        // Assert
        result.Success.ShouldBeTrue();
        var action = result.Action.ShouldBeOfType<CreateRecurringTransactionAction>();
        action.Recurrence.ShouldNotBeNull();
        action.Recurrence.Frequency.ShouldBe(RecurrenceFrequency.BiWeekly);
        action.Recurrence.DayOfWeek.ShouldBe(DayOfWeek.Friday);
    }

    [Fact]
    public void ParseResponse_TransactionWithAccountId_UsesProvidedId()
    {
        // Arrange
        var json = """
            {
              "intent": "transaction",
              "confidence": 0.95,
              "response": "Done.",
              "data": {
                "accountId": "11111111-1111-1111-1111-111111111111",
                "amount": -20.00,
                "description": "Test"
              }
            }
            """;

        // Act
        var result = ChatActionParser.ParseResponse(json, _accounts, _categories, null);

        // Assert
        result.Success.ShouldBeTrue();
        var action = result.Action.ShouldBeOfType<CreateTransactionAction>();
        action.AccountId.ShouldBe(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public void ParseResponse_TransactionWithExplicitDate_UsesProvidedDate()
    {
        // Arrange
        var json = """
            {
              "intent": "transaction",
              "confidence": 0.95,
              "response": "Done.",
              "data": {
                "accountName": "Checking",
                "amount": -15.00,
                "date": "2026-06-15",
                "description": "Test"
              }
            }
            """;

        // Act
        var result = ChatActionParser.ParseResponse(json, _accounts, _categories, null);

        // Assert
        result.Success.ShouldBeTrue();
        var action = result.Action.ShouldBeOfType<CreateTransactionAction>();
        action.Date.ShouldBe(new DateOnly(2026, 6, 15));
    }
}
