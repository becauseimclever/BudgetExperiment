// <copyright file="ReconciliationMapper.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Reconciliation;

/// <summary>
/// Mappers for reconciliation-related domain entities to DTOs.
/// </summary>
public static class ReconciliationMapper
{
    /// <summary>
    /// Maps a <see cref="ReconciliationMatch"/> to a <see cref="ReconciliationMatchDto"/>.
    /// </summary>
    /// <param name="match">The reconciliation match entity.</param>
    /// <param name="importedTransaction">Optional imported transaction for enriched response.</param>
    /// <param name="recurringDescription">Optional recurring transaction description.</param>
    /// <param name="expectedAmount">Optional expected amount from recurring transaction.</param>
    /// <returns>The mapped DTO.</returns>
    public static ReconciliationMatchDto ToDto(
        ReconciliationMatch match,
        Transaction? importedTransaction = null,
        string? recurringDescription = null,
        MoneyValue? expectedAmount = null)
    {
        return new ReconciliationMatchDto
        {
            Id = match.Id,
            ImportedTransactionId = match.ImportedTransactionId,
            RecurringTransactionId = match.RecurringTransactionId,
            RecurringInstanceDate = match.RecurringInstanceDate,
            ConfidenceScore = match.ConfidenceScore,
            ConfidenceLevel = match.ConfidenceLevel.ToString(),
            Status = match.Status.ToString(),
            AmountVariance = match.AmountVariance,
            DateOffsetDays = match.DateOffsetDays,
            CreatedAtUtc = match.CreatedAtUtc,
            ResolvedAtUtc = match.ResolvedAtUtc,
            ImportedTransaction = importedTransaction != null ? AccountMapper.ToDto(importedTransaction) : null,
            RecurringTransactionDescription = recurringDescription,
            ExpectedAmount = expectedAmount != null ? CommonMapper.ToDto(expectedAmount) : null,
        };
    }

    /// <summary>
    /// Maps a <see cref="MatchingTolerances"/> to a <see cref="MatchingTolerancesDto"/>.
    /// </summary>
    /// <param name="tolerances">The matching tolerances value object.</param>
    /// <returns>The mapped DTO.</returns>
    public static MatchingTolerancesDto ToDto(MatchingTolerances tolerances)
    {
        return new MatchingTolerancesDto
        {
            DateToleranceDays = tolerances.DateToleranceDays,
            AmountTolerancePercent = tolerances.AmountTolerancePercent,
            AmountToleranceAbsolute = tolerances.AmountToleranceAbsolute,
            DescriptionSimilarityThreshold = tolerances.DescriptionSimilarityThreshold,
            AutoMatchThreshold = tolerances.AutoMatchThreshold,
        };
    }

    /// <summary>
    /// Maps a <see cref="MatchingTolerancesDto"/> to a <see cref="MatchingTolerances"/>.
    /// </summary>
    /// <param name="dto">The tolerances DTO.</param>
    /// <returns>The domain value object.</returns>
    public static MatchingTolerances ToDomain(MatchingTolerancesDto dto)
    {
        return MatchingTolerances.Create(
            dto.DateToleranceDays,
            dto.AmountTolerancePercent,
            dto.AmountToleranceAbsolute,
            dto.DescriptionSimilarityThreshold,
            dto.AutoMatchThreshold);
    }
}
