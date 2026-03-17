// <copyright file="ChatPanelTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Chat;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Chat;

/// <summary>
/// Unit tests for the <see cref="ChatPanel"/> component.
/// </summary>
public class ChatPanelTests : BunitContext, IAsyncLifetime
{
    private readonly StubChatApiService _chatApi = new();
    private readonly StubChatContextService _chatContext = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatPanelTests"/> class.
    /// </summary>
    public ChatPanelTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IChatApiService>(this._chatApi);
        this.Services.AddSingleton<IChatContextService>(this._chatContext);
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies closed panel renders with closed CSS class.
    /// </summary>
    [Fact]
    public void Panel_WhenClosed_HasClosedClass()
    {
        var cut = Render<ChatPanel>(p => p
            .Add(x => x.IsOpen, false));

        Assert.Contains("chat-panel-closed", cut.Markup);
        Assert.DoesNotContain("chat-panel-open", cut.Markup);
    }

    /// <summary>
    /// Verifies open panel renders with open CSS class.
    /// </summary>
    [Fact]
    public void Panel_WhenOpen_HasOpenClass()
    {
        this._chatApi.SessionToReturn = CreateSession();
        this._chatApi.MessagesToReturn = [];

        var cut = Render<ChatPanel>(p => p
            .Add(x => x.IsOpen, true));

        Assert.Contains("chat-panel-open", cut.Markup);
    }

    /// <summary>
    /// Verifies the header always renders with title.
    /// </summary>
    [Fact]
    public void Panel_RendersHeader_WithTitle()
    {
        var cut = Render<ChatPanel>(p => p
            .Add(x => x.IsOpen, true));

        Assert.Contains("AI Assistant", cut.Markup);
    }

    /// <summary>
    /// Verifies loading state when panel opens.
    /// </summary>
    [Fact]
    public void Panel_WhenOpened_ShowsLoadingWhileInitializing()
    {
        this._chatApi.SessionToReturn = null;
        this._chatApi.DelayMs = 5000;

        var cut = Render<ChatPanel>(p => p
            .Add(x => x.IsOpen, true));

        Assert.Contains("Initializing chat", cut.Markup);
    }

    /// <summary>
    /// Verifies error state when session fails to load.
    /// </summary>
    [Fact]
    public void Panel_WhenSessionNull_ShowsErrorState()
    {
        this._chatApi.SessionToReturn = null;

        var cut = Render<ChatPanel>(p => p
            .Add(x => x.IsOpen, true));

        Assert.Contains("Unable to start chat session", cut.Markup);
        Assert.Contains("Try Again", cut.Markup);
    }

    /// <summary>
    /// Verifies welcome screen when session has no messages.
    /// </summary>
    [Fact]
    public void Panel_WithSessionNoMessages_ShowsWelcome()
    {
        this._chatApi.SessionToReturn = CreateSession();
        this._chatApi.MessagesToReturn = [];

        var cut = Render<ChatPanel>(p => p
            .Add(x => x.IsOpen, true));

        Assert.Contains("budget assistant", cut.Markup);
        Assert.Contains("Add $50 grocery purchase at Walmart", cut.Markup);
    }

    /// <summary>
    /// Verifies welcome screen includes example messages.
    /// </summary>
    [Fact]
    public void Panel_WelcomeScreen_ContainsExamples()
    {
        this._chatApi.SessionToReturn = CreateSession();
        this._chatApi.MessagesToReturn = [];

        var cut = Render<ChatPanel>(p => p
            .Add(x => x.IsOpen, true));

        var examples = cut.FindAll(".chat-examples li");
        Assert.Equal(3, examples.Count);
    }

    /// <summary>
    /// Verifies messages render when session has existing messages.
    /// </summary>
    [Fact]
    public void Panel_WithMessages_RendersMessageBubbles()
    {
        var sessionId = Guid.NewGuid();
        this._chatApi.SessionToReturn = CreateSession(sessionId);
        this._chatApi.MessagesToReturn =
        [
            CreateMessage(sessionId, ChatRole.User, "Hello"),
            CreateMessage(sessionId, ChatRole.Assistant, "Hi there!"),
        ];

        var cut = Render<ChatPanel>(p => p
            .Add(x => x.IsOpen, true));

        Assert.Contains("Hello", cut.Markup);
        Assert.Contains("Hi there!", cut.Markup);
        Assert.DoesNotContain("budget assistant", cut.Markup);
    }

    /// <summary>
    /// Verifies the ChatInput component is present when session is active.
    /// </summary>
    [Fact]
    public void Panel_WithSession_ShowsChatInput()
    {
        this._chatApi.SessionToReturn = CreateSession();
        this._chatApi.MessagesToReturn = [];

        var cut = Render<ChatPanel>(p => p
            .Add(x => x.IsOpen, true));

        Assert.Contains("chat-input", cut.Markup);
    }

    /// <summary>
    /// Verifies toggle callback fires when close button clicked.
    /// </summary>
    [Fact]
    public void Panel_CloseButton_FiresOnToggle()
    {
        bool toggled = false;
        this._chatApi.SessionToReturn = CreateSession();
        this._chatApi.MessagesToReturn = [];

        var cut = Render<ChatPanel>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.OnToggle, () =>
            {
                toggled = true;
                return Task.CompletedTask;
            }));

        var closeBtn = cut.Find("button[title='Close panel']");
        closeBtn.Click();

        Assert.True(toggled);
    }

    /// <summary>
    /// Verifies the new conversation button is shown when session exists.
    /// </summary>
    [Fact]
    public void Panel_WithSession_ShowsNewConversationButton()
    {
        this._chatApi.SessionToReturn = CreateSession();
        this._chatApi.MessagesToReturn = [];

        var cut = Render<ChatPanel>(p => p
            .Add(x => x.IsOpen, true));

        var newBtn = cut.Find("button[title='New conversation']");
        Assert.NotNull(newBtn);
    }

    /// <summary>
    /// Verifies the new conversation button closes existing session and reinitializes.
    /// </summary>
    [Fact]
    public void Panel_NewConversation_ClosesAndReinitializesSession()
    {
        this._chatApi.SessionToReturn = CreateSession();
        this._chatApi.MessagesToReturn = [];

        var cut = Render<ChatPanel>(p => p
            .Add(x => x.IsOpen, true));

        var newBtn = cut.Find("button[title='New conversation']");
        newBtn.Click();

        Assert.True(this._chatApi.CloseSessionCalled);
    }

    /// <summary>
    /// Verifies sending a message invokes the API.
    /// </summary>
    [Fact]
    public void Panel_SendMessage_InvokesApi()
    {
        var sessionId = Guid.NewGuid();
        this._chatApi.SessionToReturn = CreateSession(sessionId);
        this._chatApi.MessagesToReturn = [];
        this._chatApi.SendMessageResponseToReturn = new SendMessageResponse
        {
            Success = true,
            UserMessage = CreateMessage(sessionId, ChatRole.User, "test message"),
            AssistantMessage = CreateMessage(sessionId, ChatRole.Assistant, "response"),
        };

        var cut = Render<ChatPanel>(p => p
            .Add(x => x.IsOpen, true));

        // Find the input and submit a message
        var input = cut.Find("input.chat-input");
        input.Input("test message");
        var form = cut.Find("form");
        form.Submit();

        cut.WaitForAssertion(() => Assert.Equal("test message", this._chatApi.LastSentContent));
    }

    /// <summary>
    /// Verifies sent messages appear in the panel.
    /// </summary>
    [Fact]
    public void Panel_SendMessage_ShowsResponseMessages()
    {
        var sessionId = Guid.NewGuid();
        this._chatApi.SessionToReturn = CreateSession(sessionId);
        this._chatApi.MessagesToReturn = [];
        this._chatApi.SendMessageResponseToReturn = new SendMessageResponse
        {
            Success = true,
            UserMessage = CreateMessage(sessionId, ChatRole.User, "buy groceries"),
            AssistantMessage = CreateMessage(sessionId, ChatRole.Assistant, "I'll create that transaction."),
        };

        var cut = Render<ChatPanel>(p => p
            .Add(x => x.IsOpen, true));

        var input = cut.Find("input.chat-input");
        input.Input("buy groceries");
        var form = cut.Find("form");
        form.Submit();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("buy groceries", cut.Markup);
            Assert.Contains("I'll create that transaction.", cut.Markup);
        });
    }

    /// <summary>
    /// Verifies context summary renders when context is set.
    /// </summary>
    [Fact]
    public void Panel_WithContext_ShowsContextHint()
    {
        this._chatContext.SetPageType("Calendar");
        this._chatApi.SessionToReturn = CreateSession();
        this._chatApi.MessagesToReturn = [];

        var cut = Render<ChatPanel>(p => p
            .Add(x => x.IsOpen, true));

        Assert.Contains("Calendar", cut.Markup);
    }

    private static ChatSessionDto CreateSession(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        IsActive = true,
        CreatedAtUtc = DateTime.UtcNow,
    };

    private static ChatMessageDto CreateMessage(Guid sessionId, ChatRole role, string content) => new()
    {
        Id = Guid.NewGuid(),
        SessionId = sessionId,
        Role = role,
        Content = content,
        CreatedAtUtc = DateTime.UtcNow,
    };
}
