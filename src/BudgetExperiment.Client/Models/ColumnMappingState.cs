// <copyright file="ColumnMappingState.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Client-side model for tracking column mapping state during wizard.
/// </summary>
public sealed class ColumnMappingState
{
    /// <summary>
    /// Gets or sets the zero-based column index.
    /// </summary>
    public int ColumnIndex { get; set; }

    /// <summary>
    /// Gets or sets the column header from CSV.
    /// </summary>
    public string ColumnHeader { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target field.
    /// </summary>
    public ImportField? TargetField { get; set; }

    /// <summary>
    /// Gets or sets sample values from the first few rows.
    /// </summary>
    public IReadOnlyList<string> SampleValues { get; set; } = [];

    /// <summary>
    /// Converts to DTO for API request.
    /// </summary>
    /// <returns>The DTO.</returns>
    public ColumnMappingDto? ToDto()
    {
        if (!this.TargetField.HasValue || this.TargetField == ImportField.Ignore)
        {
            return null;
        }

        return new ColumnMappingDto
        {
            ColumnIndex = this.ColumnIndex,
            ColumnHeader = this.ColumnHeader,
            TargetField = this.TargetField.Value,
        };
    }
}
