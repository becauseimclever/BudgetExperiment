// <copyright file="AccountService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Accounts;

/// <summary>
/// Application service for account use cases.
/// </summary>
public sealed class AccountService
{
    private const int DefaultTransactionLookbackDays = 90;
    private readonly IAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountService"/> class.
    /// </summary>
    /// <param name="repository">The account repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="userContext">The user context for scope and user identification.</param>
    public AccountService(IAccountRepository repository, IUnitOfWork unitOfWork, IUserContext userContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _userContext = userContext;
    }

    /// <summary>
    /// Gets an account by its identifier.
    /// </summary>
    /// <param name="id">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The account DTO, or null if not found.</returns>
    public async Task<AccountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = endDate.AddDays(-DefaultTransactionLookbackDays);
        var account = await _repository.GetByIdWithTransactionsAsync(id, startDate, endDate, cancellationToken);
        if (account is null)
        {
            return null;
        }

        var version = _unitOfWork.GetConcurrencyToken(account);
        return AccountMapper.ToDto(account, version);
    }

    /// <summary>
    /// Gets all accounts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of account DTOs.</returns>
    public async Task<IReadOnlyList<AccountDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _repository.GetAllAsync(cancellationToken);
        return accounts.Select(AccountMapper.ToDto).ToList();
    }

    /// <summary>
    /// Creates a new account.
    /// </summary>
    /// <param name="dto">The account creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created account DTO.</returns>
    /// <exception cref="DomainException">Thrown when the account type is invalid.</exception>
    public async Task<AccountDto> CreateAsync(AccountCreateDto dto, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<AccountType>(dto.Type, ignoreCase: true, out var accountType))
        {
            throw new DomainException($"Invalid account type: {dto.Type}");
        }

        if (!Enum.TryParse<BudgetScope>(dto.Scope, ignoreCase: true, out var scope))
        {
            throw new DomainException($"Invalid scope: {dto.Scope}. Valid values are 'Shared' or 'Personal'.");
        }

        var userId = _userContext.UserIdAsGuid
            ?? throw new DomainException("User is not authenticated.");

        var initialBalance = MoneyValue.Create(dto.InitialBalanceCurrency, dto.InitialBalance);
        var initialBalanceDate = dto.InitialBalanceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        Account account = scope switch
        {
            BudgetScope.Shared => Account.CreateShared(dto.Name, accountType, userId, initialBalance, initialBalanceDate),
            BudgetScope.Personal => Account.CreatePersonal(dto.Name, accountType, userId, initialBalance, initialBalanceDate),
            _ => throw new DomainException($"Invalid scope: {scope}"),
        };

        await _repository.AddAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return AccountMapper.ToDto(account);
    }

    /// <summary>
    /// Updates an existing account.
    /// </summary>
    /// <param name="id">The account identifier.</param>
    /// <param name="dto">The account update data.</param>
    /// <param name="expectedVersion">The expected concurrency token for optimistic concurrency, or null to skip.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated account DTO, or null if not found.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public async Task<AccountDto?> UpdateAsync(Guid id, AccountUpdateDto dto, string? expectedVersion = null, CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetByIdAsync(id, cancellationToken);
        if (account is null)
        {
            return null;
        }

        if (expectedVersion is not null)
        {
            _unitOfWork.SetExpectedConcurrencyToken(account, expectedVersion);
        }

        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            account.UpdateName(dto.Name);
        }

        if (!string.IsNullOrWhiteSpace(dto.Type))
        {
            if (!Enum.TryParse<AccountType>(dto.Type, ignoreCase: true, out var accountType))
            {
                throw new DomainException($"Invalid account type: {dto.Type}");
            }

            account.UpdateType(accountType);
        }

        if (dto.InitialBalance.HasValue || dto.InitialBalanceCurrency != null || dto.InitialBalanceDate.HasValue)
        {
            var currentBalance = account.InitialBalance;
            var newAmount = dto.InitialBalance ?? currentBalance.Amount;
            var newCurrency = dto.InitialBalanceCurrency ?? currentBalance.Currency;
            var newDate = dto.InitialBalanceDate ?? account.InitialBalanceDate;

            var newBalance = MoneyValue.Create(newCurrency, newAmount);
            account.UpdateInitialBalance(newBalance, newDate);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var version = _unitOfWork.GetConcurrencyToken(account);
        return AccountMapper.ToDto(account, version);
    }

    /// <summary>
    /// Removes an account by its identifier.
    /// </summary>
    /// <param name="id">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if removed, false if not found.</returns>
    public async Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetByIdAsync(id, cancellationToken);
        if (account is null)
        {
            return false;
        }

        await _repository.RemoveAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
