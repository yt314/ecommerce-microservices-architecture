# ADR 0001 — ProductCatalogService uses MongoDB (document database)

- **Status:** Accepted (Phase 2)
- **Service:** ProductCatalogService

## Context

The product catalog must store many product types. Different categories need
different attributes: a laptop has `ram` and `cpu`, a shirt has `size` and
`color`, a book has `author` and `isbn`. In a single relational table this leads
to either dozens of mostly-null columns or an awkward EAV (entity-attribute-value)
design. Catalog access is also **read-heavy** (browsing) and rarely needs
multi-row transactions. Each product is a self-contained aggregate.

## Decision

Use **MongoDB**, a document database. Each product is one JSON/BSON document with
a flexible `Attributes` bag for category-specific fields. The `_id` is a MongoDB
`ObjectId` exposed to other services as a string.

## Consequences

- ✅ Flexible, evolving schema with no migrations for new attributes.
- ✅ A product aggregate is read/written in a single document — no joins.
- ✅ Horizontal scaling (sharding) and read scaling fit a large catalog.
- ⚠️ No cross-document ACID transactions in our usage; not a problem because a
  product is a single document.
- ⚠️ Weaker built-in referential integrity — the application owns consistency of
  references (e.g. product ids used by other services).

## ACID / BASE

MongoDB here follows the **BASE** philosophy (Basically Available, Soft state,
Eventually consistent) rather than strict ACID. Single-document writes *are*
atomic, which is all the catalog needs. We deliberately do **not** rely on
multi-document transactions. This is acceptable because a catalog can tolerate a
brief propagation delay; nothing about money or stock is stored here.

## CAP

In a replica-set deployment MongoDB is typically a **CP**-leaning system: on a
network partition it keeps a single primary for writes (consistency) and may make
the minority side unavailable for writes (sacrificing availability). Reads can be
tuned toward availability via secondary reads / read preferences. For a catalog
we favour correctness of the current primary's data while allowing scalable reads.

## Consistency model

**Strong consistency** for reads against the primary; **eventual consistency** if
reading from secondaries. For Phase 2 (single node) reads are read-your-writes.

## Why this database fits this service

The catalog is the textbook document-database use case: heterogeneous,
self-contained, read-heavy records with a flexible schema and no need for
multi-entity transactions.
