// <copyright file="BudgetScopeMiddleware.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Api.Middleware;

/// <summary>
/// Middleware that reads the X-Budget-Scope header from incoming requests
/// and sets the scope on the current IUserContext.
/// </summary>
public sealed class BudgetScopeMiddleware
{
    /// <summary>
    /// The header name for budget scope.
    /// </summary>
    public const string ScopeHeaderName = "X-Budget-Scope";

    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetScopeMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next request delegate.</param>
    public BudgetScopeMiddleware(RequestDelegate next)
    {
        this._next = next;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="userContext">The user context.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task InvokeAsync(HttpContext context, IUserContext userContext)
    {
        if (context.Request.Headers.TryGetValue(ScopeHeaderName, out var scopeHeader))
        {
            var scopeValue = scopeHeader.FirstOrDefault();
            if (!string.IsNullOrEmpty(scopeValue))
            {
                BudgetScope? scope = scopeValue.ToUpperInvariant() switch
                {
                    "SHARED" => BudgetScope.Shared,
                    "PERSONAL" => BudgetScope.Personal,
                    "ALL" => null,
                    _ => null,
                };

                userContext.SetScope(scope);
            }
        }

        // If no header is provided, CurrentScope remains null (All scopes)
        await this._next(context);
    }
}
