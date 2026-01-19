// <copyright file="CsvParserService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Service for parsing CSV files with automatic delimiter detection.
/// </summary>
public class CsvParserService : ICsvParserService
{
    private static readonly char[] _supportedDelimiters = { ',', ';', '\t', '|' };

    /// <inheritdoc/>
    public async Task<CsvParseResult> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var content = await reader.ReadToEndAsync(ct);

            // Remove BOM if present
            if (content.Length > 0 && content[0] == '\uFEFF')
            {
                content = content[1..];
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return CsvParseResult.CreateFailure("File is empty.");
            }

            // Normalize line endings to \n
            content = content.Replace("\r\n", "\n").Replace("\r", "\n");

            var lines = SplitCsvLines(content);
            if (lines.Count == 0)
            {
                return CsvParseResult.CreateFailure("File is empty.");
            }

            // Detect delimiter from first line
            var delimiter = DetectDelimiter(lines[0]);

            // Parse header row
            var headers = ParseCsvLine(lines[0], delimiter);
            if (headers.Count == 0)
            {
                return CsvParseResult.CreateFailure("No columns detected in header row.");
            }

            // Parse data rows
            var rows = new List<IReadOnlyList<string>>();
            for (int i = 1; i < lines.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue; // Skip empty lines
                }

                var row = ParseCsvLine(line, delimiter);
                rows.Add(row);
            }

            return CsvParseResult.CreateSuccess(headers, rows, delimiter, hasHeaderRow: true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return CsvParseResult.CreateFailure($"Failed to parse CSV: {ex.Message}");
        }
    }

    /// <summary>
    /// Splits CSV content into lines, respecting quoted fields that may contain newlines.
    /// </summary>
    private static List<string> SplitCsvLines(string content)
    {
        var lines = new List<string>();
        var currentLine = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
                currentLine.Append(c);
            }
            else if (c == '\n' && !inQuotes)
            {
                var line = currentLine.ToString();
                if (!string.IsNullOrEmpty(line))
                {
                    lines.Add(line);
                }

                currentLine.Clear();
            }
            else
            {
                currentLine.Append(c);
            }
        }

        // Add last line if not empty
        if (currentLine.Length > 0)
        {
            lines.Add(currentLine.ToString());
        }

        return lines;
    }

    /// <summary>
    /// Detects the delimiter used in a CSV line.
    /// </summary>
    private static char DetectDelimiter(string line)
    {
        var delimiterCounts = new Dictionary<char, int>();

        foreach (var delimiter in _supportedDelimiters)
        {
            delimiterCounts[delimiter] = CountDelimiterOccurrences(line, delimiter);
        }

        // Return the delimiter with highest count, defaulting to comma
        char bestDelimiter = ',';
        int bestCount = 0;

        foreach (var kvp in delimiterCounts)
        {
            if (kvp.Value > bestCount)
            {
                bestCount = kvp.Value;
                bestDelimiter = kvp.Key;
            }
        }

        return bestDelimiter;
    }

    /// <summary>
    /// Counts delimiter occurrences outside of quoted sections.
    /// </summary>
    private static int CountDelimiterOccurrences(string line, char delimiter)
    {
        int count = 0;
        bool inQuotes = false;

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == delimiter && !inQuotes)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Parses a single CSV line into fields.
    /// </summary>
    private static List<string> ParseCsvLine(string line, char delimiter)
    {
        var fields = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;
        int i = 0;

        while (i < line.Length)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // Check for escaped quote
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i += 2;
                        continue;
                    }
                    else
                    {
                        inQuotes = false;
                        i++;
                        continue;
                    }
                }
                else
                {
                    currentField.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == delimiter)
                {
                    fields.Add(currentField.ToString().Trim());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            i++;
        }

        // Add last field
        fields.Add(currentField.ToString().Trim());

        return fields;
    }
}
