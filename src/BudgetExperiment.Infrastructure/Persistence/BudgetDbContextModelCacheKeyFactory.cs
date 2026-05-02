// <copyright file="BudgetDbContextModelCacheKeyFactory.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BudgetExperiment.Infrastructure.Persistence;

/// <summary>
/// Provides an EF Core model cache key that differentiates encrypted and non-encrypted model variants.
/// </summary>
internal sealed class BudgetDbContextModelCacheKeyFactory : IModelCacheKeyFactory
{
    /// <inheritdoc />
    public object Create(DbContext context, bool designTime)
    {
        if (context is BudgetDbContext budgetContext)
        {
            return (context.GetType(), budgetContext.HasEncryptionService, designTime);
        }

        return (context.GetType(), designTime);
    }
}
