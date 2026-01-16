// <copyright file="RecurringTransferService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Application.Mapping;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Application service for recurring transfer use cases.
/// </summary>
public sealed class RecurringTransferService
{
    private readonly IRecurringTransferRepository _repository;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransferService"/> class.
    /// </summary>
    /// <param name="repository">The recurring transfer repository.</param>
    /// <param name="accountRepository">The account repository.</param>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public RecurringTransferService(
        IRecurringTransferRepository repository,
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
    /// Gets a recurring transfer by its identifier.
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recurring transfer DTO, or null if not found.</returns>
    public async Task<RecurringTransferDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return null;
        }

        var accounts = await this.GetAccountNamesAsync(recurring.SourceAccountId, recurring.DestinationAccountId, cancellationToken);
        return DomainToDtoMapper.ToDto(recurring, accounts.sourceName, accounts.destName);
    }

    /// <summary>
    /// Gets all recurring transfers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of recurring transfer DTOs.</returns>
    public async Task<IReadOnlyList<RecurringTransferDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetAllAsync(cancellationToken);
        var accounts = await this._accountRepository.GetAllAsync(cancellationToken);
        var accountMap = accounts.ToDictionary(a => a.Id, a => a.Name);

        return recurring
            .Select(r => DomainToDtoMapper.ToDto(
                r,
                accountMap.GetValueOrDefault(r.SourceAccountId, string.Empty),
                accountMap.GetValueOrDefault(r.DestinationAccountId, string.Empty)))
            .ToList();
    }

    /// <summary>
    /// Gets recurring transfers for a specific account (as source or destination).
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of recurring transfer DTOs.</returns>
    public async Task<IReadOnlyList<RecurringTransferDto>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetByAccountIdAsync(accountId, cancellationToken);
        var accounts = await this._accountRepository.GetAllAsync(cancellationToken);
        var accountMap = accounts.ToDictionary(a => a.Id, a => a.Name);

        return recurring
            .Select(r => DomainToDtoMapper.ToDto(
                r,
                accountMap.GetValueOrDefault(r.SourceAccountId, string.Empty),
                accountMap.GetValueOrDefault(r.DestinationAccountId, string.Empty)))
            .ToList();
    }

    /// <summary>
    /// Gets all active recurring transfers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of active recurring transfer DTOs.</returns>
    public async Task<IReadOnlyList<RecurringTransferDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetActiveAsync(cancellationToken);
        var accounts = await this._accountRepository.GetAllAsync(cancellationToken);
        var accountMap = accounts.ToDictionary(a => a.Id, a => a.Name);

        return recurring
            .Select(r => DomainToDtoMapper.ToDto(
                r,
                accountMap.GetValueOrDefault(r.SourceAccountId, string.Empty),
                accountMap.GetValueOrDefault(r.DestinationAccountId, string.Empty)))
            .ToList();
    }

    /// <summary>
    /// Creates a new recurring transfer.
    /// </summary>
    /// <param name="dto">The creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created recurring transfer DTO.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public async Task<RecurringTransferDto> CreateAsync(RecurringTransferCreateDto dto, CancellationToken cancellationToken = default)
    {
        var sourceAccount = await this._accountRepository.GetByIdAsync(dto.SourceAccountId, cancellationToken);
        if (sourceAccount is null)
        {
            throw new DomainException("Source account not found.");
        }

        var destAccount = await this._accountRepository.GetByIdAsync(dto.DestinationAccountId, cancellationToken);
        if (destAccount is null)
        {
            throw new DomainException("Destination account not found.");
        }

        var amount = MoneyValue.Create(dto.Amount.Currency, dto.Amount.Amount);
        var pattern = CreateRecurrencePattern(dto.Frequency, dto.Interval, dto.DayOfMonth, dto.DayOfWeek, dto.MonthOfYear);

        var recurring = RecurringTransfer.Create(
            dto.SourceAccountId,
            dto.DestinationAccountId,
            dto.Description,
            amount,
            pattern,
            dto.StartDate,
            dto.EndDate);

        await this._repository.AddAsync(recurring, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        return DomainToDtoMapper.ToDto(recurring, sourceAccount.Name, destAccount.Name);
    }

    /// <summary>
    /// Updates a recurring transfer.
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="dto">The update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transfer DTO, or null if not found.</returns>
    public async Task<RecurringTransferDto?> UpdateAsync(Guid id, RecurringTransferUpdateDto dto, CancellationToken cancellationToken = default)
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

        var accounts = await this.GetAccountNamesAsync(recurring.SourceAccountId, recurring.DestinationAccountId, cancellationToken);
        return DomainToDtoMapper.ToDto(recurring, accounts.sourceName, accounts.destName);
    }

    /// <summary>
    /// Deletes a recurring transfer.
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
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
    /// Pauses a recurring transfer.
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transfer DTO, or null if not found.</returns>
    public async Task<RecurringTransferDto?> PauseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return null;
        }

        recurring.Pause();
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        var accounts = await this.GetAccountNamesAsync(recurring.SourceAccountId, recurring.DestinationAccountId, cancellationToken);
        return DomainToDtoMapper.ToDto(recurring, accounts.sourceName, accounts.destName);
    }

    /// <summary>
    /// Resumes a paused recurring transfer.
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transfer DTO, or null if not found.</returns>
    public async Task<RecurringTransferDto?> ResumeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return null;
        }

        recurring.Resume(DateOnly.FromDateTime(DateTime.UtcNow));
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        var accounts = await this.GetAccountNamesAsync(recurring.SourceAccountId, recurring.DestinationAccountId, cancellationToken);
        return DomainToDtoMapper.ToDto(recurring, accounts.sourceName, accounts.destName);
    }

    /// <summary>
    /// Skips the next occurrence of a recurring transfer.
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transfer DTO, or null if not found.</returns>
    public async Task<RecurringTransferDto?> SkipNextAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return null;
        }

        // Create a skipped exception for the current next occurrence
        var exception = RecurringTransferException.CreateSkipped(recurring.Id, recurring.NextOccurrence);
        await this._repository.AddExceptionAsync(exception, cancellationToken);

        // Advance to the next occurrence
        recurring.AdvanceNextOccurrence();
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        var accounts = await this.GetAccountNamesAsync(recurring.SourceAccountId, recurring.DestinationAccountId, cancellationToken);
        return DomainToDtoMapper.ToDto(recurring, accounts.sourceName, accounts.destName);
    }

    /// <summary>
    /// Updates this instance and all future instances (modifies the series).
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="instanceDate">The date from which to apply changes.</param>
    /// <param name="dto">The update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transfer DTO, or null if not found.</returns>
    public async Task<RecurringTransferDto?> UpdateFromDateAsync(
        Guid id,
        DateOnly instanceDate,
        RecurringTransferUpdateDto dto,
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

        var accounts = await this.GetAccountNamesAsync(recurring.SourceAccountId, recurring.DestinationAccountId, cancellationToken);
        return DomainToDtoMapper.ToDto(recurring, accounts.sourceName, accounts.destName);
    }

    private async Task<(string sourceName, string destName)> GetAccountNamesAsync(
        Guid sourceAccountId,
        Guid destAccountId,
        CancellationToken cancellationToken)
    {
        var sourceAccount = await this._accountRepository.GetByIdAsync(sourceAccountId, cancellationToken);
        var destAccount = await this._accountRepository.GetByIdAsync(destAccountId, cancellationToken);
        return (sourceAccount?.Name ?? string.Empty, destAccount?.Name ?? string.Empty);
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
