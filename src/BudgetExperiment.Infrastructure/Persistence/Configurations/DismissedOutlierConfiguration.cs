// <copyright file="DismissedOutlierConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="DismissedOutlier"/> entity.
/// </summary>
internal sealed class DismissedOutlierConfiguration : IEntityTypeConfiguration<DismissedOutlier>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DismissedOutlier> builder)
    {
        builder.ToTable("DismissedOutliers");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Property(d => d.TransactionId)
            .IsRequired();

        builder.Property(d => d.DismissedAtUtc)
            .IsRequired();

        builder.HasIndex(d => d.TransactionId)
            .IsUnique();
    }
}
