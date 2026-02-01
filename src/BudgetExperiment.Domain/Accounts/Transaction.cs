// <copyright file="Transaction.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Accounts;

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
    /// Gets the optional category identifier linking to a BudgetCategory.
    /// </summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>
    /// Gets the associated budget category (navigation property for queries).
    /// </summary>
    public BudgetCategory? Category { get; private set; }

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
    /// Gets the identifier linking paired transfer transactions (null for non-transfer transactions).
    /// </summary>
    public Guid? TransferId { get; private set; }

    /// <summary>
    /// Gets the direction of this transaction in a transfer (null for non-transfer transactions).
    /// </summary>
    public TransferDirection? TransferDirection { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this transaction is part of a transfer between accounts.
    /// </summary>
    public bool IsTransfer => this.TransferId.HasValue;

    /// <summary>
    /// Gets the identifier of the recurring transfer this was generated from (null for non-recurring transfers).
    /// </summary>
    public Guid? RecurringTransferId { get; private set; }

    /// <summary>
    /// Gets the scheduled date this transaction was generated for from the recurring transfer.
    /// </summary>
    public DateOnly? RecurringTransferInstanceDate { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this transaction was generated from a recurring transfer.
    /// </summary>
    public bool IsFromRecurringTransfer => this.RecurringTransferId.HasValue;

    /// <summary>
    /// Gets the budget scope (Shared or Personal).
    /// </summary>
    public BudgetScope Scope { get; private set; }

    /// <summary>
    /// Gets the owner user ID. NULL for Shared scope, user ID for Personal scope.
    /// </summary>
    public Guid? OwnerUserId { get; private set; }

    /// <summary>
    /// Gets the user ID of who created this transaction.
    /// </summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>
    /// Gets the identifier of the import batch this transaction was created from (null for manual transactions).
    /// </summary>
    public Guid? ImportBatchId { get; private set; }

    /// <summary>
    /// Gets the external reference/ID from the imported CSV (null if not from import or not mapped).
    /// </summary>
    public string? ExternalReference { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this transaction was created via CSV import.
    /// </summary>
    public bool IsFromImport => this.ImportBatchId.HasValue;

    /// <summary>
    /// Maximum length for external reference.
    /// </summary>
    public const int MaxExternalReferenceLength = 100;

    /// <summary>
    /// Creates a new transaction.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="amount">The monetary amount.</param>
    /// <param name="date">The transaction date.</param>
    /// <param name="description">The transaction description.</param>
    /// <param name="categoryId">Optional category identifier.</param>
    /// <returns>A new <see cref="Transaction"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static Transaction Create(
        Guid accountId,
        MoneyValue amount,
        DateOnly date,
        string description,
        Guid? categoryId = null)
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
            CategoryId = categoryId,
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
    /// <param name="categoryId">Optional category identifier.</param>
    /// <returns>A new <see cref="Transaction"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static Transaction CreateFromRecurring(
        Guid accountId,
        MoneyValue amount,
        DateOnly date,
        string description,
        Guid recurringTransactionId,
        DateOnly recurringInstanceDate,
        Guid? categoryId = null)
    {
        if (recurringTransactionId == Guid.Empty)
        {
            throw new DomainException("Recurring transaction ID is required.");
        }

        var transaction = Create(accountId, amount, date, description, categoryId);
        transaction.RecurringTransactionId = recurringTransactionId;
        transaction.RecurringInstanceDate = recurringInstanceDate;
        return transaction;
    }

    /// <summary>
    /// Creates a new transaction as part of an account transfer.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="amount">The monetary amount (negative for source, positive for destination).</param>
    /// <param name="date">The transaction date.</param>
    /// <param name="description">The transaction description.</param>
    /// <param name="transferId">The identifier linking paired transfer transactions.</param>
    /// <param name="direction">The direction of this transaction in the transfer.</param>
    /// <param name="categoryId">Optional category identifier.</param>
    /// <returns>A new <see cref="Transaction"/> instance linked to a transfer.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static Transaction CreateTransfer(
        Guid accountId,
        MoneyValue amount,
        DateOnly date,
        string description,
        Guid transferId,
        TransferDirection direction,
        Guid? categoryId = null)
    {
        if (transferId == Guid.Empty)
        {
            throw new DomainException("Transfer ID is required.");
        }

        var transaction = Create(accountId, amount, date, description, categoryId);
        transaction.TransferId = transferId;
        transaction.TransferDirection = direction;
        return transaction;
    }

    /// <summary>
    /// Creates a new transaction as part of a recurring transfer.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="amount">The monetary amount (negative for source, positive for destination).</param>
    /// <param name="date">The transaction date.</param>
    /// <param name="description">The transaction description.</param>
    /// <param name="transferId">The identifier linking paired transfer transactions.</param>
    /// <param name="direction">The direction of this transaction in the transfer.</param>
    /// <param name="recurringTransferId">The recurring transfer identifier.</param>
    /// <param name="recurringTransferInstanceDate">The scheduled date this was generated for.</param>
    /// <param name="categoryId">Optional category identifier.</param>
    /// <returns>A new <see cref="Transaction"/> instance linked to a recurring transfer.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static Transaction CreateFromRecurringTransfer(
        Guid accountId,
        MoneyValue amount,
        DateOnly date,
        string description,
        Guid transferId,
        TransferDirection direction,
        Guid recurringTransferId,
        DateOnly recurringTransferInstanceDate,
        Guid? categoryId = null)
    {
        if (recurringTransferId == Guid.Empty)
        {
            throw new DomainException("Recurring transfer ID is required.");
        }

        var transaction = CreateTransfer(accountId, amount, date, description, transferId, direction, categoryId);
        transaction.RecurringTransferId = recurringTransferId;
        transaction.RecurringTransferInstanceDate = recurringTransferInstanceDate;
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
    /// <param name="categoryId">New category identifier (null to clear).</param>
    public void UpdateCategory(Guid? categoryId)
    {
        this.CategoryId = categoryId;
        this.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the scope from the parent account.
    /// </summary>
    /// <param name="scope">The budget scope.</param>
    /// <param name="ownerUserId">The owner user ID (null for shared).</param>
    /// <param name="createdByUserId">The user ID who created this transaction.</param>
    internal void SetScope(BudgetScope scope, Guid? ownerUserId, Guid createdByUserId)
    {
        this.Scope = scope;
        this.OwnerUserId = ownerUserId;
        this.CreatedByUserId = createdByUserId;
    }

    /// <summary>
    /// Associates this transaction with an import batch.
    /// </summary>
    /// <param name="batchId">The import batch identifier.</param>
    /// <param name="externalReference">Optional external reference from the CSV.</param>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public void SetImportBatch(Guid batchId, string? externalReference = null)
    {
        if (batchId == Guid.Empty)
        {
            throw new DomainException("Import batch ID is required.");
        }

        if (!string.IsNullOrWhiteSpace(externalReference))
        {
            var trimmedRef = externalReference.Trim();
            if (trimmedRef.Length > MaxExternalReferenceLength)
            {
                throw new DomainException($"External reference cannot exceed {MaxExternalReferenceLength} characters.");
            }

            this.ExternalReference = trimmedRef;
        }

        this.ImportBatchId = batchId;
        this.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Links this transaction to a recurring transaction instance during reconciliation.
    /// </summary>
    /// <param name="recurringTransactionId">The recurring transaction identifier.</param>
    /// <param name="instanceDate">The scheduled date this transaction corresponds to.</param>
    /// <exception cref="DomainException">Thrown when transaction is already linked or validation fails.</exception>
    public void LinkToRecurringInstance(Guid recurringTransactionId, DateOnly instanceDate)
    {
        if (recurringTransactionId == Guid.Empty)
        {
            throw new DomainException("Recurring transaction ID is required.");
        }

        if (this.RecurringTransactionId.HasValue)
        {
            throw new DomainException("Transaction is already linked to a recurring transaction.");
        }

        this.RecurringTransactionId = recurringTransactionId;
        this.RecurringInstanceDate = instanceDate;
        this.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unlinks this transaction from its recurring transaction instance.
    /// </summary>
    /// <exception cref="DomainException">Thrown when transaction is not linked.</exception>
    public void UnlinkFromRecurring()
    {
        if (!this.RecurringTransactionId.HasValue)
        {
            throw new DomainException("Transaction is not linked to a recurring transaction.");
        }

        this.RecurringTransactionId = null;
        this.RecurringInstanceDate = null;
        this.UpdatedAt = DateTime.UtcNow;
    }
}
