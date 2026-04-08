// <copyright file="ChatService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Chat;

/// <summary>
/// Application service for managing chat sessions and processing chat commands.
/// </summary>
public sealed class ChatService : IChatService
{
    private const string KakeiboFilterFlag = "Kakeibo:TransactionFilter";

    private readonly IChatSessionRepository _sessionRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IBudgetCategoryRepository _categoryRepository;
    private readonly ITransactionService _transactionService;
    private readonly IFeatureFlagService _featureFlagService;
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
    /// <param name="transactionService">The transaction service.</param>
    /// <param name="featureFlagService">The feature flag service.</param>
    /// <param name="parser">The natural language parser.</param>
    /// <param name="actionExecutor">The chat action executor.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public ChatService(
        IChatSessionRepository sessionRepository,
        IChatMessageRepository messageRepository,
        IAccountRepository accountRepository,
        IBudgetCategoryRepository categoryRepository,
        ITransactionService transactionService,
        IFeatureFlagService featureFlagService,
        INaturalLanguageParser parser,
        IChatActionExecutor actionExecutor,
        IUnitOfWork unitOfWork)
    {
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
        _transactionService = transactionService;
        _featureFlagService = featureFlagService;
        _parser = parser;
        _actionExecutor = actionExecutor;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<ChatSession> GetOrCreateSessionAsync(string userId, CancellationToken cancellationToken = default)
    {
        // Note: userId is reserved for future multi-user session isolation
        _ = userId;

        var existingSession = await _sessionRepository.GetActiveSessionAsync(cancellationToken);
        if (existingSession != null && existingSession.IsActive)
        {
            return existingSession;
        }

        var newSession = ChatSession.Create();
        await _sessionRepository.AddAsync(newSession, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return newSession;
    }

    /// <inheritdoc />
    public async Task<ChatSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatSession>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        // For now, return all sessions (we could add a userId filter later)
        return await _sessionRepository.ListAsync(0, 100, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatMessage>?> GetMessagesAsync(Guid sessionId, int limit = 50, CancellationToken cancellationToken = default)
    {
        // Check if session exists
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        return await _messageRepository.GetBySessionAsync(sessionId, limit, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ChatResult> SendMessageAsync(
        Guid sessionId,
        string content,
        ChatContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);

        var validationError = ValidateSessionForMessage(session);
        if (validationError is not null)
        {
            return validationError;
        }

        var userMessage = session!.AddUserMessage(content);
        await _messageRepository.AddAsync(userMessage, cancellationToken);

        var parseResult = await this.TryHandleKakeiboQueryAsync(content, context, cancellationToken)
            ?? await this.ParseUserCommandAsync(content, context, cancellationToken);

        var assistantMessage = session.AddAssistantMessage(parseResult.ResponseText, parseResult.Action);
        await _messageRepository.AddAsync(assistantMessage, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ChatResult(
            Success: parseResult.Success,
            UserMessage: userMessage,
            AssistantMessage: assistantMessage,
            ErrorMessage: parseResult.ErrorMessage);
    }

    /// <inheritdoc />
    public async Task<ActionExecutionResult> ConfirmActionAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _messageRepository.GetByIdAsync(messageId, cancellationToken);

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
        var message = await _messageRepository.GetByIdAsync(messageId, cancellationToken);
        if (message == null || message.ActionStatus != ChatActionStatus.Pending)
        {
            return false;
        }

        message.MarkActionCancelled();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> CloseSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session == null || !session.IsActive)
        {
            return false;
        }

        session.Close();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
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

    private static bool IsKakeiboQueryIntent(string normalized)
    {
        return normalized.Contains("spend", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("spent", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("spending", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("show", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("filter", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("total", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("how much", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("amount", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseKakeiboCategory(string normalized, out KakeiboCategory category)
    {
        if (normalized.Contains("essential", StringComparison.OrdinalIgnoreCase))
        {
            category = KakeiboCategory.Essentials;
            return true;
        }

        if (normalized.Contains("wants", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("want", StringComparison.OrdinalIgnoreCase))
        {
            category = KakeiboCategory.Wants;
            return true;
        }

        if (normalized.Contains("culture", StringComparison.OrdinalIgnoreCase))
        {
            category = KakeiboCategory.Culture;
            return true;
        }

        if (normalized.Contains("unexpected", StringComparison.OrdinalIgnoreCase))
        {
            category = KakeiboCategory.Unexpected;
            return true;
        }

        category = default;
        return false;
    }

    private static bool TryParseKakeiboQuery(
        string content,
        ChatContext? context,
        out KakeiboCategory kakeiboCategory,
        out DateRangeInfo range)
    {
        range = DateRangeInfo.Default;
        kakeiboCategory = default;

        var normalized = content.Trim().ToLowerInvariant();
        if (!IsKakeiboQueryIntent(normalized))
        {
            return false;
        }

        if (!TryParseKakeiboCategory(normalized, out kakeiboCategory))
        {
            return false;
        }

        var referenceDate = context?.CurrentDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        range = BuildDateRange(normalized, referenceDate);
        return true;
    }

    private static DateRangeInfo BuildDateRange(string normalized, DateOnly today)
    {
        if (normalized.Contains("yesterday", StringComparison.OrdinalIgnoreCase))
        {
            var date = today.AddDays(-1);
            return new DateRangeInfo(date, date, "yesterday");
        }

        if (normalized.Contains("today", StringComparison.OrdinalIgnoreCase))
        {
            return new DateRangeInfo(today, today, "today");
        }

        if (normalized.Contains("last week", StringComparison.OrdinalIgnoreCase))
        {
            var weekStart = GetWeekStart(today).AddDays(-7);
            return new DateRangeInfo(weekStart, weekStart.AddDays(6), "last week");
        }

        if (normalized.Contains("this week", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("week", StringComparison.OrdinalIgnoreCase))
        {
            var weekStart = GetWeekStart(today);
            return new DateRangeInfo(weekStart, weekStart.AddDays(6), "this week");
        }

        if (normalized.Contains("last month", StringComparison.OrdinalIgnoreCase))
        {
            var firstOfThisMonth = new DateOnly(today.Year, today.Month, 1);
            var firstOfLastMonth = firstOfThisMonth.AddMonths(-1);
            var endOfLastMonth = firstOfThisMonth.AddDays(-1);
            return new DateRangeInfo(firstOfLastMonth, endOfLastMonth, "last month");
        }

        if (normalized.Contains("this month", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("month", StringComparison.OrdinalIgnoreCase))
        {
            var start = new DateOnly(today.Year, today.Month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            return new DateRangeInfo(start, end, "this month");
        }

        if (normalized.Contains("last year", StringComparison.OrdinalIgnoreCase))
        {
            var start = new DateOnly(today.Year - 1, 1, 1);
            var end = new DateOnly(today.Year - 1, 12, 31);
            return new DateRangeInfo(start, end, "last year");
        }

        if (normalized.Contains("this year", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("year", StringComparison.OrdinalIgnoreCase))
        {
            var start = new DateOnly(today.Year, 1, 1);
            var end = new DateOnly(today.Year, 12, 31);
            return new DateRangeInfo(start, end, "this year");
        }

        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        return new DateRangeInfo(monthStart, monthEnd, "this month");
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        return date.AddDays(-daysFromMonday);
    }

    private async Task<ParseResult> ParseUserCommandAsync(
        string content,
        ChatContext? context,
        CancellationToken cancellationToken)
    {
        var accounts = await this.GetAccountInfoAsync(cancellationToken);
        var categories = await this.GetCategoryInfoAsync(cancellationToken);

        return await _parser.ParseCommandAsync(
            content, accounts, categories, context, cancellationToken);
    }

    private async Task<ParseResult?> TryHandleKakeiboQueryAsync(
        string content,
        ChatContext? context,
        CancellationToken cancellationToken)
    {
        if (!await _featureFlagService.IsEnabledAsync(KakeiboFilterFlag, cancellationToken))
        {
            return null;
        }

        if (!TryParseKakeiboQuery(content, context, out var kakeiboCategory, out var range))
        {
            return null;
        }

        var transactions = await _transactionService.GetByDateRangeAsync(
            range.StartDate,
            range.EndDate,
            context?.CurrentAccountId,
            kakeiboCategory,
            cancellationToken);

        var expenses = transactions
            .Where(t => !t.IsTransfer && t.Amount.Amount < 0m)
            .ToList();

        if (expenses.Count == 0)
        {
            return new ParseResult(
                Success: true,
                Action: null,
                ResponseText: $"I couldn't find any {kakeiboCategory} spending {range.Label}.");
        }

        var total = expenses.Sum(t => Math.Abs(t.Amount.Amount));
        var totalText = total.ToString("C", CultureInfo.CurrentCulture);
        var accountSuffix = !string.IsNullOrWhiteSpace(context?.CurrentAccountName)
            ? $" in {context.CurrentAccountName}"
            : string.Empty;

        return new ParseResult(
            Success: true,
            Action: null,
            ResponseText: $"You spent {totalText} on {kakeiboCategory} {range.Label}{accountSuffix}.");
    }

    private async Task<ActionExecutionResult> ExecuteAndUpdateActionStatusAsync(
        ChatMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _actionExecutor.ExecuteActionAsync(message.Action!, cancellationToken);

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

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return result;
        }
        catch (DomainException ex)
        {
            message.MarkActionFailed(ex.Message);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

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
        var accounts = await _accountRepository.GetAllAsync(cancellationToken);
        return accounts.Select(a => new AccountInfo(a.Id, a.Name, a.Type)).ToList();
    }

    private async Task<IReadOnlyList<CategoryInfo>> GetCategoryInfoAsync(CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.GetActiveAsync(cancellationToken);
        return categories.Select(c => new CategoryInfo(c.Id, c.Name, c.KakeiboCategory)).ToList();
    }

    private sealed record DateRangeInfo(DateOnly StartDate, DateOnly EndDate, string Label)
    {
        public static DateRangeInfo Default => new(DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow), "today");
    }
}
