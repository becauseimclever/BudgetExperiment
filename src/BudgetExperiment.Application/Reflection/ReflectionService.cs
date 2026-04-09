// <copyright file="ReflectionService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Reflection;

/// <summary>
/// Application service for monthly Kakeibo reflection operations.
/// </summary>
public sealed class ReflectionService : IReflectionService
{
    private readonly IMonthlyReflectionRepository _repository;
    private readonly ITransactionQueryRepository _transactionRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReflectionService"/> class.
    /// </summary>
    /// <param name="repository">The monthly reflection repository.</param>
    /// <param name="transactionRepository">The transaction repository for computing financial summaries.</param>
    public ReflectionService(
        IMonthlyReflectionRepository repository,
        ITransactionQueryRepository transactionRepository)
    {
        _repository = repository;
        _transactionRepository = transactionRepository;
    }

    /// <inheritdoc />
    public async Task<MonthlyReflectionDto?> GetByMonthAsync(
        int year,
        int month,
        Guid userId,
        CancellationToken ct = default)
    {
        var reflection = await _repository.GetByUserMonthAsync(userId, year, month, ct);
        return reflection is null ? null : ToDto(reflection);
    }

    /// <inheritdoc />
    public async Task<MonthlyReflectionDto> CreateOrUpdateAsync(
        int year,
        int month,
        CreateOrUpdateMonthlyReflectionDto dto,
        Guid userId,
        CancellationToken ct = default)
    {
        var existing = await _repository.GetByUserMonthAsync(userId, year, month, ct);

        if (existing is null)
        {
            var reflection = MonthlyReflection.Create(
                userId,
                year,
                month,
                dto.SavingsGoal,
                dto.IntentionText);

            // Apply full data including gratitude/improvement in one step
            if (dto.GratitudeText is not null || dto.ImprovementText is not null)
            {
                reflection.Update(
                    dto.SavingsGoal,
                    dto.IntentionText,
                    dto.GratitudeText,
                    dto.ImprovementText);
            }

            await _repository.AddAsync(reflection, ct);
            await _repository.SaveChangesAsync(ct);
            return ToDto(reflection);
        }

        existing.Update(
            dto.SavingsGoal,
            dto.IntentionText,
            dto.GratitudeText,
            dto.ImprovementText);

        await _repository.SaveChangesAsync(ct);
        return ToDto(existing);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<MonthlyReflectionDto> Items, int Total)> GetHistoryAsync(
        Guid userId,
        int limit,
        int offset,
        CancellationToken ct = default)
    {
        var total = await _repository.CountByUserAsync(userId, ct);
        var items = await _repository.GetHistoryAsync(userId, limit, offset, ct);
        return (items.Select(ToDto).ToList(), total);
    }

    /// <inheritdoc />
    public async Task<MonthFinancialSummaryDto> GetMonthSummaryAsync(
        int year,
        int month,
        Guid userId,
        CancellationToken ct = default)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var transactions = await _transactionRepository.GetByDateRangeAsync(
            startDate, endDate, accountId: null, ct);

        var nonTransferTransactions = transactions
            .Where(t => !t.IsTransfer)
            .ToList();

        var totalIncome = nonTransferTransactions
            .Where(t => t.Amount.Amount > 0)
            .Sum(t => t.Amount.Amount);

        var totalExpenses = nonTransferTransactions
            .Where(t => t.Amount.Amount < 0)
            .Sum(t => Math.Abs(t.Amount.Amount));

        var breakdown = BuildKakeiboBreakdown(nonTransferTransactions);

        var reflection = await _repository.GetByUserMonthAsync(userId, year, month, ct);

        return new MonthFinancialSummaryDto
        {
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            ComputedSavings = totalIncome - totalExpenses,
            ExpenseBreakdown = breakdown,
            Reflection = reflection is null ? null : ToDto(reflection),
        };
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid reflectionId, Guid userId, CancellationToken ct = default)
    {
        var reflection = await _repository.GetByIdAsync(reflectionId, ct);

        if (reflection is null)
        {
            throw new DomainException("Reflection not found.");
        }

        if (reflection.UserId != userId)
        {
            throw new DomainException("You do not have permission to delete this reflection.");
        }

        _repository.Remove(reflection);
        await _repository.SaveChangesAsync(ct);
    }

    private static KakeiboBreakdownDto BuildKakeiboBreakdown(List<Transaction> nonTransferTransactions)
    {
        var expenseTransactions = nonTransferTransactions
            .Where(t => t.Amount.Amount < 0)
            .ToList();

        return new KakeiboBreakdownDto
        {
            Essentials = SumForKakeibo(expenseTransactions, KakeiboCategory.Essentials),
            Wants = SumForKakeibo(expenseTransactions, KakeiboCategory.Wants),
            Culture = SumForKakeibo(expenseTransactions, KakeiboCategory.Culture),
            Unexpected = SumForKakeibo(expenseTransactions, KakeiboCategory.Unexpected),
        };
    }

    private static decimal SumForKakeibo(List<Transaction> expenseTransactions, KakeiboCategory category)
    {
        return expenseTransactions
            .Where(t => t.GetEffectiveKakeiboCategory() == category)
            .Sum(t => Math.Abs(t.Amount.Amount));
    }

    private static MonthlyReflectionDto ToDto(MonthlyReflection reflection)
    {
        return new MonthlyReflectionDto
        {
            Id = reflection.Id,
            Year = reflection.Year,
            Month = reflection.Month,
            SavingsGoal = reflection.SavingsGoal,
            ActualSavings = reflection.ActualSavings,
            IntentionText = reflection.IntentionText,
            GratitudeText = reflection.GratitudeText,
            ImprovementText = reflection.ImprovementText,
            CreatedAtUtc = reflection.CreatedAtUtc,
            UpdatedAtUtc = reflection.UpdatedAtUtc,
        };
    }
}
