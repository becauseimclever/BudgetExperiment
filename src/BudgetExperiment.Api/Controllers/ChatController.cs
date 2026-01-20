// <copyright file="ChatController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Mapping;
using BudgetExperiment.Application.Services;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for AI chat assistant operations.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatController"/> class.
    /// </summary>
    /// <param name="chatService">The chat service.</param>
    /// <param name="userContext">The user context.</param>
    public ChatController(IChatService chatService, IUserContext userContext)
    {
        this._chatService = chatService;
        this._userContext = userContext;
    }

    /// <summary>
    /// Gets or creates an active chat session for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active or newly created chat session.</returns>
    [HttpPost("sessions")]
    [ProducesResponseType<ChatSessionDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrCreateSessionAsync(CancellationToken cancellationToken)
    {
        var userId = this._userContext.UserId;
        var session = await this._chatService.GetOrCreateSessionAsync(userId, cancellationToken);
        return this.Ok(DomainToDtoMapper.ToDto(session));
    }

    /// <summary>
    /// Gets a chat session by ID.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The chat session if found.</returns>
    [HttpGet("sessions/{sessionId:guid}")]
    [ProducesResponseType<ChatSessionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await this._chatService.GetSessionAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return this.NotFound();
        }

        return this.Ok(DomainToDtoMapper.ToDto(session));
    }

    /// <summary>
    /// Gets the message history for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="limit">Maximum number of messages to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of messages.</returns>
    [HttpGet("sessions/{sessionId:guid}/messages")]
    [ProducesResponseType<IReadOnlyList<ChatMessageDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessagesAsync(
        Guid sessionId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var messages = await this._chatService.GetMessagesAsync(sessionId, limit, cancellationToken);
        if (messages is null)
        {
            return this.NotFound();
        }

        return this.Ok(messages.Select(DomainToDtoMapper.ToDto).ToList());
    }

    /// <summary>
    /// Sends a message to a chat session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="request">The message request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing user and assistant messages.</returns>
    [HttpPost("sessions/{sessionId:guid}/messages")]
    [ProducesResponseType<SendMessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessageAsync(
        Guid sessionId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return this.BadRequest("Message content is required.");
        }

        var result = await this._chatService.SendMessageAsync(sessionId, request.Content, null, cancellationToken);

        var response = new SendMessageResponse
        {
            Success = result.Success,
            UserMessage = result.UserMessage != null ? DomainToDtoMapper.ToDto(result.UserMessage) : null,
            AssistantMessage = result.AssistantMessage != null ? DomainToDtoMapper.ToDto(result.AssistantMessage) : null,
            ErrorMessage = result.ErrorMessage,
        };

        if (!result.Success && result.ErrorMessage?.Contains("not found") == true)
        {
            return this.NotFound(response);
        }

        return this.Ok(response);
    }

    /// <summary>
    /// Confirms and executes a pending action.
    /// </summary>
    /// <param name="messageId">The message identifier containing the action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the action execution.</returns>
    [HttpPost("messages/{messageId:guid}/confirm")]
    [ProducesResponseType<ConfirmActionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmActionAsync(Guid messageId, CancellationToken cancellationToken)
    {
        var result = await this._chatService.ConfirmActionAsync(messageId, cancellationToken);

        var response = new ConfirmActionResponse
        {
            Success = result.Success,
            ActionType = result.ActionType,
            CreatedEntityId = result.CreatedEntityId,
            Message = result.Message,
            ErrorMessage = result.ErrorMessage,
        };

        if (!result.Success && result.ErrorMessage?.Contains("not found") == true)
        {
            return this.NotFound(response);
        }

        if (!result.Success)
        {
            return this.BadRequest(response);
        }

        return this.Ok(response);
    }

    /// <summary>
    /// Cancels a pending action.
    /// </summary>
    /// <param name="messageId">The message identifier containing the action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPost("messages/{messageId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelActionAsync(Guid messageId, CancellationToken cancellationToken)
    {
        var result = await this._chatService.CancelActionAsync(messageId, cancellationToken);
        if (!result)
        {
            return this.NotFound();
        }

        return this.NoContent();
    }

    /// <summary>
    /// Closes a chat session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPost("sessions/{sessionId:guid}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloseSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var result = await this._chatService.CloseSessionAsync(sessionId, cancellationToken);
        if (!result)
        {
            return this.NotFound();
        }

        return this.NoContent();
    }
}
