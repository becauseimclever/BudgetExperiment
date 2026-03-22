// <copyright file="ChatInputTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Chat;
using BudgetExperiment.Client.Services;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Chat;

/// <summary>
/// Unit tests for the <see cref="ChatInput"/> component.
/// </summary>
public class ChatInputTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatInputTests"/> class.
    /// </summary>
    public ChatInputTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies the input renders with default placeholder.
    /// </summary>
    [Fact]
    public void Input_RendersWithDefaultPlaceholder()
    {
        var cut = Render<ChatInput>();

        var input = cut.Find("input.chat-input");
        Assert.Equal("Type a message...", input.GetAttribute("placeholder"));
    }

    /// <summary>
    /// Verifies custom placeholder renders.
    /// </summary>
    [Fact]
    public void Input_RendersCustomPlaceholder()
    {
        var cut = Render<ChatInput>(p => p
            .Add(x => x.Placeholder, "Ask a question..."));

        var input = cut.Find("input.chat-input");
        Assert.Equal("Ask a question...", input.GetAttribute("placeholder"));
    }

    /// <summary>
    /// Verifies send button is disabled when input is empty.
    /// </summary>
    [Fact]
    public void SendButton_DisabledWhenEmpty()
    {
        var cut = Render<ChatInput>();

        var sendBtn = cut.Find(".chat-send-button");
        Assert.True(sendBtn.HasAttribute("disabled"));
    }

    /// <summary>
    /// Verifies send button is enabled when text is entered.
    /// </summary>
    [Fact]
    public void SendButton_EnabledWhenTextEntered()
    {
        var cut = Render<ChatInput>();

        var input = cut.Find("input.chat-input");
        input.Input("hello");

        var sendBtn = cut.Find(".chat-send-button");
        Assert.False(sendBtn.HasAttribute("disabled"));
    }

    /// <summary>
    /// Verifies form submit fires OnSend callback with trimmed message.
    /// </summary>
    [Fact]
    public void FormSubmit_FiresOnSendWithTrimmedMessage()
    {
        string? sentMessage = null;
        var cut = Render<ChatInput>(p => p
            .Add(x => x.OnSend, (string msg) =>
            {
                sentMessage = msg;
                return Task.CompletedTask;
            }));

        var input = cut.Find("input.chat-input");
        input.Input("  hello world  ");
        var form = cut.Find("form");
        form.Submit();

        Assert.Equal("hello world", sentMessage);
    }

    /// <summary>
    /// Verifies input is cleared after sending.
    /// </summary>
    [Fact]
    public void FormSubmit_ClearsInput()
    {
        var cut = Render<ChatInput>(p => p
            .Add(x => x.OnSend, (string _) => Task.CompletedTask));

        var input = cut.Find("input.chat-input");
        input.Input("test");
        var form = cut.Find("form");
        form.Submit();

        var sendBtn = cut.Find(".chat-send-button");
        Assert.True(sendBtn.HasAttribute("disabled"));
    }

    /// <summary>
    /// Verifies empty/whitespace submit does not fire callback.
    /// </summary>
    [Fact]
    public void FormSubmit_DoesNotFireWhenEmpty()
    {
        bool sent = false;
        var cut = Render<ChatInput>(p => p
            .Add(x => x.OnSend, (string _) =>
            {
                sent = true;
                return Task.CompletedTask;
            }));

        var form = cut.Find("form");
        form.Submit();

        Assert.False(sent);
    }

    /// <summary>
    /// Verifies input is disabled when IsDisabled is true.
    /// </summary>
    [Fact]
    public void Input_DisabledWhenIsDisabledTrue()
    {
        var cut = Render<ChatInput>(p => p
            .Add(x => x.IsDisabled, true));

        var input = cut.Find("input.chat-input");
        Assert.True(input.HasAttribute("disabled"));
    }

    /// <summary>
    /// Verifies send button is disabled when IsDisabled even with text.
    /// </summary>
    [Fact]
    public void SendButton_DisabledWhenIsDisabledEvenWithText()
    {
        var cut = Render<ChatInput>(p => p
            .Add(x => x.IsDisabled, true));

        var input = cut.Find("input.chat-input");
        input.Input("hello");

        var sendBtn = cut.Find(".chat-send-button");
        Assert.True(sendBtn.HasAttribute("disabled"));
    }

    /// <summary>
    /// Verifies Enter key triggers submit.
    /// </summary>
    [Fact]
    public void EnterKey_TriggersSubmit()
    {
        string? sentMessage = null;
        var cut = Render<ChatInput>(p => p
            .Add(x => x.OnSend, (string msg) =>
            {
                sentMessage = msg;
                return Task.CompletedTask;
            }));

        var input = cut.Find("input.chat-input");
        input.Input("test message");
        input.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter", ShiftKey = false });

        Assert.Equal("test message", sentMessage);
    }
}
