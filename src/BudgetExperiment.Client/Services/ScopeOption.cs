// <copyright file="ScopeOption.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Represents a scope option for the UI.
/// </summary>
/// <param name="Scope">The budget scope value (null for All).</param>
/// <param name="DisplayName">The display name.</param>
/// <param name="IconName">The icon name.</param>
/// <param name="Description">The description tooltip.</param>
public sealed record ScopeOption(BudgetScope? Scope, string DisplayName, string IconName, string Description);
