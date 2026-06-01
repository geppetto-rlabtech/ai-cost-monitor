# AI Cost Monitor — Architectural Plan

**Date:** 2026-06-01
**Stack:** Blazor WASM PWA + ASP.NET Core Minimal API + PostgreSQL + Keycloak + Docker

---

## Goal

Multi-tenant web PWA to monitor AI provider usage costs (Anthropic, OpenAI, Mistral; Google in future). Each user inserts their own API keys and views their consumption and costs in a unified dashboard.

---

## Architecture Overview

```
[Browser / PWA]
    Blazor WASM
        │
        │ HTTPS
        ▼
[Backend — ASP.NET Core Minimal API]
    │           │           │
    ▼           ▼           ▼
[Keycloak]  [PostgreSQL]  [Provider APIs]
  OIDC auth   history       Anthropic
              + api keys    OpenAI
                            Mistral
                            (Google v3)
```

---

## Solution Structure

```
AiCostMonitor.sln
├── src/
│   ├── AiCostMonitor.Api/          ← ASP.NET Core Minimal API
│   ├── AiCostMonitor.Web/          ← Blazor WASM PWA
│   ├── AiCostMonitor.Shared/       ← Shared DTOs (Api↔Web)
│   └── AiCostMonitor.Core/         ← Domain, services, interfaces
├── tests/
│   └── AiCostMonitor.Tests/
└── docker-compose.yml
```

---

## Data Model (PostgreSQL)

```sql
-- Users (mirror from Keycloak, populated on first login)
users (
  id UUID PK,                    -- matches Keycloak subject claim
  email TEXT,
  display_name TEXT,
  created_at TIMESTAMPTZ
)

-- Provider API keys per user (encrypted at rest)
user_provider_keys (
  id UUID PK,
  user_id UUID FK → users.id,
  provider TEXT,                 -- 'anthropic' | 'openai' | 'mistral' | 'google'
  encrypted_api_key TEXT,        -- AES-256-GCM encrypted
  key_label TEXT,                -- friendly name (e.g. "Main key")
  is_active BOOL DEFAULT true,
  created_at TIMESTAMPTZ,
  last_synced_at TIMESTAMPTZ
)

-- Usage history synced from providers
usage_records (
  id UUID PK,
  user_id UUID FK → users.id,
  provider_key_id UUID FK → user_provider_keys.id,
  provider TEXT,
  model TEXT,
  period_start TIMESTAMPTZ,
  period_end TIMESTAMPTZ,
  input_tokens BIGINT,
  output_tokens BIGINT,
  cache_read_tokens BIGINT,
  cache_creation_tokens BIGINT,
  cost_usd DECIMAL(12,6),
  raw_json JSONB,                -- original provider response
  synced_at TIMESTAMPTZ
)

-- Indexes
CREATE INDEX idx_usage_user_period ON usage_records(user_id, period_start DESC);
CREATE INDEX idx_usage_provider_model ON usage_records(provider, model);
```

---

## Backend — ASP.NET Core Minimal API

### Authentication
- JWT Bearer via Keycloak (`AddAuthentication().AddJwtBearer()`)
- `ValidIssuer` = Keycloak realm URL
- `ValidAudience` = app client ID
- Claim `sub` = userId, used as key in every query

### API Endpoints

```
# Provider Keys
GET    /api/providers                      → list configured providers
POST   /api/providers/{provider}/keys      → add an API key
DELETE /api/providers/{provider}/keys/{id} → remove an API key
POST   /api/providers/{provider}/keys/{id}/test → verify key works

# Sync
POST   /api/sync                           → manual sync from all providers
POST   /api/sync/{provider}                → sync from a specific provider
GET    /api/sync/status                    → last sync status

# Usage & Costs
GET    /api/usage?from=&to=&provider=&granularity=day|month
GET    /api/usage/by-model?from=&to=
GET    /api/usage/summary

# Dashboard
GET    /api/dashboard                      → optimized payload for homepage
```

### API Key Security
- Provider API keys encrypted at rest with **AES-256-GCM**
- Encryption key in environment variable / Docker Secret (never in DB)
- Keys never returned in plain text — only `****last4`

### Sync Service
- Background service (`IHostedService`) running every 6h (configurable)
- For each user with active keys, calls providers and saves deltas
- Handles provider pagination (cursor-based for Anthropic, OpenAI)
- Rate limiting: respects provider limits (exponential backoff retry)

### Provider Adapters (Strategy pattern)

```csharp
public interface IProviderAdapter {
    string ProviderName { get; }
    Task<bool> TestKeyAsync(string apiKey);
    Task<IEnumerable<UsageRecord>> FetchUsageAsync(
        string apiKey, DateTimeOffset from, DateTimeOffset to);
}

// Implementations:
// AnthropicAdapter  → GET /v1/organizations/usage
// OpenAiAdapter     → GET /organization/usage/completions + /costs
// MistralAdapter    → GET /v1/usage
// GoogleAdapter     → BigQuery / Cloud Monitoring (phase 3)
```

---

## Frontend — Blazor WASM PWA

### Authentication
- `Microsoft.AspNetCore.Components.WebAssembly.Authentication`
- OIDC flow with Keycloak (redirect login/logout)
- JWT token automatically attached to API calls via configured `HttpClient`

### Pages / Components

```
/               → Dashboard (monthly totals, 30-day chart, top models)
/providers      → Provider management: add/remove API keys
/usage          → Detailed table with filters (period, provider, model)
/usage/by-model → Model breakdown with bar charts
/settings       → User preferences, sync frequency
```

### Charts
- **MudBlazor** for UI components
- **ApexCharts for Blazor** for charts (line chart trend, bar chart per model, pie chart per provider)

### PWA
- `manifest.json` with icons + `theme_color`
- Service Worker for installability (not offline-first — data always from backend)
- iOS meta tags (`apple-mobile-web-app-capable`)

---

## Docker Compose

```yaml
services:
  api:
    build: ./src/AiCostMonitor.Api
    environment:
      - ConnectionStrings__Postgres=...
      - Keycloak__Authority=https://keycloak.example.com/realms/myrealm
      - Encryption__Key=${ENCRYPTION_KEY}
    depends_on: [postgres]

  web:
    build: ./src/AiCostMonitor.Web
    # Blazor WASM is static → served by nginx or the Api itself

  postgres:
    image: postgres:16
    volumes:
      - pgdata:/var/lib/postgresql/data
    environment:
      POSTGRES_DB: aicostmonitor
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}

volumes:
  pgdata:
```

---

## Roadmap

### Phase 1 — MVP
- [ ] Scaffold .NET solution (Api + Web + Shared + Core)
- [ ] Keycloak configuration (realm, OIDC client)
- [ ] Working auth on Blazor WASM
- [ ] Provider key CRUD (with encryption)
- [ ] Anthropic adapter (fetch usage + sync)
- [ ] Basic dashboard with real Anthropic data
- [ ] Working Docker Compose

### Phase 2 — Multi-provider
- [ ] OpenAI adapter
- [ ] Mistral adapter
- [ ] Automatic background sync (every 6h)
- [ ] Advanced charts (trend, model breakdown)
- [ ] Spending threshold alerts

### Phase 3 — Deploy & Google
- [ ] Public domain + HTTPS (Let's Encrypt / reverse proxy)
- [ ] CSV/PDF monthly report export
- [ ] Google Vertex AI / Gemini (BigQuery export)

### Phase 4 — Mobile (optional)
- [ ] .NET MAUI Hybrid migration
- [ ] Google Play Store
- [ ] Apple App Store (requires Apple Developer Account)

---

## Key NuGet Dependencies

```xml
<!-- Api -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
<PackageReference Include="EFCore.NamingConventions" />

<!-- Web -->
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Authentication" />
<PackageReference Include="MudBlazor" />
<PackageReference Include="ApexCharts" />

<!-- Shared / Core -->
<PackageReference Include="FluentValidation" />
```

---

## Open Questions

- **Keycloak realm/client:** configure with `openid-connect`, app redirect URI, and `sub` claim as user identifier
- **Key encryption:** evaluate using **ASP.NET Core Data Protection** instead of custom AES
- **Sync timing:** every 6h by default; users can force manual sync
- **Google:** deferred to Phase 3 due to BigQuery setup complexity
- **Multi-tenant isolation:** every DB query always filters by `user_id` — enforce with EF Core Global Query Filter (`HasQueryFilter`)
