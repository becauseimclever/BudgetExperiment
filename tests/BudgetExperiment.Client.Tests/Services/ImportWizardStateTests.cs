// <copyright file="ImportWizardStateTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Contracts.Dtos;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ImportWizardState"/> computed properties and state management.
/// </summary>
public sealed class ImportWizardStateTests
{
    /// <summary>
    /// Verifies default state values after construction.
    /// </summary>
    [Fact]
    public void Default_HasExpectedDefaults()
    {
        var state = new ImportWizardState();

        state.CurrentStep.ShouldBe(1);
        state.ParseResult.ShouldBeNull();
        state.SelectedAccountId.ShouldBeNull();
        state.ColumnMappings.ShouldBeEmpty();
        state.DateFormat.ShouldBe("MM/dd/yyyy");
        state.AmountMode.ShouldBe(AmountParseMode.NegativeIsExpense);
        state.RowsToSkip.ShouldBe(0);
        state.IndicatorSettings.ShouldBeNull();
        state.PreviewResult.ShouldBeNull();
        state.SelectedRowIndices.ShouldBeEmpty();
        state.FileName.ShouldBe(string.Empty);
        state.SavedMappingId.ShouldBeNull();
    }

    /// <summary>
    /// Verifies HasRequiredMappings is false with no mappings.
    /// </summary>
    [Fact]
    public void HasRequiredMappings_NoMappings_ReturnsFalse()
    {
        var state = new ImportWizardState();

        state.HasRequiredMappings.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies HasRequiredMappings is true when Date, Description, and Amount are mapped.
    /// </summary>
    [Fact]
    public void HasRequiredMappings_WithDateDescriptionAmount_ReturnsTrue()
    {
        var state = new ImportWizardState
        {
            ColumnMappings =
            [
                new ColumnMappingState { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingState { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingState { ColumnIndex = 2, TargetField = ImportField.Amount },
            ],
        };

        state.HasRequiredMappings.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies HasRequiredMappings is true when Date, Description, DebitAmount and CreditAmount are mapped.
    /// </summary>
    [Fact]
    public void HasRequiredMappings_WithDebitCredit_ReturnsTrue()
    {
        var state = new ImportWizardState
        {
            ColumnMappings =
            [
                new ColumnMappingState { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingState { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingState { ColumnIndex = 2, TargetField = ImportField.DebitAmount },
                new ColumnMappingState { ColumnIndex = 3, TargetField = ImportField.CreditAmount },
            ],
        };

        state.HasRequiredMappings.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies HasRequiredMappings is false when only Date is mapped (missing Description and Amount).
    /// </summary>
    [Fact]
    public void HasRequiredMappings_MissingDescription_ReturnsFalse()
    {
        var state = new ImportWizardState
        {
            ColumnMappings =
            [
                new ColumnMappingState { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingState { ColumnIndex = 1, TargetField = ImportField.Amount },
            ],
        };

        state.HasRequiredMappings.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies HasRequiredMappings is false when Description and Amount but no Date.
    /// </summary>
    [Fact]
    public void HasRequiredMappings_MissingDate_ReturnsFalse()
    {
        var state = new ImportWizardState
        {
            ColumnMappings =
            [
                new ColumnMappingState { ColumnIndex = 0, TargetField = ImportField.Description },
                new ColumnMappingState { ColumnIndex = 1, TargetField = ImportField.Amount },
            ],
        };

        state.HasRequiredMappings.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies HasRequiredMappings with indicator mode requires indicator column and settings.
    /// </summary>
    [Fact]
    public void HasRequiredMappings_IndicatorMode_WithSettings_ReturnsTrue()
    {
        var state = new ImportWizardState
        {
            AmountMode = AmountParseMode.IndicatorColumn,
            IndicatorSettings = new DebitCreditIndicatorSettingsDto(),
            ColumnMappings =
            [
                new ColumnMappingState { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingState { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingState { ColumnIndex = 2, TargetField = ImportField.DebitCreditIndicator },
            ],
        };

        state.HasRequiredMappings.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies HasRequiredMappings with indicator mode returns false without settings.
    /// </summary>
    [Fact]
    public void HasRequiredMappings_IndicatorMode_WithoutSettings_ReturnsFalse()
    {
        var state = new ImportWizardState
        {
            AmountMode = AmountParseMode.IndicatorColumn,
            IndicatorSettings = null,
            ColumnMappings =
            [
                new ColumnMappingState { ColumnIndex = 0, TargetField = ImportField.Date },
                new ColumnMappingState { ColumnIndex = 1, TargetField = ImportField.Description },
                new ColumnMappingState { ColumnIndex = 2, TargetField = ImportField.DebitCreditIndicator },
            ],
        };

        state.HasRequiredMappings.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies Reset restores all values to defaults.
    /// </summary>
    [Fact]
    public void Reset_RestoresAllDefaults()
    {
        var state = new ImportWizardState
        {
            CurrentStep = 3,
            SelectedAccountId = Guid.NewGuid(),
            ColumnMappings =
            [
                new ColumnMappingState { ColumnIndex = 0, TargetField = ImportField.Date },
            ],
            DateFormat = "yyyy-MM-dd",
            AmountMode = AmountParseMode.SeparateColumns,
            RowsToSkip = 2,
            IndicatorSettings = new DebitCreditIndicatorSettingsDto(),
            SelectedRowIndices = [1, 2, 3],
            FileName = "test.csv",
            SavedMappingId = Guid.NewGuid(),
        };

        state.Reset();

        state.CurrentStep.ShouldBe(1);
        state.ParseResult.ShouldBeNull();
        state.SelectedAccountId.ShouldBeNull();
        state.ColumnMappings.ShouldBeEmpty();
        state.DateFormat.ShouldBe("MM/dd/yyyy");
        state.AmountMode.ShouldBe(AmountParseMode.NegativeIsExpense);
        state.RowsToSkip.ShouldBe(0);
        state.IndicatorSettings.ShouldBeNull();
        state.PreviewResult.ShouldBeNull();
        state.SelectedRowIndices.ShouldBeEmpty();
        state.FileName.ShouldBe(string.Empty);
        state.SavedMappingId.ShouldBeNull();
    }
}
