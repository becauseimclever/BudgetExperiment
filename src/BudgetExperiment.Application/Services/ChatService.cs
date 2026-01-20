// <copyright file="ChatService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

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
    private readonly ITransactionService _transactionService;
    private readonly ITransferService _transferService;
    private readonly IRecurringTransactionService _recurringTransactionService;
    private readonly IRecurringTransferService _recurringTransferService;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatService"/> class.
    /// </summary>
    /// <param name="sessionRepository">The session repository.</param>
    /// <param name="messageRepository">The message repository.</param>
    /// <param name="accountRepository">The account repository.</param>
    /// <param name="categoryRepository">The category repository.</param>
    /// <param name="parser">The natural language parser.</param>
    /// <param name="transactionService">The transaction service.</param>
    /// <param name="transferService">The transfer service.</param>
    /// <param name="recurringTransactionService">The recurring transaction service.</param>
    /// <param name="recurringTransferService">The recurring transfer service.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public ChatService(
        IChatSessionRepository sessionRepository,
        IChatMessageRepository messageRepository,
        IAccountRepository accountRepository,
        IBudgetCategoryRepository categoryRepository,
        INaturalLanguageParser parser,
        ITransactionService transactionService,
        ITransferService transferService,
        IRecurringTransactionService recurringTransactionService,
        IRecurringTransferService recurringTransferService,
        IUnitOfWork unitOfWork)
    {
        this._sessionRepository = sessionRepository;
        this._messageRepository = messageRepository;
        this._accountRepository = accountRepository;
        this._categoryRepository = categoryRepository;
        this._parser = parser;
        this._transactionService = transactionService;
        this._transferService = transferService;
        this._recurringTransactionService = recurringTransactionService;
        this._recurringTransferService = recurringTransferService;
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
        // Validate session
        var session = await this._sessionRepository.GetByIdAsync(sessionId, cancellationToken);
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

        // Add user message
        var userMessage = session.AddUserMessage(content);
        await this._messageRepository.AddAsync(userMessage, cancellationToken);

        // Get accounts and categories for the parser
        var accounts = await this.GetAccountInfoAsync(cancellationToken);
        var categories = await this.GetCategoryInfoAsync(cancellationToken);

        // Parse the user's command
        var parseResult = await this._parser.ParseCommandAsync(
            content,
            accounts,
            categories,
            context,
            cancellationToken);

        // Create assistant response
        var assistantMessage = session.AddAssistantMessage(
            parseResult.ResponseText,
            parseResult.Action);
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

        try
        {
            var result = await this.ExecuteActionAsync(message.Action, cancellationToken);
            if (result.Success && result.CreatedEntityId.HasValue)
            {
                message.MarkActionConfirmed(result.CreatedEntityId.Value);
            }
            else if (result.Success)
            {
                // Edge case: success but no entity (shouldn't happen in normal flow)
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
                ActionType: message.Action.Type,
                CreatedEntityId: null,
                Message: "Action failed.",
                ErrorMessage: ex.Message);
        }
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

    private async Task<ActionExecutionResult> ExecuteActionAsync(ChatAction action, CancellationToken cancellationToken)
    {
        return action switch
        {
            CreateTransactionAction txnAction => await this.ExecuteTransactionActionAsync(txnAction, cancellationToken),
            CreateTransferAction xferAction => await this.ExecuteTransferActionAsync(xferAction, cancellationToken),
            CreateRecurringTransactionAction recTxnAction => await this.ExecuteRecurringTransactionActionAsync(recTxnAction, cancellationToken),
            CreateRecurringTransferAction recXferAction => await this.ExecuteRecurringTransferActionAsync(recXferAction, cancellationToken),
            ClarificationNeededAction => new ActionExecutionResult(
                Success: false,
                ActionType: ChatActionType.ClarificationNeeded,
                CreatedEntityId: null,
                Message: "Clarification actions cannot be executed.",
                ErrorMessage: "Cannot execute clarification action."),
            _ => new ActionExecutionResult(
                Success: false,
                ActionType: action.Type,
                CreatedEntityId: null,
                Message: "Unknown action type.",
                ErrorMessage: "Unknown action type."),
        };
    }

    private async Task<ActionExecutionResult> ExecuteTransactionActionAsync(
        CreateTransactionAction action,
        CancellationToken cancellationToken)
    {
        var dto = new TransactionCreateDto
        {
            AccountId = action.AccountId,
            Amount = new MoneyDto { Currency = "USD", Amount = action.Amount },
            Date = action.Date,
            Description = action.Description,
            CategoryId = action.CategoryId,
        };

        var created = await this._transactionService.CreateAsync(dto, cancellationToken);
        return new ActionExecutionResult(
            Success: true,
            ActionType: ChatActionType.CreateTransaction,
            CreatedEntityId: created.Id,
            Message: $"Created transaction: {action.Description} for {action.Amount:C}");
    }

    private async Task<ActionExecutionResult> ExecuteTransferActionAsync(
        CreateTransferAction action,
        CancellationToken cancellationToken)
    {
        var request = new CreateTransferRequest
        {
            SourceAccountId = action.FromAccountId,
            DestinationAccountId = action.ToAccountId,
            Amount = action.Amount,
            Currency = "USD",
            Date = action.Date,
            Description = action.Description,
        };

        var created = await this._transferService.CreateAsync(request, cancellationToken);
        return new ActionExecutionResult(
            Success: true,
            ActionType: ChatActionType.CreateTransfer,
            CreatedEntityId: created.TransferId,
            Message: $"Created transfer of {action.Amount:C} from {action.FromAccountName} to {action.ToAccountName}");
    }

    private async Task<ActionExecutionResult> ExecuteRecurringTransactionActionAsync(
        CreateRecurringTransactionAction action,
        CancellationToken cancellationToken)
    {
        var dto = new RecurringTransactionCreateDto
        {
            AccountId = action.AccountId,
            Description = action.Description,
            Amount = new MoneyDto { Currency = "USD", Amount = action.Amount },
            Frequency = action.Recurrence.Frequency.ToString(),
            Interval = action.Recurrence.Interval,
            DayOfMonth = action.Recurrence.DayOfMonth,
            DayOfWeek = action.Recurrence.DayOfWeek?.ToString(),
            StartDate = action.StartDate,
            EndDate = action.EndDate,
        };

        var created = await this._recurringTransactionService.CreateAsync(dto, cancellationToken);
        return new ActionExecutionResult(
            Success: true,
            ActionType: ChatActionType.CreateRecurringTransaction,
            CreatedEntityId: created.Id,
            Message: $"Created recurring transaction: {action.Description} ({action.Recurrence.Frequency})");
    }

    private async Task<ActionExecutionResult> ExecuteRecurringTransferActionAsync(
        CreateRecurringTransferAction action,
        CancellationToken cancellationToken)
    {
        var dto = new RecurringTransferCreateDto
        {
            SourceAccountId = action.FromAccountId,
            DestinationAccountId = action.ToAccountId,
            Description = action.Description ?? "Recurring transfer",
            Amount = new MoneyDto { Currency = "USD", Amount = action.Amount },
            Frequency = action.Recurrence.Frequency.ToString(),
            Interval = action.Recurrence.Interval,
            DayOfMonth = action.Recurrence.DayOfMonth,
            DayOfWeek = action.Recurrence.DayOfWeek?.ToString(),
            StartDate = action.StartDate,
            EndDate = action.EndDate,
        };

        var created = await this._recurringTransferService.CreateAsync(dto, cancellationToken);
        return new ActionExecutionResult(
            Success: true,
            ActionType: ChatActionType.CreateRecurringTransfer,
            CreatedEntityId: created.Id,
            Message: $"Created recurring transfer: {action.Description ?? "Transfer"} ({action.Recurrence.Frequency})");
    }
}
