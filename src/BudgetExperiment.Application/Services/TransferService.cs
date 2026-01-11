// <copyright file="TransferService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Application service for account transfer operations.
/// </summary>
public sealed class TransferService : ITransferService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransferService"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="accountRepository">The account repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public TransferService(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork)
    {
        this._transactionRepository = transactionRepository;
        this._accountRepository = accountRepository;
        this._unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<TransferResponse> CreateAsync(CreateTransferRequest request, CancellationToken cancellationToken = default)
    {
        // Validate source and destination are different
        if (request.SourceAccountId == request.DestinationAccountId)
        {
            throw new DomainException("Source and destination accounts must be different.");
        }

        // Validate amount is positive
        if (request.Amount <= 0)
        {
            throw new DomainException("Transfer amount must be positive.");
        }

        // Load accounts
        var sourceAccount = await this._accountRepository.GetByIdAsync(request.SourceAccountId, cancellationToken)
            ?? throw new DomainException("Source account not found.");

        var destinationAccount = await this._accountRepository.GetByIdAsync(request.DestinationAccountId, cancellationToken)
            ?? throw new DomainException("Destination account not found.");

        // Generate the transfer ID
        var transferId = Guid.NewGuid();
        var description = string.IsNullOrWhiteSpace(request.Description)
            ? "Transfer"
            : request.Description.Trim();

        // Create source transaction (money leaving - negative)
        var sourceTransaction = Transaction.CreateTransfer(
            request.SourceAccountId,
            MoneyValue.Create(request.Currency, -request.Amount),
            request.Date,
            $"Transfer to {destinationAccount.Name}: {description}",
            transferId,
            TransferDirection.Source);

        // Create destination transaction (money entering - positive)
        var destinationTransaction = Transaction.CreateTransfer(
            request.DestinationAccountId,
            MoneyValue.Create(request.Currency, request.Amount),
            request.Date,
            $"Transfer from {sourceAccount.Name}: {description}",
            transferId,
            TransferDirection.Destination);

        // Add both transactions atomically
        await this._transactionRepository.AddAsync(sourceTransaction, cancellationToken);
        await this._transactionRepository.AddAsync(destinationTransaction, cancellationToken);
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(
            transferId,
            sourceTransaction,
            destinationTransaction,
            sourceAccount.Name,
            destinationAccount.Name);
    }

    /// <inheritdoc />
    public async Task<TransferResponse?> GetByIdAsync(Guid transferId, CancellationToken cancellationToken = default)
    {
        var transactions = await this._transactionRepository.GetByTransferIdAsync(transferId, cancellationToken);

        if (transactions.Count != 2)
        {
            return null;
        }

        var sourceTransaction = transactions.FirstOrDefault(t => t.TransferDirection == TransferDirection.Source);
        var destinationTransaction = transactions.FirstOrDefault(t => t.TransferDirection == TransferDirection.Destination);

        if (sourceTransaction is null || destinationTransaction is null)
        {
            return null;
        }

        // Load account names
        var sourceAccount = await this._accountRepository.GetByIdAsync(sourceTransaction.AccountId, cancellationToken);
        var destinationAccount = await this._accountRepository.GetByIdAsync(destinationTransaction.AccountId, cancellationToken);

        return MapToResponse(
            transferId,
            sourceTransaction,
            destinationTransaction,
            sourceAccount?.Name ?? "Unknown",
            destinationAccount?.Name ?? "Unknown");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TransferListItemResponse>> ListAsync(
        Guid? accountId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Get all transactions that have a TransferId
        // We'll need a date range to scope the query
        var startDate = fromDate ?? DateOnly.MinValue;
        var endDate = toDate ?? DateOnly.MaxValue;

        var transactions = await this._transactionRepository.GetByDateRangeAsync(startDate, endDate, accountId, cancellationToken);

        // Filter to only source transactions (to avoid duplicates) with TransferId
        var transferTransactions = transactions
            .Where(t => t.TransferId.HasValue && t.TransferDirection == TransferDirection.Source)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Load all related transactions and account names
        var result = new List<TransferListItemResponse>();
        var accountCache = new Dictionary<Guid, string>();

        foreach (var source in transferTransactions)
        {
            var pair = await this._transactionRepository.GetByTransferIdAsync(source.TransferId!.Value, cancellationToken);
            var destination = pair.FirstOrDefault(t => t.TransferDirection == TransferDirection.Destination);

            if (destination is null)
            {
                continue;
            }

            // Get account names (with caching)
            if (!accountCache.TryGetValue(source.AccountId, out var sourceAccountName))
            {
                var sourceAccount = await this._accountRepository.GetByIdAsync(source.AccountId, cancellationToken);
                sourceAccountName = sourceAccount?.Name ?? "Unknown";
                accountCache[source.AccountId] = sourceAccountName;
            }

            if (!accountCache.TryGetValue(destination.AccountId, out var destAccountName))
            {
                var destAccount = await this._accountRepository.GetByIdAsync(destination.AccountId, cancellationToken);
                destAccountName = destAccount?.Name ?? "Unknown";
                accountCache[destination.AccountId] = destAccountName;
            }

            result.Add(new TransferListItemResponse
            {
                TransferId = source.TransferId!.Value,
                SourceAccountId = source.AccountId,
                SourceAccountName = sourceAccountName,
                DestinationAccountId = destination.AccountId,
                DestinationAccountName = destAccountName,
                Amount = destination.Amount.Amount, // Use positive amount
                Currency = destination.Amount.Currency,
                Date = source.Date,
                Description = ExtractDescription(source.Description),
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<TransferResponse?> UpdateAsync(Guid transferId, UpdateTransferRequest request, CancellationToken cancellationToken = default)
    {
        // Validate amount is positive
        if (request.Amount <= 0)
        {
            throw new DomainException("Transfer amount must be positive.");
        }

        var transactions = await this._transactionRepository.GetByTransferIdAsync(transferId, cancellationToken);

        if (transactions.Count != 2)
        {
            return null;
        }

        var sourceTransaction = transactions.FirstOrDefault(t => t.TransferDirection == TransferDirection.Source);
        var destinationTransaction = transactions.FirstOrDefault(t => t.TransferDirection == TransferDirection.Destination);

        if (sourceTransaction is null || destinationTransaction is null)
        {
            return null;
        }

        // Load account names for description
        var sourceAccount = await this._accountRepository.GetByIdAsync(sourceTransaction.AccountId, cancellationToken);
        var destinationAccount = await this._accountRepository.GetByIdAsync(destinationTransaction.AccountId, cancellationToken);

        var description = string.IsNullOrWhiteSpace(request.Description)
            ? "Transfer"
            : request.Description.Trim();

        // Update source transaction
        sourceTransaction.UpdateAmount(MoneyValue.Create(request.Currency, -request.Amount));
        sourceTransaction.UpdateDate(request.Date);
        sourceTransaction.UpdateDescription($"Transfer to {destinationAccount?.Name ?? "Unknown"}: {description}");

        // Update destination transaction
        destinationTransaction.UpdateAmount(MoneyValue.Create(request.Currency, request.Amount));
        destinationTransaction.UpdateDate(request.Date);
        destinationTransaction.UpdateDescription($"Transfer from {sourceAccount?.Name ?? "Unknown"}: {description}");

        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(
            transferId,
            sourceTransaction,
            destinationTransaction,
            sourceAccount?.Name ?? "Unknown",
            destinationAccount?.Name ?? "Unknown");
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid transferId, CancellationToken cancellationToken = default)
    {
        var transactions = await this._transactionRepository.GetByTransferIdAsync(transferId, cancellationToken);

        if (transactions.Count == 0)
        {
            return false;
        }

        // Delete all transactions with this transfer ID
        foreach (var transaction in transactions)
        {
            await this._transactionRepository.RemoveAsync(transaction, cancellationToken);
        }

        await this._unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static TransferResponse MapToResponse(
        Guid transferId,
        Transaction sourceTransaction,
        Transaction destinationTransaction,
        string sourceAccountName,
        string destinationAccountName)
    {
        return new TransferResponse
        {
            TransferId = transferId,
            SourceAccountId = sourceTransaction.AccountId,
            SourceAccountName = sourceAccountName,
            DestinationAccountId = destinationTransaction.AccountId,
            DestinationAccountName = destinationAccountName,
            Amount = destinationTransaction.Amount.Amount, // Use positive amount
            Currency = destinationTransaction.Amount.Currency,
            Date = sourceTransaction.Date,
            Description = ExtractDescription(sourceTransaction.Description),
            SourceTransactionId = sourceTransaction.Id,
            DestinationTransactionId = destinationTransaction.Id,
            CreatedAtUtc = sourceTransaction.CreatedAt,
        };
    }

    private static string? ExtractDescription(string fullDescription)
    {
        // Extract the user's description from "Transfer to/from AccountName: Description"
        var colonIndex = fullDescription.IndexOf(':', StringComparison.Ordinal);
        if (colonIndex >= 0 && colonIndex < fullDescription.Length - 1)
        {
            var description = fullDescription[(colonIndex + 1)..].Trim();
            return description == "Transfer" ? null : description;
        }

        return null;
    }
}
