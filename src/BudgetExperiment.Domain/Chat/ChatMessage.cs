// <copyright file="ChatMessage.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Chat;

/// <summary>
/// Represents a single message in a chat session.
/// </summary>
public sealed class ChatMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessage"/> class.
    /// </summary>
    /// <remarks>Private constructor for factory methods and EF Core.</remarks>
    private ChatMessage()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the session identifier this message belongs to.
    /// </summary>
    public Guid SessionId { get; private set; }

    /// <summary>
    /// Gets the role of the message sender.
    /// </summary>
    public ChatRole Role { get; private set; }

    /// <summary>
    /// Gets the message content.
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the UTC timestamp when the message was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the action associated with this message (for assistant messages with pending actions).
    /// </summary>
    public ChatAction? Action { get; private set; }

    /// <summary>
    /// Gets the status of the action.
    /// </summary>
    public ChatActionStatus ActionStatus { get; private set; } = ChatActionStatus.None;

    /// <summary>
    /// Gets the identifier of the entity created when action was confirmed.
    /// </summary>
    public Guid? CreatedEntityId { get; private set; }

    /// <summary>
    /// Gets the error message if action failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Creates a new user message.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="content">The message content.</param>
    /// <returns>A new <see cref="ChatMessage"/> with user role.</returns>
    public static ChatMessage CreateUserMessage(Guid sessionId, string content)
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = ChatRole.User,
            Content = content,
            CreatedAtUtc = DateTime.UtcNow,
            ActionStatus = ChatActionStatus.None,
        };
    }

    /// <summary>
    /// Creates a new assistant message.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="content">The message content.</param>
    /// <param name="action">The optional action proposed by the assistant.</param>
    /// <returns>A new <see cref="ChatMessage"/> with assistant role.</returns>
    public static ChatMessage CreateAssistantMessage(Guid sessionId, string content, ChatAction? action = null)
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = ChatRole.Assistant,
            Content = content,
            CreatedAtUtc = DateTime.UtcNow,
            Action = action,
            ActionStatus = action != null ? ChatActionStatus.Pending : ChatActionStatus.None,
        };
    }

    /// <summary>
    /// Marks the action as confirmed and records the created entity.
    /// </summary>
    /// <param name="entityId">The identifier of the created entity.</param>
    /// <exception cref="DomainException">Thrown when action is not pending.</exception>
    public void MarkActionConfirmed(Guid entityId)
    {
        if (this.ActionStatus != ChatActionStatus.Pending)
        {
            throw new DomainException("Cannot confirm action that is not pending.");
        }

        this.ActionStatus = ChatActionStatus.Confirmed;
        this.CreatedEntityId = entityId;
    }

    /// <summary>
    /// Marks the action as cancelled.
    /// </summary>
    /// <exception cref="DomainException">Thrown when action is not pending.</exception>
    public void MarkActionCancelled()
    {
        if (this.ActionStatus != ChatActionStatus.Pending)
        {
            throw new DomainException("Cannot cancel action that is not pending.");
        }

        this.ActionStatus = ChatActionStatus.Cancelled;
    }

    /// <summary>
    /// Marks the action as failed.
    /// </summary>
    /// <param name="error">The error message describing the failure.</param>
    /// <exception cref="DomainException">Thrown when action is not pending.</exception>
    public void MarkActionFailed(string error)
    {
        if (this.ActionStatus != ChatActionStatus.Pending)
        {
            throw new DomainException("Cannot mark action as failed when it is not pending.");
        }

        this.ActionStatus = ChatActionStatus.Failed;
        this.ErrorMessage = error;
    }
}
