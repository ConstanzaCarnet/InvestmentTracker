# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Full stack — COMPILED images ("like prod"), stable, comes up in ~seconds.
# This is the base docker-compose.yml ONLY (the -f opts out of the dev override).
docker compose -f docker-compose.yml up -d --build

# Full stack — DEV hot-reload (dotnet watch). The docker-compose.override.yml is
# auto-merged on a bare `up`, switching every service to the SDK build stage +
# mounted source + `dotnet watch`. Convenient but fragile (see "Docker on Windows").
docker compose up -d

# Build a single service image
docker compose -f docker-compose.yml build holdings-service

# Generate a new EF migration (example for Holdings)
dotnet ef migrations add <MigrationName> \
  --project src/Services/Holdings/Holdings.Infrastructure/Holdings.Infrastructure.csproj \
  --startup-project src/Services/Holdings/Holdings.API/Holdings.API.csproj \
  --output-dir Data/Migrations
```

Each service is built by a **multi-stage Dockerfile** (`src/**/<Service>.API/Dockerfile`, plus `src/ApiGateways/ApiGateway/Dockerfile`): an SDK stage restores only the needed `.csproj` (the service's own projects + the `BuildingBlocks` it references) and `dotnet publish`es; an `aspnet` runtime stage runs the DLLs. Build context is the repo root; `.dockerignore` keeps `bin/obj/.git/.vs` out.

⚠️ **Do not run `dotnet build`/`dotnet publish` on the host while the dev (watch) stack is running** — it regenerates `obj/` in the mounted volume and crashes every container's polling file watcher. Use `docker compose -f docker-compose.yml build` instead, or run the compiled stack.

There are no automated tests. Migrations run automatically on startup (retry loop, 10 attempts × 5s).

After schema changes, reset the Outbox to replay failed events:
```bash
docker exec investment-tracker-sql-db bash -c \
  "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'WalletPassword123!' -No \
   -Q \"USE TransactionDb; UPDATE OutboxMessages SET ProcessedOnUtc = NULL WHERE ProcessedOnUtc IS NOT NULL;\""
```

## Services & Ports

All four services listen on **container port `8080`**. Their host port mappings were removed — they're reachable only on the internal `investment-network` (by service name), behind the gateway.

| Container | Host port | DB | Responsibility |
|---|---|---|---|
| `api-gateway` | **8000** | — | YARP reverse proxy — the only public entry point (see "API Gateway") |
| `users-service` | — (internal) | `UsersDb` | User registration + JWT issuance, publishes `UserCreatedEvent` |
| `holdings-service` | — (internal) | `HoldingsDb` | Portfolio positions, account balances, market valuation |
| `marketdata-service` | — (internal) | — | Real-time prices via Yahoo Finance (no API key, free) |
| `transactions-service` | — (internal) | `TransactionDb` | Buy/sell ledger, Outbox publisher |
| `investment-tracker-sql-db` | 1433 | — | SQL Server 2022 (published for dev tooling only) |
| `rabbitmq-invest-tracker` | 5672 / 15672 | — | RabbitMQ + management UI (dev tooling only) |

All API traffic goes through `http://localhost:8000` with the gateway's pretty prefixes. To reach a service's own Swagger (ports are closed), use the gateway as a jump host: `docker compose exec api-gateway curl http://holdings-service:8080/swagger/v1/swagger.json`, or temporarily re-add a `ports:` mapping.

RabbitMQ UI: `http://localhost:15672` (guest/guest) · SQL: `sa` / `WalletPassword123!`

## Architecture

Four microservices following Clean Architecture + DDD. Services have **no direct project references** to each other — the only shared code is `src/BuildingBlocks/EventBus.Messages/Events/` (event contracts).

```
{Service}.Domain        — Entities, enums, no framework dependencies
{Service}.Application   — Interfaces, DTOs, use-case services
{Service}.Infrastructure — EF Core DbContext, repositories, external HTTP clients
{Service}.API           — Controllers, MassTransit consumers, Program.cs
```

### Communication flows

**Async (RabbitMQ/MassTransit):**
- Users → Holdings: `UserCreatedEvent` → creates `Account`
- Transactions → Holdings: `TransactionCreatedEvent`, `TransactionUpdatedEvent`, `TransactionDeletedEvent` → updates `Position` + `PositionLots`

**Sync (HTTP):**
- Holdings → MarketData: `POST /api/v1/prices/batch` with `{ items: [{instrumentId, ticker}] }` → returns current prices keyed by `instrumentId`. Holdings passes tickers so MarketData never needs its own instrument DB.

## Key Patterns

### Transactional Outbox (Transactions service)
`TransactionService` writes domain entity + `OutboxMessage` in one DB transaction. `OutboxPublisherService` polls every 5 seconds, picks up rows where `ProcessedOnUtc IS NULL`, publishes to RabbitMQ via MassTransit, then sets `ProcessedOnUtc`.

**Critical**: `ResolveType` uses an explicit `Dictionary<string, Type>` keyed by `nameof(TEvent)` — NOT `AppDomain.GetAssemblies()` scanning, which fails when `EventBus.Messages` isn't yet loaded in a fresh process. If you add a new event type to the Outbox, add it to the `_eventTypes` dictionary in `OutboxPublisherService`.

### Holdings consumers — correct EF tracking pattern
When a consumer creates a **new** `Position`, call `AddPositionAsync` and do **not** also call `UpdatePosition`. Calling both changes EF state from `Added` → `Modified`, causing `SaveChanges` to generate an `UPDATE` on a non-existent row, followed by a FK violation when inserting the related `PositionLot`.

```csharp
var isNew = position == null;
if (isNew)
{
    position = new Position(...);
    await _repository.AddPositionAsync(position);   // EF tracks as Added
}
// ... mutate position/lots ...
if (!isNew)
    _repository.UpdatePosition(position);           // Only for existing rows
```

### Sequence number validation (Holdings)
`Position.ValidateAndApplySequence` only rejects duplicates (`sequence <= LastProcessedSequenceNumber`). Strict sequential ordering (`== previous + 1`) was intentionally removed to allow recovery after the Holdings DB is reset while TransactionDb retains history with higher sequence numbers.

### Multi-currency lots
`Position` holds `List<PositionLot>`, one per currency (e.g., ARS and USD). `GetOrCreateLot(currency)` returns an existing lot or creates one. A lot is deleted when quantity reaches zero; if all lots are deleted, the position is deleted.

### Idempotent consumers
`UserCreatedConsumer` checks for an existing `Account` before inserting — safe to replay. `TransactionCreatedConsumer` protects via sequence number deduplication.

## EF Core 9 — PendingModelChangesWarning

EF Core 9 elevates `PendingModelChangesWarning` to an error if the DbContext model snapshot is out of sync with the entity model. The Holdings service suppresses this in `AddDbContext`:

```csharp
options.ConfigureWarnings(w =>
    w.Ignore(RelationalEventId.PendingModelChangesWarning));
```

After any domain model change, generate a new migration immediately. The migration `20260602_EFCore9SnapshotFix` was added to sync the snapshot after upgrading from EF Core 8 to EF Core 9 (which also captured the refactor from flat `Position` to `Position + PositionLots`).

## Portfolio Balance Response

`GET /api/v1/Portfolio/{userId}` returns:

```
PortfolioDto
  ├── accountNumber
  ├── totalInvested / totalMarketValue / totalPnL / totalPnLPercentage
  └── positions[]
        ├── ticker, currency (primary lot), totalQuantity, totalRealQuantity
        ├── totalInvested, averagePurchasePrice, currentPrice, currentValue
        ├── pnL, pnLPercentage, portfolioPercentage
        └── lots[]
              ├── currency, quantity, realQuantity
              ├── investedAmount (base currency), investedAmountRaw (lot currency)
              ├── averagePurchasePrice
              └── totalBoughtQuantity/Amount, totalSoldQuantity/Amount
```

`totalRealQuantity` = nominal quantity adjusted by instrument's `ConversionRatio` (e.g., CEDEAR ratio 2:1 means 100 CEDEARs = 50 underlying shares).

## Error handling convention

Every service has an `ExceptionMiddleware` that distinguishes:
- `DomainException` (business rule violation) → **4xx** with `{ type, title, status, detail }` body
- Unhandled `Exception` → **500**

`DomainException` accepts a `statusCode` param — use `statusCode: 409` for conflicts, `statusCode: 404` for not-found, default is 400.  
Add this middleware to every new service via `app.UseMiddleware<ExceptionMiddleware>()` **before** `app.MapControllers()`.

## Input validation

`BuyRequest`, `SellRequest`, and `TransactionRequest` use `[Required]`, `[Range]`, and `[MinLength]` from `System.ComponentModel.DataAnnotations`. `[ApiController]` enforces these automatically before the controller action runs — no `ModelState.IsValid` check needed.  
All new request DTOs should follow the same pattern.

## Instrument validation

`TransactionService.ValidateAndGetInstrumentAsync` normalises the ticker on the application side (`Trim().ToUpperInvariant()`) before querying — never apply functions to the DB column or the unique index on `Ticker` won't be used. Throws `DomainException` (→ 400) for unknown or inactive tickers. This validation also runs on `UpdateTransactionAsync`.

## Authentication (JWT) — platform-wide

Users **issues** JWT bearer tokens; Holdings and Transactions **validate** them with the same key (no call back to Users). MarketData is intentionally left **open** (Holdings calls it server-to-server). The secret/issuer/audience must be identical across all services that validate.

**Shared helper:** `src/BuildingBlocks/Common.Authentication` (referenced by `Holdings.API` & `Transactions.API`) provides:
- `services.AddJwtAuthentication(config)` — registers the JwtBearer scheme + `TokenValidationParameters` from the `Jwt` section. Call it, then `app.UseAuthentication()` **before** `app.UseAuthorization()`.
- `User.GetUserId()` — `ClaimsPrincipal` extension that reads the `sub`/`NameIdentifier` claim as a `Guid`.

- **Login:** `POST /api/auth/login` (`AuthController`, public) → `AuthResponseDto { accessToken, expiresAtUtc, userId, email }`.
- **Password hashing:** PBKDF2-SHA512, 350k iterations, random salt, stored as `base64(salt):base64(hash)`. `UserService.HashPassword` / `VerifyPassword` are mirrors — they must use identical params or verification never matches. Verify compares with `CryptographicOperations.FixedTimeEquals` (constant-time).
- **Token generation:** `IJwtTokenGenerator` (Application interface) → `JwtTokenGenerator` (Infrastructure, `System.IdentityModel.Tokens.Jwt`). Claims: `sub`=userId, `email`, `jti`, `name`. Signed HMAC-SHA256.
- **Validation:** `Program.cs` `AddAuthentication().AddJwtBearer(...)` with `TokenValidationParameters` (issuer, audience, lifetime, signing key). **Pipeline order matters**: `UseAuthentication()` before `UseAuthorization()`, both before `MapControllers()`.
- **Config:** section `Jwt { Key, Issuer, Audience, ExpiryMinutes }` in `appsettings.json` (dev) overridden by `Jwt__*` env vars in docker-compose. Key must be ≥32 chars (256-bit). In prod the key belongs in a secret manager, never the repo.
- **Protecting endpoints:** add `[Authorize]`. Read the caller id with `User.FindFirstValue(ClaimTypes.NameIdentifier)` — JwtBearer remaps the `sub` claim to `ClaimTypes.NameIdentifier`. This is what will replace the `userId` currently passed via route/body.
- **DTO gotcha (.NET 9):** request DTOs with validation must be **records with properties** (like `BuyRequest`), NOT positional records with `[property: Required]` — the latter throws at runtime: *"validation metadata must be associated with the constructor parameter."*
- **userId from the token, not the body:** protected controllers take the caller id from `User.GetUserId()`, never from the request. `BuyRequest`/`SellRequest` no longer carry `UserId`. "Current user" endpoints use `/me` routes: `GET /api/v1/portfolio/me`, `GET /api/transaction/me`, `/me/instrument/{id}`, `/me/date/{date}`.
- **Two `JwtSettings`:** one in `Users.Infrastructure` (to ISSUE tokens) and one in `Common.Authentication` (to VALIDATE) — intentional, so `Users.Infrastructure` stays free of an ASP.NET Core dependency.
- **Ownership:** `[Authorize]` only proves *authenticated*, not *owner*. Endpoints acting on a resource by id must also check the resource's `UserId` against `User.GetUserId()`. Transactions `Update`/`Delete` do this and return **404** (not 403) when the row belongs to someone else, so they don't leak its existence.

## API Gateway (YARP)

`src/ApiGateways/ApiGateway` is a YARP reverse proxy — the single public entry point (`:8000`). It's a minimal-hosting ASP.NET app; all routing lives in `appsettings.json` under `ReverseProxy`.

- **Routes vs Clusters:** a **Route** decides *whether/how* to accept a request (`Match` path, `AuthorizationPolicy`, `RateLimiterPolicy`, `Transforms`); a **Cluster** is the *destination* (`Destinations` → `http://<service>:8080`, internal DNS). Routes reference one Cluster by `ClusterId`; multiple Routes can share a Cluster (e.g. `/portfolio/*` and `/holdings/*` both → `holdings-cluster`).
- **Pretty prefixes + transforms:** public paths are rewritten to each service's internal path via `PathPattern`. `/auth/*`,`/users/*`→`/api/...` (users); `/portfolio/*`→`/api/v1/portfolio/*`, `/holdings/*`→`/api/v1/holdings/*` (holdings); `/prices/*`→`/api/v1/prices/*` (marketdata); `/transactions/*`→`/api/transaction/*` (transactions, note singular internal).
- **Auth at the edge (defense in depth):** the gateway validates JWT via the shared `Common.Authentication` (`AddJwtAuthentication`, same Key/Issuer/Audience). Only routes where *every* endpoint is `[Authorize]` carry `"AuthorizationPolicy": "default"` — currently `portfolio` and `transactions`. `auth`/`users`/`holdings`/`prices` stay open (mixed or public; protecting `/users` would block registration). Services still validate too — the gateway forwards the `Authorization` header unchanged.
- **Rate limiting:** all routes carry `"RateLimiterPolicy": "per-user"` — a fixed-window limiter (100 req/min) keyed by userId (`sub`) when authenticated, falling back to client IP. Rejections return **429** (`RejectionStatusCode`). Pipeline order: `UseAuthentication` → `UseAuthorization` → `UseRateLimiter` → `MapReverseProxy`.

## Docker on Windows — dev workflow & the watcher trap

Two compose modes (see "Build & Run"): the **base** `docker-compose.yml` runs compiled multi-stage images (stable, no mounted volume, no watcher); `docker-compose.override.yml` (auto-merged on a bare `docker compose up`) switches to `build.target: build` + mounted source + `dotnet watch` for hot-reload.

The watch/override mode is convenient but fragile on Windows volume mounts — the `PollingDirectoryWatcher` crashes two ways: `ArgumentException: duplicate key` (when host `dotnet build` leaves both `obj/Debug` and `obj/Release`) and `IOException: Cannot allocate memory` (OOM, exit 134). A dead service drops off Docker DNS, so the gateway logs `Name or service not known (<svc>:8080)` and returns 502. Mitigations: prefer the compiled base stack for heavy work; never `dotnet build` on the host while watch is up (clean with `find src -type d \( -path '*/obj/Release' -o -path '*/bin/Release' \) -exec rm -rf {} +`); prefer `docker compose up -d <svc>` over `docker compose restart <svc>` (restart can leave stale DNS on Docker Desktop/Windows).

## Configuration by environment

| Key | Docker (env var) | Local dev (appsettings.Development.json) |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `Server=users-db;...` | `Server=localhost,1433;...` |
| `EventBusSettings:HostAddress` | `rabbitmq://rabbitmq` (via env var) | `rabbitmq://localhost` |
| `Services:MarketData` | `http://marketdata-service:8080` (via env var) | `http://localhost:5099` |

MarketData runs on port `5099` locally (see `MarketData.API/Properties/launchSettings.json`).

## Deployment

The backend is deployed to Render's free tier, which spins down after ~15 min of inactivity. `.github/workflows/keep-alive.yml` pings `https://portfolio-backend-sp68.onrender.com/health` every 10 minutes (with cold-start retries) to keep it warm. Note: that `/health` endpoint and the Render service are configured outside this repo — there is no `/health` mapping in any service's `Program.cs` here.
