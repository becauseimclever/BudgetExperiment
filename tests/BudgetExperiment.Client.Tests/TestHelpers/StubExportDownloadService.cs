// <copyright file="StubExportDownloadService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;

namespace BudgetExperiment.Client.Tests.TestHelpers;

/// <summary>
/// Shared stub implementation of <see cref="IExportDownloadService"/> for page-level bUnit tests.
/// </summary>
internal sealed class StubExportDownloadService : IExportDownloadService
{
    /// <inheritdoc/>
    public Task<ExportDownloadResult> DownloadAsync(string url, CancellationToken cancellationToken = default) =>
        Task.FromResult(ExportDownloadResult.Ok());
}
