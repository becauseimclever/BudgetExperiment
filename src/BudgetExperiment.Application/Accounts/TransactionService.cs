// <copyright file="TransactionService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Accounts;

/// <summary>
/// Application service for transaction use cases.
/// </summary>
public sealed class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repository;
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICategorizationEngine _categorizationEngine;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionService"/> class.
    /// </summary>
    /// <param name="repository">The transaction repository.</param>
    /// <param name="accountRepository">The account repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="categorizationEngine">The categorization engine for auto-categorization.</param>
    public TransactionService(
        ITransactionRepository repository,
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork,
        ICategorizationEngine categorizationEngine)
    {
        _repository = repository;
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _categorizationEngine = categorizationEngine;
    }

    /// <summary>
    /// Gets a transaction by its identifier.
    /// </summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transaction DTO, or null if not found.</returns>
    public async Task<TransactionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await _repository.GetByIdAsync(id, cancellationToken);
        if (transaction is null)
        {
            return null;
        }

        var version = _unitOfWork.GetConcurrencyToken(transaction);
        return AccountMapper.ToTransactionDto(transaction, version);
    }

    /// <summary>
    /// Gets transactions within a date range.
    /// </summary>
    /// <param name="startDate">The start date (inclusive).</param>
    /// <param name="endDate">The end date (inclusive).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of transaction DTOs.</returns>
    public async Task<IReadOnlyList<TransactionDto>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null, CancellationToken cancellationToken = default)
    {
        var transactions = await _repository.GetByDateRangeAsync(startDate, endDate, accountId, cancellationToken);
        return transactions.Select(AccountMapper.ToDto).ToList();
    }

    /// <summary>
    /// Creates a new transaction.
    /// </summary>
    /// <param name="dto">The transaction creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created transaction DTO.</returns>
    /// <exception cref="DomainException">Thrown when the account is not found.</exception>
    public async Task<TransactionDto> CreateAsync(TransactionCreateDto dto, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(dto.AccountId, cancellationToken);
        if (account is null)
        {
            throw new DomainException("Account not found.", DomainExceptionType.NotFound);
        }

        // Determine category: use manual category if provided, otherwise auto-categorize
        Guid? categoryId = dto.CategoryId;
        if (!categoryId.HasValue)
        {
            categoryId = await _categorizationEngine.FindMatchingCategoryAsync(dto.Description, cancellationToken);
        }

        var amount = MoneyValue.Create(dto.Amount.Currency, dto.Amount.Amount);
        var transaction = account.AddTransaction(amount, dto.Date, dto.Description, categoryId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return AccountMapper.ToDto(transaction);
    }

    /// <summary>
    /// Updates an existing transaction.
    /// </summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="dto">The transaction update data.</param>
    /// <param name="expectedVersion">The expected concurrency token for optimistic concurrency, or null to skip.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated transaction DTO, or null if not found.</returns>
    public async Task<TransactionDto?> UpdateAsync(Guid id, TransactionUpdateDto dto, string? expectedVersion = null, CancellationToken cancellationToken = default)
    {
        var transaction = await _repository.GetByIdAsync(id, cancellationToken);
        if (transaction is null)
        {
            return null;
        }

        if (expectedVersion is not null)
        {
            _unitOfWork.SetExpectedConcurrencyToken(transaction, expectedVersion);
        }

        var amount = MoneyValue.Create(dto.Amount.Currency, dto.Amount.Amount);
        transaction.UpdateAmount(amount);
        transaction.UpdateDate(dto.Date);
        transaction.UpdateDescription(dto.Description);
        transaction.UpdateCategory(dto.CategoryId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var version = _unitOfWork.GetConcurrencyToken(transaction);
        return AccountMapper.ToTransactionDto(transaction, version);
    }

    /// <summary>
    /// Deletes a transaction by its identifier.
    /// </summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the transaction was deleted; false if not found.</returns>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await _repository.GetByIdAsync(id, cancellationToken);
        if (transaction is null)
        {
            return false;
        }

        await _repository.RemoveAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Updates the location on a transaction.
    /// </summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="dto">The location update data.</param>
    /// <param name="expectedVersion">The expected concurrency token for optimistic concurrency, or null to skip.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated transaction DTO, or null if not found.</returns>
    public async Task<TransactionDto?> UpdateLocationAsync(Guid id, TransactionLocationUpdateDto dto, string? expectedVersion = null, CancellationToken cancellationToken = default)
    {
        var transaction = await _repository.GetByIdAsync(id, cancellationToken);
        if (transaction is null)
        {
            return null;
        }

        if (expectedVersion is not null)
        {
            _unitOfWork.SetExpectedConcurrencyToken(transaction, expectedVersion);
        }

        GeoCoordinateValue? coordinates = null;
        if (dto.Latitude.HasValue && dto.Longitude.HasValue)
        {
            coordinates = GeoCoordinateValue.Create(dto.Latitude.Value, dto.Longitude.Value);
        }

        var location = TransactionLocationValue.Create(
            dto.City,
            dto.StateOrRegion,
            dto.Country,
            dto.PostalCode,
            coordinates,
            LocationSource.Manual);

        transaction.SetLocation(location);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var version = _unitOfWork.GetConcurrencyToken(transaction);
        return AccountMapper.ToTransactionDto(transaction, version);
    }

    /// <summary>
    /// Updates the category on a transaction (quick category assignment).
    /// </summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="dto">The category update data.</param>
    /// <param name="expectedVersion">The expected concurrency token for optimistic concurrency, or null to skip.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated transaction DTO, or null if not found.</returns>
    public async Task<TransactionDto?> UpdateCategoryAsync(Guid id, TransactionCategoryUpdateDto dto, string? expectedVersion = null, CancellationToken cancellationToken = default)
    {
        var transaction = await _repository.GetByIdAsync(id, cancellationToken);
        if (transaction is null)
        {
            return null;
        }

        if (expectedVersion is not null)
        {
            _unitOfWork.SetExpectedConcurrencyToken(transaction, expectedVersion);
        }

        transaction.UpdateCategory(dto.CategoryId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var version = _unitOfWork.GetConcurrencyToken(transaction);
        return AccountMapper.ToTransactionDto(transaction, version);
    }

    /// <summary>
    /// Clears the location from a transaction.
    /// </summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the transaction was found and location cleared; false if not found.</returns>
    public async Task<bool> ClearLocationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await _repository.GetByIdAsync(id, cancellationToken);
        if (transaction is null)
        {
            return false;
        }

        transaction.ClearLocation();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Clears location data from all transactions (bulk privacy operation).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of transactions whose location was cleared.</returns>
    public async Task<int> ClearAllLocationDataAsync(CancellationToken cancellationToken = default)
    {
        var transactions = await _repository.GetAllWithLocationAsync(cancellationToken);
        if (transactions.Count == 0)
        {
            return 0;
        }

        foreach (var transaction in transactions)
        {
            transaction.ClearLocation();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return transactions.Count;
    }
}
