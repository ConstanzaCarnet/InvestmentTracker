# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Full stack (all 6 containers)
docker compose up -d

# Build a single service (from solution root)
dotnet build src/Services/Holdings/Holdings.API/Holdings.API.csproj

# Generate a new EF migration (example for Holdings)
dotnet ef migrations add <MigrationName> \
  --project src/Services/Holdings/Holdings.Infrastructure/Holdings.Infrastructure.csproj \
  --startup-project src/Services/Holdings/Holdings.API/Holdings.API.csproj \
  --output-dir Data/Migrations
```

There are no automated tests. Migrations run automatically on startup (retry loop, 10 attempts × 5s).

After schema changes, reset the Outbox to replay failed events:
```bash
docker exec investment-tracker-sql-db bash -c \
  "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'WalletPassword123!' -No \
   -Q \"USE TransactionDb; UPDATE OutboxMessages SET ProcessedOnUtc = NULL WHERE ProcessedOnUtc IS NOT NULL;\""
```

## Services & Ports

| Container | Local port | DB | Responsibility |
|---|---|---|---|
| `users-service` | 8080 | `UsersDb` | User registration, publishes `UserCreatedEvent` |
| `holdings-service` | 8081 | `HoldingsDb` | Portfolio positions, account balances, market valuation |
| `marketdata-service` | 8082 | — | Real-time prices via Yahoo Finance (no API key, free) |
| `transactions-service` | 8083 | `TransactionDb` | Buy/sell ledger, Outbox publisher |
| `investment-tracker-sql-db` | 1433 | — | SQL Server 2022 |
| `rabbitmq-invest-tracker` | 5672 / 15672 | — | RabbitMQ + management UI |

Swagger: `http://localhost:{port}/swagger`  
RabbitMQ UI: `http://localhost:15672` (guest/guest)  
SQL: `sa` / `WalletPassword123!`

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
- **Ownership:** `[Authorize]` only proves *authenticated*, not *owner*. Endpoints acting on a resource by id (e.g. Transactions `Update`/`Delete`) must additionally check the resource's `UserId` against `User.GetUserId()`.

## Docker on Windows — known issues

`dotnet watch` with Windows volume mounts can crash with `ArgumentException: duplicate key` in `PollingDirectoryWatcher`. Set `DOTNET_USE_POLLING_FILE_WATCHER: "true"` in docker-compose to mitigate. When `dotnet watch` is stuck in crash loop, use `docker compose restart <service>` to force a clean rebuild.

## Configuration by environment

| Key | Docker (env var) | Local dev (appsettings.Development.json) |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `Server=users-db;...` | `Server=localhost,1433;...` |
| `EventBusSettings:HostAddress` | `rabbitmq://rabbitmq` (via env var) | `rabbitmq://localhost` |
| `Services:MarketData` | `http://marketdata-service:8080` (via env var) | `http://localhost:5099` |

MarketData runs on port `5099` locally (see `MarketData.API/Properties/launchSettings.json`).
