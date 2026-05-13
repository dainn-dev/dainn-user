#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build DainnUser documentation using DocFX
.DESCRIPTION
    This script builds the DainnUser solution to generate XML documentation,
    installs DocFX if needed, and builds the documentation site.
.PARAMETER Clean
    Clean the output directory before building
.PARAMETER Serve
    Serve the documentation site locally after building
.EXAMPLE
    .\build.ps1
    .\build.ps1 -Clean
    .\build.ps1 -Serve
#>

[CmdletBinding()]
param(
    [switch]$Clean,
    [switch]$Serve
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Paths
$ScriptDir = $PSScriptRoot
$RootDir = Split-Path $ScriptDir -Parent
$SolutionFile = Join-Path $RootDir "dainn-user.sln"
$DocFxJson = Join-Path $ScriptDir "docfx.json"
$SiteDir = Join-Path $ScriptDir "_site"

Write-Host "==> DainnUser Documentation Build Script" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean if requested
if ($Clean -and (Test-Path $SiteDir)) {
    Write-Host "[1/4] Cleaning output directory..." -ForegroundColor Yellow
    Remove-Item $SiteDir -Recurse -Force
    Write-Host "      Cleaned: $SiteDir" -ForegroundColor Green
} else {
    Write-Host "[1/4] Skipping clean" -ForegroundColor Gray
}

# Step 2: Build solution to generate XML documentation
Write-Host "[2/4] Building solution..." -ForegroundColor Yellow
Push-Location $RootDir
try {
    dotnet restore $SolutionFile
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed" }

    dotnet build $SolutionFile --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }

    Write-Host "      Solution built successfully" -ForegroundColor Green
} finally {
    Pop-Location
}

# Step 3: Install DocFX if not available
Write-Host "[3/4] Checking DocFX..." -ForegroundColor Yellow
$DocFxVersion = "2.77.0"
$DocFxExe = $null

# Check if docfx is in PATH
$DocFxInPath = Get-Command docfx -ErrorAction SilentlyContinue
if ($DocFxInPath) {
    $DocFxExe = $DocFxInPath.Source
    Write-Host "      Found DocFX in PATH: $DocFxExe" -ForegroundColor Green
} else {
    # Check if docfx is installed as dotnet tool
    $DocFxToolCheck = dotnet tool list --global | Select-String "docfx"
    if ($DocFxToolCheck) {
        Write-Host "      Found DocFX as dotnet tool" -ForegroundColor Green
        $DocFxExe = "docfx"
    } else {
        Write-Host "      Installing DocFX as dotnet tool..." -ForegroundColor Yellow
        dotnet tool install --global docfx --version $DocFxVersion
        if ($LASTEXITCODE -ne 0) { throw "Failed to install DocFX" }
        $DocFxExe = "docfx"
        Write-Host "      DocFX installed successfully" -ForegroundColor Green
    }
}

# Step 4: Build documentation
Write-Host "[4/4] Building documentation..." -ForegroundColor Yellow
Push-Location $ScriptDir
try {
    & $DocFxExe build $DocFxJson
    if ($LASTEXITCODE -ne 0) { throw "DocFX build failed" }

    Write-Host "      Documentation built successfully" -ForegroundColor Green
    Write-Host "      Output: $SiteDir" -ForegroundColor Green
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "==> Build completed successfully!" -ForegroundColor Green

# Serve if requested
if ($Serve) {
    Write-Host ""
    Write-Host "==> Starting local server..." -ForegroundColor Cyan
    Push-Location $ScriptDir
    try {
        & $DocFxExe serve $SiteDir
    } finally {
        Pop-Location
    }
}
