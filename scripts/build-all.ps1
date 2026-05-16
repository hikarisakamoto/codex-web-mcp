#requires -Version 5.1
<#
.SYNOPSIS
    Builds every available implementation of codex-web-mcp. Stacks whose
    toolchain is missing are skipped with a warning.

.PARAMETER SkipCsharp
.PARAMETER SkipFsharp
.PARAMETER SkipGo
.PARAMETER SkipJava
.PARAMETER SkipNode
.PARAMETER SkipPython
#>
param(
    [switch]$SkipCsharp,
    [switch]$SkipFsharp,
    [switch]$SkipGo,
    [switch]$SkipJava,
    [switch]$SkipNode,
    [switch]$SkipPython
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
Write-Host "Repository root: $repoRoot" -ForegroundColor Cyan

function Has-Cmd($name) {
    return [bool](Get-Command $name -ErrorAction SilentlyContinue)
}

# --- C# ---
if (-not $SkipCsharp) {
    if (-not (Has-Cmd dotnet)) {
        Write-Host "==> Skipping C# build (dotnet not on PATH)" -ForegroundColor DarkYellow
    } else {
        Write-Host ""
        Write-Host "==> Building C# implementation (Release / win-x64)..." -ForegroundColor Yellow
        $csproj    = Join-Path $repoRoot 'csharp/src/CodexWebMcp/CodexWebMcp.csproj'
        $publishOut = Join-Path $repoRoot 'csharp/src/CodexWebMcp/publish'
        & dotnet publish $csproj -c Release -r win-x64 -o $publishOut
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
        Write-Host "    OK: $(Join-Path $publishOut 'CodexWebMcp.exe')" -ForegroundColor Green
    }
} else { Write-Host "==> Skipping C# build (-SkipCsharp)" -ForegroundColor DarkGray }

# --- F# ---
if (-not $SkipFsharp) {
    if (-not (Has-Cmd dotnet)) {
        Write-Host "==> Skipping F# build (dotnet not on PATH)" -ForegroundColor DarkYellow
    } else {
        Write-Host ""
        Write-Host "==> Building F# implementation (Release / win-x64)..." -ForegroundColor Yellow
        $fsproj     = Join-Path $repoRoot 'fsharp/src/CodexWebMcp.FSharp/CodexWebMcp.FSharp.fsproj'
        $fsPublish  = Join-Path $repoRoot 'fsharp/src/CodexWebMcp.FSharp/publish'
        & dotnet publish $fsproj -c Release -r win-x64 --self-contained -o $fsPublish
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish (F#) failed with exit code $LASTEXITCODE" }
        Write-Host "    OK: $(Join-Path $fsPublish 'CodexWebMcp.FSharp.exe')" -ForegroundColor Green
    }
} else { Write-Host "==> Skipping F# build (-SkipFsharp)" -ForegroundColor DarkGray }

# --- Go ---
if (-not $SkipGo) {
    if (-not (Has-Cmd go)) {
        Write-Host "==> Skipping Go build (go not on PATH)" -ForegroundColor DarkYellow
    } else {
        Write-Host ""
        Write-Host "==> Building Go implementation..." -ForegroundColor Yellow
        Push-Location (Join-Path $repoRoot 'go')
        try {
            & go build -ldflags='-s -w' -o bin/codex-web-mcp.exe ./cmd/codex-web-mcp
            if ($LASTEXITCODE -ne 0) { throw "go build failed with exit code $LASTEXITCODE" }
        } finally { Pop-Location }
        Write-Host "    OK: $(Join-Path $repoRoot 'go/bin/codex-web-mcp.exe')" -ForegroundColor Green
    }
} else { Write-Host "==> Skipping Go build (-SkipGo)" -ForegroundColor DarkGray }

# --- Java ---
if (-not $SkipJava) {
    if (-not (Has-Cmd mvn)) {
        Write-Host "==> Skipping Java build (mvn not on PATH)" -ForegroundColor DarkYellow
    } else {
        Write-Host ""
        Write-Host "==> Building Java implementation (mvn -B package)..." -ForegroundColor Yellow
        Push-Location (Join-Path $repoRoot 'java')
        try {
            & mvn -B package -DskipTests
            if ($LASTEXITCODE -ne 0) { throw "mvn package failed with exit code $LASTEXITCODE" }
        } finally { Pop-Location }
        Write-Host "    OK: $(Join-Path $repoRoot 'java/target/codex-web-mcp.jar')" -ForegroundColor Green
    }
} else { Write-Host "==> Skipping Java build (-SkipJava)" -ForegroundColor DarkGray }

# --- Node ---
if (-not $SkipNode) {
    if (-not (Has-Cmd npm)) {
        Write-Host "==> Skipping Node build (npm not on PATH)" -ForegroundColor DarkYellow
    } else {
        Write-Host ""
        Write-Host "==> Building Node TypeScript implementation..." -ForegroundColor Yellow
        Push-Location (Join-Path $repoRoot 'node')
        try {
            & npm install --no-audit --no-fund
            if ($LASTEXITCODE -ne 0) { throw "npm install failed with exit code $LASTEXITCODE" }
            & npm run build
            if ($LASTEXITCODE -ne 0) { throw "npm run build failed with exit code $LASTEXITCODE" }
        } finally { Pop-Location }
        Write-Host "    OK: $(Join-Path $repoRoot 'node/dist/index.js')" -ForegroundColor Green
    }
} else { Write-Host "==> Skipping Node build (-SkipNode)" -ForegroundColor DarkGray }

# --- Python ---
if (-not $SkipPython) {
    if (-not (Has-Cmd python) -and -not (Has-Cmd py)) {
        Write-Host "==> Skipping Python build (python not on PATH)" -ForegroundColor DarkYellow
    } else {
        Write-Host ""
        Write-Host "==> Installing Python implementation (pip install -e)..." -ForegroundColor Yellow
        $pyExe = if (Has-Cmd python) { 'python' } else { 'py' }
        Push-Location (Join-Path $repoRoot 'python')
        try {
            & $pyExe -m pip install --quiet -e .
            if ($LASTEXITCODE -ne 0) { throw "pip install failed with exit code $LASTEXITCODE" }
        } finally { Pop-Location }
        Write-Host "    OK: Python package installed (run with: python -m codex_web_mcp)" -ForegroundColor Green
    }
} else { Write-Host "==> Skipping Python build (-SkipPython)" -ForegroundColor DarkGray }

Write-Host ""
Write-Host "All requested builds completed." -ForegroundColor Green
