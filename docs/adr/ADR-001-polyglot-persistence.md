# ADR-001: Polyglot Persistence Strategy for Phase 2 Microservices

- **Status:** Approved  
- **Date:** 2026-07-05  
- **Decision Type:** Architecture / Data Platform  
- **Scope:** Production microservices data layer (CatalogService, IdentityService, TicketingService, DrawReportService)

## Context

Phase 2 of the migration decomposed the monolithic system into bounded-context microservices with heterogeneous workload profiles and divergent consistency requirements. A single database paradigm would force suboptimal trade-offs across services with different latency SLOs, integrity constraints, and throughput patterns.

The architecture therefore adopts **polyglot persistence**, selecting the storage engine whose operational semantics best align with each domain model. This ADR formalizes those choices and their distributed-systems rationale using:

- **CAP Theorem** trade-offs under network partitions
- **ACID vs. BASE** consistency models
- **Read/write throughput** characteristics
- **Schema model** suitability (relational, document, wide-column)

---

## Decision

### 1) CatalogService → MongoDB

#### Decision
Use MongoDB as the primary datastore for catalog and gift/product domain data.

#### Architectural Justification
- **Schema model:** Catalog entities are highly dynamic (optional attributes, category-specific metadata, evolving product descriptors). A **document schema** supports polymorphic fields and incremental schema evolution without high-friction DDL cycles.
- **Read throughput profile:** Catalog traffic is read-heavy (browse/search/detail retrieval). MongoDB’s document locality and indexing patterns are well-suited for high read throughput and low-latency retrieval of aggregate product views.
- **CAP positioning:** In replica-set deployments, MongoDB generally behaves as **CP-leaning for primary reads/writes** (stronger consistency on primary) while offering **AP-leaning behavior at read preference edges** (e.g., secondary reads with eventual consistency). This boundary flexibility is aligned to catalog UX, where occasional read staleness is tolerable relative to availability.
- **Consistency model fit:** Catalog data tolerates bounded staleness better than identity or ticketing domains. Therefore, a mixed consistency posture (strong on primary writes, optionally eventual on distributed reads) is acceptable.

---

### 2) IdentityService → PostgreSQL

#### Decision
Use PostgreSQL as the system of record for users, credentials, roles, and authorization-linked identity data.

#### Architectural Justification
- **ACID requirement:** Identity and access control are **integrity-critical**. Authentication and authorization boundaries require strict correctness guarantees (atomic updates, durable credential state, isolation from concurrent anomalies).
- **Schema model:** Identity data is naturally **relational** (users, credentials, claims/roles, token metadata, revocation artifacts) with explicit keys and constraints. PostgreSQL enforces referential integrity and transactional invariants natively.
- **Immediate consistency:** Security decisions must not observe stale credential state. Strong immediate consistency is required to avoid privilege escalation and authorization drift.
- **CAP positioning:** Operationally **CP-oriented** under partition-sensitive conditions; consistency is prioritized for security correctness, accepting availability trade-offs when necessary.

---

### 3) TicketingService → SQL Server

#### Decision
Use SQL Server for ticket inventory, cart lifecycle, booking, and payment-adjacent transactional workflows.

#### Architectural Justification
- **Business-critical transactional safety:** Ticketing and checkout flows are monetary and inventory-sensitive. The domain requires strict prevention of over-allocation and double-booking.
- **ACID + transaction scopes:** Explicit transaction scopes and locking/concurrency controls are required to guarantee serializable business outcomes where needed (e.g., reserve seat, confirm payment, finalize order).
- **Immediate consistency model:** Cart and payment lifecycle transitions must be strongly consistent to prevent race-condition artifacts (phantom availability, duplicate captures, inconsistent order state).
- **Schema model:** Strongly relational schema with normalized entities and constraints is appropriate for deterministic workflow enforcement and auditable state transitions.
- **CAP positioning:** **CP-oriented** for correctness-first transactional behavior.

---

### 4) DrawReportService → Cassandra

#### Decision
Use Apache Cassandra for draw logs, audit events, and precomputed/reporting-oriented analytical projections.

#### Architectural Justification
- **Workload shape:** This domain is append-heavy and write-dominant. Cassandra’s architecture is optimized for **massive write throughput** and horizontal scale-out.
- **Schema model:** **Wide-column** data modeling is suitable for time-series-like partitions, denormalized query-driven tables, and large event volumes.
- **BASE model:** Reporting/audit projections can tolerate eventual convergence; strict immediate consistency is not required for every read path.
- **Tunable consistency under CAP:** Cassandra is operationally **AP-leaning** by default under partition scenarios, while allowing per-operation consistency tuning (`ONE`, `QUORUM`, `ALL`) to calibrate latency vs. consistency for specific query classes.
- **Consistency fit:** Eventual consistency is acceptable for analytical/reporting surfaces where freshness windows are bounded and known.

---

## Consequences

### Positive Consequences
- Domain-specific optimization of data engines improves both performance and correctness.
- Reduced impedance mismatch between domain model and persistence model.
- Strong consistency preserved where correctness is non-negotiable (identity, ticketing).
- High read scalability for catalog workloads and high write scalability for reporting/audit streams.
- Clear bounded-context ownership of data contracts and schema evolution paths.

### Trade-offs / Costs
- Increased operational complexity (multiple engines, backup/restore strategies, observability stacks, patch lifecycles).
- Higher cognitive load for teams (different query languages, indexing models, consistency semantics).
- Cross-service reporting requires explicit integration patterns (events, CDC, materialized views) rather than shared relational joins.
- Governance burden for data contracts and compatibility policies across services.

### Risk Mitigations
- Standardize SLOs and runbooks per datastore.
- Enforce service-level ownership and schema versioning discipline.
- Use idempotent event processing and retry-safe write paths.
- Define consistency SLAs explicitly per API (strong vs eventual) to avoid implicit assumptions.
- Centralize backup verification and disaster-recovery drills across all engines.

---

## Decision Summary

The system adopts a deliberate **polyglot persistence** architecture:

- **CatalogService:** MongoDB for flexible document schema and high read throughput with acceptable bounded staleness.
- **IdentityService:** PostgreSQL for strict ACID and immediate consistency at authorization boundaries.
- **TicketingService:** SQL Server for transaction-safe, consistency-critical booking and payment workflows.
- **DrawReportService:** Cassandra for high-volume write ingestion and BASE/eventual-consistency analytics with tunable consistency.

This decision aligns persistence technology with bounded-context invariants, throughput characteristics, and CAP-consistency requirements expected in production-grade distributed systems.
