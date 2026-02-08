// <copyright file="MobileChatSheetTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Chat;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Chat;

/// <summary>
/// Unit tests for the MobileChatSheet component.
/// </summary>
public class MobileChatSheetTests : BunitContext, IAsyncLifetime
{
    private readonly StubChatApiService chatApi = new();
    private readonly StubChatContextService chatContext = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MobileChatSheetTests"/> class.
    /// </summary>
    public MobileChatSheetTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<IChatApiService>(this.chatApi);
        this.Services.AddSingleton<IChatContextService>(this.chatContext);
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies the sheet does not render content when not visible.
    /// </summary>
    [Fact]
    public void Sheet_NotVisible_RendersNothing()
    {
        // Arrange & Act
        var cut = Render<MobileChatSheet>(p => p
            .Add(x => x.IsVisible, false));

        // Assert
        Assert.DoesNotContain("AI Assistant", cut.Markup);
    }

    /// <summary>
    /// Verifies the sheet shows loading state when first opened.
    /// </summary>
    [Fact]
    public void Sheet_WhenVisible_ShowsLoadingIfNoSession()
    {
        // Arrange
        this.chatApi.SessionToReturn = null;
        this.chatApi.DelayMs = 5000; // simulate long load

        // Act
        var cut = Render<MobileChatSheet>(p => p
            .Add(x => x.IsVisible, true));

        // Assert
        Assert.Contains("Starting chat", cut.Markup);
    }

    /// <summary>
    /// Verifies the welcome screen renders when session starts with no messages.
    /// </summary>
    [Fact]
    public void Sheet_WithSession_ShowsWelcomeWhenNoMessages()
    {
        // Arrange
        this.chatApi.SessionToReturn = new ChatSessionDto
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        this.chatApi.MessagesToReturn = [];

        // Act
        var cut = Render<MobileChatSheet>(p => p
            .Add(x => x.IsVisible, true));

        // Assert
        Assert.Contains("Budget Assistant", cut.Markup);
        Assert.Contains("Add $50 grocery purchase", cut.Markup);
    }

    /// <summary>
    /// Verifies quick example messages are tappable.
    /// </summary>
    [Fact]
    public void Sheet_WelcomeExamples_AreClickable()
    {
        // Arrange
        this.chatApi.SessionToReturn = new ChatSessionDto
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        this.chatApi.MessagesToReturn = [];

        var cut = Render<MobileChatSheet>(p => p
            .Add(x => x.IsVisible, true));

        // Assert - examples are rendered as clickable list items
        var examples = cut.FindAll(".mobile-chat-examples li");
        Assert.Equal(3, examples.Count);
    }

    /// <summary>
    /// Verifies the chat input is present when session is active.
    /// </summary>
    [Fact]
    public void Sheet_WithSession_ShowsChatInput()
    {
        // Arrange
        this.chatApi.SessionToReturn = new ChatSessionDto
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        this.chatApi.MessagesToReturn = [];

        // Act
        var cut = Render<MobileChatSheet>(p => p
            .Add(x => x.IsVisible, true));

        // Assert
        Assert.Contains("mobile-chat-input-area", cut.Markup);
    }

    /// <summary>
    /// Verifies messages render when session has existing messages.
    /// </summary>
    [Fact]
    public void Sheet_WithMessages_RendersMessageBubbles()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        this.chatApi.SessionToReturn = new ChatSessionDto
        {
            Id = sessionId,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        this.chatApi.MessagesToReturn =
        [
            new ChatMessageDto
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Role = ChatRole.User,
                Content = "Add $50 for groceries",
                CreatedAtUtc = DateTime.UtcNow,
            },
            new ChatMessageDto
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Role = ChatRole.Assistant,
                Content = "I'll create a $50 grocery transaction.",
                CreatedAtUtc = DateTime.UtcNow,
            },
        ];

        // Act
        var cut = Render<MobileChatSheet>(p => p
            .Add(x => x.IsVisible, true));

        // Assert - welcome is not shown, messages are displayed
        Assert.DoesNotContain("Budget Assistant", cut.Markup);
        Assert.Contains("Add $50 for groceries", cut.Markup);
        Assert.Contains("I'll create a $50 grocery transaction.", cut.Markup);
    }

    /// <summary>
    /// Verifies the close callback fires when OnClose is triggered.
    /// </summary>
    [Fact]
    public async Task Sheet_OnClose_FiresCallback()
    {
        // Arrange
        bool closed = false;
        this.chatApi.SessionToReturn = new ChatSessionDto
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        this.chatApi.MessagesToReturn = [];

        var cut = Render<MobileChatSheet>(p => p
            .Add(x => x.IsVisible, true)
            .Add(x => x.OnClose, () => { closed = true; return Task.CompletedTask; }));

        // Act - find and click the close button on the BottomSheet
        var closeBtn = cut.Find(".bottom-sheet__close");
        closeBtn.Click();

        // Wait for the BottomSheet close animation delay
        await Task.Delay(300);

        // Assert
        Assert.True(closed);
    }

    /// <summary>
    /// Verifies the footer contains the Expand button.
    /// </summary>
    [Fact]
    public void Sheet_Footer_ContainsExpandButton()
    {
        // Arrange
        this.chatApi.SessionToReturn = new ChatSessionDto
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        this.chatApi.MessagesToReturn = [];

        // Act
        var cut = Render<MobileChatSheet>(p => p
            .Add(x => x.IsVisible, true));

        // Assert
        Assert.Contains("Expand", cut.Markup);
    }

    /// <summary>
    /// Verifies the footer contains the New Chat button when session exists.
    /// </summary>
    [Fact]
    public void Sheet_Footer_ContainsNewChatButton()
    {
        // Arrange
        this.chatApi.SessionToReturn = new ChatSessionDto
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        this.chatApi.MessagesToReturn = [];

        // Act
        var cut = Render<MobileChatSheet>(p => p
            .Add(x => x.IsVisible, true));

        // Assert
        Assert.Contains("New Chat", cut.Markup);
    }

    /// <summary>
    /// Verifies the error state renders when session fails to load.
    /// </summary>
    [Fact]
    public void Sheet_SessionFails_ShowsError()
    {
        // Arrange - session returns null
        this.chatApi.SessionToReturn = null;

        // Act
        var cut = Render<MobileChatSheet>(p => p
            .Add(x => x.IsVisible, true));

        // Assert
        Assert.Contains("Unable to start chat session", cut.Markup);
        Assert.Contains("Try Again", cut.Markup);
    }

    /// <summary>
    /// Verifies context summary renders when context is available.
    /// </summary>
    [Fact]
    public void Sheet_WithContext_ShowsContextHint()
    {
        // Arrange
        this.chatContext.SetPageType("Calendar");
        this.chatApi.SessionToReturn = new ChatSessionDto
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        this.chatApi.MessagesToReturn = [];

        // Act
        var cut = Render<MobileChatSheet>(p => p
            .Add(x => x.IsVisible, true));

        // Assert
        Assert.Contains("Calendar", cut.Markup);
    }

    /// <summary>
    /// Stub implementation of IChatApiService for testing.
    /// </summary>
    private sealed class StubChatApiService : IChatApiService
    {
        public ChatSessionDto? SessionToReturn { get; set; }

        public IReadOnlyList<ChatMessageDto> MessagesToReturn { get; set; } = [];

        public int DelayMs { get; set; }

        public async Task<ChatSessionDto?> GetOrCreateSessionAsync()
        {
            if (this.DelayMs > 0)
            {
                await Task.Delay(this.DelayMs);
            }

            return this.SessionToReturn;
        }

        public Task<ChatSessionDto?> GetSessionAsync(Guid sessionId) =>
            Task.FromResult(this.SessionToReturn);

        public Task<IReadOnlyList<ChatMessageDto>> GetMessagesAsync(Guid sessionId, int limit = 50) =>
            Task.FromResult(this.MessagesToReturn);

        public Task<SendMessageResponse?> SendMessageAsync(Guid sessionId, string content) =>
            Task.FromResult<SendMessageResponse?>(null);

        public Task<ConfirmActionResponse?> ConfirmActionAsync(Guid messageId) =>
            Task.FromResult<ConfirmActionResponse?>(null);

        public Task<bool> CancelActionAsync(Guid messageId) =>
            Task.FromResult(true);

        public Task<bool> CloseSessionAsync(Guid sessionId) =>
            Task.FromResult(true);
    }

    /// <summary>
    /// Stub implementation of IChatContextService for testing.
    /// </summary>
    private sealed class StubChatContextService : IChatContextService
    {
        private readonly ChatPageContext context = new();

        public ChatPageContext CurrentContext => this.context;

        public event EventHandler? ContextChanged;

        public void SetAccountContext(Guid? accountId, string? accountName)
        {
            this.context.CurrentAccountId = accountId;
            this.context.CurrentAccountName = accountName;
            this.ContextChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetCategoryContext(Guid? categoryId, string? categoryName)
        {
            this.context.CurrentCategoryId = categoryId;
            this.context.CurrentCategoryName = categoryName;
            this.ContextChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetPageType(string? pageType)
        {
            this.context.PageType = pageType;
            this.ContextChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ClearContext()
        {
            this.context.CurrentAccountId = null;
            this.context.CurrentAccountName = null;
            this.context.CurrentCategoryId = null;
            this.context.CurrentCategoryName = null;
            this.context.PageType = null;
            this.ContextChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
