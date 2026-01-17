// <copyright file="UserDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for the current authenticated user's profile.
/// </summary>
public sealed class UserProfileDto
{
    /// <summary>Gets or sets the user ID.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the username.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Gets or sets the email address.</summary>
    public string? Email { get; set; }

    /// <summary>Gets or sets the display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the avatar URL.</summary>
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// DTO for user settings.
/// </summary>
public sealed class UserSettingsDto
{
    /// <summary>Gets or sets the user ID.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the default budget scope ("Shared", "Personal", or null for All).</summary>
    public string DefaultScope { get; set; } = "Shared";

    /// <summary>Gets or sets a value indicating whether past-due items are auto-realized.</summary>
    public bool AutoRealizePastDueItems { get; set; }

    /// <summary>Gets or sets the number of days to look back for past-due items.</summary>
    public int PastDueLookbackDays { get; set; } = 30;

    /// <summary>Gets or sets the preferred currency code.</summary>
    public string? PreferredCurrency { get; set; }

    /// <summary>Gets or sets the user's time zone ID (IANA format).</summary>
    public string? TimeZoneId { get; set; }
}

/// <summary>
/// DTO for updating user settings.
/// </summary>
public sealed class UserSettingsUpdateDto
{
    /// <summary>Gets or sets the default budget scope ("Shared", "Personal", or null for All).</summary>
    public string? DefaultScope { get; set; }

    /// <summary>Gets or sets a value indicating whether past-due items are auto-realized.</summary>
    public bool? AutoRealizePastDueItems { get; set; }

    /// <summary>Gets or sets the number of days to look back for past-due items.</summary>
    public int? PastDueLookbackDays { get; set; }

    /// <summary>Gets or sets the preferred currency code.</summary>
    public string? PreferredCurrency { get; set; }

    /// <summary>Gets or sets the user's time zone ID (IANA format).</summary>
    public string? TimeZoneId { get; set; }
}

/// <summary>
/// DTO for the current user's scope selection.
/// </summary>
public sealed class ScopeDto
{
    /// <summary>Gets or sets the current scope ("Shared", "Personal", or null for All).</summary>
    public string? Scope { get; set; }
}
