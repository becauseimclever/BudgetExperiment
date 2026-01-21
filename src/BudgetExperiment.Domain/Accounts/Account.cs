// <copyright file="Account.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Accounts;

/// <summary>
/// Represents a financial account (aggregate root).
/// </summary>
public sealed class Account
{
    private readonly List<Transaction> _transactions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Account"/> class.
    /// </summary>
    /// <remarks>
    /// Private constructor for EF Core and factory method.
    /// </remarks>
    private Account()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the account name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the account type.
    /// </summary>
    public AccountType Type { get; private set; }

    /// <summary>
    /// Gets the initial balance for this account.
    /// </summary>
    public MoneyValue InitialBalance { get; private set; } = MoneyValue.Zero("USD");

    /// <summary>
    /// Gets the date as of which the initial balance was recorded.
    /// </summary>
    public DateOnly InitialBalanceDate { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the account was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the account was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Gets the budget scope (Shared or Personal).
    /// </summary>
    public BudgetScope Scope { get; private set; }

    /// <summary>
    /// Gets the owner user ID. NULL for Shared scope, user ID for Personal scope.
    /// </summary>
    public Guid? OwnerUserId { get; private set; }

    /// <summary>
    /// Gets the user ID of who created this account.
    /// </summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>
    /// Gets the transactions in this account.
    /// </summary>
    public IReadOnlyCollection<Transaction> Transactions => this._transactions.AsReadOnly();

    /// <summary>
    /// Creates a new account.
    /// </summary>
    /// <param name="name">The account name.</param>
    /// <param name="type">The account type.</param>
    /// <param name="initialBalance">Optional initial balance (defaults to zero USD).</param>
    /// <param name="initialBalanceDate">Optional date for initial balance (defaults to today).</param>
    /// <returns>A new <see cref="Account"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static Account Create(
        string name,
        AccountType type,
        MoneyValue? initialBalance = null,
        DateOnly? initialBalanceDate = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Account name is required.");
        }

        var now = DateTime.UtcNow;
        return new Account
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Type = type,
            InitialBalance = initialBalance ?? MoneyValue.Zero("USD"),
            InitialBalanceDate = initialBalanceDate ?? DateOnly.FromDateTime(now),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>
    /// Creates a new shared account (visible to all authenticated users).
    /// </summary>
    /// <param name="name">The account name.</param>
    /// <param name="type">The account type.</param>
    /// <param name="createdByUserId">The user ID of who is creating this account.</param>
    /// <param name="initialBalance">Optional initial balance (defaults to zero USD).</param>
    /// <param name="initialBalanceDate">Optional date for initial balance (defaults to today).</param>
    /// <returns>A new <see cref="Account"/> instance with Shared scope.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static Account CreateShared(
        string name,
        AccountType type,
        Guid createdByUserId,
        MoneyValue? initialBalance = null,
        DateOnly? initialBalanceDate = null)
    {
        if (createdByUserId == Guid.Empty)
        {
            throw new DomainException("Created by user ID is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Account name is required.");
        }

        var now = DateTime.UtcNow;
        return new Account
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Type = type,
            InitialBalance = initialBalance ?? MoneyValue.Zero("USD"),
            InitialBalanceDate = initialBalanceDate ?? DateOnly.FromDateTime(now),
            Scope = BudgetScope.Shared,
            OwnerUserId = null,
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>
    /// Creates a new personal account (visible only to the owner).
    /// </summary>
    /// <param name="name">The account name.</param>
    /// <param name="type">The account type.</param>
    /// <param name="ownerUserId">The user ID who owns this personal account.</param>
    /// <param name="initialBalance">Optional initial balance (defaults to zero USD).</param>
    /// <param name="initialBalanceDate">Optional date for initial balance (defaults to today).</param>
    /// <returns>A new <see cref="Account"/> instance with Personal scope.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static Account CreatePersonal(
        string name,
        AccountType type,
        Guid ownerUserId,
        MoneyValue? initialBalance = null,
        DateOnly? initialBalanceDate = null)
    {
        if (ownerUserId == Guid.Empty)
        {
            throw new DomainException("Owner user ID is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Account name is required.");
        }

        var now = DateTime.UtcNow;
        return new Account
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Type = type,
            InitialBalance = initialBalance ?? MoneyValue.Zero("USD"),
            InitialBalanceDate = initialBalanceDate ?? DateOnly.FromDateTime(now),
            Scope = BudgetScope.Personal,
            OwnerUserId = ownerUserId,
            CreatedByUserId = ownerUserId,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>
    /// Updates the account name.
    /// </summary>
    /// <param name="name">New name.</param>
    /// <exception cref="DomainException">Thrown when name is empty.</exception>
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Account name is required.");
        }

        this.Name = name.Trim();
        this.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the account type.
    /// </summary>
    /// <param name="type">New type.</param>
    public void UpdateType(AccountType type)
    {
        this.Type = type;
        this.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the initial balance and its effective date.
    /// </summary>
    /// <param name="balance">The new initial balance.</param>
    /// <param name="asOfDate">The date as of which the balance is recorded.</param>
    /// <exception cref="DomainException">Thrown when balance is null.</exception>
    public void UpdateInitialBalance(MoneyValue balance, DateOnly asOfDate)
    {
        if (balance is null)
        {
            throw new DomainException("Initial balance is required.");
        }

        this.InitialBalance = balance;
        this.InitialBalanceDate = asOfDate;
        this.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a new transaction to this account.
    /// </summary>
    /// <param name="amount">The monetary amount.</param>
    /// <param name="date">The transaction date.</param>
    /// <param name="description">The transaction description.</param>
    /// <param name="categoryId">Optional category identifier.</param>
    /// <returns>The created transaction.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public Transaction AddTransaction(
        MoneyValue amount,
        DateOnly date,
        string description,
        Guid? categoryId = null)
    {
        if (amount is null)
        {
            throw new DomainException("Amount is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Description is required.");
        }

        var transaction = Transaction.Create(this.Id, amount, date, description, categoryId);
        transaction.SetScope(this.Scope, this.OwnerUserId, this.CreatedByUserId);
        this._transactions.Add(transaction);
        this.UpdatedAt = DateTime.UtcNow;
        return transaction;
    }

    /// <summary>
    /// Removes a transaction from this account.
    /// </summary>
    /// <param name="transactionId">The transaction ID to remove.</param>
    /// <returns>True if removed; false if not found.</returns>
    public bool RemoveTransaction(Guid transactionId)
    {
        var transaction = this._transactions.FirstOrDefault(t => t.Id == transactionId);
        if (transaction is null)
        {
            return false;
        }

        this._transactions.Remove(transaction);
        this.UpdatedAt = DateTime.UtcNow;
        return true;
    }
}
