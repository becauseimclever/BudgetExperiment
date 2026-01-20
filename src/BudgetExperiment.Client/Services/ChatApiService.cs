// <copyright file="ChatApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net.Http.Json;
using System.Text.Json;

using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// HTTP client service for communicating with the Chat API endpoints.
/// </summary>
public sealed class ChatApiService : IChatApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatApiService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    public ChatApiService(HttpClient httpClient)
    {
        this._httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<ChatSessionDto?> GetOrCreateSessionAsync()
    {
        try
        {
            var response = await this._httpClient.PostAsync("api/v1/chat/sessions", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ChatSessionDto>(JsonOptions);
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ChatSessionDto?> GetSessionAsync(Guid sessionId)
    {
        try
        {
            return await this._httpClient.GetFromJsonAsync<ChatSessionDto>(
                $"api/v1/chat/sessions/{sessionId}",
                JsonOptions);
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatMessageDto>> GetMessagesAsync(Guid sessionId, int limit = 50)
    {
        try
        {
            var result = await this._httpClient.GetFromJsonAsync<List<ChatMessageDto>>(
                $"api/v1/chat/sessions/{sessionId}/messages?limit={limit}",
                JsonOptions);
            return result ?? [];
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<SendMessageResponse?> SendMessageAsync(Guid sessionId, string content)
    {
        try
        {
            var request = new SendMessageRequest { Content = content };
            var response = await this._httpClient.PostAsJsonAsync(
                $"api/v1/chat/sessions/{sessionId}/messages",
                request,
                JsonOptions);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SendMessageResponse>(JsonOptions);
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ConfirmActionResponse?> ConfirmActionAsync(Guid messageId)
    {
        try
        {
            var response = await this._httpClient.PostAsync(
                $"api/v1/chat/messages/{messageId}/confirm",
                null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ConfirmActionResponse>(JsonOptions);
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> CancelActionAsync(Guid messageId)
    {
        try
        {
            var response = await this._httpClient.PostAsync(
                $"api/v1/chat/messages/{messageId}/cancel",
                null);
            return response.IsSuccessStatusCode;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> CloseSessionAsync(Guid sessionId)
    {
        try
        {
            var response = await this._httpClient.PostAsync(
                $"api/v1/chat/sessions/{sessionId}/close",
                null);
            return response.IsSuccessStatusCode;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }
}
