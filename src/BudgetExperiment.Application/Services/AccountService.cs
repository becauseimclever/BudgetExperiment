// <copyright file="AccountService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Application.Mapping;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Application service for account use cases.
/// </summary>
public sealed class AccountService
{
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
        this._repository = repository;
        this._unitOfWork = unitOfWork;
        this._userContext = userContext;
    }

    /// <summary>
    /// Gets an account by its identifier.
    /// </summary>
    /// <param name="id">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The account DTO, or null if not found.</returns>
    public async Task<AccountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await this._repository.GetByIdWithTransactionsAsync(id, cancellationToken);
        return account is null ? null : DomainToDtoMapper.ToDto(account);
    }

    /// <summary>
    /// Gets all accounts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of account DTOs.</returns>
    public async Task<IReadOnlyList<AccountDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await this._repository.GetAllAsync(cancellationToken);
        return accounts.Select(DomainToDtoMapper.ToDto).ToList();
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

        var userId = this._userContext.UserIdAsGuid
            ?? throw new DomainException("User is not authenticated.");

        var initialBalance = MoneyValue.Create(dto.InitialBalanceCurrency, dto.InitialBalance);
        var initialBalanceDate = dto.InitialBalanceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        Account account = scope switch
        {
            BudgetScope.Shared => Account.CreateShared(dto.Name, accountType, userId, initialBalance, initialBalanceDate),
            BudgetScope.Personal => Account.CreatePersonal(dto.Name, accountType, userId, initialBalance, initialBalanceDate),
            _ => throw new DomainException($"Invalid scope: {scope}"),
        };

        await this._repository.AddAsync(account, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return DomainToDtoMapper.ToDto(account);
    }

    /// <summary>
    /// Updates an existing account.
    /// </summary>
    /// <param name="id">The account identifier.</param>
    /// <param name="dto">The account update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated account DTO, or null if not found.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public async Task<AccountDto?> UpdateAsync(Guid id, AccountUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var account = await this._repository.GetByIdAsync(id, cancellationToken);
        if (account is null)
        {
            return null;
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

        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return DomainToDtoMapper.ToDto(account);
    }

    /// <summary>
    /// Removes an account by its identifier.
    /// </summary>
    /// <param name="id">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if removed, false if not found.</returns>
    public async Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await this._repository.GetByIdAsync(id, cancellationToken);
        if (account is null)
        {
            return false;
        }

        await this._repository.RemoveAsync(account, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
