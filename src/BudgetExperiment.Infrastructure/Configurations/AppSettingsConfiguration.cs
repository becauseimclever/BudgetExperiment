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

        // AI Settings
        builder.Property(s => s.AiOllamaEndpoint)
            .HasMaxLength(500)
            .HasDefaultValue("http://localhost:11434")
            .IsRequired();

        builder.Property(s => s.AiModelName)
            .HasMaxLength(100)
            .HasDefaultValue("llama3.2")
            .IsRequired();

        builder.Property(s => s.AiTemperature)
            .HasPrecision(3, 2)
            .HasDefaultValue(0.3m)
            .IsRequired();

        builder.Property(s => s.AiMaxTokens)
            .HasDefaultValue(2000)
            .IsRequired();

        builder.Property(s => s.AiTimeoutSeconds)
            .HasDefaultValue(120)
            .IsRequired();

        builder.Property(s => s.AiIsEnabled)
            .HasDefaultValue(true)
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

        // AI Settings defaults
        typeof(AppSettings).GetProperty(nameof(AppSettings.AiOllamaEndpoint))!
            .SetValue(settings, "http://localhost:11434");
        typeof(AppSettings).GetProperty(nameof(AppSettings.AiModelName))!
            .SetValue(settings, "llama3.2");
        typeof(AppSettings).GetProperty(nameof(AppSettings.AiTemperature))!
            .SetValue(settings, 0.3m);
        typeof(AppSettings).GetProperty(nameof(AppSettings.AiMaxTokens))!
            .SetValue(settings, 2000);
        typeof(AppSettings).GetProperty(nameof(AppSettings.AiTimeoutSeconds))!
            .SetValue(settings, 120);
        typeof(AppSettings).GetProperty(nameof(AppSettings.AiIsEnabled))!
            .SetValue(settings, true);

        return settings!;
    }
}
