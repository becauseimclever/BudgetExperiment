// <copyright file="TransactionFactory.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Accounts;

/// <summary>
/// Factory for creating <see cref="Transaction"/> instances across all creation scenarios.
/// </summary>
/// <remarks>
/// All guard clauses and construction logic that was previously on the entity live here.
/// <see cref="Transaction"/> retains state management, invariants, and update methods only.
/// </remarks>
public static class TransactionFactory
{
    /// <summary>
    /// Creates a new standard transaction.
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
        return Transaction.CreateRaw(
            Guid.NewGuid(),
            accountId,
            amount,
            date,
            description.Trim(),
            categoryId,
            now);
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
        transaction.SetRecurringLink(recurringTransactionId, recurringInstanceDate);
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
        transaction.SetTransferLink(transferId, direction);
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
        transaction.SetRecurringTransferLink(recurringTransferId, recurringTransferInstanceDate);
        return transaction;
    }
}
