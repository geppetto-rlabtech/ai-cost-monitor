# CLAUDE.md

> Project context and conventions for the AI assistant working on this repository.

---

## Project: AI Cost Monitor

Multi-tenant PWA to monitor AI provider costs (Anthropic, OpenAI, Mistral) in a single dashboard. Each user manages their own API keys and views their usage and spending in real time.

---

## Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core Minimal API (.NET 9) |
| Frontend | Blazor WASM PWA |
| UI Components | MudBlazor |
| Charts | ApexCharts for Blazor |
| Database | PostgreSQL 16 (EF Core + Npgsql) |
| Auth | Keycloak (OIDC / JWT Bearer) |
| Deploy | Docker Compose |
| Future | .NET MAUI Hybrid → App Store |

---

## Solution Structure

```
AiCostMonitor.sln
├── src/
│   ├── AiCostMonitor.Api/        ← ASP.NET Core Minimal API
│   ├── AiCostMonitor.Web/        ← Blazor WASM PWA
│   ├── AiCostMonitor.Shared/     ← Shared DTOs
│   └── AiCostMonitor.Core/       ← Domain, services, interfaces
├── tests/
│   └── AiCostMonitor.Tests/
├── docker-compose.yml
├── docker-compose.override.yml   ← local configuration
└── CLAUDE.md
```

---

## Development Conventions

### General
- Code language: **English** (class names, methods, variables, technical comments)
- C# style: file-scoped namespaces, primary constructors, target-typed new
- EF Core: snake_case for table/column names (`EFCore.NamingConventions`)
- Every DB query **must** filter by `userId` — use Global Query Filter on all user-owned entities

### Branch Strategy
- `main` — stable, deployable
- `develop` — feature integration
- `feature/<name>` — individual features
- `fix/<name>` — bug fixes

### Commit Convention
```
feat: short description
fix: short description
chore: short description
docs: short description
```

### Security (mandatory rules)
- Provider API keys are **always encrypted at rest** (AES-256-GCM or ASP.NET Data Protection)
- Keys are **never** returned in plain text in API responses — only `****last4`
- Encryption key lives in environment variable / Docker Secret, **never in DB or source code**
- No secrets in commits (use `.env` + `.gitignore`)

---

## Supported AI Providers

| Provider | Usage endpoint | Auth required | Status |
|---|---|---|---|
| Anthropic | `GET /v1/organizations/usage` | Admin API Key (`sk-ant-admin...`) | Phase 1 |
| OpenAI | `GET /organization/usage/completions` + `/costs` | Admin API Key (`sk-admin-...`) | Phase 2 |
| Mistral | `GET /v1/usage` | API Key (admin scope) | Phase 2 |
| Google (Vertex/Gemini) | BigQuery export + Cloud Monitoring | OAuth2 + IAM `billing.viewer` | Phase 3 |

---

## Authentication

- **Keycloak** as Identity Provider
- Flow: OIDC Authorization Code + PKCE (Blazor WASM)
- Claim `sub` = unique user identifier in the app
- JWT validated server-side with `AddJwtBearer`

---

## Roadmap

### ✅ Phase 0 — Planning (done)
- Architecture defined
- Stack chosen
- Plan written

### 🚧 Phase 1 — MVP
- [ ] Scaffold .NET solution
- [ ] Keycloak configuration (realm, OIDC client)
- [ ] Working auth on Blazor WASM
- [ ] Provider key CRUD (with encryption)
- [ ] Anthropic adapter
- [ ] Basic dashboard with real data
- [ ] Working Docker Compose

### 📋 Phase 2 — Multi-provider
- [ ] OpenAI adapter
- [ ] Mistral adapter
- [ ] Automatic background sync (every 6h)
- [ ] Advanced charts (trend, model breakdown)
- [ ] Spending threshold alerts

### 📋 Phase 3 — Deploy & Google
- [ ] Public domain + HTTPS (Let's Encrypt / reverse proxy)
- [ ] CSV/PDF monthly report export
- [ ] Google Vertex AI / Gemini (BigQuery export)

### 📋 Phase 4 — Mobile (optional)
- [ ] .NET MAUI Hybrid migration
- [ ] Google Play Store
- [ ] Apple App Store (requires Apple Developer Account)
