// <copyright file="ChatMessageRepositoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure.Repositories;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for <see cref="ChatMessageRepository"/>.
/// </summary>
[Collection("InMemoryDb")]
public class ChatMessageRepositoryTests
{
    private readonly InMemoryDbFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessageRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared in-memory database fixture.</param>
    public ChatMessageRepositoryTests(InMemoryDbFixture fixture)
    {
        this._fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_Persists_Message()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var sessionRepo = new ChatSessionRepository(context);
        var messageRepo = new ChatMessageRepository(context);

        var session = ChatSession.Create();
        await sessionRepo.AddAsync(session);

        var message = session.AddUserMessage("Hello AI");
        await context.SaveChangesAsync();

        // Assert - use shared context to verify persistence
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new ChatMessageRepository(verifyContext);
        var retrieved = await verifyRepo.GetByIdAsync(message.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(message.Id, retrieved.Id);
        Assert.Equal("Hello AI", retrieved.Content);
        Assert.Equal(ChatRole.User, retrieved.Role);
    }

    [Fact]
    public async Task GetBySessionAsync_Returns_Messages_In_Chronological_Order()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var sessionRepo = new ChatSessionRepository(context);

        var session = ChatSession.Create();
        await sessionRepo.AddAsync(session);

        session.AddUserMessage("First message");
        await Task.Delay(10); // Ensure different timestamps
        session.AddAssistantMessage("Second message");
        await Task.Delay(10);
        session.AddUserMessage("Third message");
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new ChatMessageRepository(verifyContext);
        var messages = await verifyRepo.GetBySessionAsync(session.Id);

        // Assert
        Assert.Equal(3, messages.Count);
        Assert.Equal("First message", messages[0].Content);
        Assert.Equal("Second message", messages[1].Content);
        Assert.Equal("Third message", messages[2].Content);
    }

    [Fact]
    public async Task GetBySessionAsync_Respects_Limit()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var sessionRepo = new ChatSessionRepository(context);

        var session = ChatSession.Create();
        await sessionRepo.AddAsync(session);

        for (int i = 1; i <= 10; i++)
        {
            session.AddUserMessage($"Message {i}");
        }

        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new ChatMessageRepository(verifyContext);
        var messages = await verifyRepo.GetBySessionAsync(session.Id, limit: 5);

        // Assert
        Assert.Equal(5, messages.Count);
    }

    [Fact]
    public async Task GetPendingActionsAsync_Returns_Only_Pending_Messages()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var sessionRepo = new ChatSessionRepository(context);

        var session = ChatSession.Create();
        await sessionRepo.AddAsync(session);

        session.AddUserMessage("Hello");

        var pendingAction = new CreateTransactionAction
        {
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
            Amount = 50m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Description = "Test",
        };
        var pendingMessage = session.AddAssistantMessage("Here's a transaction:", pendingAction);

        session.AddAssistantMessage("Just a text response");

        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new ChatMessageRepository(verifyContext);
        var pendingMessages = await verifyRepo.GetPendingActionsAsync(session.Id);

        // Assert
        Assert.Single(pendingMessages);
        Assert.Equal(pendingMessage.Id, pendingMessages[0].Id);
        Assert.Equal(ChatActionStatus.Pending, pendingMessages[0].ActionStatus);
    }

    [Fact]
    public async Task Message_With_Action_Persists_Action_As_Json()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var sessionRepo = new ChatSessionRepository(context);

        var session = ChatSession.Create();
        await sessionRepo.AddAsync(session);

        var action = new CreateTransactionAction
        {
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
            Amount = 123.45m,
            Date = new DateOnly(2026, 1, 19),
            Description = "Groceries at Walmart",
            Category = "Groceries",
            CategoryId = Guid.NewGuid(),
        };
        session.AddAssistantMessage("Here's your transaction:", action);
        await context.SaveChangesAsync();

        // Act - retrieve with fresh context
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new ChatMessageRepository(verifyContext);
        var messages = await verifyRepo.GetBySessionAsync(session.Id);

        // Assert
        Assert.Single(messages);
        var retrieved = messages[0];
        Assert.NotNull(retrieved.Action);
        Assert.IsType<CreateTransactionAction>(retrieved.Action);

        var retrievedAction = (CreateTransactionAction)retrieved.Action;
        Assert.Equal(action.AccountId, retrievedAction.AccountId);
        Assert.Equal(action.AccountName, retrievedAction.AccountName);
        Assert.Equal(action.Amount, retrievedAction.Amount);
        Assert.Equal(action.Date, retrievedAction.Date);
        Assert.Equal(action.Description, retrievedAction.Description);
        Assert.Equal(action.Category, retrievedAction.Category);
        Assert.Equal(action.CategoryId, retrievedAction.CategoryId);
    }

    [Fact]
    public async Task Transfer_Action_Persists_Correctly()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var sessionRepo = new ChatSessionRepository(context);

        var session = ChatSession.Create();
        await sessionRepo.AddAsync(session);

        var action = new CreateTransferAction
        {
            FromAccountId = Guid.NewGuid(),
            FromAccountName = "Checking",
            ToAccountId = Guid.NewGuid(),
            ToAccountName = "Savings",
            Amount = 500m,
            Date = new DateOnly(2026, 1, 19),
            Description = "Monthly savings",
        };
        session.AddAssistantMessage("Here's your transfer:", action);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new ChatMessageRepository(verifyContext);
        var messages = await verifyRepo.GetBySessionAsync(session.Id);

        // Assert
        var retrieved = messages[0];
        Assert.IsType<CreateTransferAction>(retrieved.Action);

        var retrievedAction = (CreateTransferAction)retrieved.Action;
        Assert.Equal(action.FromAccountId, retrievedAction.FromAccountId);
        Assert.Equal(action.ToAccountName, retrievedAction.ToAccountName);
        Assert.Equal(action.Amount, retrievedAction.Amount);
    }

    [Fact]
    public async Task Recurring_Transaction_Action_Persists_Correctly()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var sessionRepo = new ChatSessionRepository(context);

        var session = ChatSession.Create();
        await sessionRepo.AddAsync(session);

        var action = new CreateRecurringTransactionAction
        {
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
            Amount = 1800m,
            Description = "Rent",
            Recurrence = RecurrencePattern.CreateMonthly(1, 1),
            StartDate = new DateOnly(2026, 2, 1),
            EndDate = new DateOnly(2027, 1, 1),
        };
        session.AddAssistantMessage("Here's your recurring transaction:", action);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new ChatMessageRepository(verifyContext);
        var messages = await verifyRepo.GetBySessionAsync(session.Id);

        // Assert
        var retrieved = messages[0];
        Assert.IsType<CreateRecurringTransactionAction>(retrieved.Action);

        var retrievedAction = (CreateRecurringTransactionAction)retrieved.Action;
        Assert.Equal(action.Description, retrievedAction.Description);
        Assert.Equal(action.Amount, retrievedAction.Amount);
        Assert.NotNull(retrievedAction.Recurrence);
        Assert.Equal(RecurrenceFrequency.Monthly, retrievedAction.Recurrence.Frequency);
    }

    [Fact]
    public async Task Clarification_Action_Persists_Correctly()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var sessionRepo = new ChatSessionRepository(context);

        var session = ChatSession.Create();
        await sessionRepo.AddAsync(session);

        var accountId1 = Guid.NewGuid();
        var accountId2 = Guid.NewGuid();
        var action = new ClarificationNeededAction
        {
            Question = "Which account?",
            FieldName = "AccountId",
            Options = new List<ClarificationOption>
            {
                new ClarificationOption { Label = "Checking", Value = "checking", EntityId = accountId1 },
                new ClarificationOption { Label = "Savings", Value = "savings", EntityId = accountId2 },
            },
        };
        session.AddAssistantMessage("I need some clarification:", action);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new ChatMessageRepository(verifyContext);
        var messages = await verifyRepo.GetBySessionAsync(session.Id);

        // Assert
        var retrieved = messages[0];
        Assert.IsType<ClarificationNeededAction>(retrieved.Action);

        var retrievedAction = (ClarificationNeededAction)retrieved.Action;
        Assert.Equal("Which account?", retrievedAction.Question);
        Assert.Equal("AccountId", retrievedAction.FieldName);
        Assert.Equal(2, retrievedAction.Options.Count);
        Assert.Equal("Checking", retrievedAction.Options[0].Label);
        Assert.Equal(accountId1, retrievedAction.Options[0].EntityId);
    }

    [Fact]
    public async Task CountAsync_Returns_Total_Count()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var sessionRepo = new ChatSessionRepository(context);
        var messageRepo = new ChatMessageRepository(context);

        var session = ChatSession.Create();
        await sessionRepo.AddAsync(session);

        session.AddUserMessage("Message 1");
        session.AddAssistantMessage("Message 2");
        session.AddUserMessage("Message 3");
        await context.SaveChangesAsync();

        // Act
        var count = await messageRepo.CountAsync();

        // Assert
        Assert.Equal(3, count);
    }
}
