// <copyright file="UserProfileDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for the current authenticated user's profile.
/// </summary>
public sealed class UserProfileDto
{
    /// <summary>Gets or sets the user ID.</summary>
    public Guid UserId
    {
        get; set;
    }

    /// <summary>Gets or sets the username.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Gets or sets the email address.</summary>
    public string? Email
    {
        get; set;
    }

    /// <summary>Gets or sets the display name.</summary>
    public string? DisplayName
    {
        get; set;
    }

    /// <summary>Gets or sets the avatar URL.</summary>
    public string? AvatarUrl
    {
        get; set;
    }
}
