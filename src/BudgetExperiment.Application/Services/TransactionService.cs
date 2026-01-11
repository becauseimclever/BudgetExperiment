// <copyright file="TransactionService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Application.Mapping;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Application service for transaction use cases.
/// </summary>
public sealed class TransactionService
{
    private readonly ITransactionRepository _repository;
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionService"/> class.
    /// </summary>
    /// <param name="repository">The transaction repository.</param>
    /// <param name="accountRepository">The account repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public TransactionService(ITransactionRepository repository, IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    {
        this._repository = repository;
        this._accountRepository = accountRepository;
        this._unitOfWork = unitOfWork;
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

        var amount = MoneyValue.Create(dto.Amount.Currency, dto.Amount.Amount);
        var transaction = account.AddTransaction(amount, dto.Date, dto.Description, dto.Category);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return DomainToDtoMapper.ToDto(transaction);
    }
}
