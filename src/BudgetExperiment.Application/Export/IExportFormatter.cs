// <copyright file="IExportFormatter.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Export;

/// <summary>
/// Formats export tables into specific file formats.
/// </summary>
public interface IExportFormatter
{
    /// <summary>
    /// Gets the export format handled by this formatter.
    /// </summary>
    ExportFormat Format { get; }

    /// <summary>
    /// Gets the content type for the format.
    /// </summary>
    string ContentType { get; }

    /// <summary>
    /// Gets the file extension for the format.
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// Exports the table as bytes for the target format.
    /// </summary>
    /// <param name="table">The table to export.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exported file bytes.</returns>
    Task<byte[]> ExportTableAsync(ExportTable table, CancellationToken cancellationToken = default);
}
