# E-Commerce Order System — Monolith → Microservices

A course project that evolves an e-commerce order system from a monolith into
production-style microservices, phase by phase.

- **Phase 1 (done):** a single .NET 8 WebAPI monolith + one SQL Server database.
- **Phase 2 (done):** split into **4 microservices** with **database-per-service**
  and **polyglot persistence**.
- **Phase 3 (done):** **API Gateway (YARP)**, a **BFF**, and **load balancing**
  (2 ProductCatalog replicas behind Nginx).
- **Phase 4 (done):** **async order saga over RabbitMQ** (choreography),
  happy + compensation paths, idempotent consumers, and **Redis cache-aside** for
  ProductCatalog reads.
- **Phase 5 (current):** **monitoring & observability** — structured logging
  (Serilog) in every service, aggregated to **Seq**, `/health` endpoints wired into
  docker-compose healthchecks, and a **Correlation ID** that traces one order
  end-to-end across HTTP **and** the RabbitMQ saga.

> CI/CD and the bonus phases are out of scope for now.

---

## Phase 5 — Monitoring & Observability (current)

Every service now logs **structured** events with **Serilog** to the console and to
a central **Seq** aggregator, and a single **Correlation ID** ties one order's whole
journey together — including across the message broker.

### What was added

- **Serilog → Seq.** All 7 apps write structured logs to console + Seq. Browse and
  filter them at **http://localhost:5341**. Each event carries `Service`,
  `CorrelationId`, and (where relevant) `OrderId`.
- **Correlation ID.** `X-Correlation-ID` is created (or accepted) at the **API
  Gateway** boundary and flows down every hop:
  - over **HTTP** via a middleware (`UseCorrelationId`) + a propagation handler on
    outgoing calls (Order→Catalog, BFF→Order/Catalog), and
  - over **RabbitMQ** via the message's native `BasicProperties.CorrelationId` —
    the publisher stamps it, the consumer reads it back, so the **same id survives
    the broker hops**, not just HTTP headers.
- **Healthchecks.** Every .NET service exposes `/health` and has a docker-compose
  `healthcheck` (a self-probe: `dotnet <Service>.dll --healthcheck`, no extra image
  tooling needed). `docker ps` shows each app as `(healthy)`.

### The correlation flow

```
Client ─[X-Correlation-ID?]→ API Gateway (creates id if absent, forwards it)
  → OrderService  (Pending, PUBLISH order.placed  + CorrelationId)
     → RabbitMQ → InventoryService (reserve, PUBLISH inventory.reserved/rejected + same CorrelationId)
        → RabbitMQ → OrderService  (Confirm/Reject, PUBLISH order.confirmed/rejected + same CorrelationId)
           → RabbitMQ → NotificationService (records + logs with the same CorrelationId)
```

### Trace one order in Seq

```bash
# Send your own id so it's easy to find, or let the gateway generate one:
curl -X POST http://localhost:8080/orders/api/orders \
  -H "Content-Type: application/json" -H "X-Correlation-ID: PHASE5-DEMO-001" \
  -d '{"customerEmail":"obs@demo.com","items":[{"productId":"<PID>","quantity":3}]}'
```

1. Open **http://localhost:5341**.
2. In the filter box enter: `CorrelationId = 'PHASE5-DEMO-001'`
3. You see one timeline spanning **ApiGateway → OrderService → InventoryService →
   OrderService → NotificationService**, including the `PUBLISH` / `CONSUME` events
   that crossed RabbitMQ — all sharing that one id.

You can also see it on the console:

```bash
docker logs ecommerce-order        2>&1 | grep PHASE5-DEMO-001
docker logs ecommerce-inventory    2>&1 | grep PHASE5-DEMO-001
docker logs ecommerce-notification 2>&1 | grep PHASE5-DEMO-001
```

### Phase 5 checkpoint checklist

- [x] Seq runs (UI at http://localhost:5341)
- [x] Every service writes structured logs (console + Seq)
- [x] `/health` exists per service
- [x] docker-compose healthchecks exist (apps show `(healthy)`)
- [x] Correlation ID is created or accepted at the Gateway/API boundary
- [x] Correlation ID appears in HTTP logs
- [x] Correlation ID appears in the RabbitMQ message flow (survives broker hops)
- [x] One saga can be traced end-to-end by one CorrelationId
- [x] README and architecture docs updated

---

## Phase 4 — Async Messaging, Saga & Caching (done)

Order placement is now **asynchronous**. `POST /orders` returns a **Pending**
order immediately; the final status is decided by a **RabbitMQ choreography saga**.

```
POST /orders → Pending → [OrderPlaced] → Inventory reserves → [InventoryReserved]
            → Order Confirmed → [OrderConfirmed] → Notification records "Confirmed"
out of stock → [InventoryRejected] → Order Rejected → [OrderRejected] → "Rejected"
```

- **Broker:** RabbitMQ, durable topic exchange `ecommerce.events`, durable queues,
  persistent messages, **management UI at http://localhost:15672 (guest/guest)**.
- **Idempotency:** Inventory has a `ProcessedOrders` table; OrderService only acts
  while `Pending`; NotificationService uses Redis `SET ... NX`. Consumers are
  prefetch=1. (Details in [docs/microservices-architecture.md](docs/microservices-architecture.md).)
- **Cache-aside:** `GET /api/products/{id}` caches to Redis key
  `catalog:product:{id}` (logical **DB 1**); `PUT` invalidates it.

> **⚠️ Upgrading from a previous phase?** Phase 4 adds a table to the Inventory
> database and the app uses `EnsureCreated` (not migrations), which won't alter an
> existing DB. Run a **one-time** `docker compose down -v` before `up` so all
> schemas are recreated. (On a fresh machine this is automatic.)

### Run it

```bash
./publish-all.sh            # or  .\publish-all.ps1
docker compose down -v      # only when upgrading from an earlier phase's volumes
docker compose up --build   # gateway + bff + nginx LB + RabbitMQ + 4 services + 4 DBs
```

### Test the async happy path

```bash
# create a product + stock
PID=...   # id returned by POST /catalog/api/products, then PUT /inventory/api/inventory/$PID {"quantityAvailable":5,...}
# place order — returns status "Pending" immediately
curl -X POST http://localhost:8080/orders/api/orders -H "Content-Type: application/json" \
  -d '{"customerEmail":"a@b.com","items":[{"productId":"'$PID'","quantity":2}]}'
# poll — becomes "Confirmed" within ~1-2s; inventory available drops, reserved rises
curl http://localhost:8080/orders/api/orders/<ORDER_ID>
curl http://localhost:8080/notifications/api/notifications     # a "Confirmed" record appears
```

### Test the out-of-stock failure path

```bash
# product with only 1 in stock, order 10:
curl -X POST http://localhost:8080/orders/api/orders -H "Content-Type: application/json" \
  -d '{"customerEmail":"a@b.com","items":[{"productId":"'$PID'","quantity":10}]}'
curl http://localhost:8080/orders/api/orders/<ORDER_ID>   # becomes "Rejected" with a reason
# inventory stays UNCHANGED; a "Rejected" notification is recorded
```

### Prove cache miss vs hit

```bash
# GET the same product twice, then update it, then GET again:
curl http://localhost:8080/catalog/api/products/<PID>   # x2
curl -X PUT http://localhost:8080/catalog/api/products/<PID> -H "Content-Type: application/json" -d '{...}'
curl http://localhost:8080/catalog/api/products/<PID>
# inspect logs of both replicas:
docker logs project-ai-productcatalog-1 | grep CACHE
docker logs project-ai-productcatalog-2 | grep CACHE
# expect: CACHE MISS, then CACHE HIT, then CACHE INVALIDATE, then CACHE MISS
```

### Phase 4 checkpoint checklist

- [ ] RabbitMQ runs (UI at http://localhost:15672, guest/guest)
- [ ] OrderService publishes `OrderPlaced`; order returns as Pending
- [ ] InventoryService consumes `OrderPlaced` and reserves stock
- [ ] InventoryService publishes `InventoryReserved` / `InventoryRejected`
- [ ] OrderService confirms/rejects the order asynchronously
- [ ] NotificationService records the final notification from events
- [ ] Out-of-stock order becomes Rejected, inventory unchanged
- [ ] Cache-aside works: `CACHE MISS` then `CACHE HIT` in catalog logs
- [ ] Gateway and BFF still work
- [ ] `docker compose up` runs everything from the root
- [ ] README and architecture docs updated

---

## Phase 3 — API Gateway, BFF & Load Balancing (current)

**Everything is reached through the API Gateway at `http://localhost:8080`.** The
individual services are no longer exposed to the host.

### Gateway routes

| Through the gateway | Goes to |
|---|---|
| `http://localhost:8080/catalog/api/products` | ProductCatalogService (via Nginx LB → 2 replicas) |
| `http://localhost:8080/inventory/api/inventory/{productId}` | InventoryService |
| `http://localhost:8080/orders/api/orders` | OrderService |
| `http://localhost:8080/notifications/api/notifications` | NotificationService |
| `http://localhost:8080/bff/api/order-details/{orderId}` | WebBffService (BFF) |

### Gateway vs BFF

- **Gateway (YARP):** single entry point + generic, domain-agnostic **routing**
  (and future edge concerns like rate limiting). One path prefix → one service.
- **BFF (WebBffService):** **client-specific aggregation** — `order-details`
  combines an order (OrderService) with each item's product (ProductCatalogService)
  into one response. This domain logic belongs in the BFF, not the gateway.

### Run it

```bash
./publish-all.sh            # or  .\publish-all.ps1
docker compose up --build   # starts gateway + bff + nginx LB + 4 services + 4 DBs
```

`docker compose up` honors `deploy.replicas: 2` for ProductCatalogService. To use
more replicas: `docker compose up -d --scale productcatalog=3`.

### Test through the gateway (end-to-end)

```bash
# 1) create a product (note the returned id)
curl -X POST http://localhost:8080/catalog/api/products -H "Content-Type: application/json" \
  -d '{"name":"Mechanical Keyboard","price":75.00,"category":"Accessories","isActive":true,"attributes":{"switch":"blue"}}'

# 2) set inventory (use the id)
curl -X PUT http://localhost:8080/inventory/api/inventory/<ID> -H "Content-Type: application/json" \
  -d '{"quantityAvailable":20,"quantityReserved":0}'

# 3) place an order (use the id) -> note the order id
curl -X POST http://localhost:8080/orders/api/orders -H "Content-Type: application/json" \
  -d '{"customerEmail":"buyer@example.com","items":[{"productId":"<ID>","quantity":2}]}'

# 4) BFF aggregated order details (order + live product data)
curl http://localhost:8080/bff/api/order-details/<ORDER_ID>
```

### Prove load balancing

```bash
# Repeated calls alternate between the two replica container ids:
for i in $(seq 1 10); do curl -s http://localhost:8080/catalog/api/products/instance; echo; done

# Resilience: kill one replica, requests still succeed from the other:
docker stop project-ai-productcatalog-1
curl -s http://localhost:8080/catalog/api/products/instance
docker start project-ai-productcatalog-1
```

### Phase 3 checkpoint checklist

- [ ] Gateway runs and is reachable at http://localhost:8080/health
- [ ] Client can access all APIs through the gateway (catalog/inventory/orders/notifications)
- [ ] Internal services still run (and are no longer exposed directly to the host)
- [ ] BFF aggregates order + product data at `/bff/api/order-details/{id}`
- [ ] ProductCatalogService runs 2+ replicas
- [ ] Load-balancing proof works (alternating `instanceId` / `X-Instance-Id`)
- [ ] `docker compose up` runs everything from the root
- [ ] README and architecture docs are updated

---

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

> **⚠️ Temporary build workaround.** NuGet restore fails *inside* Docker on this
> machine (`NU1301`), so we publish each service **on the host** first and the
> Docker images only run the published output. Once Docker can reach NuGet again,
> the Dockerfiles can return to normal multi-stage builds.
>
> After **any code change**: `./publish-all.sh && docker compose up -d --build --force-recreate`.
> To stop: `docker compose down` (add `-v` to delete the data volumes).

---

## Phase 2 — Microservices (preserved, now behind the gateway)

The four services and their databases from Phase 2 are unchanged. **In Phase 3
their direct host ports are no longer exposed** — reach them through the gateway
(see the Phase 3 section above).

| Service | Responsibility | Database | Family | Gateway prefix |
|---|---|---|---|---|
| ProductCatalogService | products: create/list/get/update | **MongoDB** | document | `/catalog` |
| InventoryService | stock: get/update/reserve/release | **PostgreSQL** | relational | `/inventory` |
| OrderService | orders: place/list/get + orchestration | **SQL Server** | relational | `/orders` |
| NotificationService | record/"send" notifications | **Redis** | key-value | `/notifications` |

Each service owns its own database and **never** accesses another service's
database — the only cross-service access is HTTP. Database design rationale is in
the ADRs in [docs/adr/](docs/adr); architecture details in
[docs/microservices-architecture.md](docs/microservices-architecture.md).

Order placement (now also reachable via the gateway): OrderService validates each
product against ProductCatalogService, reserves stock via InventoryService,
persists a Confirmed/Rejected order, and records a notification via
NotificationService.

### Ports summary

| Component | URL / Port | Exposed to host? |
|---|---|---|
| **API Gateway** | http://localhost:8080 | **Yes (only app entry)** |
| **RabbitMQ management UI** | http://localhost:15672 (guest/guest) | Yes (dev) |
| **Seq log aggregator (Phase 5)** | http://localhost:5341 | Yes (dev) |
| ProductCatalogService (×2), Inventory, Order, Notification, BFF, catalog-lb | — | No (internal) |
| MongoDB / PostgreSQL / SQL Server / Redis / RabbitMQ-AMQP | 27017 / 5432 / 1433 / 6379 / 5672 | Yes (dev convenience) |

---

## Phase 1 — Monolith (baseline, preserved)

The original monolith lives in [src/ECommerce.Monolith.Api/](src/ECommerce.Monolith.Api)
and its compose file is [docker-compose.phase1.yml](docker-compose.phase1.yml).
It is kept for "before vs. after" comparison. To run it on its own:

```bash
dotnet publish ./src/ECommerce.Monolith.Api/ECommerce.Monolith.Api.csproj -c Release -o ./src/ECommerce.Monolith.Api/publish
docker compose -f docker-compose.phase1.yml up --build
```

Monolith docs: [docs/monolith-architecture.md](docs/monolith-architecture.md).

---

## Repository structure

```
docker-compose.yml            # Phase 4: gateway + bff + nginx LB + RabbitMQ + 4 services + 4 DBs
docker-compose.phase1.yml     # Phase 1 monolith (preserved)
publish-all.sh / .ps1         # publish all services before compose
infra/
  catalog-lb/nginx.conf       # Nginx load balancer for catalog replicas
src/
  ECommerce.Monolith.Api/     # Phase 1 baseline
  Shared.Messaging/           # Phase 4: RabbitMQ contracts + publish/consume helpers
                              #   + Phase 5: CorrelationContext (survives broker hops)
  Shared.Observability/       # Phase 5: Serilog/Seq setup, correlation middleware,
                              #   HTTP propagation handler, health-probe
  ProductCatalogService/      # MongoDB (2 replicas) + Redis cache-aside (Phase 4)
  InventoryService/           # PostgreSQL + saga consumer (Phase 4)
  OrderService/               # SQL Server + saga publisher/consumer (Phase 4)
  NotificationService/        # Redis + saga consumer (Phase 4)
  WebBffService/              # BFF (aggregation, no DB)
  ApiGateway/                 # YARP gateway (single entry point)
docs/
  monolith-architecture.md
  microservices-architecture.md   # includes the Phase 3 + Phase 4 sections + diagrams
  adr/                        # one ADR per database choice
```
