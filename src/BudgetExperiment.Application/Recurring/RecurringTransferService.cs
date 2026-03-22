// <copyright file="RecurringTransferService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Recurring;

/// <summary>
/// Application service for recurring transfer use cases.
/// </summary>
public sealed class RecurringTransferService : IRecurringTransferService
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
        var version = this._unitOfWork.GetConcurrencyToken(recurring);
        return RecurringMapper.ToDto(recurring, accounts.SourceName, accounts.DestName, version);
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
            .Select(r => RecurringMapper.ToDto(
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
            .Select(r => RecurringMapper.ToDto(
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
            .Select(r => RecurringMapper.ToDto(
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
            throw new DomainException("Source account not found.", DomainExceptionType.NotFound);
        }

        var destAccount = await this._accountRepository.GetByIdAsync(dto.DestinationAccountId, cancellationToken);
        if (destAccount is null)
        {
            throw new DomainException("Destination account not found.", DomainExceptionType.NotFound);
        }

        var amount = MoneyValue.Create(dto.Amount.Currency, dto.Amount.Amount);
        var pattern = RecurrencePatternFactory.Create(dto.Frequency, dto.Interval, dto.DayOfMonth, dto.DayOfWeek, dto.MonthOfYear);

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

        return RecurringMapper.ToDto(recurring, sourceAccount.Name, destAccount.Name);
    }

    /// <summary>
    /// Updates a recurring transfer.
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="dto">The update data.</param>
    /// <param name="expectedVersion">Optional concurrency token for optimistic concurrency.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transfer DTO, or null if not found.</returns>
    public async Task<RecurringTransferDto?> UpdateAsync(Guid id, RecurringTransferUpdateDto dto, string? expectedVersion = null, CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return null;
        }

        if (expectedVersion is not null)
        {
            this._unitOfWork.SetExpectedConcurrencyToken(recurring, expectedVersion);
        }

        var amount = MoneyValue.Create(dto.Amount.Currency, dto.Amount.Amount);
        var pattern = RecurrencePatternFactory.Create(dto.Frequency, dto.Interval, dto.DayOfMonth, dto.DayOfWeek, dto.MonthOfYear);

        recurring.Update(dto.Description, amount, pattern, dto.EndDate);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        var accounts = await this.GetAccountNamesAsync(recurring.SourceAccountId, recurring.DestinationAccountId, cancellationToken);
        var version = this._unitOfWork.GetConcurrencyToken(recurring);
        return RecurringMapper.ToDto(recurring, accounts.SourceName, accounts.DestName, version);
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
        return RecurringMapper.ToDto(recurring, accounts.SourceName, accounts.DestName);
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
        return RecurringMapper.ToDto(recurring, accounts.SourceName, accounts.DestName);
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
        return RecurringMapper.ToDto(recurring, accounts.SourceName, accounts.DestName);
    }

    /// <summary>
    /// Updates this instance and all future instances (modifies the series).
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="instanceDate">The date from which to apply changes.</param>
    /// <param name="dto">The update data.</param>
    /// <param name="expectedVersion">Optional concurrency token for optimistic concurrency.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transfer DTO, or null if not found.</returns>
    public async Task<RecurringTransferDto?> UpdateFromDateAsync(
        Guid id,
        DateOnly instanceDate,
        RecurringTransferUpdateDto dto,
        string? expectedVersion = null,
        CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return null;
        }

        if (expectedVersion is not null)
        {
            this._unitOfWork.SetExpectedConcurrencyToken(recurring, expectedVersion);
        }

        // Remove all exceptions from this date forward
        await this._repository.RemoveExceptionsFromDateAsync(id, instanceDate, cancellationToken);

        // Update the series
        var amount = MoneyValue.Create(dto.Amount.Currency, dto.Amount.Amount);
        var pattern = RecurrencePatternFactory.Create(dto.Frequency, dto.Interval, dto.DayOfMonth, dto.DayOfWeek, dto.MonthOfYear);
        recurring.Update(dto.Description, amount, pattern, dto.EndDate);

        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        var accounts = await this.GetAccountNamesAsync(recurring.SourceAccountId, recurring.DestinationAccountId, cancellationToken);
        var version = this._unitOfWork.GetConcurrencyToken(recurring);
        return RecurringMapper.ToDto(recurring, accounts.SourceName, accounts.DestName, version);
    }

    private async Task<(string SourceName, string DestName)> GetAccountNamesAsync(
        Guid sourceAccountId,
        Guid destAccountId,
        CancellationToken cancellationToken)
    {
        var sourceAccount = await this._accountRepository.GetByIdAsync(sourceAccountId, cancellationToken);
        var destAccount = await this._accountRepository.GetByIdAsync(destAccountId, cancellationToken);
        return (sourceAccount?.Name ?? string.Empty, destAccount?.Name ?? string.Empty);
    }
}
