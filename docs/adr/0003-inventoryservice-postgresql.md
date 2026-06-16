# ADR 0003 — InventoryService uses PostgreSQL (relational database)

- **Status:** Accepted (Phase 2)
- **Service:** InventoryService

## Context

Inventory tracks `QuantityAvailable` and `QuantityReserved` per product. Reserving
stock is a **read-modify-write** that must be correct under concurrency: two
orders must not both reserve the last unit. This calls for a consistent,
transactional store. The brief allows SQL Server *or* PostgreSQL here.

## Decision

Use **PostgreSQL** (via EF Core + Npgsql). Each reserve/release is a single
atomic `SaveChanges`.

### Why PostgreSQL instead of a second SQL Server (the technology substitution)

- **Resource cost:** a second SQL Server container needs ~2 GB RAM. PostgreSQL is
  far lighter, which matters when running 8 containers on a laptop.
- **Cleaner database-per-service:** giving Inventory its *own engine* (not just a
  second database inside OrderService's SQL Server instance) makes the ownership
  boundary unambiguous.
- **Polyglot demonstration:** it shows two different relational engines chosen per
  service, strengthening the polyglot-persistence story.
- **Parity:** PostgreSQL is fully ACID and a first-class EF Core provider, so we
  lose none of the guarantees inventory needs.

## Consequences

- ✅ ACID transactions protect concurrent reservations.
- ✅ Lightweight container, fast startup.
- ⚠️ A second relational dialect to operate (minor; EF Core abstracts most of it).
- ⚠️ Like any single primary, horizontal write scaling needs replicas/partitioning.

## ACID / BASE

**ACID.** Reserving stock is a correctness-critical, transactional operation;
overselling is unacceptable, so BASE/eventual consistency is the wrong fit.

## CAP

A single PostgreSQL primary is **CP**: under a partition it favours **consistency**
over availability. For inventory that is the right trade-off — better to reject a
reservation than to oversell.

## Consistency model

**Strong / immediate consistency.** Reservations see the latest committed
quantities; row-level locking within a transaction prevents lost updates.

## Why this database fits this service

Inventory needs transactional correctness under concurrency but is simple,
structured data — a relational database is ideal, and PostgreSQL delivers the same
ACID guarantees as SQL Server with a smaller footprint.
