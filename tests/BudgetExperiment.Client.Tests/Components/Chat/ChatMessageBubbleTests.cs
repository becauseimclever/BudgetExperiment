// <copyright file="ChatMessageBubbleTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;
using BudgetExperiment.Client.Components.Chat;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Chat;

/// <summary>
/// Unit tests for the <see cref="ChatMessageBubble"/> component.
/// </summary>
public class ChatMessageBubbleTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessageBubbleTests"/> class.
    /// </summary>
    public ChatMessageBubbleTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies user message renders with user CSS class.
    /// </summary>
    [Fact]
    public void UserMessage_HasUserClass()
    {
        var cut = RenderBubble(ChatRole.User, "Hello");

        Assert.Contains("message-user", cut.Markup);
    }

    /// <summary>
    /// Verifies assistant message renders with assistant CSS class.
    /// </summary>
    [Fact]
    public void AssistantMessage_HasAssistantClass()
    {
        var cut = RenderBubble(ChatRole.Assistant, "Hi there!");

        Assert.Contains("message-assistant", cut.Markup);
    }

    /// <summary>
    /// Verifies system message renders with system CSS class.
    /// </summary>
    [Fact]
    public void SystemMessage_HasSystemClass()
    {
        var cut = RenderBubble(ChatRole.System, "System info");

        Assert.Contains("message-system", cut.Markup);
    }

    /// <summary>
    /// Verifies message content is displayed.
    /// </summary>
    [Fact]
    public void Message_DisplaysContent()
    {
        var cut = RenderBubble(ChatRole.User, "Add $50 for groceries");

        Assert.Contains("Add $50 for groceries", cut.Markup);
    }

    /// <summary>
    /// Verifies message time is displayed.
    /// </summary>
    [Fact]
    public void Message_DisplaysTime()
    {
        var cut = RenderBubble(ChatRole.User, "test", createdAt: new DateTime(2026, 3, 7, 14, 30, 0, DateTimeKind.Utc));

        Assert.Contains("message-time", cut.Markup);
    }

    /// <summary>
    /// Verifies pending action shows action card with confirm/cancel buttons.
    /// </summary>
    [Fact]
    public void PendingAction_ShowsActionCard()
    {
        var message = CreateMessageWithAction(
            ChatActionType.CreateTransaction,
            ChatActionStatus.Pending,
            amount: 50m,
            description: "Grocery purchase");

        var cut = RenderBubble(message);

        Assert.Contains("action-card", cut.Markup);
        Assert.Contains("Confirm", cut.Markup);
        Assert.Contains("Cancel", cut.Markup);
    }

    /// <summary>
    /// Verifies action card displays amount.
    /// </summary>
    [Fact]
    public void ActionCard_DisplaysAmount()
    {
        var message = CreateMessageWithAction(
            ChatActionType.CreateTransaction,
            ChatActionStatus.Pending,
            amount: 75.50m);

        var cut = RenderBubble(message);

        Assert.Contains("$75.50", cut.Markup);
    }

    /// <summary>
    /// Verifies action card displays account name.
    /// </summary>
    [Fact]
    public void ActionCard_DisplaysAccountName()
    {
        var message = CreateMessageWithAction(
            ChatActionType.CreateTransaction,
            ChatActionStatus.Pending,
            accountName: "Checking");

        var cut = RenderBubble(message);

        Assert.Contains("Checking", cut.Markup);
    }

    /// <summary>
    /// Verifies action card displays description.
    /// </summary>
    [Fact]
    public void ActionCard_DisplaysDescription()
    {
        var message = CreateMessageWithAction(
            ChatActionType.CreateTransaction,
            ChatActionStatus.Pending,
            description: "Weekly groceries");

        var cut = RenderBubble(message);

        Assert.Contains("Weekly groceries", cut.Markup);
    }

    /// <summary>
    /// Verifies action card displays category name.
    /// </summary>
    [Fact]
    public void ActionCard_DisplaysCategoryName()
    {
        var message = CreateMessageWithAction(
            ChatActionType.CreateTransaction,
            ChatActionStatus.Pending,
            categoryName: "Food");

        var cut = RenderBubble(message);

        Assert.Contains("Food", cut.Markup);
    }

    /// <summary>
    /// Verifies action card displays date.
    /// </summary>
    [Fact]
    public void ActionCard_DisplaysDate()
    {
        var message = CreateMessageWithAction(
            ChatActionType.CreateTransaction,
            ChatActionStatus.Pending,
            date: new DateOnly(2026, 3, 7));

        var cut = RenderBubble(message);

        Assert.Contains("Mar 7, 2026", cut.Markup);
    }

    /// <summary>
    /// Verifies transfer action shows from/to accounts.
    /// </summary>
    [Fact]
    public void TransferAction_ShowsFromToAccounts()
    {
        var message = new ChatMessageDto
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Role = ChatRole.Assistant,
            Content = "Transfer preview",
            CreatedAtUtc = DateTime.UtcNow,
            ActionStatus = ChatActionStatus.Pending,
            Action = new ChatActionDto
            {
                Type = ChatActionType.CreateTransfer,
                PreviewSummary = "Transfer $200",
                FromAccountName = "Checking",
                ToAccountName = "Savings",
                Amount = 200m,
            },
        };

        var cut = RenderBubble(message);

        Assert.Contains("Checking", cut.Markup);
        Assert.Contains("Savings", cut.Markup);
    }

    /// <summary>
    /// Verifies action type label for transaction.
    /// </summary>
    [Fact]
    public void ActionTypeBadge_ShowsTransaction()
    {
        var message = CreateMessageWithAction(
            ChatActionType.CreateTransaction,
            ChatActionStatus.Pending);

        var cut = RenderBubble(message);

        Assert.Contains("Transaction", cut.Markup);
    }

    /// <summary>
    /// Verifies action type label for transfer.
    /// </summary>
    [Fact]
    public void ActionTypeBadge_ShowsTransfer()
    {
        var message = CreateMessageWithAction(
            ChatActionType.CreateTransfer,
            ChatActionStatus.Pending);

        var cut = RenderBubble(message);

        Assert.Contains("Transfer", cut.Markup);
    }

    /// <summary>
    /// Verifies action type label for recurring transaction.
    /// </summary>
    [Fact]
    public void ActionTypeBadge_ShowsRecurringTransaction()
    {
        var message = CreateMessageWithAction(
            ChatActionType.CreateRecurringTransaction,
            ChatActionStatus.Pending);

        var cut = RenderBubble(message);

        Assert.Contains("Recurring Transaction", cut.Markup);
    }

    /// <summary>
    /// Verifies confirmed action shows success status.
    /// </summary>
    [Fact]
    public void ConfirmedAction_ShowsSuccessStatus()
    {
        var message = CreateMessageWithAction(
            ChatActionType.CreateTransaction,
            ChatActionStatus.Confirmed);

        var cut = RenderBubble(message);

        Assert.Contains("action-status-success", cut.Markup);
        Assert.Contains("Action completed", cut.Markup);
        Assert.DoesNotContain("action-card", cut.Markup);
    }

    /// <summary>
    /// Verifies cancelled action shows cancelled status.
    /// </summary>
    [Fact]
    public void CancelledAction_ShowsCancelledStatus()
    {
        var message = CreateMessageWithAction(
            ChatActionType.CreateTransaction,
            ChatActionStatus.Cancelled);

        var cut = RenderBubble(message);

        Assert.Contains("action-status-cancelled", cut.Markup);
        Assert.Contains("Cancelled", cut.Markup);
    }

    /// <summary>
    /// Verifies failed action shows failed status.
    /// </summary>
    [Fact]
    public void FailedAction_ShowsFailedStatus()
    {
        var message = CreateMessageWithAction(
            ChatActionType.CreateTransaction,
            ChatActionStatus.Failed);

        var cut = RenderBubble(message);

        Assert.Contains("action-status-failed", cut.Markup);
        Assert.Contains("Failed", cut.Markup);
    }

    /// <summary>
    /// Verifies confirm button fires callback with message ID.
    /// </summary>
    [Fact]
    public void ConfirmButton_FiresCallbackWithMessageId()
    {
        Guid? confirmedId = null;
        var message = CreateMessageWithAction(
            ChatActionType.CreateTransaction,
            ChatActionStatus.Pending,
            amount: 50m);

        var cut = Render<ChatMessageBubble>(p => p
            .Add(x => x.Message, message)
            .Add(x => x.OnConfirm, (Guid id) =>
            {
                confirmedId = id;
                return Task.CompletedTask;
            }));

        var confirmBtn = cut.Find(".btn-success");
        confirmBtn.Click();

        Assert.Equal(message.Id, confirmedId);
    }

    /// <summary>
    /// Verifies cancel button fires callback with message ID.
    /// </summary>
    [Fact]
    public void CancelButton_FiresCallbackWithMessageId()
    {
        Guid? cancelledId = null;
        var message = CreateMessageWithAction(
            ChatActionType.CreateTransaction,
            ChatActionStatus.Pending,
            amount: 50m);

        var cut = Render<ChatMessageBubble>(p => p
            .Add(x => x.Message, message)
            .Add(x => x.OnCancel, (Guid id) =>
            {
                cancelledId = id;
                return Task.CompletedTask;
            }));

        var cancelBtn = cut.Find(".btn-secondary");
        cancelBtn.Click();

        Assert.Equal(message.Id, cancelledId);
    }

    /// <summary>
    /// Verifies clarification options render as buttons.
    /// </summary>
    [Fact]
    public void ClarificationNeeded_ShowsOptionButtons()
    {
        var message = new ChatMessageDto
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Role = ChatRole.Assistant,
            Content = "Which account?",
            CreatedAtUtc = DateTime.UtcNow,
            ActionStatus = ChatActionStatus.Pending,
            Action = new ChatActionDto
            {
                Type = ChatActionType.ClarificationNeeded,
                PreviewSummary = "Need more info",
                Options =
                [
                    new ClarificationOptionDto { Label = "Checking", Value = "checking" },
                    new ClarificationOptionDto { Label = "Savings", Value = "savings" },
                ],
            },
        };

        var cut = RenderBubble(message);

        Assert.Contains("Checking", cut.Markup);
        Assert.Contains("Savings", cut.Markup);
        var optionButtons = cut.FindAll(".option-button");
        Assert.Equal(2, optionButtons.Count);
    }

    /// <summary>
    /// Verifies clicking a clarification option fires the callback.
    /// </summary>
    [Fact]
    public void ClarificationOption_Click_FiresCallback()
    {
        string? selectedValue = null;
        var message = new ChatMessageDto
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Role = ChatRole.Assistant,
            Content = "Which account?",
            CreatedAtUtc = DateTime.UtcNow,
            ActionStatus = ChatActionStatus.Pending,
            Action = new ChatActionDto
            {
                Type = ChatActionType.ClarificationNeeded,
                PreviewSummary = "Need more info",
                Options =
                [
                    new ClarificationOptionDto { Label = "Checking", Value = "checking" },
                ],
            },
        };

        var cut = Render<ChatMessageBubble>(p => p
            .Add(x => x.Message, message)
            .Add(x => x.OnOptionSelected, (string v) =>
            {
                selectedValue = v;
                return Task.CompletedTask;
            }));

        var optionBtn = cut.Find(".option-button");
        optionBtn.Click();

        Assert.Equal("checking", selectedValue);
    }

    /// <summary>
    /// Verifies action buttons are disabled when processing.
    /// </summary>
    [Fact]
    public void ActionButtons_DisabledWhenProcessing()
    {
        var message = CreateMessageWithAction(
            ChatActionType.CreateTransaction,
            ChatActionStatus.Pending,
            amount: 50m);

        var cut = Render<ChatMessageBubble>(p => p
            .Add(x => x.Message, message)
            .Add(x => x.IsProcessing, true));

        var buttons = cut.FindAll(".action-buttons button");
        Assert.All(buttons, btn => Assert.True(btn.HasAttribute("disabled")));
    }

    private static ChatMessageDto CreateMessageWithAction(
        ChatActionType actionType,
        ChatActionStatus status,
        decimal? amount = null,
        string? accountName = null,
        string? description = null,
        string? categoryName = null,
        DateOnly? date = null)
    {
        return new ChatMessageDto
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Role = ChatRole.Assistant,
            Content = "Action preview",
            CreatedAtUtc = DateTime.UtcNow,
            ActionStatus = status,
            Action = new ChatActionDto
            {
                Type = actionType,
                PreviewSummary = "Preview summary",
                Amount = amount,
                AccountName = accountName,
                Description = description,
                CategoryName = categoryName,
                Date = date,
            },
        };
    }

    private IRenderedComponent<ChatMessageBubble> RenderBubble(ChatRole role, string content, DateTime? createdAt = null)
    {
        var message = new ChatMessageDto
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Role = role,
            Content = content,
            CreatedAtUtc = createdAt ?? DateTime.UtcNow,
        };

        return RenderBubble(message);
    }

    private IRenderedComponent<ChatMessageBubble> RenderBubble(ChatMessageDto message)
    {
        return Render<ChatMessageBubble>(p => p
            .Add(x => x.Message, message));
    }
}
