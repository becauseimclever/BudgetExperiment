// <copyright file="StubCsvParserService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;

namespace BudgetExperiment.Client.Tests.TestHelpers;

/// <summary>
/// Shared stub implementation of <see cref="ICsvParserService"/> for page-level bUnit tests.
/// </summary>
internal sealed class StubCsvParserService : ICsvParserService
{
    /// <summary>
    /// Gets or sets the parse result to return.
    /// </summary>
    public CsvParseResult? Result { get; set; }

    /// <inheritdoc/>
    public Task<CsvParseResult> ParseAsync(Stream fileStream, string fileName, int rowsToSkip = 0, CancellationToken ct = default)
    {
        return Task.FromResult(this.Result ?? CsvParseResult.CreateFailure("No stub result configured"));
    }
}
