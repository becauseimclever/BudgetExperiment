// -----------------------------------------------------------------------
// <copyright file="ThemeOption.cs" company="Budget Experiment">
//     Copyright (c) Budget Experiment. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Represents a theme option for display.
/// </summary>
/// <param name="Value">The theme value.</param>
/// <param name="Label">The display label.</param>
/// <param name="Icon">The icon name.</param>
public sealed record ThemeOption(string Value, string Label, string Icon);
