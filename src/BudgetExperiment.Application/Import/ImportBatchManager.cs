// <copyright file="ImportBatchManager.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Import;

/// <summary>
/// Manages import batch history and batch deletion operations.
/// </summary>
public sealed class ImportBatchManager : IImportBatchManager
{
    private readonly IImportBatchRepository _batchRepository;
    private readonly IImportMappingRepository _mappingRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportBatchManager"/> class.
    /// </summary>
    /// <param name="batchRepository">Import batch repository.</param>
    /// <param name="mappingRepository">Import mapping repository.</param>
    /// <param name="accountRepository">Account repository.</param>
    /// <param name="transactionRepository">Transaction repository.</param>
    /// <param name="userContext">User context.</param>
    /// <param name="unitOfWork">Unit of work.</param>
    public ImportBatchManager(
        IImportBatchRepository batchRepository,
        IImportMappingRepository mappingRepository,
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        this._batchRepository = batchRepository;
        this._mappingRepository = mappingRepository;
        this._accountRepository = accountRepository;
        this._transactionRepository = transactionRepository;
        this._userContext = userContext;
        this._unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ImportBatchDto>> GetImportHistoryAsync(CancellationToken cancellationToken = default)
    {
        var userId = this.GetRequiredUserId();
        var batches = await this._batchRepository.GetByUserAsync(userId, cancellationToken: cancellationToken);

        var result = new List<ImportBatchDto>();
        foreach (var batch in batches.OrderByDescending(b => b.ImportedAtUtc))
        {
            string? mappingName = await this.ResolveMappingNameAsync(batch.MappingId, cancellationToken);
            var account = await this._accountRepository.GetByIdAsync(batch.AccountId, cancellationToken);

            result.Add(new ImportBatchDto
            {
                Id = batch.Id,
                AccountId = batch.AccountId,
                AccountName = account?.Name ?? "Unknown",
                FileName = batch.FileName,
                TransactionCount = batch.ImportedCount,
                Status = batch.Status,
                ImportedAtUtc = batch.ImportedAtUtc,
                MappingId = batch.MappingId,
                MappingName = mappingName,
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<int> DeleteImportBatchAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        var userId = this.GetRequiredUserId();

        var batch = await this._batchRepository.GetByIdAsync(batchId, cancellationToken);
        if (batch is null)
        {
            return 0;
        }

        if (batch.UserId != userId)
        {
            throw new DomainException("Cannot delete import batch owned by another user.");
        }

        var transactions = await this._transactionRepository.GetByImportBatchAsync(batchId, cancellationToken);
        var count = transactions.Count;

        foreach (var transaction in transactions)
        {
            await this._transactionRepository.RemoveAsync(transaction, cancellationToken);
        }

        batch.MarkDeleted();
        await this._unitOfWork.SaveChangesAsync(cancellationToken);

        return count;
    }

    private Guid GetRequiredUserId()
    {
        return this._userContext.UserIdAsGuid
            ?? throw new DomainException("User ID is required for import operations.");
    }

    private async Task<string?> ResolveMappingNameAsync(Guid? mappingId, CancellationToken cancellationToken)
    {
        if (!mappingId.HasValue)
        {
            return null;
        }

        var mapping = await this._mappingRepository.GetByIdAsync(mappingId.Value, cancellationToken);
        return mapping?.Name;
    }
}
