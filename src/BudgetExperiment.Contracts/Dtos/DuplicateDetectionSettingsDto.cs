// <copyright file="DuplicateDetectionSettingsDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for duplicate detection settings.
/// </summary>
public sealed record DuplicateDetectionSettingsDto
{
    /// <summary>
    /// Gets a value indicating whether duplicate detection is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets the number of days to look back for duplicates.
    /// </summary>
    public int LookbackDays { get; init; } = 30;

    /// <summary>
    /// Gets how descriptions should be matched for duplicate detection.
    /// </summary>
    public DescriptionMatchMode DescriptionMatch { get; init; } = DescriptionMatchMode.Exact;
}
