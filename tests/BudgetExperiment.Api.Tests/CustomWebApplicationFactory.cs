using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;

using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Custom factory for API integration tests using an in-memory database.
/// Uses test authentication that auto-authenticates all requests.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// The test user ID used for authenticated requests.
    /// </summary>
    public static readonly Guid TestUserId = new("22222222-2222-2222-2222-222222222222");

    /// <summary>
    /// The test username used for authenticated requests.
    /// </summary>
    public const string TestUsername = "integrationuser";

    private readonly string _dbName = "TestDb_" + Guid.NewGuid().ToString();

    /// <summary>Creates an <see cref="HttpClient"/> for the API.</summary>
    /// <returns>Client.</returns>
    public HttpClient CreateApiClient()
    {
        var client = this.CreateClient();

        // Auto-authenticate all requests
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestAuto", "authenticated");
        return client;
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide a dummy connection string to satisfy Infrastructure DI validation
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AppDb"] = "Host=localhost;Database=test",
                ["Authentication:Authentik:Enabled"] = "true",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the Npgsql-configured DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<BudgetDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Remove existing IUnitOfWork registration
            var uowDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IUnitOfWork));
            if (uowDescriptor != null)
            {
                services.Remove(uowDescriptor);
            }

            // Build a separate internal service provider for InMemory only
            var inMemoryServiceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            // Create a fresh DbContext with only InMemory using isolated provider
            services.AddDbContext<BudgetDbContext>(options =>
            {
                options.UseInMemoryDatabase(this._dbName)
                       .UseInternalServiceProvider(inMemoryServiceProvider);
            });

            // Register IUnitOfWork for the test DbContext
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BudgetDbContext>());

            // Override authentication with auto-authenticating test scheme
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestAuto";
                options.DefaultChallengeScheme = "TestAuto";
            })
            .AddScheme<AuthenticationSchemeOptions, AutoAuthenticatingTestHandler>("TestAuto", options => { });
        });
    }
}

/// <summary>
/// Test authentication handler that auto-authenticates all requests.
/// Used for backward-compatible integration tests that don't need to test auth specifically.
/// </summary>
public sealed class AutoAuthenticatingTestHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AutoAuthenticatingTestHandler"/> class.
    /// </summary>
    /// <param name="options">The authentication scheme options.</param>
    /// <param name="logger">The logger factory.</param>
    /// <param name="encoder">The URL encoder.</param>
    public AutoAuthenticatingTestHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Auto-authenticate all requests with a test user
        var claims = new[]
        {
            new Claim("sub", CustomWebApplicationFactory.TestUserId.ToString()),
            new Claim("preferred_username", CustomWebApplicationFactory.TestUsername),
            new Claim(ClaimTypes.NameIdentifier, CustomWebApplicationFactory.TestUserId.ToString()),
            new Claim(ClaimTypes.Name, CustomWebApplicationFactory.TestUsername),
        };

        var identity = new ClaimsIdentity(claims, "TestAuto");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestAuto");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
