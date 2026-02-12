// <copyright file="CustomReportLayoutUpdateDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for updating a custom report layout.
/// </summary>
public sealed class CustomReportLayoutUpdateDto
{
    /// <summary>Gets or sets the layout name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the layout JSON definition.</summary>
    public string? LayoutJson { get; set; }
}
