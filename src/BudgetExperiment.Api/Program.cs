// <copyright file="Program.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.

using BudgetExperiment.Api;
using BudgetExperiment.Api.HealthChecks;
using BudgetExperiment.Application;
using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure;
using BudgetExperiment.Infrastructure.Persistence;
using BudgetExperiment.Infrastructure.Seeding;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Scalar.AspNetCore;

/// <summary>
/// Application entry point.
/// </summary>
public partial class Program
{
    /// <summary>
    /// Main entry point. Configures and runs the ASP.NET Core host.
    /// </summary>
    /// <param name="args">Runtime arguments.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add controllers + OpenAPI (ASP.NET Core built-in OpenAPI services).
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        // Authentication & Authorization
        builder.Services.Configure<AuthentikOptions>(builder.Configuration.GetSection(AuthentikOptions.SectionName));
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IUserContext, UserContext>();
        ConfigureAuthentication(builder.Services, builder.Configuration);

        // Application & Infrastructure
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        // Health checks: basic + database connectivity + migration status
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<BudgetDbContext>("database")
            .AddCheck<MigrationHealthCheck>("migrations");

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("dev", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        });

        var app = builder.Build();

        // Apply migrations based on configuration (defaults to enabled)
        await ApplyMigrationsAsync(app);

        // Seed data ONLY in development
        if (app.Environment.IsDevelopment())
        {
            await SeedDevelopmentDataAsync(app);
        }

        // Handle forwarded headers from reverse proxy (NGINX/Cloudflare)
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        });

        app.UseHttpsRedirection();
        app.UseCors("dev");

        // Serve Blazor WebAssembly client (static web assets from referenced Client project).
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        // Expose OpenAPI document (AspNetCore.OpenApi)
        app.MapOpenApi();
        app.MapScalarApiReference();

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Budget scope extraction from X-Budget-Scope header
        app.UseMiddleware<BudgetExperiment.Api.Middleware.BudgetScopeMiddleware>();

        app.MapControllers();
        app.MapHealthChecks("/health");

        // Fallback to index.html for client-side routes.
        app.MapFallbackToFile("index.html");

        // Custom exception handling
        app.UseMiddleware<BudgetExperiment.Api.Middleware.ExceptionHandlingMiddleware>();

        await app.RunAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Applies pending database migrations. Runs in ALL environments to ensure schema is always up-to-date.
    /// Can be disabled via configuration (Database:AutoMigrate = false).
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private static async Task ApplyMigrationsAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        if (!options.AutoMigrate)
        {
            logger.LogInformation("Automatic database migrations are disabled via configuration.");
            return;
        }

        try
        {
            // Set command timeout for migration operations
            context.Database.SetCommandTimeout(TimeSpan.FromSeconds(options.MigrationTimeoutSeconds));

            // Try to apply migrations - for relational databases
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            var pendingList = pendingMigrations.ToList();

            if (pendingList.Count > 0)
            {
                logger.LogInformation(
                    "Applying {MigrationCount} pending database migration(s): {Migrations}",
                    pendingList.Count,
                    string.Join(", ", pendingList));

                await context.Database.MigrateAsync();

                logger.LogInformation("Database migrations applied successfully.");
            }
            else
            {
                logger.LogInformation("Database is up to date. No migrations to apply.");
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("relational"))
        {
            // In-memory/non-relational database (e.g., for integration tests)
            logger.LogInformation("Non-relational database detected. Ensuring schema is created.");
            await context.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to apply database migrations. Application cannot start.");
            throw;
        }
    }

    /// <summary>
    /// Seeds the database with development/test data. Only runs in Development environment.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private static async Task SeedDevelopmentDataAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            await DatabaseSeeder.SeedAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to seed development data. Continuing startup...");

            // Don't throw - seed failure shouldn't prevent app from starting in dev
        }
    }

    /// <summary>
    /// Configures JWT Bearer authentication for Authentik OIDC integration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    private static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var authentikOptions = configuration.GetSection(AuthentikOptions.SectionName).Get<AuthentikOptions>() ?? new AuthentikOptions();

        if (string.IsNullOrWhiteSpace(authentikOptions.Authority))
        {
            throw new InvalidOperationException("'Authentication:Authentik:Authority' is not configured. Authentication is required.");
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = authentikOptions.Authority;
            options.Audience = authentikOptions.Audience;
            options.RequireHttpsMetadata = authentikOptions.RequireHttpsMetadata;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = !string.IsNullOrWhiteSpace(authentikOptions.Audience),
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(1),
            };

            // Map Authentik claims to standard .NET claims
            options.MapInboundClaims = false;
        });

        services.AddAuthorization();
    }
}
