// <copyright file="Program.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.

using BudgetExperiment.Application;
using BudgetExperiment.Infrastructure;

using Microsoft.EntityFrameworkCore;

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

        // Application & Infrastructure
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddHealthChecks();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("dev", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        });

        var app = builder.Build();

        // Apply migrations and seed data in development
        if (app.Environment.IsDevelopment())
        {
            await InitializeDatabaseAsync(app);
        }

        app.UseHttpsRedirection();
        app.UseCors("dev");

        // Serve Blazor WebAssembly client (static web assets from referenced Client project).
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        // Expose OpenAPI document (AspNetCore.OpenApi)
        app.MapOpenApi();
        app.MapScalarApiReference();

        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/health");

        // Fallback to index.html for client-side routes.
        app.MapFallbackToFile("index.html");

        // Custom exception handling
        app.UseMiddleware<BudgetExperiment.Api.Middleware.ExceptionHandlingMiddleware>();

        await app.RunAsync().ConfigureAwait(false);
    }

    private static async Task InitializeDatabaseAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BudgetDbContext>>();

        try
        {
            logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");

            await DatabaseSeeder.SeedAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }
}
