// <copyright file="Transaction.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Accounts;

/// <summary>
/// Represents a financial transaction within an account.
/// </summary>
public sealed class Transaction
{
    /// <summary>
    /// Maximum length for external reference.
    /// </summary>
    public const int MaxExternalReferenceLength = 100;

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
    public Guid Id
    {
        get; private set;
    }

    /// <summary>
    /// Gets the identifier of the account this transaction belongs to.
    /// </summary>
    public Guid AccountId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the monetary amount of the transaction.
    /// </summary>
    public MoneyValue Amount { get; private set; } = null!;

    /// <summary>
    /// Gets the date of the transaction.
    /// </summary>
    public DateOnly Date
    {
        get; private set;
    }

    /// <summary>
    /// Gets the description of the transaction.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the optional category identifier linking to a BudgetCategory.
    /// </summary>
    public Guid? CategoryId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the associated budget category (navigation property for queries).
    /// </summary>
    public BudgetCategory? Category
    {
        get; private set;
    }

    /// <summary>
    /// Gets the UTC timestamp when the transaction was created.
    /// </summary>
    public DateTime CreatedAtUtc
    {
        get; private set;
    }

    /// <summary>
    /// Gets the UTC timestamp when the transaction was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc
    {
        get; private set;
    }

    /// <summary>
    /// Gets the identifier of the recurring transaction this was generated from (null for manual transactions).
    /// </summary>
    public Guid? RecurringTransactionId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the scheduled date this transaction was generated for from the recurring transaction.
    /// </summary>
    public DateOnly? RecurringInstanceDate
    {
        get; private set;
    }

    /// <summary>
    /// Gets a value indicating whether this transaction was generated from a recurring transaction.
    /// </summary>
    public bool IsFromRecurringTransaction => this.RecurringTransactionId.HasValue;

    /// <summary>
    /// Gets the identifier linking paired transfer transactions (null for non-transfer transactions).
    /// </summary>
    public Guid? TransferId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the direction of this transaction in a transfer (null for non-transfer transactions).
    /// </summary>
    public TransferDirection? TransferDirection
    {
        get; private set;
    }

    /// <summary>
    /// Gets a value indicating whether this transaction is part of a transfer between accounts.
    /// </summary>
    public bool IsTransfer => this.TransferId.HasValue;

    /// <summary>
    /// Gets the identifier of the recurring transfer this was generated from (null for non-recurring transfers).
    /// </summary>
    public Guid? RecurringTransferId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the scheduled date this transaction was generated for from the recurring transfer.
    /// </summary>
    public DateOnly? RecurringTransferInstanceDate
    {
        get; private set;
    }

    /// <summary>
    /// Gets a value indicating whether this transaction was generated from a recurring transfer.
    /// </summary>
    public bool IsFromRecurringTransfer => this.RecurringTransferId.HasValue;

    /// <summary>
    /// Gets the budget scope (Shared or Personal).
    /// </summary>
    public BudgetScope Scope
    {
        get; private set;
    }

    /// <summary>
    /// Gets the owner user ID. NULL for Shared scope, user ID for Personal scope.
    /// </summary>
    public Guid? OwnerUserId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the user ID of who created this transaction.
    /// </summary>
    public Guid CreatedByUserId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the identifier of the import batch this transaction was created from (null for manual transactions).
    /// </summary>
    public Guid? ImportBatchId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the external reference/ID from the imported CSV (null if not from import or not mapped).
    /// </summary>
    public string? ExternalReference
    {
        get; private set;
    }

    /// <summary>
    /// Gets a value indicating whether this transaction was created via CSV import.
    /// </summary>
    public bool IsFromImport => this.ImportBatchId.HasValue;

    /// <summary>
    /// Gets a value indicating whether this transaction has been cleared by the account holder.
    /// </summary>
    public bool IsCleared
    {
        get; private set;
    }

    /// <summary>
    /// Gets the date on which this transaction was marked as cleared (null if not cleared).
    /// </summary>
    public DateOnly? ClearedDate
    {
        get; private set;
    }

    /// <summary>
    /// Gets the identifier of the reconciliation record this transaction is locked to (null if not reconciled).
    /// </summary>
    public Guid? ReconciliationRecordId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the geographic location of the transaction (null if not set).
    /// </summary>
    public TransactionLocationValue? Location
    {
        get; private set;
    }

    /// <summary>
    /// Gets the per-transaction Kakeibo override.
    /// If null, the effective Kakeibo category is derived from <see cref="Category"/>.<see cref="BudgetCategory.KakeiboCategory"/>.
    /// </summary>
    public KakeiboCategory? KakeiboOverride
    {
        get; private set;
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
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets or clears the per-transaction Kakeibo override.
    /// </summary>
    /// <param name="kakeiboOverride">The override, or null to clear and defer to category routing.</param>
    public void SetKakeiboOverride(KakeiboCategory? kakeiboOverride)
    {
        this.KakeiboOverride = kakeiboOverride;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns the effective Kakeibo category for this transaction.
    /// Uses <see cref="KakeiboOverride"/> if set; otherwise falls back to the category's routing.
    /// Returns <see cref="KakeiboCategory.Wants"/> as the ultimate fallback for uncategorized transactions.
    /// </summary>
    /// <returns>The effective <see cref="KakeiboCategory"/> for this transaction.</returns>
    public KakeiboCategory GetEffectiveKakeiboCategory()
        => KakeiboOverride ?? Category?.KakeiboCategory ?? KakeiboCategory.Wants;

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
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the date.
    /// </summary>
    /// <param name="date">New date.</param>
    public void UpdateDate(DateOnly date)
    {
        this.Date = date;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the category.
    /// </summary>
    /// <param name="categoryId">New category identifier (null to clear).</param>
    public void UpdateCategory(Guid? categoryId)
    {
        this.CategoryId = categoryId;
        this.UpdatedAtUtc = DateTime.UtcNow;
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
        this.UpdatedAtUtc = DateTime.UtcNow;
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
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets or replaces the location of this transaction.
    /// </summary>
    /// <param name="location">The location to assign (may be null to clear).</param>
    public void SetLocation(TransactionLocationValue? location)
    {
        this.Location = location;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Clears the location of this transaction.
    /// </summary>
    public void ClearLocation()
    {
        this.Location = null;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this transaction as cleared on the given date.
    /// </summary>
    /// <param name="clearedDate">The date the transaction was cleared.</param>
    public void MarkCleared(DateOnly clearedDate)
    {
        IsCleared = true;
        ClearedDate = clearedDate;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Unclears this transaction, removing the cleared date.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the transaction is locked to a reconciliation record.</exception>
    public void MarkUncleared()
    {
        if (ReconciliationRecordId is not null)
        {
            throw new DomainException(
                "Cannot unclear a reconciled transaction. Unlock it first.",
                DomainExceptionType.InvalidOperation);
        }

        IsCleared = false;
        ClearedDate = null;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Locks this transaction to a completed reconciliation record.
    /// </summary>
    /// <param name="reconciliationRecordId">The reconciliation record identifier.</param>
    /// <exception cref="DomainException">Thrown when the transaction is not cleared.</exception>
    public void LockToReconciliation(Guid reconciliationRecordId)
    {
        if (!IsCleared)
        {
            throw new DomainException(
                "Cannot reconcile an uncleared transaction.",
                DomainExceptionType.InvalidOperation);
        }

        ReconciliationRecordId = reconciliationRecordId;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Unlocks this transaction from its reconciliation record.
    /// </summary>
    public void UnlockFromReconciliation()
    {
        ReconciliationRecordId = null;
        this.UpdatedAtUtc = DateTime.UtcNow;
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
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a raw transaction with core fields. For use by <see cref="TransactionFactory"/> only.
    /// </summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="amount">The monetary amount.</param>
    /// <param name="date">The transaction date.</param>
    /// <param name="description">The trimmed description.</param>
    /// <param name="categoryId">Optional category identifier.</param>
    /// <param name="now">The UTC timestamp for both created/updated fields.</param>
    /// <returns>A new <see cref="Transaction"/> instance with core fields set.</returns>
    internal static Transaction CreateRaw(
        Guid id,
        Guid accountId,
        MoneyValue amount,
        DateOnly date,
        string description,
        Guid? categoryId,
        DateTime now)
    {
        return new Transaction
        {
            Id = id,
            AccountId = accountId,
            Amount = amount,
            Date = date,
            Description = description,
            CategoryId = categoryId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
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
    /// Links this transaction to a recurring transaction at creation time. For use by <see cref="TransactionFactory"/> only.
    /// </summary>
    /// <param name="recurringTransactionId">The recurring transaction identifier.</param>
    /// <param name="instanceDate">The scheduled instance date.</param>
    internal void SetRecurringLink(Guid recurringTransactionId, DateOnly instanceDate)
    {
        this.RecurringTransactionId = recurringTransactionId;
        this.RecurringInstanceDate = instanceDate;
    }

    /// <summary>
    /// Links this transaction to a transfer at creation time. For use by <see cref="TransactionFactory"/> only.
    /// </summary>
    /// <param name="transferId">The transfer identifier.</param>
    /// <param name="direction">The transfer direction.</param>
    internal void SetTransferLink(Guid transferId, TransferDirection direction)
    {
        this.TransferId = transferId;
        this.TransferDirection = direction;
    }

    /// <summary>
    /// Links this transaction to a recurring transfer at creation time. For use by <see cref="TransactionFactory"/> only.
    /// </summary>
    /// <param name="recurringTransferId">The recurring transfer identifier.</param>
    /// <param name="instanceDate">The scheduled instance date.</param>
    internal void SetRecurringTransferLink(Guid recurringTransferId, DateOnly instanceDate)
    {
        this.RecurringTransferId = recurringTransferId;
        this.RecurringTransferInstanceDate = instanceDate;
    }
}
