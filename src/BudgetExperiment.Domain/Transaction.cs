// <copyright file="Transaction.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Represents a financial transaction within an account.
/// </summary>
public sealed class Transaction
{
    private readonly List<object> _domainEvents = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Transaction"/> class.
    /// </summary>
    /// <remarks>
    /// Private constructor for EF Core and factory method.
    /// </remarks>
    private Transaction()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the identifier of the account this transaction belongs to.
    /// </summary>
    public Guid AccountId { get; private set; }

    /// <summary>
    /// Gets the monetary amount of the transaction.
    /// </summary>
    public MoneyValue Amount { get; private set; } = null!;

    /// <summary>
    /// Gets the date of the transaction.
    /// </summary>
    public DateOnly Date { get; private set; }

    /// <summary>
    /// Gets the description of the transaction.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the optional category of the transaction.
    /// </summary>
    public string? Category { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the transaction was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the transaction was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Gets the identifier of the recurring transaction this was generated from (null for manual transactions).
    /// </summary>
    public Guid? RecurringTransactionId { get; private set; }

    /// <summary>
    /// Gets the scheduled date this transaction was generated for from the recurring transaction.
    /// </summary>
    public DateOnly? RecurringInstanceDate { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this transaction was generated from a recurring transaction.
    /// </summary>
    public bool IsFromRecurringTransaction => this.RecurringTransactionId.HasValue;

    /// <summary>
    /// Creates a new transaction.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="amount">The monetary amount.</param>
    /// <param name="date">The transaction date.</param>
    /// <param name="description">The transaction description.</param>
    /// <param name="category">Optional category.</param>
    /// <returns>A new <see cref="Transaction"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static Transaction Create(
        Guid accountId,
        MoneyValue amount,
        DateOnly date,
        string description,
        string? category = null)
    {
        if (accountId == Guid.Empty)
        {
            throw new DomainException("Account ID is required.");
        }

        if (amount is null)
        {
            throw new DomainException("Amount is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Description is required.");
        }

        var now = DateTime.UtcNow;
        return new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Amount = amount,
            Date = date,
            Description = description.Trim(),
            Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            RecurringTransactionId = null,
            RecurringInstanceDate = null,
        };
    }

    /// <summary>
    /// Creates a new transaction generated from a recurring transaction.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="amount">The monetary amount.</param>
    /// <param name="date">The transaction date.</param>
    /// <param name="description">The transaction description.</param>
    /// <param name="recurringTransactionId">The recurring transaction identifier.</param>
    /// <param name="recurringInstanceDate">The scheduled date this was generated for.</param>
    /// <param name="category">Optional category.</param>
    /// <returns>A new <see cref="Transaction"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static Transaction CreateFromRecurring(
        Guid accountId,
        MoneyValue amount,
        DateOnly date,
        string description,
        Guid recurringTransactionId,
        DateOnly recurringInstanceDate,
        string? category = null)
    {
        if (recurringTransactionId == Guid.Empty)
        {
            throw new DomainException("Recurring transaction ID is required.");
        }

        var transaction = Create(accountId, amount, date, description, category);
        transaction.RecurringTransactionId = recurringTransactionId;
        transaction.RecurringInstanceDate = recurringInstanceDate;
        return transaction;
    }

    /// <summary>
    /// Updates the description.
    /// </summary>
    /// <param name="description">New description.</param>
    /// <exception cref="DomainException">Thrown when description is empty.</exception>
    public void UpdateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Description is required.");
        }

        this.Description = description.Trim();
        this.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the amount.
    /// </summary>
    /// <param name="amount">New amount.</param>
    /// <exception cref="DomainException">Thrown when amount is null.</exception>
    public void UpdateAmount(MoneyValue amount)
    {
        if (amount is null)
        {
            throw new DomainException("Amount is required.");
        }

        this.Amount = amount;
        this.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the date.
    /// </summary>
    /// <param name="date">New date.</param>
    public void UpdateDate(DateOnly date)
    {
        this.Date = date;
        this.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the category.
    /// </summary>
    /// <param name="category">New category (null to clear).</param>
    public void UpdateCategory(string? category)
    {
        this.Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        this.UpdatedAt = DateTime.UtcNow;
    }
}
