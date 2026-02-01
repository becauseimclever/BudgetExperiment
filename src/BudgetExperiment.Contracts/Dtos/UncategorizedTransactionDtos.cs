// <copyright file="UncategorizedTransactionDtos.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Filter parameters for querying uncategorized transactions.
/// </summary>
public sealed class UncategorizedTransactionFilterDto
{
    /// <summary>Gets or sets the optional start date filter (inclusive).</summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>Gets or sets the optional end date filter (inclusive).</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>Gets or sets the optional minimum amount filter (absolute value).</summary>
    public decimal? MinAmount { get; set; }

    /// <summary>Gets or sets the optional maximum amount filter (absolute value).</summary>
    public decimal? MaxAmount { get; set; }

    /// <summary>Gets or sets the optional description contains filter (case-insensitive).</summary>
    public string? DescriptionContains { get; set; }

    /// <summary>Gets or sets the optional account filter.</summary>
    public Guid? AccountId { get; set; }

    /// <summary>Gets or sets the sort field: "Date", "Amount", or "Description".</summary>
    public string SortBy { get; set; } = "Date";

    /// <summary>Gets or sets a value indicating whether to sort descending (default: true).</summary>
    public bool SortDescending { get; set; } = true;

    /// <summary>Gets or sets the page number (1-based).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Gets or sets the page size (default: 50, max: 100).</summary>
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Paged response for uncategorized transactions.
/// </summary>
public sealed class UncategorizedTransactionPageDto
{
    /// <summary>Gets or sets the transaction items for the current page.</summary>
    public IReadOnlyList<TransactionDto> Items { get; set; } = [];

    /// <summary>Gets or sets the total count of matching transactions.</summary>
    public int TotalCount { get; set; }

    /// <summary>Gets or sets the current page number (1-based).</summary>
    public int Page { get; set; }

    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }

    /// <summary>Gets the total number of pages.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

/// <summary>
/// Request to bulk categorize transactions.
/// </summary>
public sealed class BulkCategorizeRequest
{
    /// <summary>Gets or sets the transaction IDs to categorize.</summary>
    public IReadOnlyList<Guid> TransactionIds { get; set; } = [];

    /// <summary>Gets or sets the target category ID.</summary>
    public Guid CategoryId { get; set; }
}

/// <summary>
/// Response from bulk categorize operation.
/// </summary>
public sealed class BulkCategorizeResponse
{
    /// <summary>Gets or sets the total number of transactions requested.</summary>
    public int TotalRequested { get; set; }

    /// <summary>Gets or sets the number of successfully updated transactions.</summary>
    public int SuccessCount { get; set; }

    /// <summary>Gets or sets the number of failed updates.</summary>
    public int FailedCount { get; set; }

    /// <summary>Gets or sets error messages for failed updates.</summary>
    public IReadOnlyList<string> Errors { get; set; } = [];
}
