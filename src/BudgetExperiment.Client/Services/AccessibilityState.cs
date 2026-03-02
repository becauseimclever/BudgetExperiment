// <copyright file="AccessibilityState.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Represents the accessibility detection state from the theme system.
/// </summary>
/// <param name="IsAccessibilityPreferenceDetected">True if system accessibility preferences were detected.</param>
/// <param name="WasThemeAutoApplied">True if the accessible theme was auto-applied based on preferences.</param>
/// <param name="HasExplicitOverride">True if user has explicitly chosen a theme override.</param>
public record AccessibilityState(
    bool IsAccessibilityPreferenceDetected,
    bool WasThemeAutoApplied,
    bool HasExplicitOverride);
