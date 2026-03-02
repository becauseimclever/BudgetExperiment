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
        this._repository = repository;
        this._accountRepository = accountRepository;
        this._unitOfWork = unitOfWork;
        this._categorizationEngine = categorizationEngine;
    }

    /// <summary>
    /// Gets a transaction by its identifier.
    /// </summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transaction DTO, or null if not found.</returns>
    public async Task<TransactionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await this._repository.GetByIdAsync(id, cancellationToken);
        return transaction is null ? null : AccountMapper.ToDto(transaction);
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
        var transactions = await this._repository.GetByDateRangeAsync(startDate, endDate, accountId, cancellationToken);
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
        var account = await this._accountRepository.GetByIdAsync(dto.AccountId, cancellationToken);
        if (account is null)
        {
            throw new DomainException("Account not found.");
        }

        // Determine category: use manual category if provided, otherwise auto-categorize
        Guid? categoryId = dto.CategoryId;
        if (!categoryId.HasValue)
        {
            categoryId = await this._categorizationEngine.FindMatchingCategoryAsync(dto.Description, cancellationToken);
        }

        var amount = MoneyValue.Create(dto.Amount.Currency, dto.Amount.Amount);
        var transaction = account.AddTransaction(amount, dto.Date, dto.Description, categoryId);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return AccountMapper.ToDto(transaction);
    }

    /// <summary>
    /// Updates an existing transaction.
    /// </summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="dto">The transaction update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated transaction DTO, or null if not found.</returns>
    public async Task<TransactionDto?> UpdateAsync(Guid id, TransactionUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var transaction = await this._repository.GetByIdAsync(id, cancellationToken);
        if (transaction is null)
        {
            return null;
        }

        var amount = MoneyValue.Create(dto.Amount.Currency, dto.Amount.Amount);
        transaction.UpdateAmount(amount);
        transaction.UpdateDate(dto.Date);
        transaction.UpdateDescription(dto.Description);
        transaction.UpdateCategory(dto.CategoryId);

        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return AccountMapper.ToDto(transaction);
    }

    /// <summary>
    /// Deletes a transaction by its identifier.
    /// </summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the transaction was deleted; false if not found.</returns>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await this._repository.GetByIdAsync(id, cancellationToken);
        if (transaction is null)
        {
            return false;
        }

        await this._repository.RemoveAsync(transaction, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Updates the location on a transaction.
    /// </summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="dto">The location update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated transaction DTO, or null if not found.</returns>
    public async Task<TransactionDto?> UpdateLocationAsync(Guid id, TransactionLocationUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var transaction = await this._repository.GetByIdAsync(id, cancellationToken);
        if (transaction is null)
        {
            return null;
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
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return AccountMapper.ToDto(transaction);
    }

    /// <summary>
    /// Clears the location from a transaction.
    /// </summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the transaction was found and location cleared; false if not found.</returns>
    public async Task<bool> ClearLocationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await this._repository.GetByIdAsync(id, cancellationToken);
        if (transaction is null)
        {
            return false;
        }

        transaction.ClearLocation();
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Clears location data from all transactions (bulk privacy operation).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of transactions whose location was cleared.</returns>
    public async Task<int> ClearAllLocationDataAsync(CancellationToken cancellationToken = default)
    {
        var transactions = await this._repository.GetAllWithLocationAsync(cancellationToken);
        if (transactions.Count == 0)
        {
            return 0;
        }

        foreach (var transaction in transactions)
        {
            transaction.ClearLocation();
        }

        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return transactions.Count;
    }
}

