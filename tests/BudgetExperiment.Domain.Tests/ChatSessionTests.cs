// <copyright file="ChatSessionTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the ChatSession entity.
/// </summary>
public class ChatSessionTests
{
    [Fact]
    public void Create_Returns_New_Session_With_Generated_Id()
    {
        // Act
        var session = ChatSession.Create();

        // Assert
        Assert.NotEqual(Guid.Empty, session.Id);
    }

    [Fact]
    public void Create_Sets_CreatedAtUtc_To_Current_Time()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var session = ChatSession.Create();

        // Assert
        var after = DateTime.UtcNow;
        Assert.True(session.CreatedAtUtc >= before && session.CreatedAtUtc <= after);
    }

    [Fact]
    public void Create_Sets_LastMessageAtUtc_To_CreatedAtUtc()
    {
        // Act
        var session = ChatSession.Create();

        // Assert
        Assert.Equal(session.CreatedAtUtc, session.LastMessageAtUtc);
    }

    [Fact]
    public void Create_Sets_IsActive_To_True()
    {
        // Act
        var session = ChatSession.Create();

        // Assert
        Assert.True(session.IsActive);
    }

    [Fact]
    public void Create_Initializes_Empty_Messages_Collection()
    {
        // Act
        var session = ChatSession.Create();

        // Assert
        Assert.Empty(session.Messages);
    }

    [Fact]
    public void AddUserMessage_Creates_Message_With_User_Role()
    {
        // Arrange
        var session = ChatSession.Create();

        // Act
        var message = session.AddUserMessage("Hello");

        // Assert
        Assert.Equal(ChatRole.User, message.Role);
        Assert.Equal("Hello", message.Content);
    }

    [Fact]
    public void AddUserMessage_Adds_Message_To_Collection()
    {
        // Arrange
        var session = ChatSession.Create();

        // Act
        session.AddUserMessage("Hello");

        // Assert
        Assert.Single(session.Messages);
    }

    [Fact]
    public void AddUserMessage_Sets_Message_SessionId()
    {
        // Arrange
        var session = ChatSession.Create();

        // Act
        var message = session.AddUserMessage("Hello");

        // Assert
        Assert.Equal(session.Id, message.SessionId);
    }

    [Fact]
    public void AddUserMessage_Updates_LastMessageAtUtc()
    {
        // Arrange
        var session = ChatSession.Create();
        var originalLastMessage = session.LastMessageAtUtc;

        // Act
        session.AddUserMessage("Hello");

        // Assert
        Assert.True(session.LastMessageAtUtc >= originalLastMessage);
    }

    [Fact]
    public void AddUserMessage_With_Empty_Content_Throws_DomainException()
    {
        // Arrange
        var session = ChatSession.Create();

        // Act & Assert
        Assert.Throws<DomainException>(() => session.AddUserMessage(string.Empty));
    }

    [Fact]
    public void AddUserMessage_With_Whitespace_Content_Throws_DomainException()
    {
        // Arrange
        var session = ChatSession.Create();

        // Act & Assert
        Assert.Throws<DomainException>(() => session.AddUserMessage("   "));
    }

    [Fact]
    public void AddAssistantMessage_Creates_Message_With_Assistant_Role()
    {
        // Arrange
        var session = ChatSession.Create();

        // Act
        var message = session.AddAssistantMessage("I can help you with that.");

        // Assert
        Assert.Equal(ChatRole.Assistant, message.Role);
        Assert.Equal("I can help you with that.", message.Content);
    }

    [Fact]
    public void AddAssistantMessage_With_Action_Attaches_Action()
    {
        // Arrange
        var session = ChatSession.Create();
        var action = new CreateTransactionAction
        {
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
            Amount = 50.00m,
            Date = new DateOnly(2026, 1, 19),
            Description = "Test",
        };

        // Act
        var message = session.AddAssistantMessage("Here's your transaction:", action);

        // Assert
        Assert.NotNull(message.Action);
        Assert.Equal(ChatActionType.CreateTransaction, message.Action.Type);
    }

    [Fact]
    public void AddAssistantMessage_With_Action_Sets_Pending_Status()
    {
        // Arrange
        var session = ChatSession.Create();
        var action = new CreateTransactionAction
        {
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
            Amount = 50.00m,
            Date = new DateOnly(2026, 1, 19),
            Description = "Test",
        };

        // Act
        var message = session.AddAssistantMessage("Here's your transaction:", action);

        // Assert
        Assert.Equal(ChatActionStatus.Pending, message.ActionStatus);
    }

    [Fact]
    public void AddAssistantMessage_Without_Action_Sets_None_Status()
    {
        // Arrange
        var session = ChatSession.Create();

        // Act
        var message = session.AddAssistantMessage("How can I help?");

        // Assert
        Assert.Equal(ChatActionStatus.None, message.ActionStatus);
    }

    [Fact]
    public void Close_Sets_IsActive_To_False()
    {
        // Arrange
        var session = ChatSession.Create();

        // Act
        session.Close();

        // Assert
        Assert.False(session.IsActive);
    }

    [Fact]
    public void AddUserMessage_On_Closed_Session_Throws_DomainException()
    {
        // Arrange
        var session = ChatSession.Create();
        session.Close();

        // Act & Assert
        Assert.Throws<DomainException>(() => session.AddUserMessage("Hello"));
    }
}
