// <copyright file="NaturalLanguageParserTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>


using BudgetExperiment.Domain;
using Shouldly;
using Xunit;

namespace BudgetExperiment.Application.Tests.Services;

/// <summary>
/// Unit tests for <see cref="NaturalLanguageParser"/>.
/// </summary>
public class NaturalLanguageParserTests
{
    private readonly MockAiService _mockAiService;
    private readonly NaturalLanguageParser _parser;

    private readonly List<AccountInfo> _accounts;
    private readonly List<CategoryInfo> _categories;

    public NaturalLanguageParserTests()
    {
        this._mockAiService = new MockAiService();
        this._parser = new NaturalLanguageParser(this._mockAiService);

        this._accounts = new List<AccountInfo>
        {
            new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Checking", AccountType.Checking),
            new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Savings", AccountType.Savings),
            new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Credit Card", AccountType.CreditCard),
        };

        this._categories = new List<CategoryInfo>
        {
            new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Groceries"),
            new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Utilities"),
            new(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Entertainment"),
        };
    }

    [Fact]
    public async Task ParseCommandAsync_EmptyInput_ReturnsFailure()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = await this._parser.ParseCommandAsync(input, this._accounts, this._categories);

        // Assert
        result.Success.ShouldBeFalse();
        result.Action.ShouldBeNull();
        result.ErrorMessage.ShouldBe("Empty input");
    }

    [Fact]
    public async Task ParseCommandAsync_WhitespaceInput_ReturnsFailure()
    {
        // Arrange
        var input = "   ";

        // Act
        var result = await this._parser.ParseCommandAsync(input, this._accounts, this._categories);

        // Assert
        result.Success.ShouldBeFalse();
        result.Action.ShouldBeNull();
    }

    [Fact]
    public async Task ParseCommandAsync_AiServiceFails_ReturnsFailure()
    {
        // Arrange
        this._mockAiService.SetupFailure("AI service unavailable");

        // Act
        var result = await this._parser.ParseCommandAsync("Add $50 groceries", this._accounts, this._categories);

        // Assert
        result.Success.ShouldBeFalse();
        result.Action.ShouldBeNull();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("AI service unavailable");
    }

    [Fact]
    public async Task ParseCommandAsync_TransactionIntent_ReturnsCreateTransactionAction()
    {
        // Arrange
        var aiResponse = """
            {
                "intent": "transaction",
                "confidence": 0.95,
                "response": "I'll add a $45.99 grocery purchase at Walmart to your Checking account.",
                "data": {
                    "accountId": "11111111-1111-1111-1111-111111111111",
                    "accountName": "Checking",
                    "amount": -45.99,
                    "date": "2026-01-15",
                    "description": "Walmart",
                    "category": "Groceries",
                    "categoryId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
                }
            }
            """;
        this._mockAiService.SetupSuccess(aiResponse);

        // Act
        var result = await this._parser.ParseCommandAsync(
            "Add $45.99 groceries at Walmart to checking",
            this._accounts,
            this._categories);

        // Assert
        result.Success.ShouldBeTrue();
        result.Action.ShouldBeOfType<CreateTransactionAction>();

        var action = (CreateTransactionAction)result.Action!;
        action.AccountId.ShouldBe(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        action.AccountName.ShouldBe("Checking");
        action.Amount.ShouldBe(-45.99m);
        action.Date.ShouldBe(new DateOnly(2026, 1, 15));
        action.Description.ShouldBe("Walmart");
        action.Category.ShouldBe("Groceries");
        result.Confidence.ShouldBe(0.95m);
    }

    [Fact]
    public async Task ParseCommandAsync_TransactionWithAccountNameLookup_ResolvesAccount()
    {
        // Arrange
        var aiResponse = """
            {
                "intent": "transaction",
                "confidence": 0.9,
                "response": "Adding $20 ATM withdrawal.",
                "data": {
                    "accountName": "Checking",
                    "amount": -20.00,
                    "date": "2026-01-16",
                    "description": "ATM Withdrawal"
                }
            }
            """;
        this._mockAiService.SetupSuccess(aiResponse);

        // Act
        var result = await this._parser.ParseCommandAsync(
            "Withdrew $20 from ATM from checking",
            this._accounts,
            this._categories);

        // Assert
        result.Success.ShouldBeTrue();
        var action = result.Action.ShouldBeOfType<CreateTransactionAction>();
        action.AccountId.ShouldBe(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public async Task ParseCommandAsync_TransactionWithSingleAccount_DefaultsToOnlyAccount()
    {
        // Arrange
        var singleAccount = new List<AccountInfo>
        {
            new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "My Account", AccountType.Checking),
        };

        var aiResponse = """
            {
                "intent": "transaction",
                "confidence": 0.85,
                "response": "Adding $10 purchase.",
                "data": {
                    "amount": -10.00,
                    "date": "2026-01-17",
                    "description": "Coffee"
                }
            }
            """;
        this._mockAiService.SetupSuccess(aiResponse);

        // Act
        var result = await this._parser.ParseCommandAsync(
            "Spent $10 on coffee",
            singleAccount,
            this._categories);

        // Assert
        result.Success.ShouldBeTrue();
        var action = result.Action.ShouldBeOfType<CreateTransactionAction>();
        action.AccountId.ShouldBe(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        action.AccountName.ShouldBe("My Account");
    }

    [Fact]
    public async Task ParseCommandAsync_TransferIntent_ReturnsCreateTransferAction()
    {
        // Arrange
        var aiResponse = """
            {
                "intent": "transfer",
                "confidence": 0.92,
                "response": "I'll transfer $500 from Checking to Savings.",
                "data": {
                    "fromAccountId": "11111111-1111-1111-1111-111111111111",
                    "fromAccountName": "Checking",
                    "toAccountId": "22222222-2222-2222-2222-222222222222",
                    "toAccountName": "Savings",
                    "amount": 500.00,
                    "date": "2026-01-18",
                    "description": "Monthly savings transfer"
                }
            }
            """;
        this._mockAiService.SetupSuccess(aiResponse);

        // Act
        var result = await this._parser.ParseCommandAsync(
            "Transfer $500 from checking to savings",
            this._accounts,
            this._categories);

        // Assert
        result.Success.ShouldBeTrue();
        result.Action.ShouldBeOfType<CreateTransferAction>();

        var action = (CreateTransferAction)result.Action!;
        action.FromAccountId.ShouldBe(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        action.FromAccountName.ShouldBe("Checking");
        action.ToAccountId.ShouldBe(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        action.ToAccountName.ShouldBe("Savings");
        action.Amount.ShouldBe(500.00m);
    }

    [Fact]
    public async Task ParseCommandAsync_TransferWithNegativeAmount_ConvertsToPositive()
    {
        // Arrange
        var aiResponse = """
            {
                "intent": "transfer",
                "confidence": 0.9,
                "response": "Transferring $100.",
                "data": {
                    "fromAccountId": "11111111-1111-1111-1111-111111111111",
                    "fromAccountName": "Checking",
                    "toAccountId": "22222222-2222-2222-2222-222222222222",
                    "toAccountName": "Savings",
                    "amount": -100.00,
                    "date": "2026-01-19"
                }
            }
            """;
        this._mockAiService.SetupSuccess(aiResponse);

        // Act
        var result = await this._parser.ParseCommandAsync(
            "Move $100 to savings",
            this._accounts,
            this._categories);

        // Assert
        result.Success.ShouldBeTrue();
        var action = result.Action.ShouldBeOfType<CreateTransferAction>();
        action.Amount.ShouldBe(100.00m);
    }

    [Fact]
    public async Task ParseCommandAsync_RecurringTransactionIntent_ReturnsCreateRecurringTransactionAction()
    {
        // Arrange
        var aiResponse = """
            {
                "intent": "recurring_transaction",
                "confidence": 0.88,
                "response": "I'll set up a monthly $1500 rent payment on the 1st.",
                "data": {
                    "accountId": "11111111-1111-1111-1111-111111111111",
                    "accountName": "Checking",
                    "amount": -1500.00,
                    "description": "Rent",
                    "category": "Housing",
                    "frequency": "monthly",
                    "interval": 1,
                    "dayOfMonth": 1,
                    "startDate": "2026-02-01"
                }
            }
            """;
        this._mockAiService.SetupSuccess(aiResponse);

        // Act
        var result = await this._parser.ParseCommandAsync(
            "Set up monthly rent payment of $1500 on the 1st starting February",
            this._accounts,
            this._categories);

        // Assert
        result.Success.ShouldBeTrue();
        result.Action.ShouldBeOfType<CreateRecurringTransactionAction>();

        var action = (CreateRecurringTransactionAction)result.Action!;
        action.AccountId.ShouldBe(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        action.Amount.ShouldBe(-1500.00m);
        action.Description.ShouldBe("Rent");
        action.Recurrence.ShouldNotBeNull();
        action.Recurrence.Frequency.ShouldBe(RecurrenceFrequency.Monthly);
        action.Recurrence.DayOfMonth.ShouldBe(1);
        action.StartDate.ShouldBe(new DateOnly(2026, 2, 1));
    }

    [Fact]
    public async Task ParseCommandAsync_WeeklyRecurringTransaction_ParsesCorrectly()
    {
        // Arrange
        var aiResponse = """
            {
                "intent": "recurring_transaction",
                "confidence": 0.85,
                "response": "Setting up weekly grocery shopping.",
                "data": {
                    "accountId": "11111111-1111-1111-1111-111111111111",
                    "accountName": "Checking",
                    "amount": -150.00,
                    "description": "Grocery shopping",
                    "category": "Groceries",
                    "frequency": "weekly",
                    "interval": 1,
                    "dayOfWeek": "Saturday",
                    "startDate": "2026-01-25"
                }
            }
            """;
        this._mockAiService.SetupSuccess(aiResponse);

        // Act
        var result = await this._parser.ParseCommandAsync(
            "Every Saturday spend $150 on groceries",
            this._accounts,
            this._categories);

        // Assert
        result.Success.ShouldBeTrue();
        var action = result.Action.ShouldBeOfType<CreateRecurringTransactionAction>();
        action.Recurrence.Frequency.ShouldBe(RecurrenceFrequency.Weekly);
        action.Recurrence.DayOfWeek.ShouldBe(DayOfWeek.Saturday);
    }

    [Fact]
    public async Task ParseCommandAsync_RecurringTransferIntent_ReturnsCreateRecurringTransferAction()
    {
        // Arrange
        var aiResponse = """
            {
                "intent": "recurring_transfer",
                "confidence": 0.91,
                "response": "Setting up bi-weekly transfer to savings.",
                "data": {
                    "fromAccountId": "11111111-1111-1111-1111-111111111111",
                    "fromAccountName": "Checking",
                    "toAccountId": "22222222-2222-2222-2222-222222222222",
                    "toAccountName": "Savings",
                    "amount": 200.00,
                    "description": "Bi-weekly savings",
                    "frequency": "biweekly",
                    "dayOfWeek": "Friday",
                    "startDate": "2026-01-24"
                }
            }
            """;
        this._mockAiService.SetupSuccess(aiResponse);

        // Act
        var result = await this._parser.ParseCommandAsync(
            "Transfer $200 to savings every other Friday",
            this._accounts,
            this._categories);

        // Assert
        result.Success.ShouldBeTrue();
        result.Action.ShouldBeOfType<CreateRecurringTransferAction>();

        var action = (CreateRecurringTransferAction)result.Action!;
        action.FromAccountId.ShouldBe(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        action.ToAccountId.ShouldBe(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        action.Amount.ShouldBe(200.00m);
        action.Recurrence.Frequency.ShouldBe(RecurrenceFrequency.BiWeekly);
    }

    [Fact]
    public async Task ParseCommandAsync_ClarificationNeeded_ReturnsClarificationAction()
    {
        // Arrange
        var aiResponse = """
            {
                "intent": "transaction",
                "confidence": 0.6,
                "response": "Which account should I use for this transaction?",
                "clarification": {
                    "needed": true,
                    "question": "Which account should I use?",
                    "field": "account",
                    "options": [
                        {"label": "Checking", "value": "Checking", "entityId": "11111111-1111-1111-1111-111111111111"},
                        {"label": "Savings", "value": "Savings", "entityId": "22222222-2222-2222-2222-222222222222"},
                        {"label": "Credit Card", "value": "Credit Card", "entityId": "33333333-3333-3333-3333-333333333333"}
                    ]
                }
            }
            """;
        this._mockAiService.SetupSuccess(aiResponse);

        // Act
        var result = await this._parser.ParseCommandAsync(
            "Add $50 groceries",
            this._accounts,
            this._categories);

        // Assert
        result.Success.ShouldBeTrue();
        result.Action.ShouldBeOfType<ClarificationNeededAction>();

        var action = (ClarificationNeededAction)result.Action!;
        action.Question.ShouldBe("Which account should I use?");
        action.FieldName.ShouldBe("account");
        action.Options.Count.ShouldBe(3);
        action.Options[0].Label.ShouldBe("Checking");
        action.Options[0].EntityId.ShouldBe(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public async Task ParseCommandAsync_UnknownIntent_ReturnsFailure()
    {
        // Arrange
        var aiResponse = """
            {
                "intent": "unknown",
                "confidence": 0.3,
                "response": "I'm not sure what you want me to do. Try saying something like 'Add $50 groceries' or 'Transfer $100 to savings'."
            }
            """;
        this._mockAiService.SetupSuccess(aiResponse);

        // Act
        var result = await this._parser.ParseCommandAsync(
            "What's the weather?",
            this._accounts,
            this._categories);

        // Assert
        result.Success.ShouldBeFalse();
        result.Action.ShouldBeNull();
        result.ResponseText.ShouldContain("I'm not sure");
    }

    [Fact]
    public async Task ParseCommandAsync_InvalidJson_ReturnsFailure()
    {
        // Arrange
        this._mockAiService.SetupSuccess("This is not valid JSON");

        // Act
        var result = await this._parser.ParseCommandAsync(
            "Add transaction",
            this._accounts,
            this._categories);

        // Assert
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("No JSON found");
    }

    [Fact]
    public async Task ParseCommandAsync_JsonWithExtraText_ExtractsJsonCorrectly()
    {
        // Arrange
        var aiResponse = """
            Here's the parsed result:
            {
                "intent": "transaction",
                "confidence": 0.9,
                "response": "Adding $25 coffee.",
                "data": {
                    "accountId": "11111111-1111-1111-1111-111111111111",
                    "amount": -25.00,
                    "date": "2026-01-20",
                    "description": "Starbucks"
                }
            }
            Hope that helps!
            """;
        this._mockAiService.SetupSuccess(aiResponse);

        // Act
        var result = await this._parser.ParseCommandAsync(
            "$25 at Starbucks",
            this._accounts,
            this._categories);

        // Assert
        result.Success.ShouldBeTrue();
        result.Action.ShouldBeOfType<CreateTransactionAction>();
    }

    [Fact]
    public async Task ParseCommandAsync_WithChatContext_IncludesContextInPrompt()
    {
        // Arrange
        var context = new ChatContext(
            CurrentAccountName: "Checking",
            CurrentCategoryName: "Groceries",
            CurrentDate: new DateOnly(2026, 1, 15),
            CurrentPage: "transactions");
        var aiResponse = """
            {
                "intent": "transaction",
                "confidence": 0.95,
                "response": "Adding $30 groceries.",
                "data": {
                    "accountId": "11111111-1111-1111-1111-111111111111",
                    "amount": -30.00,
                    "date": "2026-01-15",
                    "description": "Grocery store"
                }
            }
            """;
        this._mockAiService.SetupSuccess(aiResponse);

        // Act
        var result = await this._parser.ParseCommandAsync(
            "Add $30",
            this._accounts,
            this._categories,
            context);

        // Assert
        result.Success.ShouldBeTrue();

        // Verify context was included in the prompt
        var sentPrompt = this._mockAiService.LastPrompt;
        sentPrompt.ShouldNotBeNull();
        sentPrompt.SystemPrompt.ShouldContain("Context from the user's current view:");
        sentPrompt.SystemPrompt.ShouldContain("The user has selected January 15, 2026 on the calendar.");
        sentPrompt.SystemPrompt.ShouldContain("The user is viewing the 'Checking' account.");
        sentPrompt.SystemPrompt.ShouldContain("The user is viewing the 'Groceries' category.");
        sentPrompt.SystemPrompt.ShouldContain("The user is on the transactions page.");
    }

    [Fact]
    public async Task ParseCommandAsync_TransactionWithoutDate_UsesContextDate()
    {
        // Arrange
        var context = new ChatContext(
            CurrentAccountName: "Checking",
            CurrentDate: new DateOnly(2026, 2, 10));
        var aiResponse = """
            {
                "intent": "transaction",
                "confidence": 0.9,
                "response": "Adding $25 coffee.",
                "data": {
                    "accountId": "11111111-1111-1111-1111-111111111111",
                    "amount": -25.00,
                    "description": "Coffee"
                }
            }
            """;
        this._mockAiService.SetupSuccess(aiResponse);

        // Act
        var result = await this._parser.ParseCommandAsync(
            "Add $25 coffee",
            this._accounts,
            this._categories,
            context);

        // Assert
        result.Success.ShouldBeTrue();
        var action = result.Action.ShouldBeOfType<CreateTransactionAction>();
        action.Date.ShouldBe(new DateOnly(2026, 2, 10));
    }

    [Fact]
    public async Task ParseCommandAsync_TransactionWithDate_PrefersExplicitDateOverContext()
    {
        // Arrange
        var context = new ChatContext(
            CurrentAccountName: "Checking",
            CurrentDate: new DateOnly(2026, 2, 10));
        var aiResponse = """
            {
                "intent": "transaction",
                "confidence": 0.9,
                "response": "Adding $25 coffee.",
                "data": {
                    "accountId": "11111111-1111-1111-1111-111111111111",
                    "amount": -25.00,
                    "date": "2026-02-12",
                    "description": "Coffee"
                }
            }
            """;
        this._mockAiService.SetupSuccess(aiResponse);

        // Act
        var result = await this._parser.ParseCommandAsync(
            "Add $25 coffee",
            this._accounts,
            this._categories,
            context);

        // Assert
        result.Success.ShouldBeTrue();
        var action = result.Action.ShouldBeOfType<CreateTransactionAction>();
        action.Date.ShouldBe(new DateOnly(2026, 2, 12));
    }

    [Fact]
    public async Task ParseCommandAsync_DailyRecurrence_ParsesCorrectly()
    {
        // Arrange
        var aiResponse = """
            {
                "intent": "recurring_transaction",
                "confidence": 0.9,
                "response": "Setting up daily coffee expense.",
                "data": {
                    "accountId": "11111111-1111-1111-1111-111111111111",
                    "accountName": "Checking",
                    "amount": -5.00,
                    "description": "Daily coffee",
                    "frequency": "daily",
                    "interval": 1,
                    "startDate": "2026-01-21"
                }
            }
            """;
        this._mockAiService.SetupSuccess(aiResponse);

        // Act
        var result = await this._parser.ParseCommandAsync(
            "Daily $5 coffee expense",
            this._accounts,
            this._categories);

        // Assert
        result.Success.ShouldBeTrue();
        var action = result.Action.ShouldBeOfType<CreateRecurringTransactionAction>();
        action.Recurrence.Frequency.ShouldBe(RecurrenceFrequency.Daily);
        action.Recurrence.Interval.ShouldBe(1);
    }

    [Fact]
    public async Task ParseCommandAsync_QuarterlyRecurrence_ParsesCorrectly()
    {
        // Arrange
        var aiResponse = """
            {
                "intent": "recurring_transaction",
                "confidence": 0.85,
                "response": "Setting up quarterly tax payment.",
                "data": {
                    "accountId": "11111111-1111-1111-1111-111111111111",
                    "accountName": "Checking",
                    "amount": -5000.00,
                    "description": "Quarterly tax payment",
                    "frequency": "quarterly",
                    "dayOfMonth": 15,
                    "startDate": "2026-04-15"
                }
            }
            """;
        this._mockAiService.SetupSuccess(aiResponse);

        // Act
        var result = await this._parser.ParseCommandAsync(
            "Quarterly tax payment of $5000 on the 15th",
            this._accounts,
            this._categories);

        // Assert
        result.Success.ShouldBeTrue();
        var action = result.Action.ShouldBeOfType<CreateRecurringTransactionAction>();
        action.Recurrence.Frequency.ShouldBe(RecurrenceFrequency.Quarterly);
    }

    [Fact]
    public async Task ParseCommandAsync_YearlyRecurrence_ParsesCorrectly()
    {
        // Arrange
        var aiResponse = """
            {
                "intent": "recurring_transaction",
                "confidence": 0.88,
                "response": "Setting up annual insurance premium.",
                "data": {
                    "accountId": "11111111-1111-1111-1111-111111111111",
                    "accountName": "Checking",
                    "amount": -1200.00,
                    "description": "Annual insurance",
                    "frequency": "yearly",
                    "dayOfMonth": 1,
                    "startDate": "2027-01-01"
                }
            }
            """;
        this._mockAiService.SetupSuccess(aiResponse);

        // Act
        var result = await this._parser.ParseCommandAsync(
            "Annual insurance premium of $1200 in January",
            this._accounts,
            this._categories);

        // Assert
        result.Success.ShouldBeTrue();
        var action = result.Action.ShouldBeOfType<CreateRecurringTransactionAction>();
        action.Recurrence.Frequency.ShouldBe(RecurrenceFrequency.Yearly);
    }

    [Fact]
    public async Task ParseCommandAsync_TransferWithAccountNameResolution_ResolvesFromAndToAccounts()
    {
        // Arrange
        var aiResponse = """
            {
                "intent": "transfer",
                "confidence": 0.9,
                "response": "Transferring $300.",
                "data": {
                    "fromAccountName": "Checking",
                    "toAccountName": "Credit Card",
                    "amount": 300.00,
                    "date": "2026-01-22",
                    "description": "Credit card payment"
                }
            }
            """;
        this._mockAiService.SetupSuccess(aiResponse);

        // Act
        var result = await this._parser.ParseCommandAsync(
            "Pay $300 to credit card from checking",
            this._accounts,
            this._categories);

        // Assert
        result.Success.ShouldBeTrue();
        var action = result.Action.ShouldBeOfType<CreateTransferAction>();
        action.FromAccountId.ShouldBe(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        action.ToAccountId.ShouldBe(Guid.Parse("33333333-3333-3333-3333-333333333333"));
    }

    [Fact]
    public async Task ParseCommandAsync_TransferMissingFromAccount_ReturnsFailure()
    {
        // Arrange
        var aiResponse = """
            {
                "intent": "transfer",
                "confidence": 0.7,
                "response": "Transferring money.",
                "data": {
                    "fromAccountName": "NonExistent",
                    "toAccountName": "Savings",
                    "amount": 100.00,
                    "date": "2026-01-23"
                }
            }
            """;
        this._mockAiService.SetupSuccess(aiResponse);

        // Act
        var result = await this._parser.ParseCommandAsync(
            "Transfer from nonexistent to savings",
            this._accounts,
            this._categories);

        // Assert
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("Failed to parse");
    }

    /// <summary>
    /// Mock AI service for testing.
    /// </summary>
    private sealed class MockAiService : IAiService
    {
        private bool _shouldFail;
        private string _responseContent = string.Empty;
        private string _errorMessage = string.Empty;

        public AiPrompt? LastPrompt { get; private set; }

        public void SetupSuccess(string content)
        {
            this._shouldFail = false;
            this._responseContent = content;
        }

        public void SetupFailure(string errorMessage)
        {
            this._shouldFail = true;
            this._errorMessage = errorMessage;
        }

        public Task<AiServiceStatus> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AiServiceStatus(
                IsAvailable: !this._shouldFail,
                CurrentModel: "test-model",
                ErrorMessage: this._shouldFail ? this._errorMessage : null));
        }

        public Task<IReadOnlyList<AiModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AiModelInfo>>(new List<AiModelInfo>
            {
                new("test-model", DateTime.UtcNow, 1000000),
            });
        }

        public Task<AiResponse> CompleteAsync(AiPrompt prompt, CancellationToken cancellationToken = default)
        {
            this.LastPrompt = prompt;

            if (this._shouldFail)
            {
                return Task.FromResult(new AiResponse(
                    Success: false,
                    Content: string.Empty,
                    ErrorMessage: this._errorMessage,
                    TokensUsed: 0,
                    Duration: TimeSpan.Zero));
            }

            return Task.FromResult(new AiResponse(
                Success: true,
                Content: this._responseContent,
                ErrorMessage: null,
                TokensUsed: 100,
                Duration: TimeSpan.FromMilliseconds(500)));
        }
    }
}
