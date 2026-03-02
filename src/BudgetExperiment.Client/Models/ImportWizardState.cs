// <copyright file="ImportWizardState.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Tracks the state of the import wizard.
/// </summary>
public sealed class ImportWizardState
{
    /// <summary>
    /// Gets or sets the current step (1-4).
    /// </summary>
    public int CurrentStep { get; set; } = 1;

    /// <summary>
    /// Gets or sets the parsed CSV result.
    /// </summary>
    public CsvParseResultModel? ParseResult { get; set; }

    /// <summary>
    /// Gets or sets the selected account ID.
    /// </summary>
    public Guid? SelectedAccountId { get; set; }

    /// <summary>
    /// Gets or sets the column mappings.
    /// </summary>
    public List<ColumnMappingState> ColumnMappings { get; set; } = [];

    /// <summary>
    /// Gets or sets the date format.
    /// </summary>
    public string DateFormat { get; set; } = "MM/dd/yyyy";

    /// <summary>
    /// Gets or sets the amount parse mode.
    /// </summary>
    public AmountParseMode AmountMode { get; set; } = AmountParseMode.NegativeIsExpense;

    /// <summary>
    /// Gets or sets the duplicate detection settings.
    /// </summary>
    public DuplicateDetectionSettingsDto DuplicateSettings { get; set; } = new();

    /// <summary>
    /// Gets or sets the number of data rows to skip after the header.
    /// </summary>
    public int RowsToSkip { get; set; }

    /// <summary>
    /// Gets or sets the debit/credit indicator settings.
    /// </summary>
    public DebitCreditIndicatorSettingsDto? IndicatorSettings { get; set; }

    /// <summary>
    /// Gets or sets the preview result.
    /// </summary>
    public ImportPreviewResult? PreviewResult { get; set; }

    /// <summary>
    /// Gets or sets the selected row indices for import.
    /// </summary>
    public HashSet<int> SelectedRowIndices { get; set; } = [];

    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the saved mapping ID if using one.
    /// </summary>
    public Guid? SavedMappingId { get; set; }

    /// <summary>
    /// Gets a value indicating whether required fields are mapped.
    /// </summary>
    public bool HasRequiredMappings
    {
        get
        {
            var hasDate = this.ColumnMappings.Any(m => m.TargetField == ImportField.Date);
            var hasDescription = this.ColumnMappings.Any(m => m.TargetField == ImportField.Description);
            var hasAmount = this.ColumnMappings.Any(m => m.TargetField == ImportField.Amount);
            var hasDebitCredit = this.ColumnMappings.Any(m => m.TargetField == ImportField.DebitAmount) &&
                                  this.ColumnMappings.Any(m => m.TargetField == ImportField.CreditAmount);
            var hasIndicator = this.AmountMode == AmountParseMode.IndicatorColumn &&
                                this.ColumnMappings.Any(m => m.TargetField == ImportField.DebitCreditIndicator) &&
                                this.IndicatorSettings != null;

            return hasDate && hasDescription && (hasAmount || hasDebitCredit || hasIndicator);
        }
    }

    /// <summary>
    /// Resets the wizard state.
    /// </summary>
    public void Reset()
    {
        this.CurrentStep = 1;
        this.ParseResult = null;
        this.SelectedAccountId = null;
        this.ColumnMappings = [];
        this.DateFormat = "MM/dd/yyyy";
        this.AmountMode = AmountParseMode.NegativeIsExpense;
        this.DuplicateSettings = new();
        this.RowsToSkip = 0;
        this.IndicatorSettings = null;
        this.PreviewResult = null;
        this.SelectedRowIndices = [];
        this.FileName = string.Empty;
        this.SavedMappingId = null;
    }
}
