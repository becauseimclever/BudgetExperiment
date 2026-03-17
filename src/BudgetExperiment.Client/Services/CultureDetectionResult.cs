// <copyright file="CultureDetectionResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Result from browser culture detection JS interop.
/// </summary>
public sealed class CultureDetectionResult
{
    /// <summary>
    /// Gets or sets the browser's preferred language (e.g., "en-US", "fr-FR").
    /// </summary>
    public string Language { get; set; } = "en-US";

    /// <summary>
    /// Gets or sets the browser's detected timezone (IANA format, e.g., "America/New_York").
    /// </summary>
    public string TimeZone { get; set; } = "UTC";
}
