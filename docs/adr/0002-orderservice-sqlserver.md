# ADR 0002 — OrderService uses SQL Server (relational database)

- **Status:** Accepted (Phase 2)
- **Service:** OrderService

## Context

Orders represent **money and commitments**. An order and its line items form a
single unit that must be saved completely or not at all. We need strong
guarantees: no half-written orders, accurate totals, and a reliable history for
auditing. The data is highly structured (orders → order items) and benefits from
foreign keys and well-defined schema.

## Decision

Use **SQL Server**, a relational database, via EF Core. An order and its items
are written together in a single transaction (one `SaveChanges`). This also keeps
continuity with the Phase 1 monolith, which already used SQL Server.

## Consequences

- ✅ Full ACID transactions: order + items commit atomically.
- ✅ Strong schema, foreign keys, and `decimal(18,2)` money precision.
- ✅ Mature querying/reporting for order history.
- ⚠️ Heavier to scale horizontally than NoSQL; vertical scaling / read replicas
  are the usual path. Acceptable — order volume is lower than catalog browsing.
- ⚠️ Schema changes need migrations.

## ACID / BASE

This service is firmly **ACID**. Atomicity, Consistency, Isolation and Durability
are exactly what an order needs: a confirmed order with its items is all-or-nothing
and durably persisted. BASE/eventual consistency would be inappropriate where
money is involved.

## CAP

A single relational primary is a **CP** system: under a partition it preserves
**consistency** and gives up some availability rather than accept divergent writes.
For orders, correctness beats being writable during a partition — we would rather
reject an order than create an inconsistent one.

## Consistency model

**Strong / immediate consistency** with serializable-capable transactions. A
read after a committed write always sees the committed state.

## Why this database fits this service

Money demands ACID. Orders are structured, relational, and must never be partially
written — the classic case for a relational database with real transactions.
