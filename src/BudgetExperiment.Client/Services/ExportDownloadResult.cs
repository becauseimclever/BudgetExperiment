// <copyright file="ExportDownloadResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Represents the result of an export download.
/// </summary>
public sealed record ExportDownloadResult(bool Success, string? ErrorMessage = null)
{
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A success result.</returns>
    public static ExportDownloadResult Ok() => new(true);

    /// <summary>
    /// Creates a failure result with a message.
    /// </summary>
    /// <param name="message">Failure message.</param>
    /// <returns>A failure result.</returns>
    public static ExportDownloadResult Fail(string message) => new(false, message);
}
