using AiCostMonitor.Api.Adapters;
using AiCostMonitor.Api.Data;
using AiCostMonitor.Api.Endpoints;
using AiCostMonitor.Api.Services;
using AiCostMonitor.Core.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
       .UseSnakeCaseNamingConvention());

// ── Authentication (Keycloak) ─────────────────────────────────────
// Authority/ValidIssuer = public URL (issuer in JWT issued to the browser)
// MetadataAddress = internal Docker URL (for JWKS fetch — avoids split-horizon DNS issue)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        var internalAuthority = builder.Configuration["Keycloak:Authority"]!;
        var externalAuthority = builder.Configuration["Keycloak:ExternalAuthority"] ?? internalAuthority;

        opt.Authority = externalAuthority;
        opt.MetadataAddress = internalAuthority + "/.well-known/openid-configuration";
        opt.Audience = builder.Configuration["Keycloak:ClientId"];
        opt.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = externalAuthority,
            ValidateAudience = true,
            ValidateLifetime = true,
            NameClaimType = "preferred_username",
            RoleClaimType = "roles"
        };
    });

builder.Services.AddAuthorization();

// ── CORS (allow Blazor frontend) ──────────────────────────────────
builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p =>
        p.WithOrigins(builder.Configuration["Cors:AllowedOrigins"]?.Split(',') ?? ["http://localhost:5173"])
         .AllowAnyMethod()
         .AllowAnyHeader()
         .AllowCredentials()));

// ── Services ──────────────────────────────────────────────────────
builder.Services.AddHttpClient();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddHostedService<SyncBackgroundService>();

// Provider Adapters
builder.Services.AddHttpClient<AnthropicAdapter>(c =>
    c.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddScoped<IProviderAdapter, AnthropicAdapter>();

var app = builder.Build();

// ── Migrations ────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Environment.IsDevelopment())
        await db.Database.MigrateAsync();
}

// ── Middleware ────────────────────────────────────────────────────
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// ── Endpoints ─────────────────────────────────────────────────────
app.MapProviderKeyEndpoints();
app.MapUsageEndpoints();
app.MapDashboardEndpoints();
app.MapSyncEndpoints();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();