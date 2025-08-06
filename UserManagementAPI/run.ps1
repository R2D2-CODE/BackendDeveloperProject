# Build and Run Script for User Management API
# This script builds the solution and runs the API

Write-Host "======================================" -ForegroundColor Green
Write-Host "User Management API - Build & Run" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green

# Check if .NET 9 is installed
Write-Host "`nChecking .NET version..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ .NET is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install .NET 9.0 SDK from https://dotnet.microsoft.com/download" -ForegroundColor Red
    exit 1
}

# Restore dependencies
Write-Host "`nRestoring dependencies..." -ForegroundColor Yellow
dotnet restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Failed to restore dependencies" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Dependencies restored successfully" -ForegroundColor Green

# Build the solution
Write-Host "`nBuilding solution..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Build completed successfully" -ForegroundColor Green

# Run the API
Write-Host "`nStarting User Management API..." -ForegroundColor Yellow
Write-Host "The API will be available at:" -ForegroundColor Cyan
Write-Host "  • HTTPS: https://localhost:7001" -ForegroundColor Cyan
Write-Host "  • HTTP:  http://localhost:5001" -ForegroundColor Cyan
Write-Host "  • Swagger UI: https://localhost:7001" -ForegroundColor Cyan
Write-Host "`nPress Ctrl+C to stop the API" -ForegroundColor Yellow
Write-Host "======================================`n" -ForegroundColor Green

Set-Location "src/WebApi"
dotnet run --configuration Release --no-build
