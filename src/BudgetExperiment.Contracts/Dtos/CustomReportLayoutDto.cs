// <copyright file="CustomReportLayoutDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for a saved custom report layout.
/// </summary>
public sealed class CustomReportLayoutDto
{
    /// <summary>Gets or sets the layout ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the layout name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the layout JSON definition.</summary>
    public string LayoutJson { get; set; } = "{}";

    /// <summary>Gets or sets the scope (Shared or Personal).</summary>
    public string Scope { get; set; } = "Shared";

    /// <summary>Gets or sets when the layout was created (UTC).</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Gets or sets when the layout was last updated (UTC).</summary>
    public DateTime UpdatedAtUtc { get; set; }
}
