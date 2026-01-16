// <copyright file="IUserContext.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Provides access to the current authenticated user's identity and claims.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Gets the unique identifier of the current user (from the 'sub' claim).
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// Gets the username of the current user (from the 'preferred_username' claim).
    /// </summary>
    string Username { get; }

    /// <summary>
    /// Gets the email address of the current user (from the 'email' claim).
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets the display name of the current user (from the 'name' claim).
    /// </summary>
    string? DisplayName { get; }

    /// <summary>
    /// Gets the avatar URL of the current user (from the 'picture' claim).
    /// </summary>
    string? AvatarUrl { get; }

    /// <summary>
    /// Gets a value indicating whether the current request is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}
