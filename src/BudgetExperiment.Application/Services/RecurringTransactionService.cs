// <copyright file="RecurringTransactionService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Application.Mapping;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Application service for recurring transaction use cases.
/// </summary>
public sealed class RecurringTransactionService
{
    private readonly IRecurringTransactionRepository _repository;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransactionService"/> class.
    /// </summary>
    /// <param name="repository">The recurring transaction repository.</param>
    /// <param name="accountRepository">The account repository.</param>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public RecurringTransactionService(
        IRecurringTransactionRepository repository,
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork)
    {
        this._repository = repository;
        this._accountRepository = accountRepository;
        this._transactionRepository = transactionRepository;
        this._unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Gets a recurring transaction by its identifier.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recurring transaction DTO, or null if not found.</returns>
    public async Task<RecurringTransactionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return null;
        }

        var account = await this._accountRepository.GetByIdAsync(recurring.AccountId, cancellationToken);
        return DomainToDtoMapper.ToDto(recurring, account?.Name ?? string.Empty);
    }

    /// <summary>
    /// Gets all recurring transactions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of recurring transaction DTOs.</returns>
    public async Task<IReadOnlyList<RecurringTransactionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetAllAsync(cancellationToken);
        var accounts = await this._accountRepository.GetAllAsync(cancellationToken);
        var accountMap = accounts.ToDictionary(a => a.Id, a => a.Name);

        return recurring
            .Select(r => DomainToDtoMapper.ToDto(r, accountMap.GetValueOrDefault(r.AccountId, string.Empty)))
            .ToList();
    }

    /// <summary>
    /// Gets recurring transactions for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of recurring transaction DTOs.</returns>
    public async Task<IReadOnlyList<RecurringTransactionDto>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetByAccountIdAsync(accountId, cancellationToken);
        var account = await this._accountRepository.GetByIdAsync(accountId, cancellationToken);
        var accountName = account?.Name ?? string.Empty;

        return recurring
            .Select(r => DomainToDtoMapper.ToDto(r, accountName))
            .ToList();
    }

    /// <summary>
    /// Creates a new recurring transaction.
    /// </summary>
    /// <param name="dto">The creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created recurring transaction DTO.</returns>
    /// <exception cref="DomainException">Thrown when the account is not found.</exception>
    public async Task<RecurringTransactionDto> CreateAsync(RecurringTransactionCreateDto dto, CancellationToken cancellationToken = default)
    {
        var account = await this._accountRepository.GetByIdAsync(dto.AccountId, cancellationToken);
        if (account is null)
        {
            throw new DomainException("Account not found.");
        }

        var amount = MoneyValue.Create(dto.Amount.Currency, dto.Amount.Amount);
        var pattern = CreateRecurrencePattern(dto.Frequency, dto.Interval, dto.DayOfMonth, dto.DayOfWeek, dto.MonthOfYear);

        var recurring = RecurringTransaction.Create(
            dto.AccountId,
            dto.Description,
            amount,
            pattern,
            dto.StartDate,
            dto.EndDate);

        await this._repository.AddAsync(recurring, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        return DomainToDtoMapper.ToDto(recurring, account.Name);
    }

    /// <summary>
    /// Updates a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="dto">The update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transaction DTO, or null if not found.</returns>
    public async Task<RecurringTransactionDto?> UpdateAsync(Guid id, RecurringTransactionUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return null;
        }

        var amount = MoneyValue.Create(dto.Amount.Currency, dto.Amount.Amount);
        var pattern = CreateRecurrencePattern(dto.Frequency, dto.Interval, dto.DayOfMonth, dto.DayOfWeek, dto.MonthOfYear);

        recurring.Update(dto.Description, amount, pattern, dto.EndDate);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        var account = await this._accountRepository.GetByIdAsync(recurring.AccountId, cancellationToken);
        return DomainToDtoMapper.ToDto(recurring, account?.Name ?? string.Empty);
    }

    /// <summary>
    /// Deletes a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return false;
        }

        await this._repository.RemoveAsync(recurring, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Pauses a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transaction DTO, or null if not found.</returns>
    public async Task<RecurringTransactionDto?> PauseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return null;
        }

        recurring.Pause();
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        var account = await this._accountRepository.GetByIdAsync(recurring.AccountId, cancellationToken);
        return DomainToDtoMapper.ToDto(recurring, account?.Name ?? string.Empty);
    }

    /// <summary>
    /// Resumes a paused recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transaction DTO, or null if not found.</returns>
    public async Task<RecurringTransactionDto?> ResumeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return null;
        }

        recurring.Resume();
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        var account = await this._accountRepository.GetByIdAsync(recurring.AccountId, cancellationToken);
        return DomainToDtoMapper.ToDto(recurring, account?.Name ?? string.Empty);
    }

    /// <summary>
    /// Skips the next occurrence of a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transaction DTO, or null if not found.</returns>
    public async Task<RecurringTransactionDto?> SkipNextAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return null;
        }

        // Create a skipped exception for the current next occurrence
        var exception = RecurringTransactionException.CreateSkipped(recurring.Id, recurring.NextOccurrence);
        await this._repository.AddExceptionAsync(exception, cancellationToken);

        // Advance to the next occurrence
        recurring.AdvanceNextOccurrence();
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        var account = await this._accountRepository.GetByIdAsync(recurring.AccountId, cancellationToken);
        return DomainToDtoMapper.ToDto(recurring, account?.Name ?? string.Empty);
    }

    /// <summary>
    /// Gets projected instances for a recurring transaction within a date range.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="fromDate">Start date (inclusive).</param>
    /// <param name="toDate">End date (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of instance DTOs, or null if recurring transaction not found.</returns>
    public async Task<IReadOnlyList<RecurringInstanceDto>?> GetInstancesAsync(
        Guid id,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return null;
        }

        var exceptions = await this._repository.GetExceptionsByDateRangeAsync(id, fromDate, toDate, cancellationToken);
        var exceptionMap = exceptions.ToDictionary(e => e.OriginalDate);

        // Get generated transactions in range
        var transactions = await this._transactionRepository.GetByDateRangeAsync(fromDate, toDate, recurring.AccountId, cancellationToken);
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

    /// <summary>
    /// Modifies a single instance of a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="instanceDate">The original scheduled date of the instance.</param>
    /// <param name="dto">The modification data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The modified instance DTO, or null if not found.</returns>
    public async Task<RecurringInstanceDto?> ModifyInstanceAsync(
        Guid id,
        DateOnly instanceDate,
        RecurringInstanceModifyDto dto,
        CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return null;
        }

        var exception = await this._repository.GetExceptionAsync(id, instanceDate, cancellationToken);
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
            await this._repository.AddExceptionAsync(exception, cancellationToken);
        }
        else
        {
            // Update existing exception
            exception.Update(modifiedAmount, dto.Description, dto.Date);
        }

        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        return DomainToDtoMapper.ToInstanceDto(recurring, instanceDate, exception);
    }

    /// <summary>
    /// Skips (deletes) a single instance of a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="instanceDate">The original scheduled date of the instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if skipped, false if recurring transaction not found.</returns>
    public async Task<bool> SkipInstanceAsync(
        Guid id,
        DateOnly instanceDate,
        CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return false;
        }

        var existingException = await this._repository.GetExceptionAsync(id, instanceDate, cancellationToken);
        if (existingException != null)
        {
            await this._repository.RemoveExceptionAsync(existingException, cancellationToken);
        }

        var exception = RecurringTransactionException.CreateSkipped(id, instanceDate);
        await this._repository.AddExceptionAsync(exception, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Updates this instance and all future instances (modifies the series).
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="instanceDate">The date from which to apply changes.</param>
    /// <param name="dto">The update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transaction DTO, or null if not found.</returns>
    public async Task<RecurringTransactionDto?> UpdateFromDateAsync(
        Guid id,
        DateOnly instanceDate,
        RecurringTransactionUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return null;
        }

        // Remove all exceptions from this date forward
        await this._repository.RemoveExceptionsFromDateAsync(id, instanceDate, cancellationToken);

        // Update the series
        var amount = MoneyValue.Create(dto.Amount.Currency, dto.Amount.Amount);
        var pattern = CreateRecurrencePattern(dto.Frequency, dto.Interval, dto.DayOfMonth, dto.DayOfWeek, dto.MonthOfYear);
        recurring.Update(dto.Description, amount, pattern, dto.EndDate);

        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        var account = await this._accountRepository.GetByIdAsync(recurring.AccountId, cancellationToken);
        return DomainToDtoMapper.ToDto(recurring, account?.Name ?? string.Empty);
    }

    /// <summary>
    /// Gets all projected recurring transaction instances across all active recurring transactions.
    /// </summary>
    /// <param name="fromDate">Start date (inclusive).</param>
    /// <param name="toDate">End date (inclusive).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of instance DTOs.</returns>
    public async Task<IReadOnlyList<RecurringInstanceDto>> GetProjectedInstancesAsync(
        DateOnly fromDate,
        DateOnly toDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var recurring = accountId.HasValue
            ? await this._repository.GetByAccountIdAsync(accountId.Value, cancellationToken)
            : await this._repository.GetActiveAsync(cancellationToken);

        var result = new List<RecurringInstanceDto>();

        foreach (var r in recurring.Where(r => r.IsActive))
        {
            var instances = await this.GetInstancesAsync(r.Id, fromDate, toDate, cancellationToken);
            if (instances != null)
            {
                result.AddRange(instances.Where(i => !i.IsSkipped));
            }
        }

        return result.OrderBy(i => i.EffectiveDate).ToList();
    }

    /// <summary>
    /// Realizes a recurring transaction instance, converting it to an actual transaction.
    /// </summary>
    /// <param name="recurringTransactionId">The recurring transaction identifier.</param>
    /// <param name="request">The realization request with optional overrides.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created transaction DTO.</returns>
    /// <exception cref="DomainException">Thrown when the recurring transaction is not found or already realized.</exception>
    public async Task<TransactionDto> RealizeInstanceAsync(
        Guid recurringTransactionId,
        RealizeRecurringTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetByIdAsync(recurringTransactionId, cancellationToken);
        if (recurring is null)
        {
            throw new DomainException("Recurring transaction not found.");
        }

        // Check if already realized
        var existing = await this._transactionRepository.GetByRecurringInstanceAsync(
            recurringTransactionId, request.InstanceDate, cancellationToken);
        if (existing != null)
        {
            throw new DomainException("This instance has already been realized.");
        }

        // Get any exception modifications
        var exception = await this._repository.GetExceptionAsync(
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
            category: null);

        await this._transactionRepository.AddAsync(transaction, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        return DomainToDtoMapper.ToDto(transaction);
    }

    private static RecurrencePattern CreateRecurrencePattern(
        string frequency,
        int interval,
        int? dayOfMonth,
        string? dayOfWeek,
        int? monthOfYear)
    {
        if (!Enum.TryParse<RecurrenceFrequency>(frequency, ignoreCase: true, out var freq))
        {
            throw new DomainException($"Invalid frequency: {frequency}");
        }

        return freq switch
        {
            RecurrenceFrequency.Daily => RecurrencePattern.CreateDaily(interval),
            RecurrenceFrequency.Weekly => RecurrencePattern.CreateWeekly(interval, ParseDayOfWeek(dayOfWeek)),
            RecurrenceFrequency.BiWeekly => RecurrencePattern.CreateBiWeekly(ParseDayOfWeek(dayOfWeek)),
            RecurrenceFrequency.Monthly => RecurrencePattern.CreateMonthly(interval, dayOfMonth ?? 1),
            RecurrenceFrequency.Quarterly => RecurrencePattern.CreateQuarterly(dayOfMonth ?? 1),
            RecurrenceFrequency.Yearly => RecurrencePattern.CreateYearly(dayOfMonth ?? 1, monthOfYear ?? 1),
            _ => throw new DomainException($"Unsupported frequency: {frequency}"),
        };
    }

    private static DayOfWeek ParseDayOfWeek(string? dayOfWeek)
    {
        if (string.IsNullOrWhiteSpace(dayOfWeek))
        {
            throw new DomainException("Day of week is required for weekly/biweekly patterns.");
        }

        if (!Enum.TryParse<DayOfWeek>(dayOfWeek, ignoreCase: true, out var dow))
        {
            throw new DomainException($"Invalid day of week: {dayOfWeek}");
        }

        return dow;
    }
}
