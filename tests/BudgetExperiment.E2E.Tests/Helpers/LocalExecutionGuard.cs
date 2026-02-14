// <copyright file="LocalExecutionGuard.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.E2E.Tests.Helpers;

/// <summary>
/// Guard utilities for environment-sensitive E2E tests.
/// </summary>
public static class LocalExecutionGuard
{
    /// <summary>
    /// Skips the current test when the target base URL is not local.
    /// </summary>
    /// <param name="baseUrl">Current test base URL.</param>
    public static bool IsLocalBaseUrl(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase);
    }
}
