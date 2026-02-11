// <copyright file="IExportDownloadService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Defines a service for downloading export files.
/// </summary>
public interface IExportDownloadService
{
    /// <summary>
    /// Downloads an export file from the provided URL.
    /// </summary>
    /// <param name="url">The export URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The export download result.</returns>
    Task<ExportDownloadResult> DownloadAsync(string url, CancellationToken cancellationToken = default);
}
