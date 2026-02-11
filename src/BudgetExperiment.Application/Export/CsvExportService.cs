// <copyright file="CsvExportService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text;

namespace BudgetExperiment.Application.Export;

/// <summary>
/// CSV export formatter.
/// </summary>
public sealed class CsvExportService : IExportFormatter
{
    /// <inheritdoc />
    public ExportFormat Format => ExportFormat.Csv;

    /// <inheritdoc />
    public string ContentType => "text/csv";

    /// <inheritdoc />
    public string FileExtension => "csv";

    /// <inheritdoc />
    public Task<byte[]> ExportTableAsync(ExportTable table, CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', table.Columns.Select(EscapeValue)));

        foreach (var row in table.Rows)
        {
            builder.AppendLine(string.Join(',', row.Select(EscapeValue)));
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(builder.ToString()));
    }

    private static string EscapeValue(string value)
    {
        var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        if (!needsQuotes)
        {
            return value;
        }

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
