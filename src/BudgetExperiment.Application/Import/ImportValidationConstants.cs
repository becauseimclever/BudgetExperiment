// <copyright file="ImportValidationConstants.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Import;

/// <summary>
/// Validation constants for import operations.
/// </summary>
public static class ImportValidationConstants
{
    /// <summary>
    /// Maximum number of transactions allowed per import batch.
    /// </summary>
    public const int MaxTransactionsPerImport = 5000;

    /// <summary>
    /// Maximum length for a transaction description.
    /// </summary>
    public const int MaxDescriptionLength = 500;

    /// <summary>
    /// Maximum length for a category name.
    /// </summary>
    public const int MaxCategoryLength = 100;

    /// <summary>
    /// Maximum number of days in the future a transaction date can be.
    /// </summary>
    public const int MaxFutureDateDays = 365;

    /// <summary>
    /// Maximum absolute value allowed for a transaction amount.
    /// </summary>
    public const decimal MaxAmountAbsoluteValue = 99_999_999.99m;

    /// <summary>
    /// Maximum request body size in bytes for the execute endpoint (5 MB).
    /// </summary>
    public const int ExecuteRequestSizeLimitBytes = 5 * 1024 * 1024;

    /// <summary>
    /// Maximum number of rows allowed in a preview request.
    /// </summary>
    public const int MaxPreviewRows = 10_000;

    /// <summary>
    /// Maximum request body size in bytes for the preview endpoint (10 MB).
    /// </summary>
    public const int PreviewRequestSizeLimitBytes = 10 * 1024 * 1024;
}
