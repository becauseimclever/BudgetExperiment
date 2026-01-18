// <copyright file="UserContextTests.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

using System.Security.Claims;

using Microsoft.AspNetCore.Http;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Unit tests for <see cref="UserContext"/>.
/// </summary>
public sealed class UserContextTests
{
    /// <summary>
    /// When user is not authenticated, IsAuthenticated returns false.
    /// </summary>
    [Fact]
    public void IsAuthenticated_WhenUserNotAuthenticated_ReturnsFalse()
    {
        // Arrange
        var httpContextAccessor = CreateHttpContextAccessor(authenticated: false);
        var userContext = new UserContext(httpContextAccessor);

        // Act
        var result = userContext.IsAuthenticated;

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// When user is authenticated, IsAuthenticated returns true.
    /// </summary>
    [Fact]
    public void IsAuthenticated_WhenUserAuthenticated_ReturnsTrue()
    {
        // Arrange
        var claims = new[] { new Claim("sub", "user-123") };
        var httpContextAccessor = CreateHttpContextAccessor(authenticated: true, claims: claims);
        var userContext = new UserContext(httpContextAccessor);

        // Act
        var result = userContext.IsAuthenticated;

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// UserId extracts the 'sub' claim value.
    /// </summary>
    [Fact]
    public void UserId_ExtractsSubClaim()
    {
        // Arrange
        var claims = new[] { new Claim("sub", "user-abc-123") };
        var httpContextAccessor = CreateHttpContextAccessor(authenticated: true, claims: claims);
        var userContext = new UserContext(httpContextAccessor);

        // Act
        var result = userContext.UserId;

        // Assert
        Assert.Equal("user-abc-123", result);
    }

    /// <summary>
    /// UserId falls back to NameIdentifier claim when 'sub' is not present.
    /// </summary>
    [Fact]
    public void UserId_FallsBackToNameIdentifierClaim()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-from-nameidentifier") };
        var httpContextAccessor = CreateHttpContextAccessor(authenticated: true, claims: claims);
        var userContext = new UserContext(httpContextAccessor);

        // Act
        var result = userContext.UserId;

        // Assert
        Assert.Equal("user-from-nameidentifier", result);
    }

    /// <summary>
    /// Username extracts the 'preferred_username' claim value.
    /// </summary>
    [Fact]
    public void Username_ExtractsPreferredUsernameClaim()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("sub", "user-123"),
            new Claim("preferred_username", "johndoe"),
        };
        var httpContextAccessor = CreateHttpContextAccessor(authenticated: true, claims: claims);
        var userContext = new UserContext(httpContextAccessor);

        // Act
        var result = userContext.Username;

        // Assert
        Assert.Equal("johndoe", result);
    }

    /// <summary>
    /// Username falls back to Name claim when 'preferred_username' is not present.
    /// </summary>
    [Fact]
    public void Username_FallsBackToNameClaim()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("sub", "user-123"),
            new Claim(ClaimTypes.Name, "Jane Doe"),
        };
        var httpContextAccessor = CreateHttpContextAccessor(authenticated: true, claims: claims);
        var userContext = new UserContext(httpContextAccessor);

        // Act
        var result = userContext.Username;

        // Assert
        Assert.Equal("Jane Doe", result);
    }

    /// <summary>
    /// Email extracts the 'email' claim value.
    /// </summary>
    [Fact]
    public void Email_ExtractsEmailClaim()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("sub", "user-123"),
            new Claim("email", "john@example.com"),
        };
        var httpContextAccessor = CreateHttpContextAccessor(authenticated: true, claims: claims);
        var userContext = new UserContext(httpContextAccessor);

        // Act
        var result = userContext.Email;

        // Assert
        Assert.Equal("john@example.com", result);
    }

    /// <summary>
    /// Email falls back to ClaimTypes.Email when 'email' is not present.
    /// </summary>
    [Fact]
    public void Email_FallsBackToClaimTypesEmail()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("sub", "user-123"),
            new Claim(ClaimTypes.Email, "jane@example.com"),
        };
        var httpContextAccessor = CreateHttpContextAccessor(authenticated: true, claims: claims);
        var userContext = new UserContext(httpContextAccessor);

        // Act
        var result = userContext.Email;

        // Assert
        Assert.Equal("jane@example.com", result);
    }

    /// <summary>
    /// DisplayName extracts the 'name' claim value.
    /// </summary>
    [Fact]
    public void DisplayName_ExtractsNameClaim()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("sub", "user-123"),
            new Claim("name", "John Doe"),
        };
        var httpContextAccessor = CreateHttpContextAccessor(authenticated: true, claims: claims);
        var userContext = new UserContext(httpContextAccessor);

        // Act
        var result = userContext.DisplayName;

        // Assert
        Assert.Equal("John Doe", result);
    }

    /// <summary>
    /// AvatarUrl extracts the 'picture' claim value.
    /// </summary>
    [Fact]
    public void AvatarUrl_ExtractsPictureClaim()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("sub", "user-123"),
            new Claim("picture", "https://example.com/avatar.jpg"),
        };
        var httpContextAccessor = CreateHttpContextAccessor(authenticated: true, claims: claims);
        var userContext = new UserContext(httpContextAccessor);

        // Act
        var result = userContext.AvatarUrl;

        // Assert
        Assert.Equal("https://example.com/avatar.jpg", result);
    }

    /// <summary>
    /// When claims are missing, properties return empty string or null.
    /// </summary>
    [Fact]
    public void Properties_ReturnDefaultsWhenClaimsMissing()
    {
        // Arrange
        var httpContextAccessor = CreateHttpContextAccessor(authenticated: true, claims: Array.Empty<Claim>());
        var userContext = new UserContext(httpContextAccessor);

        // Act & Assert
        Assert.Equal(string.Empty, userContext.UserId);
        Assert.Equal(string.Empty, userContext.Username);
        Assert.Null(userContext.Email);
        Assert.Null(userContext.DisplayName);
        Assert.Null(userContext.AvatarUrl);
    }

    /// <summary>
    /// Constructor throws ArgumentNullException when httpContextAccessor is null.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsWhenHttpContextAccessorIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UserContext(null!));
    }

    /// <summary>
    /// When HttpContext is null, properties return defaults safely.
    /// </summary>
    [Fact]
    public void Properties_ReturnDefaultsWhenHttpContextIsNull()
    {
        // Arrange
        var httpContextAccessor = new HttpContextAccessor { HttpContext = null };
        var userContext = new UserContext(httpContextAccessor);

        // Act & Assert
        Assert.False(userContext.IsAuthenticated);
        Assert.Equal(string.Empty, userContext.UserId);
        Assert.Null(userContext.UserIdAsGuid);
        Assert.Equal(string.Empty, userContext.Username);
        Assert.Null(userContext.Email);
        Assert.Null(userContext.DisplayName);
        Assert.Null(userContext.AvatarUrl);
    }

    /// <summary>
    /// UserIdAsGuid returns the UserId parsed as a GUID when valid.
    /// </summary>
    [Fact]
    public void UserIdAsGuid_WhenUserIdIsValidGuid_ReturnsGuid()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid();
        var claims = new[] { new Claim("sub", expectedGuid.ToString()) };
        var httpContextAccessor = CreateHttpContextAccessor(authenticated: true, claims: claims);
        var userContext = new UserContext(httpContextAccessor);

        // Act
        var result = userContext.UserIdAsGuid;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedGuid, result.Value);
    }

    /// <summary>
    /// UserIdAsGuid derives a GUID from Authentik's 64-character hex format.
    /// </summary>
    [Fact]
    public void UserIdAsGuid_WhenAuthentikHexFormat_ReturnsDerivedGuid()
    {
        // Arrange - Real Authentik sub claim format (64 hex chars)
        var authentikSub = "2aeb3c500a39985c209121ba8aa440a1254c7533fbe5bf1ee2be8be5a5907637";
        var claims = new[] { new Claim("sub", authentikSub) };
        var httpContextAccessor = CreateHttpContextAccessor(authenticated: true, claims: claims);
        var userContext = new UserContext(httpContextAccessor);

        // Act
        var result = userContext.UserIdAsGuid;

        // Assert - Should derive GUID from first 32 hex chars: "2aeb3c500a39985c209121ba8aa440a1"
        Assert.NotNull(result);
        Assert.Equal(Guid.Parse("2aeb3c50-0a39-985c-2091-21ba8aa440a1"), result.Value);
    }

    /// <summary>
    /// UserIdAsGuid returns consistent GUID for the same Authentik sub claim.
    /// </summary>
    [Fact]
    public void UserIdAsGuid_WhenAuthentikHexFormat_ReturnsDeterministicGuid()
    {
        // Arrange
        var authentikSub = "2aeb3c500a39985c209121ba8aa440a1254c7533fbe5bf1ee2be8be5a5907637";
        var claims1 = new[] { new Claim("sub", authentikSub) };
        var claims2 = new[] { new Claim("sub", authentikSub) };
        var userContext1 = new UserContext(CreateHttpContextAccessor(authenticated: true, claims: claims1));
        var userContext2 = new UserContext(CreateHttpContextAccessor(authenticated: true, claims: claims2));

        // Act
        var result1 = userContext1.UserIdAsGuid;
        var result2 = userContext2.UserIdAsGuid;

        // Assert - Same input should always produce same GUID
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.Value, result2.Value);
    }

    /// <summary>
    /// UserIdAsGuid returns null when UserId is not a valid GUID or hex string.
    /// </summary>
    [Fact]
    public void UserIdAsGuid_WhenUserIdIsNotValidGuidOrHex_ReturnsNull()
    {
        // Arrange
        var claims = new[] { new Claim("sub", "not-a-guid-or-hex") };
        var httpContextAccessor = CreateHttpContextAccessor(authenticated: true, claims: claims);
        var userContext = new UserContext(httpContextAccessor);

        // Act
        var result = userContext.UserIdAsGuid;

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// UserIdAsGuid returns null when user is not authenticated.
    /// </summary>
    [Fact]
    public void UserIdAsGuid_WhenNotAuthenticated_ReturnsNull()
    {
        // Arrange
        var httpContextAccessor = CreateHttpContextAccessor(authenticated: false);
        var userContext = new UserContext(httpContextAccessor);

        // Act
        var result = userContext.UserIdAsGuid;

        // Assert
        Assert.Null(result);
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(bool authenticated, Claim[]? claims = null)
    {
        var httpContext = new DefaultHttpContext();

        if (authenticated)
        {
            var identity = new ClaimsIdentity(claims ?? Array.Empty<Claim>(), "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);
        }
        else
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        }

        return new HttpContextAccessor { HttpContext = httpContext };
    }
}
