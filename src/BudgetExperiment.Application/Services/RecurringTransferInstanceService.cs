// <copyright file="RecurringTransferInstanceService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Mapping;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Service for recurring transfer instance operations.
/// </summary>
public sealed class RecurringTransferInstanceService : IRecurringTransferInstanceService
{
    private readonly IRecurringTransferRepository _repository;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransferInstanceService"/> class.
    /// </summary>
    /// <param name="repository">The recurring transfer repository.</param>
    /// <param name="accountRepository">The account repository.</param>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public RecurringTransferInstanceService(
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
    public async Task<IReadOnlyList<RecurringTransferInstanceDto>?> GetInstancesAsync(
        Guid id,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var recurring = await _repository.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return null;
        }

        var exceptions = await _repository.GetExceptionsByDateRangeAsync(id, fromDate, toDate, cancellationToken);
        var exceptionMap = exceptions.ToDictionary(e => e.OriginalDate);

        // Get generated transactions in range for source account
        var sourceTransactions = await _transactionRepository.GetByDateRangeAsync(fromDate, toDate, recurring.SourceAccountId, cancellationToken);
        var sourceMap = sourceTransactions
            .Where(t => t.RecurringTransferId == id && t.RecurringTransferInstanceDate.HasValue)
            .ToDictionary(t => t.RecurringTransferInstanceDate!.Value, t => t.Id);

        // Get generated transactions in range for destination account
        var destTransactions = await _transactionRepository.GetByDateRangeAsync(fromDate, toDate, recurring.DestinationAccountId, cancellationToken);
        var destMap = destTransactions
            .Where(t => t.RecurringTransferId == id && t.RecurringTransferInstanceDate.HasValue)
            .ToDictionary(t => t.RecurringTransferInstanceDate!.Value, t => t.Id);

        var accounts = await GetAccountNamesAsync(recurring.SourceAccountId, recurring.DestinationAccountId, cancellationToken);
        var occurrences = recurring.GetOccurrencesBetween(fromDate, toDate);
        var result = new List<RecurringTransferInstanceDto>();

        foreach (var date in occurrences)
        {
            exceptionMap.TryGetValue(date, out var exception);
            sourceMap.TryGetValue(date, out var sourceTransactionId);
            destMap.TryGetValue(date, out var destTransactionId);

            var instance = DomainToDtoMapper.ToTransferInstanceDto(
                recurring,
                date,
                accounts.SourceName,
                accounts.DestName,
                exception,
                sourceTransactionId != Guid.Empty ? sourceTransactionId : null,
                destTransactionId != Guid.Empty ? destTransactionId : null);

            result.Add(instance);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<RecurringTransferInstanceDto?> ModifyInstanceAsync(
        Guid id,
        DateOnly instanceDate,
        RecurringTransferInstanceModifyDto dto,
        CancellationToken cancellationToken = default)
    {
        var recurring = await _repository.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return null;
        }

        var exception = await _repository.GetExceptionAsync(id, instanceDate, cancellationToken);
        var modifiedAmount = dto.Amount != null ? MoneyValue.Create(dto.Amount.Currency, dto.Amount.Amount) : null;

        if (exception is null)
        {
            // Create new exception
            exception = RecurringTransferException.CreateModified(
                id,
                instanceDate,
                modifiedAmount,
                dto.Description,
                dto.Date);
            await _repository.AddExceptionAsync(exception, cancellationToken);
        }
        else
        {
            // Update existing exception
            exception.Update(modifiedAmount, dto.Description, dto.Date);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accounts = await GetAccountNamesAsync(recurring.SourceAccountId, recurring.DestinationAccountId, cancellationToken);
        return DomainToDtoMapper.ToTransferInstanceDto(recurring, instanceDate, accounts.SourceName, accounts.DestName, exception);
    }

    /// <inheritdoc/>
    public async Task<bool> SkipInstanceAsync(
        Guid id,
        DateOnly instanceDate,
        CancellationToken cancellationToken = default)
    {
        var recurring = await _repository.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return false;
        }

        var existingException = await _repository.GetExceptionAsync(id, instanceDate, cancellationToken);
        if (existingException != null)
        {
            await _repository.RemoveExceptionAsync(existingException, cancellationToken);
        }

        var exception = RecurringTransferException.CreateSkipped(id, instanceDate);
        await _repository.AddExceptionAsync(exception, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RecurringTransferInstanceDto>> GetProjectedInstancesAsync(
        DateOnly fromDate,
        DateOnly toDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var recurring = accountId.HasValue
            ? await _repository.GetByAccountIdAsync(accountId.Value, cancellationToken)
            : await _repository.GetActiveAsync(cancellationToken);

        var result = new List<RecurringTransferInstanceDto>();

        foreach (var r in recurring.Where(r => r.IsActive))
        {
            var instances = await GetInstancesAsync(r.Id, fromDate, toDate, cancellationToken);
            if (instances != null)
            {
                result.AddRange(instances.Where(i => !i.IsSkipped));
            }
        }

        return result.OrderBy(i => i.EffectiveDate).ToList();
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
