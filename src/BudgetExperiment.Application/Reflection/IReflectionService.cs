// <copyright file="IReflectionService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Reflection;

/// <summary>
/// Application service for monthly Kakeibo reflection operations.
/// </summary>
public interface IReflectionService
{
    /// <summary>
    /// Gets the reflection for a specific user and month, or null if none exists.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The reflection DTO, or null if not found.</returns>
    Task<MonthlyReflectionDto?> GetByMonthAsync(
        int year,
        int month,
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new reflection or updates the existing one for the given month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="dto">The create/update data.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created or updated reflection DTO.</returns>
    Task<MonthlyReflectionDto> CreateOrUpdateAsync(
        int year,
        int month,
        CreateOrUpdateMonthlyReflectionDto dto,
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a paginated history of reflections for the user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <param name="offset">Number of items to skip.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The paged items and total count.</returns>
    Task<(IReadOnlyList<MonthlyReflectionDto> Items, int Total)> GetHistoryAsync(
        Guid userId,
        int limit,
        int offset,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the complete financial summary for a month, including the user's reflection.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The monthly financial summary DTO.</returns>
    Task<MonthFinancialSummaryDto> GetMonthSummaryAsync(
        int year,
        int month,
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a reflection belonging to the specified user.
    /// </summary>
    /// <param name="reflectionId">The reflection identifier.</param>
    /// <param name="userId">The user identifier (must match the reflection owner).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="DomainException">Thrown when the reflection is not found or does not belong to the user.</exception>
    Task DeleteAsync(Guid reflectionId, Guid userId, CancellationToken ct = default);
}
