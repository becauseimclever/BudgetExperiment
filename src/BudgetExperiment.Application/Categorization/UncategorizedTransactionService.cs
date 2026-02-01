// <copyright file="UncategorizedTransactionService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Accounts;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain.Repositories;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Service for managing uncategorized transactions.
/// </summary>
public sealed class UncategorizedTransactionService : IUncategorizedTransactionService
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 50;

    private readonly ITransactionRepository _transactionRepository;
    private readonly IBudgetCategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="UncategorizedTransactionService"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="categoryRepository">The category repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public UncategorizedTransactionService(
        ITransactionRepository transactionRepository,
        IBudgetCategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        this._transactionRepository = transactionRepository;
        this._categoryRepository = categoryRepository;
        this._unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<UncategorizedTransactionPageDto> GetPagedAsync(
        UncategorizedTransactionFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        // Normalize and clamp parameters
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);
        var skip = (page - 1) * pageSize;

        var (items, totalCount) = await this._transactionRepository.GetUncategorizedPagedAsync(
            filter.StartDate,
            filter.EndDate,
            filter.MinAmount,
            filter.MaxAmount,
            filter.DescriptionContains,
            filter.AccountId,
            filter.SortBy,
            filter.SortDescending,
            skip,
            pageSize,
            cancellationToken);

        return new UncategorizedTransactionPageDto
        {
            Items = items.Select(AccountMapper.ToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    /// <inheritdoc />
    public async Task<BulkCategorizeResponse> BulkCategorizeAsync(
        BulkCategorizeRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var successCount = 0;

        // Early return if no transactions to process
        if (request.TransactionIds.Count == 0)
        {
            return new BulkCategorizeResponse
            {
                TotalRequested = 0,
                SuccessCount = 0,
                FailedCount = 0,
                Errors = [],
            };
        }

        // Validate category exists
        var category = await this._categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null)
        {
            return new BulkCategorizeResponse
            {
                TotalRequested = request.TransactionIds.Count,
                SuccessCount = 0,
                FailedCount = request.TransactionIds.Count,
                Errors = [$"Category not found: {request.CategoryId}"],
            };
        }

        // Process each transaction
        foreach (var transactionId in request.TransactionIds)
        {
            var transaction = await this._transactionRepository.GetByIdAsync(transactionId, cancellationToken);
            if (transaction is null)
            {
                errors.Add($"Transaction not found: {transactionId}");
                continue;
            }

            transaction.UpdateCategory(request.CategoryId);
            successCount++;
        }

        // Save all changes in a single transaction
        if (successCount > 0)
        {
            await this._unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new BulkCategorizeResponse
        {
            TotalRequested = request.TransactionIds.Count,
            SuccessCount = successCount,
            FailedCount = errors.Count,
            Errors = errors,
        };
    }
}
