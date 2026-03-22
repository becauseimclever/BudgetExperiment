// <copyright file="StubChatApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Tests.TestHelpers;

/// <summary>
/// Shared stub implementation of <see cref="IChatApiService"/> for bUnit tests.
/// </summary>
internal sealed class StubChatApiService : IChatApiService
{
    /// <summary>
    /// Gets or sets the session to return from GetOrCreateSession / GetSession.
    /// </summary>
    public ChatSessionDto? SessionToReturn
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the messages to return from GetMessages.
    /// </summary>
    public IReadOnlyList<ChatMessageDto> MessagesToReturn { get; set; } = [];

    /// <summary>
    /// Gets or sets the response to return from SendMessage.
    /// </summary>
    public SendMessageResponse? SendMessageResponseToReturn
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the response to return from ConfirmAction.
    /// </summary>
    public ConfirmActionResponse? ConfirmActionResponseToReturn
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether CancelAction returns success.
    /// </summary>
    public bool CancelActionResult { get; set; } = true;

    /// <summary>
    /// Gets or sets a delay (in ms) to simulate async loading.
    /// </summary>
    public int DelayMs
    {
        get; set;
    }

    /// <summary>
    /// Gets the last content sent via SendMessage.
    /// </summary>
    public string? LastSentContent
    {
        get; private set;
    }

    /// <summary>
    /// Gets the last message ID targeted by ConfirmAction.
    /// </summary>
    public Guid? LastConfirmedMessageId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the last message ID targeted by CancelAction.
    /// </summary>
    public Guid? LastCancelledMessageId
    {
        get; private set;
    }

    /// <summary>
    /// Gets a value indicating whether CloseSession was called.
    /// </summary>
    public bool CloseSessionCalled
    {
        get; private set;
    }

    /// <inheritdoc/>
    public async Task<ChatSessionDto?> GetOrCreateSessionAsync()
    {
        if (this.DelayMs > 0)
        {
            await Task.Delay(this.DelayMs);
        }

        return this.SessionToReturn;
    }

    /// <inheritdoc/>
    public Task<ChatSessionDto?> GetSessionAsync(Guid sessionId) =>
        Task.FromResult(this.SessionToReturn);

    /// <inheritdoc/>
    public Task<IReadOnlyList<ChatMessageDto>> GetMessagesAsync(Guid sessionId, int limit = 50) =>
        Task.FromResult(this.MessagesToReturn);

    /// <inheritdoc/>
    public Task<SendMessageResponse?> SendMessageAsync(Guid sessionId, string content, ChatContextDto? context = null)
    {
        this.LastSentContent = content;
        return Task.FromResult(this.SendMessageResponseToReturn);
    }

    /// <inheritdoc/>
    public Task<ConfirmActionResponse?> ConfirmActionAsync(Guid messageId)
    {
        this.LastConfirmedMessageId = messageId;
        return Task.FromResult(this.ConfirmActionResponseToReturn);
    }

    /// <inheritdoc/>
    public Task<bool> CancelActionAsync(Guid messageId)
    {
        this.LastCancelledMessageId = messageId;
        return Task.FromResult(this.CancelActionResult);
    }

    /// <inheritdoc/>
    public Task<bool> CloseSessionAsync(Guid sessionId)
    {
        this.CloseSessionCalled = true;
        return Task.FromResult(true);
    }
}
