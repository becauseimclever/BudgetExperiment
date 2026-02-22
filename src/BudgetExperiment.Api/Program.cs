// <copyright file="Program.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.

using BudgetExperiment.Api;
using BudgetExperiment.Api.Authentication;
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
#pragma warning disable CS0618 // AuthentikOptions is obsolete but still registered for backward compat
        builder.Services.Configure<AuthentikOptions>(builder.Configuration.GetSection(AuthentikOptions.SectionName));
#pragma warning restore CS0618
        builder.Services.Configure<AuthenticationOptions>(builder.Configuration.GetSection(AuthenticationOptions.SectionName));
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IUserContext, UserContext>();
        ConfigureAuthentication(builder.Services, builder.Configuration);

        // Client configuration (exposed via /api/v1/config endpoint)
        ConfigureClientConfig(builder.Services, builder.Configuration);

        // Application & Infrastructure
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        // Request timeouts for long-running AI operations
        builder.Services.AddRequestTimeouts(options =>
        {
            // Default timeout for most endpoints
            options.DefaultPolicy = new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy
            {
                Timeout = TimeSpan.FromSeconds(30),
            };

            // Extended timeout for AI endpoints (matches Ollama timeout + buffer)
            options.AddPolicy("AiAnalysis", new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy
            {
                Timeout = TimeSpan.FromMinutes(5),
                WriteTimeoutResponse = async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        message = "AI analysis request timed out.",
                        suggestion = "The AI service took too long to respond. Try again or check AI settings.",
                    });
                },
            });
        });

        // Health checks: basic + database connectivity + migration status
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<BudgetDbContext>("database")
            .AddCheck<MigrationHealthCheck>("migrations");

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("dev", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        });

        var app = builder.Build();

        // Log warning when authentication is disabled
        LogAuthModeWarning(app);

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

        // Request timeouts middleware - must be before endpoints
        app.UseRequestTimeouts();

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
    /// Configures authentication based on the resolved mode and provider.
    /// Supports: Mode=None (auth off), Mode=OIDC with Provider=Authentik (default).
    /// Maintains backward compatibility with existing <c>Authentication:Authentik:*</c> config keys.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    private static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var effectiveMode = AuthenticationOptions.ResolveEffectiveMode(configuration);

        if (string.Equals(effectiveMode, AuthModeConstants.None, StringComparison.OrdinalIgnoreCase))
        {
            // Auth-off mode — register NoAuthHandler so every request is
            // automatically authenticated as the well-known "Family" user.
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = NoAuthHandler.SchemeName;
                options.DefaultChallengeScheme = NoAuthHandler.SchemeName;
            })
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, NoAuthHandler>(
                NoAuthHandler.SchemeName, _ => { });

            services.AddAuthorization();
            return;
        }

        // Mode=OIDC — resolve provider and configure JWT Bearer
        var authOptions = new AuthenticationOptions();
        configuration.GetSection(AuthenticationOptions.SectionName).Bind(authOptions);

        ConfigureOidcAuthentication(services, authOptions);
    }

    /// <summary>
    /// Configures OIDC (JWT Bearer) authentication for the resolved provider.
    /// Supports Authentik (default), Google, Microsoft Entra ID, and generic OIDC (Keycloak, Auth0, Okta, etc.).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="authOptions">The bound authentication options.</param>
    private static void ConfigureOidcAuthentication(IServiceCollection services, AuthenticationOptions authOptions)
    {
        // Resolve authority and audience based on provider
        var (authority, audience, requireHttps) = AuthenticationOptions.ResolveProviderSettings(authOptions);

        AuthenticationOptions.ValidateOidcAuthority(authority);

        var isGoogleProvider = string.Equals(
            authOptions.Provider, AuthProviderConstants.Google, StringComparison.OrdinalIgnoreCase);
        var isMicrosoftProvider = string.Equals(
            authOptions.Provider, AuthProviderConstants.Microsoft, StringComparison.OrdinalIgnoreCase);
        var isGenericOidcProvider = string.Equals(
            authOptions.Provider, AuthProviderConstants.Oidc, StringComparison.OrdinalIgnoreCase);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = audience;
            options.RequireHttpsMetadata = requireHttps;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(1),
            };

            options.MapInboundClaims = false;

            // Provider-specific claim mapping for tokens that lack preferred_username
            if (isGoogleProvider)
            {
                options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        GoogleClaimMapper.MapClaims(context.Principal);
                        return Task.CompletedTask;
                    },
                };
            }
            else if (isMicrosoftProvider)
            {
                options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        MicrosoftClaimMapper.MapClaims(context.Principal);
                        return Task.CompletedTask;
                    },
                };
            }
            else if (isGenericOidcProvider)
            {
                var claimMappings = authOptions.Oidc.ClaimMappings;
                options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        GenericOidcClaimMapper.MapClaims(context.Principal, claimMappings);
                        return Task.CompletedTask;
                    },
                };
            }
        });

        services.AddAuthorization();
    }

    /// <summary>
    /// Configures client configuration options for the /api/v1/config endpoint.
    /// Resolves OIDC settings from the active provider (Authentik, Google, etc.)
    /// using <see cref="AuthenticationOptions.ResolveProviderSettings"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    private static void ConfigureClientConfig(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ClientConfigOptions>(options =>
        {
            // Resolve mode using the shared logic (handles legacy Enabled flag)
            var effectiveMode = AuthenticationOptions.ResolveEffectiveMode(configuration);
            options.AuthMode = string.Equals(effectiveMode, AuthModeConstants.None, StringComparison.OrdinalIgnoreCase)
                ? "none"
                : "oidc";

            // Resolve OIDC settings from the active provider
            var authOptions = new AuthenticationOptions();
            configuration.GetSection(AuthenticationOptions.SectionName).Bind(authOptions);

            var (authority, clientId, _) = AuthenticationOptions.ResolveProviderSettings(authOptions);
            options.OidcAuthority = authority;

            // For Authentik, ClientId can be explicitly set or fall back to Audience
            if (string.Equals(authOptions.Provider, AuthProviderConstants.Authentik, StringComparison.OrdinalIgnoreCase))
            {
                var authentikSection = configuration.GetSection("Authentication:Authentik");
                options.OidcClientId = authentikSection.GetValue<string>("ClientId")
                    ?? authentikSection.GetValue<string>("Audience")
                    ?? string.Empty;
            }
            else if (string.Equals(authOptions.Provider, AuthProviderConstants.Oidc, StringComparison.OrdinalIgnoreCase))
            {
                // Generic OIDC: ClientId is separate from Audience (which is used for JWT validation)
                options.OidcClientId = authOptions.Oidc.ClientId;
            }
            else
            {
                options.OidcClientId = clientId;
            }

            // Apply any explicit ClientConfig overrides
            var clientSection = configuration.GetSection(ClientConfigOptions.SectionName);
            if (clientSection.Exists())
            {
                clientSection.Bind(options);
            }
        });
    }

    /// <summary>
    /// Logs a startup warning when authentication is disabled (<c>Mode=None</c>).
    /// </summary>
    /// <param name="app">The web application.</param>
    private static void LogAuthModeWarning(WebApplication app)
    {
        var effectiveMode = AuthenticationOptions.ResolveEffectiveMode(app.Configuration);

        if (string.Equals(effectiveMode, AuthModeConstants.None, StringComparison.OrdinalIgnoreCase))
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogWarning(
                "\u26a0\ufe0f Authentication is DISABLED. All requests are treated as authenticated. " +
                "Do NOT expose this instance to the internet. " +
                "Set 'Authentication:Mode' to 'OIDC' to enable authentication.");
        }
    }
}
