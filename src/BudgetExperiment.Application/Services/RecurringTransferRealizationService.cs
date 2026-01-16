// <copyright file="RecurringTransferRealizationService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Service for realizing recurring transfers into actual transfers.
/// </summary>
public sealed class RecurringTransferRealizationService : IRecurringTransferRealizationService
{
    private readonly IRecurringTransferRepository _repository;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransferRealizationService"/> class.
    /// </summary>
    /// <param name="repository">The recurring transfer repository.</param>
    /// <param name="accountRepository">The account repository.</param>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public RecurringTransferRealizationService(
        IRecurringTransferRepository repository,
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<TransferResponse> RealizeInstanceAsync(
        Guid recurringTransferId,
        RealizeRecurringTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        var recurring = await _repository.GetByIdAsync(recurringTransferId, cancellationToken);
        if (recurring is null)
        {
            throw new DomainException("Recurring transfer not found.");
        }

        // Check if already realized
        var existing = await _transactionRepository.GetByRecurringTransferInstanceAsync(
            recurringTransferId, request.InstanceDate, cancellationToken);
        if (existing.Count > 0)
        {
            throw new DomainException("This instance has already been realized.");
        }

        // Get any exception modifications
        var exception = await _repository.GetExceptionAsync(
            recurringTransferId, request.InstanceDate, cancellationToken);

        // Determine actual values: request overrides > exception > recurring defaults
        var actualDate = request.Date ?? exception?.ModifiedDate ?? request.InstanceDate;
        var actualAmount = request.Amount != null
            ? MoneyValue.Create(request.Amount.Currency, request.Amount.Amount)
            : exception?.ModifiedAmount ?? recurring.Amount;

        var transferId = Guid.NewGuid();

        // Create source transaction (negative - money leaving)
        var sourceTransaction = Transaction.CreateFromRecurringTransfer(
            recurring.SourceAccountId,
            MoneyValue.Create(actualAmount.Currency, -actualAmount.Amount),
            actualDate,
            recurring.Description,
            transferId,
            TransferDirection.Source,
            recurringTransferId,
            request.InstanceDate,
            category: null);

        // Create destination transaction (positive - money entering)
        var destTransaction = Transaction.CreateFromRecurringTransfer(
            recurring.DestinationAccountId,
            actualAmount,
            actualDate,
            recurring.Description,
            transferId,
            TransferDirection.Destination,
            recurringTransferId,
            request.InstanceDate,
            category: null);

        await _transactionRepository.AddAsync(sourceTransaction, cancellationToken);
        await _transactionRepository.AddAsync(destTransaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accounts = await GetAccountNamesAsync(recurring.SourceAccountId, recurring.DestinationAccountId, cancellationToken);

        return new TransferResponse
        {
            TransferId = transferId,
            SourceAccountId = recurring.SourceAccountId,
            SourceAccountName = accounts.SourceName,
            DestinationAccountId = recurring.DestinationAccountId,
            DestinationAccountName = accounts.DestName,
            Amount = actualAmount.Amount,
            Currency = actualAmount.Currency,
            Date = actualDate,
            Description = recurring.Description,
            SourceTransactionId = sourceTransaction.Id,
            DestinationTransactionId = destTransaction.Id,
            CreatedAtUtc = sourceTransaction.CreatedAt,
        };
    }

    private async Task<(string SourceName, string DestName)> GetAccountNamesAsync(
        Guid sourceAccountId,
        Guid destAccountId,
        CancellationToken cancellationToken)
    {
        var sourceAccount = await _accountRepository.GetByIdAsync(sourceAccountId, cancellationToken);
        var destAccount = await _accountRepository.GetByIdAsync(destAccountId, cancellationToken);
        return (sourceAccount?.Name ?? string.Empty, destAccount?.Name ?? string.Empty);
    }
}
