#pragma warning disable SA1633 // File header temporarily disabled
using BudgetExperiment.Client;
using BudgetExperiment.Client.Api;
using BudgetExperiment.Client.Services;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add FluentUI services
builder.Services.AddFluentUIComponents();

// Add API client services
builder.Services.AddScoped<IBudgetApiClient, BudgetApiClient>();
builder.Services.AddScoped<ICsvImportApi, CsvImportApi>();

// Add calendar data service
builder.Services.AddScoped<CalendarDataService>();

await builder.Build().RunAsync();
#pragma warning restore SA1633
