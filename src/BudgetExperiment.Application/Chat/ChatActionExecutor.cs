// <copyright file="ChatActionExecutor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Settings;

namespace BudgetExperiment.Application.Chat;

/// <summary>
/// Executes chat actions by dispatching to the appropriate domain service.
/// </summary>
public sealed class ChatActionExecutor : IChatActionExecutor
{
    private readonly ITransactionService _transactionService;
    private readonly ITransferService _transferService;
    private readonly IRecurringTransactionService _recurringTransactionService;
    private readonly IRecurringTransferService _recurringTransferService;
    private readonly ICurrencyProvider _currencyProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatActionExecutor"/> class.
    /// </summary>
    /// <param name="transactionService">The transaction service.</param>
    /// <param name="transferService">The transfer service.</param>
    /// <param name="recurringTransactionService">The recurring transaction service.</param>
    /// <param name="recurringTransferService">The recurring transfer service.</param>
    /// <param name="currencyProvider">The currency provider.</param>
    public ChatActionExecutor(
        ITransactionService transactionService,
        ITransferService transferService,
        IRecurringTransactionService recurringTransactionService,
        IRecurringTransferService recurringTransferService,
        ICurrencyProvider currencyProvider)
    {
        this._transactionService = transactionService;
        this._transferService = transferService;
        this._recurringTransactionService = recurringTransactionService;
        this._recurringTransferService = recurringTransferService;
        this._currencyProvider = currencyProvider;
    }

    /// <inheritdoc/>
    public async Task<ActionExecutionResult> ExecuteActionAsync(ChatAction action, CancellationToken cancellationToken = default)
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
        var currency = await this._currencyProvider.GetCurrencyAsync(cancellationToken);
        var dto = new TransactionCreateDto
        {
            AccountId = action.AccountId,
            Amount = new MoneyDto { Currency = currency, Amount = action.Amount },
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
        var currency = await this._currencyProvider.GetCurrencyAsync(cancellationToken);
        var request = new CreateTransferRequest
        {
            SourceAccountId = action.FromAccountId,
            DestinationAccountId = action.ToAccountId,
            Amount = action.Amount,
            Currency = currency,
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
        var currency = await this._currencyProvider.GetCurrencyAsync(cancellationToken);
        var dto = new RecurringTransactionCreateDto
        {
            AccountId = action.AccountId,
            Description = action.Description,
            Amount = new MoneyDto { Currency = currency, Amount = action.Amount },
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
        var currency = await this._currencyProvider.GetCurrencyAsync(cancellationToken);
        var dto = new RecurringTransferCreateDto
        {
            SourceAccountId = action.FromAccountId,
            DestinationAccountId = action.ToAccountId,
            Description = action.Description ?? "Recurring transfer",
            Amount = new MoneyDto { Currency = currency, Amount = action.Amount },
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
