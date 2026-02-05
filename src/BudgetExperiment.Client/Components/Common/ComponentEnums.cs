// <copyright file="ComponentEnums.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Common;

/// <summary>
/// Modal size options.
/// </summary>
public enum ModalSize
{
    /// <summary>Small modal (300px).</summary>
    Small,

    /// <summary>Medium modal (450px, default).</summary>
    Medium,

    /// <summary>Large modal (600px).</summary>
    Large,
}

/// <summary>
/// Spinner size options.
/// </summary>
public enum SpinnerSize
{
    /// <summary>Small spinner (20px).</summary>
    Small,

    /// <summary>Medium spinner (32px, default).</summary>
    Medium,

    /// <summary>Large spinner (48px).</summary>
    Large,
}

/// <summary>
/// Button size options.
/// </summary>
public enum ButtonSize
{
    /// <summary>Small button.</summary>
    Small,

    /// <summary>Medium button (default).</summary>
    Medium,

    /// <summary>Large button.</summary>
    Large,
}

/// <summary>
/// Button variant/style options.
/// </summary>
public enum ButtonVariant
{
    /// <summary>Primary action button.</summary>
    Primary,

    /// <summary>Secondary action button.</summary>
    Secondary,

    /// <summary>Success/positive action button.</summary>
    Success,

    /// <summary>Danger/destructive action button.</summary>
    Danger,

    /// <summary>Warning action button.</summary>
    Warning,

    /// <summary>Ghost/minimal button.</summary>
    Ghost,

    /// <summary>Outline button.</summary>
    Outline,
}

/// <summary>
/// Badge variant/style options.
/// </summary>
public enum BadgeVariant
{
    /// <summary>Default badge style.</summary>
    Default,

    /// <summary>Success badge.</summary>
    Success,

    /// <summary>Warning badge.</summary>
    Warning,

    /// <summary>Danger badge.</summary>
    Danger,

    /// <summary>Info badge.</summary>
    Info,
}

/// <summary>
/// Badge size options.
/// </summary>
public enum BadgeSize
{
    /// <summary>Small badge.</summary>
    Small,

    /// <summary>Medium badge (default).</summary>
    Medium,

    /// <summary>Large badge.</summary>
    Large,
}

/// <summary>
/// Alert variant/style options.
/// </summary>
public enum AlertVariant
{
    /// <summary>Informational alert.</summary>
    Info,

    /// <summary>Success alert.</summary>
    Success,

    /// <summary>Warning alert.</summary>
    Warning,

    /// <summary>Danger/error alert.</summary>
    Danger,
}

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
