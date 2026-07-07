# Phase 3 Architecture Documentation and Prompt Guide (Gateway, Web BFF, Load Balancing)

## 1) Architectural Overview

### 1.1 Public Entry Point Pattern (YARP ApiGateway)

Phase 3 is implemented with a single public entry point:

- Service: ApiGateway
- Technology: YARP (Yet Another Reverse Proxy)
- Public port: 8080

All external client traffic is intentionally routed through ApiGateway, not directly to business services. This establishes a stable ingress layer and cleanly separates external API exposure from internal service topology.

### 1.2 Internal-Only Microservices (Docker Network Isolation)

The four core microservices are internal to the Docker network:

- IdentityService
- CatalogService
- TicketingService
- DrawReportService

In the Compose topology, these services no longer expose host ports. Only ApiGateway exposes host port 8080. This enforces the requirement that core services are private and reachable only through gateway routing.

### 1.3 WebBff Role and Aggregation Flow

WebBff is implemented as a dedicated client-oriented orchestration layer behind the gateway.

Main responsibilities:

- Accept web-facing BFF requests under /api/web/* through ApiGateway.
- Enforce JWT authentication at the BFF layer.
- Validate JWT tokens using the same IdentityService security contract (issuer, audience, signing key policy).
- Aggregate user cart data from TicketingService with gift details from CatalogService into a single response payload.

Implemented BFF endpoint family:

- GET /api/web/orders/me
- GET /api/web/orders/{userId}

Behavioral notes:

- The BFF forwards the Authorization Bearer token downstream to internal services.
- The {userId} route rejects cross-user access and only allows the authenticated user to request their own data.
- The response merges cart line information with current gift metadata into one client-ready JSON shape.

### 1.4 Load Balancing Topology (CatalogService x2)

Load balancing is applied to CatalogService using two replicas and gateway-level round-robin routing.

- Scaled service: CatalogService
- Replica count: 2
- Balancing algorithm: RoundRobin in ApiGateway YARP cluster configuration
- Proof header: X-Container-ID (added by CatalogService middleware)

The CatalogService middleware appends X-Container-ID to each response using container hostname/machine identity. Repeated calls through the gateway demonstrate alternating backend container IDs.

---

## 2) Deployment Strategy

Use this exact startup command from repository root:

```bash
docker compose up -d --build --scale catalogservice=2
```

What this command ensures:

- Builds latest gateway, BFF, and service images.
- Starts infrastructure and all services.
- Runs two CatalogService replicas for live load-balancing validation.

---

## 3) Exact Verification and Smoke Tests

## 3.1 Load Balancing Proof (Gateway Route + X-Container-ID Rotation)

```bash
for i in 1 2 3 4 5 6 7 8; do
  curl -s -D - http://localhost:8080/api/gift -o /dev/null | grep -i '^X-Container-ID:'
done
```

Expected result:

- You should see at least two different X-Container-ID values alternating over repeated calls.

## 3.2 BFF Unauthorized Smoke Test (Expect 401)

```bash
curl -i http://localhost:8080/api/web/orders/me
```

Expected result:

- HTTP status is 401 Unauthorized.

## 3.3 BFF Authorized Aggregation Test (Bearer Token)

Step A: Obtain token from gateway-auth route.

```bash
TOKEN=$(curl -s -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"YOUR_USER_EMAIL","password":"YOUR_PASSWORD"}' \
  | jq -r '.token')

echo "$TOKEN"
```

Step B: Call aggregated BFF endpoint.

```bash
curl -s -i http://localhost:8080/api/web/orders/me \
  -H "Authorization: Bearer $TOKEN"
```

Expected result:

- HTTP 200 OK
- Single aggregated JSON payload containing cart items plus corresponding gift details.

Optional self-check with explicit user route:

```bash
curl -s -i http://localhost:8080/api/web/orders/YOUR_USER_ID \
  -H "Authorization: Bearer $TOKEN"
```

Expected result:

- 200 for own user ID
- 403 if trying to read another user ID

---

## 4) Direct Cursor/Copilot Workspace Prompt (Copy-Paste Block)

Use the block below directly in Cursor/Copilot chat to update root README.md while preserving Phase 1 and Phase 2 sections.

```text
You are updating the repository root README.md for Phase 3 only.

Critical constraints:
1) Do not remove or rewrite existing Phase 1 and Phase 2 milestone content.
2) Keep all existing startup instructions that are still valid.
3) Add a new clearly titled section: "Phase 3 - API Gateway, Web BFF, and Load Balancing".
4) Reflect the actual implemented architecture in this repository:
   - ApiGateway (YARP) is the single public ingress on port 8080.
   - IdentityService, CatalogService, TicketingService, and DrawReportService are internal-only on Docker network.
   - WebBff provides /api/web/orders/me and /api/web/orders/{userId} aggregation endpoints.
   - WebBff validates JWT using IdentityService issuer/audience/signing-key contract and forwards bearer token to downstream services.
   - CatalogService is scaled to 2 replicas and load balanced via YARP round-robin.
   - Catalog responses include X-Container-ID for balancing proof.
5) Add a "Phase 3 Deployment" subsection including this exact command:
   docker compose up -d --build --scale catalogservice=2
6) Add a "Phase 3 Verification" subsection with three smoke tests:
   - load-balancing proof using repeated curl calls to /api/gift and checking X-Container-ID
   - unauthorized BFF test expecting 401 on /api/web/orders/me
   - authorized BFF aggregation test with Bearer token
7) Keep formatting clean and concise. Do not add speculative features.

Now apply the README update.
```

---

## Implementation Snapshot (Phase 3)

The following were introduced/updated in the current implementation:

- ApiGateway YARP configuration and route mapping
- WebBff service and aggregation controller
- Internal-only network exposure for core microservices
- CatalogService response-header middleware for X-Container-ID
- Compose scale strategy for CatalogService replicas
