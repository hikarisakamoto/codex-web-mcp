#requires -Version 5.1
<#
.SYNOPSIS
    Builds both the C# and Go implementations of codex-web-mcp.

.PARAMETER SkipCsharp
    Skip building the C# implementation.

.PARAMETER SkipGo
    Skip building the Go implementation.
#>
param(
    [switch]$SkipCsharp,
    [switch]$SkipGo
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
Write-Host "Repository root: $repoRoot" -ForegroundColor Cyan

if (-not $SkipCsharp) {
    Write-Host ""
    Write-Host "==> Building C# implementation (Release / win-x64)..." -ForegroundColor Yellow
    $csproj    = Join-Path $repoRoot 'csharp/src/CodexWebMcp/CodexWebMcp.csproj'
    $publishOut = Join-Path $repoRoot 'csharp/src/CodexWebMcp/publish'
    & dotnet publish $csproj -c Release -r win-x64 -o $publishOut
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
    $csBinary = Join-Path $publishOut 'CodexWebMcp.exe'
    Write-Host "    OK: $csBinary" -ForegroundColor Green
} else {
    Write-Host "==> Skipping C# build (-SkipCsharp)" -ForegroundColor DarkGray
}

if (-not $SkipGo) {
    Write-Host ""
    Write-Host "==> Building Go implementation..." -ForegroundColor Yellow
    Push-Location (Join-Path $repoRoot 'go')
    try {
        & go build -ldflags='-s -w' -o bin/codex-web-mcp.exe ./cmd/codex-web-mcp
        if ($LASTEXITCODE -ne 0) { throw "go build failed with exit code $LASTEXITCODE" }
    } finally {
        Pop-Location
    }
    $goBinary = Join-Path $repoRoot 'go/bin/codex-web-mcp.exe'
    Write-Host "    OK: $goBinary" -ForegroundColor Green
} else {
    Write-Host "==> Skipping Go build (-SkipGo)" -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "All requested builds completed successfully." -ForegroundColor Green
