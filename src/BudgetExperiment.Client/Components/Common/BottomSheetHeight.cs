// <copyright file="BottomSheetHeight.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Common;

/// <summary>
/// Bottom sheet height options for mobile presentation.
/// </summary>
public enum BottomSheetHeight
{
    /// <summary>Small height (40% of viewport).</summary>
    Small,

    /// <summary>Medium height (60% of viewport, default).</summary>
    Medium,

    /// <summary>Large height (80% of viewport).</summary>
    Large,

    /// <summary>Full screen (90% of viewport, leaving header visible).</summary>
    FullScreen,
}
