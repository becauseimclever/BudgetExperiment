// <copyright file="NoAuthHandlerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Security.Claims;

using BudgetExperiment.Api.Authentication;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Unit tests for <see cref="NoAuthHandler"/>.
/// </summary>
public sealed class NoAuthHandlerTests
{
    /// <summary>
    /// HandleAuthenticateAsync always returns a success result.
    /// </summary>
    [Fact]
    public async Task HandleAuthenticateAsync_ReturnsSuccess()
    {
        // Arrange
        var result = await AuthenticateWithNoAuthHandlerAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Ticket);
    }

    /// <summary>
    /// The authentication ticket uses the <see cref="NoAuthHandler.SchemeName"/> scheme.
    /// </summary>
    [Fact]
    public async Task HandleAuthenticateAsync_UsesNoAuthScheme()
    {
        // Arrange / Act
        var result = await AuthenticateWithNoAuthHandlerAsync();

        // Assert
        Assert.Equal(NoAuthHandler.SchemeName, result.Ticket!.AuthenticationScheme);
    }

    /// <summary>
    /// The principal identity is authenticated (not anonymous).
    /// </summary>
    [Fact]
    public async Task HandleAuthenticateAsync_IdentityIsAuthenticated()
    {
        // Arrange / Act
        var result = await AuthenticateWithNoAuthHandlerAsync();
        var identity = result.Principal!.Identity as ClaimsIdentity;

        // Assert
        Assert.NotNull(identity);
        Assert.True(identity.IsAuthenticated);
        Assert.Equal(NoAuthHandler.SchemeName, identity.AuthenticationType);
    }

    /// <summary>
    /// The principal contains the <see cref="FamilyUserContext.FamilyUserId"/> as the NameIdentifier claim.
    /// </summary>
    [Fact]
    public async Task HandleAuthenticateAsync_ContainsNameIdentifierClaim()
    {
        // Arrange / Act
        var result = await AuthenticateWithNoAuthHandlerAsync();
        var nameId = result.Principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Assert
        Assert.Equal(FamilyUserContext.FamilyUserId.ToString(), nameId);
    }

    /// <summary>
    /// The principal contains the <see cref="FamilyUserContext.FamilyUserId"/> as the "sub" claim.
    /// </summary>
    [Fact]
    public async Task HandleAuthenticateAsync_ContainsSubClaim()
    {
        // Arrange / Act
        var result = await AuthenticateWithNoAuthHandlerAsync();
        var sub = result.Principal!.FindFirst("sub")?.Value;

        // Assert
        Assert.Equal(FamilyUserContext.FamilyUserId.ToString(), sub);
    }

    /// <summary>
    /// The principal contains the <see cref="FamilyUserContext.FamilyUserName"/> as the Name claim.
    /// </summary>
    [Fact]
    public async Task HandleAuthenticateAsync_ContainsNameClaim()
    {
        // Arrange / Act
        var result = await AuthenticateWithNoAuthHandlerAsync();
        var name = result.Principal!.FindFirst(ClaimTypes.Name)?.Value;

        // Assert
        Assert.Equal(FamilyUserContext.FamilyUserName, name);
    }

    /// <summary>
    /// The principal contains the <see cref="FamilyUserContext.FamilyUserEmail"/> as the Email claim.
    /// </summary>
    [Fact]
    public async Task HandleAuthenticateAsync_ContainsEmailClaim()
    {
        // Arrange / Act
        var result = await AuthenticateWithNoAuthHandlerAsync();
        var email = result.Principal!.FindFirst(ClaimTypes.Email)?.Value;

        // Assert
        Assert.Equal(FamilyUserContext.FamilyUserEmail, email);
    }

    /// <summary>
    /// The principal contains the <see cref="FamilyUserContext.FamilyUserName"/> as the "name" claim
    /// (used by <see cref="UserContext.DisplayName"/>).
    /// </summary>
    [Fact]
    public async Task HandleAuthenticateAsync_ContainsShortNameClaim()
    {
        // Arrange / Act
        var result = await AuthenticateWithNoAuthHandlerAsync();
        var name = result.Principal!.FindFirst("name")?.Value;

        // Assert
        Assert.Equal(FamilyUserContext.FamilyUserName, name);
    }

    /// <summary>
    /// The principal contains the <see cref="FamilyUserContext.FamilyUserName"/> as the "preferred_username" claim
    /// (used by <see cref="UserContext.Username"/>).
    /// </summary>
    [Fact]
    public async Task HandleAuthenticateAsync_ContainsPreferredUsernameClaim()
    {
        // Arrange / Act
        var result = await AuthenticateWithNoAuthHandlerAsync();
        var preferredUsername = result.Principal!.FindFirst("preferred_username")?.Value;

        // Assert
        Assert.Equal(FamilyUserContext.FamilyUserName, preferredUsername);
    }

    /// <summary>
    /// The scheme name constant is "NoAuth".
    /// </summary>
    [Fact]
    public void SchemeName_IsNoAuth()
    {
        Assert.Equal("NoAuth", NoAuthHandler.SchemeName);
    }

    /// <summary>
    /// Invokes the <see cref="NoAuthHandler"/> via the ASP.NET Core authentication services
    /// to exercise <c>HandleAuthenticateAsync</c> without using reflection.
    /// </summary>
    private static async Task<AuthenticateResult> AuthenticateWithNoAuthHandlerAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = NoAuthHandler.SchemeName;
            options.DefaultChallengeScheme = NoAuthHandler.SchemeName;
        })
        .AddScheme<AuthenticationSchemeOptions, NoAuthHandler>(
            NoAuthHandler.SchemeName, _ => { });

        var serviceProvider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };

        var authService = serviceProvider.GetRequiredService<IAuthenticationService>();
        return await authService.AuthenticateAsync(httpContext, NoAuthHandler.SchemeName);
    }
}
