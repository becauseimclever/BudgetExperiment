// <copyright file="IExportService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Export;

/// <summary>
/// Export service interface for generating export files.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exports a table to the requested format.
    /// </summary>
    /// <param name="table">The table to export.</param>
    /// <param name="format">The export format.</param>
    /// <param name="fileName">The base file name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The export document.</returns>
    Task<ExportDocument> ExportTableAsync(ExportTable table, ExportFormat format, string fileName, CancellationToken cancellationToken = default);
}
