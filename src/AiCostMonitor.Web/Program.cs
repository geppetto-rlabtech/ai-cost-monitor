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
builder.Services.AddHttpClient("api", client =>
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]
        ?? builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("api"));

// ── MudBlazor ────────────────────────────────────────────────────
builder.Services.AddMudServices();

await builder.Build().RunAsync();
