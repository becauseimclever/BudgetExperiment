// <copyright file="ChatMessageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the ChatMessage entity.
/// </summary>
public class ChatMessageTests
{
    [Fact]
    public void CreateUserMessage_Returns_Message_With_User_Role()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        var message = ChatMessage.CreateUserMessage(sessionId, "Hello");

        // Assert
        Assert.Equal(ChatRole.User, message.Role);
    }

    [Fact]
    public void CreateUserMessage_Sets_Content()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        var message = ChatMessage.CreateUserMessage(sessionId, "Hello");

        // Assert
        Assert.Equal("Hello", message.Content);
    }

    [Fact]
    public void CreateUserMessage_Generates_Unique_Id()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        var message = ChatMessage.CreateUserMessage(sessionId, "Hello");

        // Assert
        Assert.NotEqual(Guid.Empty, message.Id);
    }

    [Fact]
    public void CreateUserMessage_Sets_SessionId()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        var message = ChatMessage.CreateUserMessage(sessionId, "Hello");

        // Assert
        Assert.Equal(sessionId, message.SessionId);
    }

    [Fact]
    public void CreateUserMessage_Sets_CreatedAtUtc()
    {
        // Arrange
        var before = DateTime.UtcNow;
        var sessionId = Guid.NewGuid();

        // Act
        var message = ChatMessage.CreateUserMessage(sessionId, "Hello");

        // Assert
        var after = DateTime.UtcNow;
        Assert.True(message.CreatedAtUtc >= before && message.CreatedAtUtc <= after);
    }

    [Fact]
    public void CreateUserMessage_Sets_ActionStatus_To_None()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        var message = ChatMessage.CreateUserMessage(sessionId, "Hello");

        // Assert
        Assert.Equal(ChatActionStatus.None, message.ActionStatus);
    }

    [Fact]
    public void CreateAssistantMessage_Returns_Message_With_Assistant_Role()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        var message = ChatMessage.CreateAssistantMessage(sessionId, "I can help.");

        // Assert
        Assert.Equal(ChatRole.Assistant, message.Role);
    }

    [Fact]
    public void CreateAssistantMessage_With_Action_Sets_Action()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var action = new CreateTransactionAction
        {
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
            Amount = 100m,
            Date = new DateOnly(2026, 1, 19),
            Description = "Groceries",
        };

        // Act
        var message = ChatMessage.CreateAssistantMessage(sessionId, "Preview:", action);

        // Assert
        Assert.Same(action, message.Action);
    }

    [Fact]
    public void CreateAssistantMessage_With_Action_Sets_Pending_Status()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var action = new CreateTransactionAction
        {
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
            Amount = 100m,
            Date = new DateOnly(2026, 1, 19),
            Description = "Groceries",
        };

        // Act
        var message = ChatMessage.CreateAssistantMessage(sessionId, "Preview:", action);

        // Assert
        Assert.Equal(ChatActionStatus.Pending, message.ActionStatus);
    }

    [Fact]
    public void MarkActionConfirmed_Sets_Confirmed_Status()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var action = new CreateTransactionAction
        {
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
            Amount = 100m,
            Date = new DateOnly(2026, 1, 19),
            Description = "Groceries",
        };
        var message = ChatMessage.CreateAssistantMessage(sessionId, "Preview:", action);
        var entityId = Guid.NewGuid();

        // Act
        message.MarkActionConfirmed(entityId);

        // Assert
        Assert.Equal(ChatActionStatus.Confirmed, message.ActionStatus);
        Assert.Equal(entityId, message.CreatedEntityId);
    }

    [Fact]
    public void MarkActionConfirmed_On_Non_Pending_Throws_DomainException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var message = ChatMessage.CreateAssistantMessage(sessionId, "Just text");

        // Act & Assert
        Assert.Throws<DomainException>(() => message.MarkActionConfirmed(Guid.NewGuid()));
    }

    [Fact]
    public void MarkActionCancelled_Sets_Cancelled_Status()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var action = new CreateTransactionAction
        {
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
            Amount = 100m,
            Date = new DateOnly(2026, 1, 19),
            Description = "Groceries",
        };
        var message = ChatMessage.CreateAssistantMessage(sessionId, "Preview:", action);

        // Act
        message.MarkActionCancelled();

        // Assert
        Assert.Equal(ChatActionStatus.Cancelled, message.ActionStatus);
    }

    [Fact]
    public void MarkActionCancelled_On_Non_Pending_Throws_DomainException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var message = ChatMessage.CreateAssistantMessage(sessionId, "Just text");

        // Act & Assert
        Assert.Throws<DomainException>(() => message.MarkActionCancelled());
    }

    [Fact]
    public void MarkActionFailed_Sets_Failed_Status_And_ErrorMessage()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var action = new CreateTransactionAction
        {
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
            Amount = 100m,
            Date = new DateOnly(2026, 1, 19),
            Description = "Groceries",
        };
        var message = ChatMessage.CreateAssistantMessage(sessionId, "Preview:", action);

        // Act
        message.MarkActionFailed("Account not found");

        // Assert
        Assert.Equal(ChatActionStatus.Failed, message.ActionStatus);
        Assert.Equal("Account not found", message.ErrorMessage);
    }

    [Fact]
    public void MarkActionFailed_On_Non_Pending_Throws_DomainException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var message = ChatMessage.CreateAssistantMessage(sessionId, "Just text");

        // Act & Assert
        Assert.Throws<DomainException>(() => message.MarkActionFailed("Error"));
    }
}
