// <copyright file="AppSettingsConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for the AppSettings entity.
/// </summary>
internal sealed class AppSettingsConfiguration : IEntityTypeConfiguration<AppSettings>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AppSettings> builder)
    {
        builder.ToTable("AppSettings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.AutoRealizePastDueItems)
            .IsRequired();

        builder.Property(s => s.PastDueLookbackDays)
            .IsRequired();

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired();

        builder.Property(s => s.UpdatedAtUtc)
            .IsRequired();

        // Seed the singleton settings record
        builder.HasData(CreateSeedData());
    }

    private static AppSettings CreateSeedData()
    {
        // Use reflection to set properties since entity has private setters
        var settings = typeof(AppSettings)
            .GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null)!
            .Invoke(null) as AppSettings;

        typeof(AppSettings).GetProperty(nameof(AppSettings.Id))!
            .SetValue(settings, AppSettings.SingletonId);
        typeof(AppSettings).GetProperty(nameof(AppSettings.AutoRealizePastDueItems))!
            .SetValue(settings, false);
        typeof(AppSettings).GetProperty(nameof(AppSettings.PastDueLookbackDays))!
            .SetValue(settings, 30);
        typeof(AppSettings).GetProperty(nameof(AppSettings.CreatedAtUtc))!
            .SetValue(settings, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        typeof(AppSettings).GetProperty(nameof(AppSettings.UpdatedAtUtc))!
            .SetValue(settings, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        return settings!;
    }
}
