using System.Net.Http.Json;

using BudgetExperiment.Client;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Fetch configuration from API before configuring services
using var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
ClientConfigDto? clientConfig = null;
try
{
    clientConfig = await httpClient.GetFromJsonAsync<ClientConfigDto>("api/v1/config");
}
catch (HttpRequestException)
{
    // API not available - will fall back to static config if present
}

// Register config as singleton for injection (if available)
if (clientConfig is not null)
{
    builder.Services.AddSingleton(clientConfig);
    builder.Services.AddSingleton(clientConfig.Authentication);
}

// Configure OIDC authentication
if (clientConfig?.Authentication.Mode == "oidc" && clientConfig.Authentication.Oidc is not null)
{
    // Use configuration from API
    var oidc = clientConfig.Authentication.Oidc;
    builder.Services.AddOidcAuthentication(options =>
    {
        options.ProviderOptions.Authority = oidc.Authority;
        options.ProviderOptions.ClientId = oidc.ClientId;
        options.ProviderOptions.ResponseType = oidc.ResponseType;
        options.ProviderOptions.PostLogoutRedirectUri = oidc.PostLogoutRedirectUri;
        options.ProviderOptions.RedirectUri = oidc.RedirectUri;

        foreach (var scope in oidc.Scopes)
        {
            options.ProviderOptions.DefaultScopes.Add(scope);
        }
    });
}
else
{
    // Fall back to static configuration (legacy support or API unavailable)
    builder.Services.AddOidcAuthentication(options =>
    {
        builder.Configuration.Bind("Authentication:Authentik", options.ProviderOptions);
        options.ProviderOptions.ResponseType = "code";
        options.ProviderOptions.DefaultScopes.Add("openid");
        options.ProviderOptions.DefaultScopes.Add("profile");
        options.ProviderOptions.DefaultScopes.Add("email");
    });
}

// Register ScopeService as singleton so it persists across the app lifetime
builder.Services.AddSingleton<ScopeService>();

// Register the ScopeMessageHandler as transient (DelegatingHandler instances should be transient)
builder.Services.AddTransient<ScopeMessageHandler>();

// Configure HttpClient with authorization message handler to include auth token
// and scope message handler to include X-Budget-Scope header
builder.Services.AddHttpClient(
    "BudgetApi",
    client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>()
    .AddHttpMessageHandler<ScopeMessageHandler>();

// Provide scoped HttpClient for injection (uses BudgetApi named client)
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("BudgetApi"));

builder.Services.AddScoped<IBudgetApiService, BudgetApiService>();
builder.Services.AddScoped<IAiApiService, AiApiService>();
builder.Services.AddScoped<IAiAvailabilityService, AiAvailabilityService>();
builder.Services.AddScoped<ICategorySuggestionApiService, CategorySuggestionApiService>();
builder.Services.AddScoped<IChatApiService, ChatApiService>();
builder.Services.AddScoped<IChatContextService, ChatContextService>();
builder.Services.AddScoped<IImportApiService, ImportApiService>();
builder.Services.AddScoped<IReconciliationApiService, ReconciliationApiService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<VersionService>();

await builder.Build().RunAsync();
