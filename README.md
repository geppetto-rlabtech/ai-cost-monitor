# AI Cost Monitor

Multi-tenant PWA to monitor AI provider costs (Anthropic, OpenAI, Mistral) in a single dashboard. Each user manages their own API keys and views usage and spending in real time.

## Stack

- **Backend:** ASP.NET Core Minimal API (.NET 9)
- **Frontend:** Blazor WASM PWA (MudBlazor + ApexCharts)
- **Auth:** Keycloak (OIDC)
- **DB:** PostgreSQL 16
- **Deploy:** Docker Compose

## Documentation

- [Architectural Plan](docs/plan.md)
- [Project Conventions](CLAUDE.md)
