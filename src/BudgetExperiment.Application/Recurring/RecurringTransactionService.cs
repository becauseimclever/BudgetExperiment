// <copyright file="RecurringTransactionService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Recurring;

/// <summary>
/// Application service for recurring transaction use cases.
/// </summary>
public sealed class RecurringTransactionService : IRecurringTransactionService
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
        var version = this._unitOfWork.GetConcurrencyToken(recurring);
        return RecurringMapper.ToDto(recurring, account?.Name ?? string.Empty, version);
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
            .Select(r => RecurringMapper.ToDto(r, accountMap.GetValueOrDefault(r.AccountId, string.Empty)))
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
            .Select(r => RecurringMapper.ToDto(r, accountName))
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
            throw new DomainException("Account not found.", DomainExceptionType.NotFound);
        }

        var amount = MoneyValue.Create(dto.Amount.Currency, dto.Amount.Amount);
        var pattern = RecurrencePatternFactory.Create(dto.Frequency, dto.Interval, dto.DayOfMonth, dto.DayOfWeek, dto.MonthOfYear);

        var recurring = RecurringTransaction.Create(
            dto.AccountId,
            dto.Description,
            amount,
            pattern,
            dto.StartDate,
            dto.EndDate);

        await this._repository.AddAsync(recurring, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        return RecurringMapper.ToDto(recurring, account.Name);
    }

    /// <summary>
    /// Updates a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="dto">The update data.</param>
    /// <param name="expectedVersion">Optional concurrency token for optimistic concurrency.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transaction DTO, or null if not found.</returns>
    public async Task<RecurringTransactionDto?> UpdateAsync(Guid id, RecurringTransactionUpdateDto dto, string? expectedVersion = null, CancellationToken cancellationToken = default)
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

        recurring.Update(dto.Description, amount, pattern, dto.EndDate, dto.CategoryId);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        var account = await this._accountRepository.GetByIdAsync(recurring.AccountId, cancellationToken);
        var version = this._unitOfWork.GetConcurrencyToken(recurring);
        return RecurringMapper.ToDto(recurring, account?.Name ?? string.Empty, version);
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
        return RecurringMapper.ToDto(recurring, account?.Name ?? string.Empty);
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
        return RecurringMapper.ToDto(recurring, account?.Name ?? string.Empty);
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
        return RecurringMapper.ToDto(recurring, account?.Name ?? string.Empty);
    }

    /// <summary>
    /// Updates this instance and all future instances (modifies the series).
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="instanceDate">The date from which to apply changes.</param>
    /// <param name="dto">The update data.</param>
    /// <param name="expectedVersion">Optional concurrency token for optimistic concurrency.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transaction DTO, or null if not found.</returns>
    public async Task<RecurringTransactionDto?> UpdateFromDateAsync(
        Guid id,
        DateOnly instanceDate,
        RecurringTransactionUpdateDto dto,
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
        recurring.Update(dto.Description, amount, pattern, dto.EndDate, dto.CategoryId);

        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        var account = await this._accountRepository.GetByIdAsync(recurring.AccountId, cancellationToken);
        var version = this._unitOfWork.GetConcurrencyToken(recurring);
        return RecurringMapper.ToDto(recurring, account?.Name ?? string.Empty, version);
    }

    /// <inheritdoc />
    public async Task<ImportPatternsDto?> GetImportPatternsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return null;
        }

        return new ImportPatternsDto
        {
            Patterns = recurring.ImportPatterns.Select(p => p.Pattern).ToList(),
        };
    }

    /// <inheritdoc />
    public async Task<ImportPatternsDto?> UpdateImportPatternsAsync(
        Guid id,
        ImportPatternsDto dto,
        CancellationToken cancellationToken = default)
    {
        var recurring = await this._repository.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return null;
        }

        // Clear existing patterns
        var existingPatterns = recurring.ImportPatterns.ToList();
        foreach (var pattern in existingPatterns)
        {
            recurring.RemoveImportPattern(pattern);
        }

        // Add new patterns
        foreach (var patternString in dto.Patterns)
        {
            var pattern = ImportPatternValue.Create(patternString);
            recurring.AddImportPattern(pattern);
        }

        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        return new ImportPatternsDto
        {
            Patterns = recurring.ImportPatterns.Select(p => p.Pattern).ToList(),
        };
    }
}
