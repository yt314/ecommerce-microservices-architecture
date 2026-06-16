# ADR 0004 — NotificationService uses Redis (key-value database)

- **Status:** Accepted (Phase 2)
- **Service:** NotificationService

## Context

NotificationService records that a customer was informed of an order outcome
(Confirmed/Rejected). The records are simple, accessed by key, write-once, and not
business-critical in the way money or stock are. We want something fast and
lightweight. (Note: this is **not** the Phase 4 cache-aside use of Redis — here
Redis is the service's primary store of notification records.)

## Decision

Use **Redis**, a key-value store, via StackExchange.Redis. Layout:
- `notification:nextid` — an integer counter (`INCR`) for ids.
- `notification:{id}` — the JSON record.
- `notifications` — a list of ids for browsing.

"Sending" is simulated by logging; no real email provider is used.

## Consequences

- ✅ Extremely fast, simple key/value access; trivial to operate.
- ✅ Adds a third NoSQL family (key-value) to the polyglot mix for the course.
- ⚠️ Not designed for rich queries (no "find by email" without extra index keys).
- ⚠️ Durability is weaker than a relational DB (RDB/AOF persistence); acceptable
  because a lost notification record is not catastrophic.

## ACID / BASE

**BASE.** Individual Redis commands are atomic, but we do not need multi-key ACID
transactions. Availability and speed are prioritised over strict consistency —
appropriate for non-critical notification records.

## CAP

A single Redis node is not partitioned. In a clustered/replicated setup Redis
leans **AP**: it stays available and uses asynchronous replication, accepting
**eventual consistency** (a failover can lose the last unreplicated writes). That
trade-off is fine for notifications.

## Consistency model

**Eventual consistency** in a replicated deployment (read-your-writes on a single
node, as in Phase 2). Good enough for "we recorded that we notified the customer".

## Why this database fits this service

Notification records are simple, keyed, write-mostly, and tolerant of weaker
durability/consistency — the canonical key-value-store use case, and it gives us a
key-value member of the polyglot-persistence set.
