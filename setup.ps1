# ============================================================
# setup.ps1 – VisioPed Backend Setup
# Jalankan dari folder Backend:
#   klik kanan folder Backend di Explorer → "Open in Terminal"
#   lalu ketik: .\setup.ps1
# ============================================================

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "   VisioPed Backend Setup" -ForegroundColor Cyan
Write-Host "   Server: LAPTOP-7B4A2GEF\SQLEXPRESS" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Restore packages
Write-Host "[1/5] Restore NuGet packages..." -ForegroundColor Yellow
dotnet restore
Write-Host "      OK" -ForegroundColor Green

# 2. Build
Write-Host ""
Write-Host "[2/5] Build project..." -ForegroundColor Yellow
dotnet build --no-restore
Write-Host "      OK" -ForegroundColor Green

# 3. Install dotnet-ef tool
Write-Host ""
Write-Host "[3/5] Install/Update dotnet-ef tool..." -ForegroundColor Yellow
$efInstalled = dotnet tool list --global | Select-String "dotnet-ef"
if ($efInstalled) {
    dotnet tool update --global dotnet-ef --version 8.0.11
} else {
    dotnet tool install --global dotnet-ef --version 8.0.11
}
Write-Host "      OK" -ForegroundColor Green

# 4. Buat EF Migration
Write-Host ""
Write-Host "[4/5] Buat EF Core migration..." -ForegroundColor Yellow
$migrationExists = Get-ChildItem -Path ".\Migrations" -ErrorAction SilentlyContinue
if ($migrationExists) {
    Write-Host "      Migration sudah ada, skip." -ForegroundColor Gray
} else {
    dotnet ef migrations add InitialCreate
    Write-Host "      Migration dibuat." -ForegroundColor Green
}

# 5. Apply migration (buat tabel via EF)
Write-Host ""
Write-Host "[5/5] Apply migration ke database..." -ForegroundColor Yellow
dotnet ef database update
Write-Host "      Database berhasil diupdate." -ForegroundColor Green

Write-Host ""
Write-Host "==================================================" -ForegroundColor Green
Write-Host "   SETUP EF SELESAI!" -ForegroundColor Green
Write-Host ""
Write-Host "   LANGKAH SELANJUTNYA:" -ForegroundColor White
Write-Host "   1. Buka SSMS" -ForegroundColor White
Write-Host "   2. Connect ke: LAPTOP-7B4A2GEF\SQLEXPRESS" -ForegroundColor White
Write-Host "   3. Jalankan: Database\create_database.sql" -ForegroundColor White
Write-Host "      (untuk Stored Procedures + Seed Data)" -ForegroundColor White
Write-Host ""
Write-Host "   4. Jalankan backend: dotnet run" -ForegroundColor White
Write-Host "   5. Swagger UI: http://localhost:5066/swagger" -ForegroundColor White
Write-Host "==================================================" -ForegroundColor Green
Write-Host ""
