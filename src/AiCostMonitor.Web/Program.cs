using AiCostMonitor.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ── Auth (Keycloak OIDC) ─────────────────────────────────────────
builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("Oidc", options.ProviderOptions);
    options.ProviderOptions.ResponseType = "code";
});

// ── HTTP Client with auth ────────────────────────────────────────
// BaseAddressAuthorizationMessageHandler allega il token SOLO verso lo stesso origin dell'app.
// L'API è su una porta diversa (5000 vs 3000), quindi serve AuthorizationMessageHandler
// configurato esplicitamente con l'URL dell'API.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddHttpClient("api", client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler(sp => sp
        .GetRequiredService<AuthorizationMessageHandler>()
        .ConfigureHandler(authorizedUrls: [apiBaseUrl]));

builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("api"));

// ── MudBlazor ────────────────────────────────────────────────────
builder.Services.AddMudServices();

await builder.Build().RunAsync();
