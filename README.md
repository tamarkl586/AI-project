# AI Project

AI Project is a Docker-first microservices application for a Chinese sale platform. The repository contains an Angular frontend, a YARP-based API gateway, a web BFF, and multiple .NET 8 backend services backed by different datastores selected per domain.

The current architecture exposes a single public entry point through the gateway on port 8080. Core business services stay internal to the Docker network, and CatalogService is designed to run with multiple replicas behind gateway load balancing.

## Architecture

### Public entry point

- ApiGateway on `http://localhost:8080`
- Frontend is served behind the gateway
- External API traffic is routed through YARP

### Internal services

- IdentityService: authentication and JWT issuance, backed by PostgreSQL
- CatalogService: gifts, donors, and categories, backed by MongoDB and Redis cache
- TicketingService: cart and purchase workflow, backed by SQL Server and RabbitMQ
- DrawReportService: draw, reporting, and event processing, backed by Cassandra and RabbitMQ
- WebBff: authenticated aggregation layer for web-specific endpoints

### Persistence strategy

- MongoDB for catalog data
- PostgreSQL for identity data
- SQL Server for transactional ticketing data
- Cassandra for reporting and draw event data

This repository follows a polyglot persistence model documented in `docs/adr/ADR-001-polyglot-persistence.md`.

## Tech stack

- Frontend: Angular 21
- Backend: .NET 8
- Gateway: YARP reverse proxy
- Messaging: RabbitMQ with MassTransit
- Caching: Redis
- Logging: Serilog with Seq
- Containers: Docker Compose

## Repository layout

```text
AI-project/
  client/chineseSale/        Angular frontend
  docs/                      Architecture notes and ADRs
  server/ApiGateway/         Public ingress via YARP
  server/WebBff/             Web aggregation endpoints
  server/IdentityService/    Auth and JWT
  server/CatalogService/     Gifts, donors, categories
  server/TicketingService/   Cart and purchase flow
  server/DrawReportService/  Draw and reporting flow
  server/project1/           Legacy monolith project kept in repo
  docker-compose.yml         Main local deployment entry point
```

## Prerequisites

- Docker Desktop with Compose support
- Optional for local non-Docker development:
  - .NET 8 SDK
  - Node.js 20+
  - npm 10+

## Configuration

The root `.env` file currently provides the JWT signing key required by the services.

Example:

```env
JWT_KEY=DevJwtKey_ChangeMe_ToALongRandomSecret_AtLeast32Chars
```

You can also override the database and broker settings exposed in `docker-compose.yml`, including:

- `MONGO_ROOT_USER`
- `MONGO_ROOT_PASSWORD`
- `CATALOG_DB_NAME`
- `IDENTITY_DB_NAME`
- `IDENTITY_DB_USER`
- `IDENTITY_DB_PASSWORD`
- `IDENTITY_DB_HOST_PORT`
- `RABBITMQ_USER`
- `RABBITMQ_PASSWORD`
- `CASSANDRA_KEYSPACE`

## Quick start

From the repository root:

```bash
docker compose up -d --build --scale catalogservice=2
```

This command:

- builds all service images
- starts infrastructure containers
- runs two CatalogService replicas for load-balancing validation
- exposes the application through the gateway on port 8080

## Public endpoints

After startup, these are the main entry points:

- Application: `http://localhost:8080`
- Gateway health: `http://localhost:8080/health`
- Seq logs UI: `http://localhost:5341`
- PostgreSQL host access: `localhost:55432`

### Routed API surface

- `/api/auth/*` -> IdentityService
- `/api/gift/*` -> CatalogService
- `/api/category/*` -> CatalogService
- `/api/donor/*` -> CatalogService
- `/api/cart/*` -> TicketingService, requires JWT
- `/api/reports/*` -> DrawReportService, requires JWT
- `/api/web/*` -> WebBff, requires JWT

### Web BFF endpoints

The BFF currently exposes:

- `GET /api/web/orders/me`
- `GET /api/web/orders/{userId}`

Behavior:

- validates JWT at the BFF layer
- forwards bearer token and user headers downstream
- aggregates cart items from TicketingService with gift details from CatalogService
- rejects cross-user access on `/api/web/orders/{userId}`

## Verification

### 1. Confirm the stack is healthy

```bash
docker compose ps
```

### 2. Verify gateway health

```bash
curl http://localhost:8080/health
```

### 3. Verify CatalogService load balancing

Repeated calls should eventually show different `X-Container-ID` response headers.

```bash
for i in 1 2 3 4 5 6 7 8; do
  curl -s -D - http://localhost:8080/api/gift -o /dev/null | grep -i '^X-Container-ID:'
done
```

### 4. Verify unauthorized BFF access returns 401

```bash
curl -i http://localhost:8080/api/web/orders/me
```

### 5. Verify authenticated BFF aggregation

```bash
TOKEN=$(curl -s -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"YOUR_USER_EMAIL","password":"YOUR_PASSWORD"}' \
  | jq -r '.token')

curl -i http://localhost:8080/api/web/orders/me \
  -H "Authorization: Bearer $TOKEN"
```

## Development notes

### Frontend only

```bash
cd client/chineseSale
npm install
npm start
```

Angular development server runs on `http://localhost:4200`.

### Backend codebase

- Most active services target .NET 8
- `server/project1` appears to be the legacy monolith kept for reference or migration continuity
- `server/project1.sln` currently includes `project1`, `ApiGateway`, and `WebBff`

## Observability

- All services expose `/health`
- Structured logs are sent to Seq
- Correlation IDs are propagated across gateway and downstream services via `X-Correlation-ID`
- CatalogService adds `X-Container-ID` to responses to make replica rotation visible

## Documentation

- `docs/phase-3-architecture-and-prompt-guide.md`
- `docs/phase-3-guide-he.md`
- `docs/adr/ADR-001-polyglot-persistence.md`
- `docs/adr/ADR-001-polyglot-persistence-he.md`

## Known scope

This README is focused on the current containerized microservices flow under `docker-compose.yml`. It does not attempt to document every controller action in each service or the older monolith in full detail.