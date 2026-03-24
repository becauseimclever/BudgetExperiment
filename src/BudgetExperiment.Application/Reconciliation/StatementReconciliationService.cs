// <copyright file="StatementReconciliationService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Reconciliation;

/// <summary>
/// Application service for statement-based reconciliation (Feature 125).
/// </summary>
public sealed class StatementReconciliationService : IStatementReconciliationService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IStatementBalanceRepository _statementBalanceRepository;
    private readonly IReconciliationRecordRepository _reconciliationRecordRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatementReconciliationService"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="accountRepository">The account repository.</param>
    /// <param name="statementBalanceRepository">The statement balance repository.</param>
    /// <param name="reconciliationRecordRepository">The reconciliation record repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="userContext">The user context.</param>
    public StatementReconciliationService(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        IStatementBalanceRepository statementBalanceRepository,
        IReconciliationRecordRepository reconciliationRecordRepository,
        IUnitOfWork unitOfWork,
        IUserContext userContext)
    {
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _statementBalanceRepository = statementBalanceRepository;
        _reconciliationRecordRepository = reconciliationRecordRepository;
        _unitOfWork = unitOfWork;
        _userContext = userContext;
    }

    /// <inheritdoc />
    public async Task<TransactionDto> MarkClearedAsync(Guid transactionId, DateOnly clearedDate, CancellationToken ct)
    {
        var transaction = await RequireTransactionAsync(transactionId, ct);
        transaction.MarkCleared(clearedDate);
        _unitOfWork.MarkAsModified(transaction);
        await _unitOfWork.SaveChangesAsync(ct);
        return AccountMapper.ToDto(transaction);
    }

    /// <inheritdoc />
    public async Task<TransactionDto> MarkUnclearedAsync(Guid transactionId, CancellationToken ct)
    {
        var transaction = await RequireTransactionAsync(transactionId, ct);
        transaction.MarkUncleared();
        _unitOfWork.MarkAsModified(transaction);
        await _unitOfWork.SaveChangesAsync(ct);
        return AccountMapper.ToDto(transaction);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TransactionDto>> BulkMarkClearedAsync(
        IReadOnlyList<Guid> transactionIds,
        DateOnly clearedDate,
        CancellationToken ct)
    {
        var transactions = await _transactionRepository.GetByIdsAsync(transactionIds, ct);
        foreach (var tx in transactions)
        {
            tx.MarkCleared(clearedDate);
            _unitOfWork.MarkAsModified(tx);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return transactions.Select(AccountMapper.ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TransactionDto>> BulkMarkUnclearedAsync(
        IReadOnlyList<Guid> transactionIds,
        CancellationToken ct)
    {
        var transactions = await _transactionRepository.GetByIdsAsync(transactionIds, ct);
        var affected = new List<Transaction>();

        foreach (var tx in transactions)
        {
            if (tx.ReconciliationRecordId is not null)
            {
                continue;
            }

            tx.MarkUncleared();
            _unitOfWork.MarkAsModified(tx);
            affected.Add(tx);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return affected.Select(AccountMapper.ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<StatementBalanceDto?> GetActiveStatementBalanceAsync(Guid accountId, CancellationToken ct)
    {
        var statementBalance = await _statementBalanceRepository.GetActiveByAccountAsync(accountId, ct);
        return statementBalance is null ? null : ToDto(statementBalance);
    }

    /// <inheritdoc />
    public async Task<ClearedBalanceDto> GetClearedBalanceAsync(Guid accountId, DateOnly? upToDate, CancellationToken ct)
    {
        var account = await _accountRepository.GetByIdAsync(accountId, ct)
            ?? throw new DomainException($"Account {accountId} not found.", DomainExceptionType.NotFound);

        var clearedSum = await _transactionRepository.GetClearedBalanceSumAsync(accountId, upToDate, ct);
        var total = account.InitialBalance + clearedSum;

        return new ClearedBalanceDto
        {
            AccountId = accountId,
            InitialBalance = account.InitialBalance.Amount,
            ClearedBalance = total.Amount,
            UpToDate = upToDate,
            Currency = total.Currency,
        };
    }

    /// <inheritdoc />
    public async Task<StatementBalanceDto> SetStatementBalanceAsync(
        Guid accountId,
        DateOnly statementDate,
        decimal balance,
        CancellationToken ct)
    {
        var account = await _accountRepository.GetByIdAsync(accountId, ct)
            ?? throw new DomainException($"Account {accountId} not found.", DomainExceptionType.NotFound);

        var currency = account.InitialBalance.Currency;
        var existing = await _statementBalanceRepository.GetActiveByAccountAsync(accountId, ct);

        if (existing is not null)
        {
            existing.UpdateBalance(MoneyValue.Create(currency, balance));
            _unitOfWork.MarkAsModified(existing);
            await _unitOfWork.SaveChangesAsync(ct);
            return ToDto(existing);
        }

        var newBalance = StatementBalance.Create(accountId, statementDate, MoneyValue.Create(currency, balance));
        await _statementBalanceRepository.AddAsync(newBalance, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return ToDto(newBalance);
    }

    /// <inheritdoc />
    public async Task<ReconciliationRecordDto> CompleteReconciliationAsync(Guid accountId, CancellationToken ct)
    {
        var statementBalance = await _statementBalanceRepository.GetActiveByAccountAsync(accountId, ct)
            ?? throw new DomainException(
                $"No active statement balance found for account {accountId}.",
                DomainExceptionType.NotFound);

        var clearedBalanceDto = await GetClearedBalanceAsync(accountId, statementBalance.StatementDate, ct);
        var account = await _accountRepository.GetByIdAsync(accountId, ct)!;
        var currency = account!.InitialBalance.Currency;
        var clearedBalance = MoneyValue.Create(currency, clearedBalanceDto.ClearedBalance);

        var clearedTransactions = await _transactionRepository.GetClearedByAccountAsync(
            accountId,
            statementBalance.StatementDate,
            ct);

        var userId = _userContext.UserIdAsGuid ?? Guid.Empty;
        var scope = _userContext.CurrentScope ?? BudgetScope.Shared;
        var ownerUserId = scope == BudgetScope.Personal ? userId : (Guid?)null;

        var record = ReconciliationRecord.Create(
            accountId,
            statementBalance.StatementDate,
            statementBalance.Balance,
            clearedBalance,
            clearedTransactions.Count,
            userId,
            scope,
            ownerUserId);

        await _reconciliationRecordRepository.AddAsync(record, ct);

        foreach (var tx in clearedTransactions)
        {
            tx.LockToReconciliation(record.Id);
            _unitOfWork.MarkAsModified(tx);
        }

        statementBalance.MarkCompleted();
        _unitOfWork.MarkAsModified(statementBalance);

        await _unitOfWork.SaveChangesAsync(ct);
        return ToDto(record);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReconciliationRecordDto>> GetReconciliationHistoryAsync(
        Guid accountId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var skip = (page - 1) * pageSize;
        var records = await _reconciliationRecordRepository.ListAsync(skip, pageSize, ct);
        return records.Select(ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TransactionDto>> GetReconciliationTransactionsAsync(
        Guid reconciliationRecordId,
        CancellationToken ct)
    {
        var transactions = await _transactionRepository.GetByReconciliationRecordAsync(reconciliationRecordId, ct);
        return transactions.Select(AccountMapper.ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task UnlockTransactionAsync(Guid transactionId, CancellationToken ct)
    {
        var transaction = await RequireTransactionAsync(transactionId, ct);
        transaction.UnlockFromReconciliation();
        _unitOfWork.MarkAsModified(transaction);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static StatementBalanceDto ToDto(StatementBalance sb)
    {
        return new StatementBalanceDto
        {
            Id = sb.Id,
            AccountId = sb.AccountId,
            StatementDate = sb.StatementDate,
            Balance = sb.Balance.Amount,
            Currency = sb.Balance.Currency,
            IsCompleted = sb.IsCompleted,
        };
    }

    private static ReconciliationRecordDto ToDto(ReconciliationRecord record)
    {
        return new ReconciliationRecordDto
        {
            Id = record.Id,
            AccountId = record.AccountId,
            StatementDate = record.StatementDate,
            StatementBalance = record.StatementBalance.Amount,
            StatementBalanceCurrency = record.StatementBalance.Currency,
            ClearedBalance = record.ClearedBalance.Amount,
            TransactionCount = record.TransactionCount,
            CompletedAtUtc = record.CompletedAtUtc,
        };
    }

    private async Task<Transaction> RequireTransactionAsync(Guid transactionId, CancellationToken ct)
    {
        return await _transactionRepository.GetByIdAsync(transactionId, ct)
            ?? throw new DomainException(
                $"Transaction {transactionId} not found.",
                DomainExceptionType.NotFound);
    }
}
