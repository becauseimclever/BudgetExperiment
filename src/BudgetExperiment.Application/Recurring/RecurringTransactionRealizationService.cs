// <copyright file="RecurringTransactionRealizationService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Recurring;

/// <summary>
/// Service for realizing recurring transactions into actual transactions.
/// </summary>
public sealed class RecurringTransactionRealizationService : IRecurringTransactionRealizationService
{
    private readonly IRecurringTransactionRepository _repository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransactionRealizationService"/> class.
    /// </summary>
    /// <param name="repository">The recurring transaction repository.</param>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public RecurringTransactionRealizationService(
        IRecurringTransactionRepository repository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<TransactionDto> RealizeInstanceAsync(
        Guid recurringTransactionId,
        RealizeRecurringTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var recurring = await _repository.GetByIdAsync(recurringTransactionId, cancellationToken);
        if (recurring is null)
        {
            throw new DomainException("Recurring transaction not found.");
        }

        // Check if already realized
        var existing = await _transactionRepository.GetByRecurringInstanceAsync(
            recurringTransactionId, request.InstanceDate, cancellationToken);
        if (existing != null)
        {
            throw new DomainException("This instance has already been realized.");
        }

        // Get any exception modifications
        var exception = await _repository.GetExceptionAsync(
            recurringTransactionId, request.InstanceDate, cancellationToken);

        // Determine actual values: request overrides > exception > recurring defaults
        var actualDate = request.Date ?? exception?.ModifiedDate ?? request.InstanceDate;
        var actualAmount = request.Amount != null
            ? MoneyValue.Create(request.Amount.Currency, request.Amount.Amount)
            : exception?.ModifiedAmount ?? recurring.Amount;
        var actualDescription = request.Description ?? exception?.ModifiedDescription ?? recurring.Description;

        var transaction = Transaction.CreateFromRecurring(
            recurring.AccountId,
            actualAmount,
            actualDate,
            actualDescription,
            recurringTransactionId,
            request.InstanceDate,
            categoryId: recurring.CategoryId);

        await _transactionRepository.AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AccountMapper.ToDto(transaction);
    }
}
