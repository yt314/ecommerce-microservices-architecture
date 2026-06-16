# Publishes all Phase 2 services to their .\publish folders so the Docker images
# (which only copy published output) can run them. Required because NuGet restore
# fails inside Docker on this machine.
$ErrorActionPreference = "Stop"

$services = @("ProductCatalogService", "InventoryService", "OrderService", "NotificationService")
foreach ($svc in $services) {
    Write-Host "=== Publishing $svc ==="
    Remove-Item -Recurse -Force ".\src\$svc\publish" -ErrorAction SilentlyContinue
    dotnet publish ".\src\$svc\$svc.csproj" -c Release -o ".\src\$svc\publish"
}

Write-Host "All services published. Now run: docker compose up --build"
