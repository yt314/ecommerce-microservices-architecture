# E-Commerce Order System — Monolith → Microservices

A course project that evolves an e-commerce order system from a monolith into
production-style microservices, phase by phase.

- **Phase 1 (done):** a single .NET 8 WebAPI monolith + one SQL Server database.
- **Phase 2 (current):** split into **4 microservices** with **database-per-service**
  and **polyglot persistence**, all run from one root `docker-compose.yml`.

> Phase 2 keeps inter-service communication as plain synchronous HTTP. No API
> gateway, BFF, load balancing, message broker, saga, cache-aside, or monitoring
> yet — those are later phases.

---

## Phase 2 — Microservices

| Service | Responsibility | Database | Family | Swagger |
|---|---|---|---|---|
| ProductCatalogService | products: create/list/get/update | **MongoDB** | document | http://localhost:8081/ |
| InventoryService | stock: get/update/reserve/release | **PostgreSQL** | relational | http://localhost:8082/ |
| OrderService | orders: place/list/get + orchestration | **SQL Server** | relational | http://localhost:8083/ |
| NotificationService | record/"send" notifications | **Redis** | key-value | http://localhost:8084/ |

Each service owns its own database and **never** accesses another service's
database — the only cross-service access is HTTP. See
[docs/microservices-architecture.md](docs/microservices-architecture.md) and the
ADRs in [docs/adr/](docs/adr).

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

> **⚠️ Temporary build workaround.** NuGet restore fails *inside* Docker on this
> machine (`NU1301`), so we publish each service **on the host** first and the
> Docker images only run the published output. Once Docker can reach NuGet again,
> the Dockerfiles can return to normal multi-stage builds.

### How to run

From the repository root, run these **two** steps:

```bash
# 1) Publish all four services on the host
./publish-all.sh            # macOS/Linux/Git-Bash
#   or, in PowerShell:
#   .\publish-all.ps1

# 2) Build the images from that output and start everything (4 services + 4 DBs)
docker compose up --build
```

First run downloads MongoDB, PostgreSQL, SQL Server and Redis images and may take
a few minutes. Each service auto-creates its schema and retries until its database
is ready.

> After **any code change**, re-run the publish step and rebuild, e.g.:
> ```bash
> ./publish-all.sh && docker compose up -d --build --force-recreate
> ```

To stop: `Ctrl+C`, then `docker compose down` (add `-v` to delete the data volumes).

### How to test with Swagger (suggested end-to-end flow)

1. **ProductCatalogService** — http://localhost:8081/ → `POST /api/products`:
   ```json
   {
     "name": "Wireless Mouse",
     "description": "Ergonomic 2.4GHz mouse",
     "price": 29.90,
     "category": "Accessories",
     "isActive": true,
     "attributes": { "color": "black", "dpi": "1600" }
   }
   ```
   Copy the returned `id` (a Mongo ObjectId string, e.g. `665f...`).

2. **InventoryService** — http://localhost:8082/ → `PUT /api/inventory/{productId}`
   using that id:
   ```json
   { "quantityAvailable": 10, "quantityReserved": 0 }
   ```

3. **OrderService** — http://localhost:8083/ → `POST /api/orders`:
   ```json
   {
     "customerEmail": "buyer@example.com",
     "items": [ { "productId": "<paste id>", "quantity": 3 } ]
   }
   ```
   Expect `status: "Confirmed"`, `totalAmount: 89.70`.

4. **InventoryService** — `GET /api/inventory/{productId}` → available `7`, reserved `3`.

5. **NotificationService** — http://localhost:8084/ → `GET /api/notifications` →
   a record for the confirmed order.

6. **Failure path:** place another order with `quantity: 999` → `status: "Rejected"`
   with a `rejectionReason`, and a matching "Rejected" notification.

### Ports summary

| Component | URL / Port |
|---|---|
| ProductCatalogService | http://localhost:8081 |
| InventoryService | http://localhost:8082 |
| OrderService | http://localhost:8083 |
| NotificationService | http://localhost:8084 |
| MongoDB | localhost:27017 |
| PostgreSQL | localhost:5432 |
| SQL Server | localhost:1433 |
| Redis | localhost:6379 |

### Phase 2 checkpoint checklist

- [ ] `./publish-all.sh` then `docker compose up --build` starts all 4 services + 4 DBs
- [ ] All databases run (mongo, postgres, sqlserver, redis)
- [ ] Products can be browsed from ProductCatalogService (MongoDB)
- [ ] Inventory can be updated from InventoryService (PostgreSQL)
- [ ] An order can be placed from OrderService (SQL Server)
- [ ] Inventory reservation works (available decreases, reserved increases)
- [ ] NotificationService records a notification (Redis)
- [ ] No service accesses another service's database (HTTP only)
- [ ] ADR files exist under docs/adr/
- [ ] Everything runs from the root docker-compose.yml

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
docker-compose.yml            # Phase 2: 4 services + 4 databases (root)
docker-compose.phase1.yml     # Phase 1 monolith (preserved)
publish-all.sh / .ps1         # publish all services before compose
src/
  ECommerce.Monolith.Api/     # Phase 1 baseline
  ProductCatalogService/      # MongoDB
  InventoryService/           # PostgreSQL
  OrderService/               # SQL Server + HTTP clients
  NotificationService/        # Redis
docs/
  monolith-architecture.md
  microservices-architecture.md
  adr/                        # one ADR per database choice
```
