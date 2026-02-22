// <copyright file="GoogleClaimMapperTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Security.Claims;

using BudgetExperiment.Api.Authentication;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Unit tests for <see cref="GoogleClaimMapper"/> claim mapping logic.
/// Google ID tokens use standard OIDC claims but lack <c>preferred_username</c>
/// which the application's <c>UserContext</c> requires for the Username property.
/// </summary>
public sealed class GoogleClaimMapperTests
{
    /// <summary>
    /// Maps email to <c>preferred_username</c> when it is missing.
    /// </summary>
    [Fact]
    public void MapClaims_AddsPreferredUsername_FromEmail()
    {
        // Arrange
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", "google-uid-123"),
            new Claim("email", "user@gmail.com"),
            new Claim("name", "Test User"),
        ], "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // Act
        GoogleClaimMapper.MapClaims(principal);

        // Assert
        var preferredUsername = identity.FindFirst("preferred_username");
        Assert.NotNull(preferredUsername);
        Assert.Equal("user@gmail.com", preferredUsername.Value);
    }

    /// <summary>
    /// Does not overwrite existing <c>preferred_username</c> claim.
    /// </summary>
    [Fact]
    public void MapClaims_DoesNotOverwrite_ExistingPreferredUsername()
    {
        // Arrange
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", "google-uid-123"),
            new Claim("email", "user@gmail.com"),
            new Claim("preferred_username", "existing-username"),
        ], "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // Act
        GoogleClaimMapper.MapClaims(principal);

        // Assert
        var claims = identity.FindAll("preferred_username").ToList();
        Assert.Single(claims);
        Assert.Equal("existing-username", claims[0].Value);
    }

    /// <summary>
    /// Does nothing when email claim is missing.
    /// </summary>
    [Fact]
    public void MapClaims_DoesNothing_WhenEmailMissing()
    {
        // Arrange
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", "google-uid-123"),
            new Claim("name", "Test User"),
        ], "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // Act
        GoogleClaimMapper.MapClaims(principal);

        // Assert
        Assert.Null(identity.FindFirst("preferred_username"));
    }

    /// <summary>
    /// Handles null principal gracefully.
    /// </summary>
    [Fact]
    public void MapClaims_HandlesNullPrincipal()
    {
        // Act & Assert — should not throw
        GoogleClaimMapper.MapClaims(null);
    }

    /// <summary>
    /// Handles principal with no identity gracefully.
    /// </summary>
    [Fact]
    public void MapClaims_HandlesNoIdentity()
    {
        // Arrange
        var principal = new ClaimsPrincipal();

        // Act & Assert — should not throw
        GoogleClaimMapper.MapClaims(principal);
    }

    /// <summary>
    /// Preserves all existing claims when mapping.
    /// </summary>
    [Fact]
    public void MapClaims_PreservesExistingClaims()
    {
        // Arrange
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", "google-uid-123"),
            new Claim("email", "user@gmail.com"),
            new Claim("name", "Test User"),
            new Claim("picture", "https://example.com/photo.jpg"),
        ], "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // Act
        GoogleClaimMapper.MapClaims(principal);

        // Assert
        Assert.Equal("google-uid-123", identity.FindFirst("sub")?.Value);
        Assert.Equal("user@gmail.com", identity.FindFirst("email")?.Value);
        Assert.Equal("Test User", identity.FindFirst("name")?.Value);
        Assert.Equal("https://example.com/photo.jpg", identity.FindFirst("picture")?.Value);
    }

    /// <summary>
    /// Does not add <c>preferred_username</c> when email is empty.
    /// </summary>
    [Fact]
    public void MapClaims_DoesNotAddPreferredUsername_WhenEmailEmpty()
    {
        // Arrange
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", "google-uid-123"),
            new Claim("email", string.Empty),
        ], "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // Act
        GoogleClaimMapper.MapClaims(principal);

        // Assert
        Assert.Null(identity.FindFirst("preferred_username"));
    }
}
