# setup.ps1 — Run this once in the "InternetCafeBackend" directory to restore,
#              add the EF migration, and launch the API.
#
# Usage:
#   cd "D:\projects\InternetCafeBackend"
#   .\setup.ps1

$ErrorActionPreference = "Stop"

Write-Host "==> Restoring NuGet packages..." -ForegroundColor Cyan
dotnet restore CyberCafe.sln

Write-Host ""
Write-Host "==> Building solution..." -ForegroundColor Cyan
dotnet build CyberCafe.sln --no-restore

Write-Host ""
Write-Host "==> Installing EF Core tools (if not already present)..." -ForegroundColor Cyan
dotnet tool install --global dotnet-ef --ignore-failed-sources 2>$null
dotnet tool update  --global dotnet-ef 2>$null

Write-Host ""
Write-Host "==> Creating initial EF migration..." -ForegroundColor Cyan
# Migrations are stored in Infrastructure; startup project is API
dotnet ef migrations add InitialCreate `
    --project   CyberCafe.Infrastructure `
    --startup-project CyberCafe.API `
    --output-dir Data/Migrations

Write-Host ""
Write-Host "==> Applying migration (creates cybercafe.db for SQLite)..." -ForegroundColor Cyan
dotnet ef database update `
    --project   CyberCafe.Infrastructure `
    --startup-project CyberCafe.API

Write-Host ""
Write-Host "==> Starting CyberCafe API..." -ForegroundColor Green
Write-Host "    Swagger UI will be available at: https://localhost:5001/swagger" -ForegroundColor Yellow
dotnet run --project CyberCafe.API
