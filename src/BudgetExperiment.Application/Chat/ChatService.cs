// <copyright file="ChatService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Chat;

/// <summary>
/// Application service for managing chat sessions and processing chat commands.
/// </summary>
public sealed class ChatService : IChatService
{
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IBudgetCategoryRepository _categoryRepository;
    private readonly INaturalLanguageParser _parser;
    private readonly IChatActionExecutor _actionExecutor;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatService"/> class.
    /// </summary>
    /// <param name="sessionRepository">The session repository.</param>
    /// <param name="messageRepository">The message repository.</param>
    /// <param name="accountRepository">The account repository.</param>
    /// <param name="categoryRepository">The category repository.</param>
    /// <param name="parser">The natural language parser.</param>
    /// <param name="actionExecutor">The chat action executor.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public ChatService(
        IChatSessionRepository sessionRepository,
        IChatMessageRepository messageRepository,
        IAccountRepository accountRepository,
        IBudgetCategoryRepository categoryRepository,
        INaturalLanguageParser parser,
        IChatActionExecutor actionExecutor,
        IUnitOfWork unitOfWork)
    {
        this._sessionRepository = sessionRepository;
        this._messageRepository = messageRepository;
        this._accountRepository = accountRepository;
        this._categoryRepository = categoryRepository;
        this._parser = parser;
        this._actionExecutor = actionExecutor;
        this._unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<ChatSession> GetOrCreateSessionAsync(string userId, CancellationToken cancellationToken = default)
    {
        // Note: userId is reserved for future multi-user session isolation
        _ = userId;

        var existingSession = await this._sessionRepository.GetActiveSessionAsync(cancellationToken);
        if (existingSession != null && existingSession.IsActive)
        {
            return existingSession;
        }

        var newSession = ChatSession.Create();
        await this._sessionRepository.AddAsync(newSession, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return newSession;
    }

    /// <inheritdoc />
    public async Task<ChatSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await this._sessionRepository.GetByIdAsync(sessionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatSession>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        // For now, return all sessions (we could add a userId filter later)
        return await this._sessionRepository.ListAsync(0, 100, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatMessage>?> GetMessagesAsync(Guid sessionId, int limit = 50, CancellationToken cancellationToken = default)
    {
        // Check if session exists
        var session = await this._sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        return await this._messageRepository.GetBySessionAsync(sessionId, limit, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ChatResult> SendMessageAsync(
        Guid sessionId,
        string content,
        ChatContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var session = await this._sessionRepository.GetByIdAsync(sessionId, cancellationToken);

        var validationError = ValidateSessionForMessage(session);
        if (validationError is not null)
        {
            return validationError;
        }

        var userMessage = session!.AddUserMessage(content);
        await this._messageRepository.AddAsync(userMessage, cancellationToken);

        var parseResult = await this.ParseUserCommandAsync(content, context, cancellationToken);

        var assistantMessage = session.AddAssistantMessage(parseResult.ResponseText, parseResult.Action);
        await this._messageRepository.AddAsync(assistantMessage, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        return new ChatResult(
            Success: parseResult.Success,
            UserMessage: userMessage,
            AssistantMessage: assistantMessage,
            ErrorMessage: parseResult.ErrorMessage);
    }

    /// <inheritdoc />
    public async Task<ActionExecutionResult> ConfirmActionAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await this._messageRepository.GetByIdAsync(messageId, cancellationToken);

        var validationError = ValidateMessageForConfirmation(message);
        if (validationError is not null)
        {
            return validationError;
        }

        return await this.ExecuteAndUpdateActionStatusAsync(message!, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> CancelActionAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await this._messageRepository.GetByIdAsync(messageId, cancellationToken);
        if (message == null || message.ActionStatus != ChatActionStatus.Pending)
        {
            return false;
        }

        message.MarkActionCancelled();
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> CloseSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await this._sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session == null || !session.IsActive)
        {
            return false;
        }

        session.Close();
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ChatResult? ValidateSessionForMessage(ChatSession? session)
    {
        if (session == null)
        {
            return new ChatResult(
                Success: false,
                UserMessage: null,
                AssistantMessage: null,
                ErrorMessage: "Session not found.");
        }

        if (!session.IsActive)
        {
            return new ChatResult(
                Success: false,
                UserMessage: null,
                AssistantMessage: null,
                ErrorMessage: "Session is closed.");
        }

        return null;
    }

    private static ActionExecutionResult? ValidateMessageForConfirmation(ChatMessage? message)
    {
        if (message == null)
        {
            return new ActionExecutionResult(
                Success: false,
                ActionType: ChatActionType.CreateTransaction,
                CreatedEntityId: null,
                Message: "Message not found.",
                ErrorMessage: "Message not found.");
        }

        if (message.Action == null)
        {
            return new ActionExecutionResult(
                Success: false,
                ActionType: ChatActionType.CreateTransaction,
                CreatedEntityId: null,
                Message: "Message has no action to confirm.",
                ErrorMessage: "No action present.");
        }

        if (message.ActionStatus != ChatActionStatus.Pending)
        {
            return new ActionExecutionResult(
                Success: false,
                ActionType: message.Action.Type,
                CreatedEntityId: null,
                Message: $"Action is already {message.ActionStatus}.",
                ErrorMessage: "Action not pending.");
        }

        return null;
    }

    private async Task<ParseResult> ParseUserCommandAsync(
        string content,
        ChatContext? context,
        CancellationToken cancellationToken)
    {
        var accounts = await this.GetAccountInfoAsync(cancellationToken);
        var categories = await this.GetCategoryInfoAsync(cancellationToken);

        return await this._parser.ParseCommandAsync(
            content, accounts, categories, context, cancellationToken);
    }

    private async Task<ActionExecutionResult> ExecuteAndUpdateActionStatusAsync(
        ChatMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await this._actionExecutor.ExecuteActionAsync(message.Action!, cancellationToken);

            if (result.Success && result.CreatedEntityId.HasValue)
            {
                message.MarkActionConfirmed(result.CreatedEntityId.Value);
            }
            else if (result.Success)
            {
                message.MarkActionFailed("No entity ID returned.");
            }
            else
            {
                message.MarkActionFailed(result.ErrorMessage ?? "Execution failed.");
            }

            await this._unitOfWork.SaveChangesAsync(cancellationToken);
            return result;
        }
        catch (DomainException ex)
        {
            message.MarkActionFailed(ex.Message);
            await this._unitOfWork.SaveChangesAsync(cancellationToken);

            return new ActionExecutionResult(
                Success: false,
                ActionType: message.Action!.Type,
                CreatedEntityId: null,
                Message: "Action failed.",
                ErrorMessage: ex.Message);
        }
    }

    private async Task<IReadOnlyList<AccountInfo>> GetAccountInfoAsync(CancellationToken cancellationToken)
    {
        var accounts = await this._accountRepository.GetAllAsync(cancellationToken);
        return accounts.Select(a => new AccountInfo(a.Id, a.Name, a.Type)).ToList();
    }

    private async Task<IReadOnlyList<CategoryInfo>> GetCategoryInfoAsync(CancellationToken cancellationToken)
    {
        var categories = await this._categoryRepository.GetActiveAsync(cancellationToken);
        return categories.Select(c => new CategoryInfo(c.Id, c.Name)).ToList();
    }
}
