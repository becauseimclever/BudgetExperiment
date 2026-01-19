// <copyright file="ChatSession.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Represents a chat session with the AI assistant.
/// </summary>
public sealed class ChatSession
{
    private readonly List<ChatMessage> _messages = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatSession"/> class.
    /// </summary>
    /// <remarks>Private constructor for factory method and EF Core.</remarks>
    private ChatSession()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the session was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp of the last message in the session.
    /// </summary>
    public DateTime LastMessageAtUtc { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the session is active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Gets the messages in this session.
    /// </summary>
    public IReadOnlyList<ChatMessage> Messages => this._messages.AsReadOnly();

    /// <summary>
    /// Creates a new chat session.
    /// </summary>
    /// <returns>A new <see cref="ChatSession"/>.</returns>
    public static ChatSession Create()
    {
        var now = DateTime.UtcNow;
        return new ChatSession
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = now,
            LastMessageAtUtc = now,
            IsActive = true,
        };
    }

    /// <summary>
    /// Adds a user message to the session.
    /// </summary>
    /// <param name="content">The message content.</param>
    /// <returns>The created <see cref="ChatMessage"/>.</returns>
    /// <exception cref="DomainException">Thrown when session is closed or content is empty.</exception>
    public ChatMessage AddUserMessage(string content)
    {
        this.EnsureActive();
        this.ValidateContent(content);

        var message = ChatMessage.CreateUserMessage(this.Id, content);
        this._messages.Add(message);
        this.LastMessageAtUtc = message.CreatedAtUtc;
        return message;
    }

    /// <summary>
    /// Adds an assistant message to the session.
    /// </summary>
    /// <param name="content">The message content.</param>
    /// <param name="action">The optional action proposed by the assistant.</param>
    /// <returns>The created <see cref="ChatMessage"/>.</returns>
    /// <exception cref="DomainException">Thrown when session is closed.</exception>
    public ChatMessage AddAssistantMessage(string content, ChatAction? action = null)
    {
        this.EnsureActive();

        var message = ChatMessage.CreateAssistantMessage(this.Id, content, action);
        this._messages.Add(message);
        this.LastMessageAtUtc = message.CreatedAtUtc;
        return message;
    }

    /// <summary>
    /// Closes the session, preventing further messages.
    /// </summary>
    public void Close()
    {
        this.IsActive = false;
    }

    private void EnsureActive()
    {
        if (!this.IsActive)
        {
            throw new DomainException("Cannot add messages to a closed session.");
        }
    }

    private void ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainException("Message content cannot be empty.");
        }
    }
}
