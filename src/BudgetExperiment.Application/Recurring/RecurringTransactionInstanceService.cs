// <copyright file="RecurringTransactionInstanceService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Mapping;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Recurring;

/// <summary>
/// Service for recurring transaction instance operations.
/// </summary>
public sealed class RecurringTransactionInstanceService : IRecurringTransactionInstanceService
{
    private readonly IRecurringTransactionRepository _repository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransactionInstanceService"/> class.
    /// </summary>
    /// <param name="repository">The recurring transaction repository.</param>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public RecurringTransactionInstanceService(
        IRecurringTransactionRepository repository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RecurringInstanceDto>?> GetInstancesAsync(
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

        // Get generated transactions in range
        var transactions = await _transactionRepository.GetByDateRangeAsync(fromDate, toDate, recurring.AccountId, cancellationToken);
        var generatedMap = transactions
            .Where(t => t.RecurringTransactionId == id && t.RecurringInstanceDate.HasValue)
            .ToDictionary(t => t.RecurringInstanceDate!.Value, t => t.Id);

        var occurrences = recurring.GetOccurrencesBetween(fromDate, toDate);
        var result = new List<RecurringInstanceDto>();

        foreach (var date in occurrences)
        {
            exceptionMap.TryGetValue(date, out var exception);
            generatedMap.TryGetValue(date, out var transactionId);

            var instance = DomainToDtoMapper.ToInstanceDto(
                recurring,
                date,
                exception,
                transactionId != Guid.Empty ? transactionId : null);

            result.Add(instance);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<RecurringInstanceDto?> ModifyInstanceAsync(
        Guid id,
        DateOnly instanceDate,
        RecurringInstanceModifyDto dto,
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
            exception = RecurringTransactionException.CreateModified(
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

        return DomainToDtoMapper.ToInstanceDto(recurring, instanceDate, exception);
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

        var exception = RecurringTransactionException.CreateSkipped(id, instanceDate);
        await _repository.AddExceptionAsync(exception, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RecurringInstanceDto>> GetProjectedInstancesAsync(
        DateOnly fromDate,
        DateOnly toDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var recurring = accountId.HasValue
            ? await _repository.GetByAccountIdAsync(accountId.Value, cancellationToken)
            : await _repository.GetActiveAsync(cancellationToken);

        var result = new List<RecurringInstanceDto>();

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
}
