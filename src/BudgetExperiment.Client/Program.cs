using BudgetExperiment.Client;
using BudgetExperiment.Client.Services;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure OIDC authentication with Authentik
builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("Authentication:Authentik", options.ProviderOptions);
    options.ProviderOptions.ResponseType = "code";
    options.ProviderOptions.DefaultScopes.Add("openid");
    options.ProviderOptions.DefaultScopes.Add("profile");
    options.ProviderOptions.DefaultScopes.Add("email");
});

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
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<VersionService>();

await builder.Build().RunAsync();
