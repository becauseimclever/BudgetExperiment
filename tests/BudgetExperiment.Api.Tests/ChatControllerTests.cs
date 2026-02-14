// <copyright file="ChatControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Chat API endpoints.
/// </summary>
public sealed class ChatControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public ChatControllerTests(CustomWebApplicationFactory factory)
    {
        this._factory = factory;
        this._client = factory.CreateApiClient();
    }

    /// <summary>
    /// POST /api/v1/chat/sessions creates a new session and returns 200 OK.
    /// </summary>
    [Fact]
    public async Task GetOrCreateSession_Returns_200_WithNewSession()
    {
        // Act
        var response = await this._client.PostAsync("/api/v1/chat/sessions", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = await response.Content.ReadFromJsonAsync<ChatSessionDto>();
        Assert.NotNull(session);
        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.True(session.IsActive);
    }

    /// <summary>
    /// GET /api/v1/chat/sessions/{id} returns 200 OK when session exists.
    /// </summary>
    [Fact]
    public async Task GetSession_Returns_200_WhenSessionExists()
    {
        // Arrange
        var createResponse = await this._client.PostAsync("/api/v1/chat/sessions", null);
        var created = await createResponse.Content.ReadFromJsonAsync<ChatSessionDto>();

        // Act
        var response = await this._client.GetAsync($"/api/v1/chat/sessions/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = await response.Content.ReadFromJsonAsync<ChatSessionDto>();
        Assert.NotNull(session);
        Assert.Equal(created.Id, session.Id);
    }

    /// <summary>
    /// GET /api/v1/chat/sessions/{id} returns 404 for non-existent session.
    /// </summary>
    [Fact]
    public async Task GetSession_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/chat/sessions/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/chat/sessions/{id}/messages returns 200 OK with empty list for new session.
    /// </summary>
    [Fact]
    public async Task GetMessages_Returns_200_WithEmptyListForNewSession()
    {
        // Arrange
        var createResponse = await this._client.PostAsync("/api/v1/chat/sessions", null);
        var created = await createResponse.Content.ReadFromJsonAsync<ChatSessionDto>();

        // Act
        var response = await this._client.GetAsync($"/api/v1/chat/sessions/{created!.Id}/messages");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var messages = await response.Content.ReadFromJsonAsync<List<ChatMessageDto>>();
        Assert.NotNull(messages);
        Assert.Empty(messages);
    }

    /// <summary>
    /// GET /api/v1/chat/sessions/{id}/messages returns 404 for non-existent session.
    /// </summary>
    [Fact]
    public async Task GetMessages_Returns_404_WhenSessionNotFound()
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/chat/sessions/{Guid.NewGuid()}/messages");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/chat/sessions/{id}/messages with empty content returns 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task SendMessage_Returns_400_WhenContentEmpty()
    {
        // Arrange
        var createResponse = await this._client.PostAsync("/api/v1/chat/sessions", null);
        var created = await createResponse.Content.ReadFromJsonAsync<ChatSessionDto>();

        // Act
        var response = await this._client.PostAsJsonAsync(
            $"/api/v1/chat/sessions/{created!.Id}/messages",
            new SendMessageRequest { Content = string.Empty });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/chat/sessions/{id}/messages with whitespace content returns 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task SendMessage_Returns_400_WhenContentWhitespace()
    {
        // Arrange
        var createResponse = await this._client.PostAsync("/api/v1/chat/sessions", null);
        var created = await createResponse.Content.ReadFromJsonAsync<ChatSessionDto>();

        // Act
        var response = await this._client.PostAsJsonAsync(
            $"/api/v1/chat/sessions/{created!.Id}/messages",
            new SendMessageRequest { Content = "   " });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/chat/sessions/{id}/messages passes context to chat service.
    /// </summary>
    [Fact]
    public async Task SendMessage_PassesContextToService()
    {
        // Arrange
        var capturingService = new CapturingChatService();
        using var factory = this.CreateFactoryWithChatService(capturingService);
        using var client = CreateAuthenticatedClient(factory);

        var request = new SendMessageRequest
        {
            Content = "Add $15 groceries",
            Context = new ChatContextDto
            {
                CurrentAccountId = Guid.NewGuid(),
                CurrentAccountName = "Checking",
                CurrentCategoryId = Guid.NewGuid(),
                CurrentCategoryName = "Groceries",
                SelectedDate = new DateOnly(2026, 2, 10),
                PageType = "calendar",
            },
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/v1/chat/sessions/{Guid.NewGuid()}/messages",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capturingService.LastContext);
        Assert.Equal(request.Context!.CurrentAccountId, capturingService.LastContext!.CurrentAccountId);
        Assert.Equal(request.Context.CurrentAccountName, capturingService.LastContext.CurrentAccountName);
        Assert.Equal(request.Context.CurrentCategoryId, capturingService.LastContext.CurrentCategoryId);
        Assert.Equal(request.Context.CurrentCategoryName, capturingService.LastContext.CurrentCategoryName);
        Assert.Equal(request.Context.SelectedDate, capturingService.LastContext.CurrentDate);
        Assert.Equal(request.Context.PageType, capturingService.LastContext.CurrentPage);
    }

    /// <summary>
    /// POST /api/v1/chat/sessions/{id}/messages allows null context.
    /// </summary>
    [Fact]
    public async Task SendMessage_AllowsNullContext()
    {
        // Arrange
        var capturingService = new CapturingChatService();
        using var factory = this.CreateFactoryWithChatService(capturingService);
        using var client = CreateAuthenticatedClient(factory);

        var request = new SendMessageRequest
        {
            Content = "Add $15 groceries",
            Context = null,
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/v1/chat/sessions/{Guid.NewGuid()}/messages",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(capturingService.LastContext);
    }

    /// <summary>
    /// POST /api/v1/chat/messages/{id}/confirm returns 404 for non-existent message.
    /// </summary>
    [Fact]
    public async Task ConfirmAction_Returns_404_WhenMessageNotFound()
    {
        // Act
        var response = await this._client.PostAsync($"/api/v1/chat/messages/{Guid.NewGuid()}/confirm", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/chat/messages/{id}/cancel returns 404 for non-existent message.
    /// </summary>
    [Fact]
    public async Task CancelAction_Returns_404_WhenMessageNotFound()
    {
        // Act
        var response = await this._client.PostAsync($"/api/v1/chat/messages/{Guid.NewGuid()}/cancel", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/chat/sessions/{id}/close returns 204 No Content when session exists.
    /// </summary>
    [Fact]
    public async Task CloseSession_Returns_204_WhenSessionExists()
    {
        // Arrange
        var createResponse = await this._client.PostAsync("/api/v1/chat/sessions", null);
        var created = await createResponse.Content.ReadFromJsonAsync<ChatSessionDto>();

        // Act
        var response = await this._client.PostAsync($"/api/v1/chat/sessions/{created!.Id}/close", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/chat/sessions/{id}/close returns 404 for non-existent session.
    /// </summary>
    [Fact]
    public async Task CloseSession_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.PostAsync($"/api/v1/chat/sessions/{Guid.NewGuid()}/close", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Closing a session marks it as inactive.
    /// </summary>
    [Fact]
    public async Task CloseSession_MarksSessionAsInactive()
    {
        // Arrange
        var createResponse = await this._client.PostAsync("/api/v1/chat/sessions", null);
        var created = await createResponse.Content.ReadFromJsonAsync<ChatSessionDto>();

        // Act
        await this._client.PostAsync($"/api/v1/chat/sessions/{created!.Id}/close", null);
        var getResponse = await this._client.GetAsync($"/api/v1/chat/sessions/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var session = await getResponse.Content.ReadFromJsonAsync<ChatSessionDto>();
        Assert.NotNull(session);
        Assert.False(session.IsActive);
    }

    private static HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestAuto", "authenticated");
        return client;
    }

    private WebApplicationFactory<Program> CreateFactoryWithChatService(CapturingChatService chatService)
    {
        return this._factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IChatService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddSingleton<IChatService>(chatService);
            });
        });
    }

    private sealed class CapturingChatService : IChatService
    {
        public ChatContext? LastContext { get; private set; }

        public Task<ChatSession> GetOrCreateSessionAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ChatSession.Create());

        public Task<ChatSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ChatSession?>(null);

        public Task<IReadOnlyList<ChatSession>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChatSession>>([]);

        public Task<IReadOnlyList<ChatMessage>?> GetMessagesAsync(Guid sessionId, int limit = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChatMessage>?>([]);

        public Task<ChatResult> SendMessageAsync(
            Guid sessionId,
            string content,
            ChatContext? context = null,
            CancellationToken cancellationToken = default)
        {
            this.LastContext = context;
            return Task.FromResult(new ChatResult(true, null, null));
        }

        public Task<ActionExecutionResult> ConfirmActionAsync(Guid messageId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ActionExecutionResult(false, default(ChatActionType), null, "Not implemented"));

        public Task<bool> CancelActionAsync(Guid messageId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> CloseSessionAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
