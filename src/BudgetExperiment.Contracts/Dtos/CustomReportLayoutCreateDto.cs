// <copyright file="CustomReportLayoutCreateDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for creating a custom report layout.
/// </summary>
public sealed class CustomReportLayoutCreateDto
{
    /// <summary>Gets or sets the layout name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the layout JSON definition.</summary>
    public string LayoutJson { get; set; } = "{}";

    /// <summary>Gets or sets the optional scope override (Shared or Personal).</summary>
    public string? Scope { get; set; }
}
