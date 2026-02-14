// <copyright file="ExportService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Export;

/// <summary>
/// Export service that delegates to registered formatters.
/// </summary>
public sealed class ExportService : IExportService
{
    private readonly IReadOnlyList<IExportFormatter> _formatters;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportService"/> class.
    /// </summary>
    /// <param name="formatters">Available formatters.</param>
    public ExportService(IEnumerable<IExportFormatter> formatters)
    {
        this._formatters = formatters.ToList();
    }

    /// <inheritdoc />
    public async Task<ExportDocument> ExportTableAsync(
        ExportTable table,
        ExportFormat format,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var formatter = this._formatters.FirstOrDefault(f => f.Format == format);
        if (formatter is null)
        {
            throw new InvalidOperationException($"No export formatter registered for {format}.");
        }

        var bytes = await formatter.ExportTableAsync(table, cancellationToken);
        var resolvedFileName = BuildFileName(fileName, formatter.FileExtension);
        return new ExportDocument(resolvedFileName, formatter.ContentType, bytes);
    }

    private static string BuildFileName(string fileName, string extension)
    {
        var trimmed = fileName.Trim();
        if (trimmed.EndsWith($".{extension}", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return string.IsNullOrWhiteSpace(trimmed) ? $"export.{extension}" : $"{trimmed}.{extension}";
    }
}
