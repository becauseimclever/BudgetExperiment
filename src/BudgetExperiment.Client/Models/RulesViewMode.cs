// <copyright file="RulesViewMode.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Models;

/// <summary>
/// The view mode for the rules listing display.
/// </summary>
public enum RulesViewMode
{
    /// <summary>Compact table view with sortable columns.</summary>
    Table,

    /// <summary>Card view showing each rule as a card.</summary>
    Card,
}
