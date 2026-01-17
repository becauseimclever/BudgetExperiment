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

// Configure HttpClient with authorization message handler to include auth token
builder.Services.AddHttpClient(
    "BudgetApi",
    client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

// Provide scoped HttpClient for injection (uses BudgetApi named client)
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("BudgetApi"));

builder.Services.AddScoped<IBudgetApiService, BudgetApiService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<ScopeService>();

await builder.Build().RunAsync();
