#!/usr/bin/env bash
# Publishes all Phase 2 services to their ./publish folders so the Docker images
# (which only copy published output) can run them. Required because NuGet restore
# fails inside Docker on this machine.
set -e

services=(ProductCatalogService InventoryService OrderService NotificationService)
for svc in "${services[@]}"; do
  echo "=== Publishing $svc ==="
  rm -rf "./src/$svc/publish"
  dotnet publish "./src/$svc/$svc.csproj" -c Release -o "./src/$svc/publish"
done

echo "All services published. Now run: docker compose up --build"
