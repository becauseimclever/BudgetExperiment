// <copyright file="TransactionService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Application.Mapping;
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
        return transaction is null ? null : DomainToDtoMapper.ToDto(transaction);
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
        return transactions.Select(DomainToDtoMapper.ToDto).ToList();
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
        return DomainToDtoMapper.ToDto(transaction);
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
        return DomainToDtoMapper.ToDto(transaction);
    }
}

