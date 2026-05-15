#requires -Version 5.1
<#
.SYNOPSIS
    Smoke-tests an MCP server binary by sending initialize + tools/list
    over stdio and checking for the three expected tool names.

.PARAMETER BinaryPath
    Path to the codex-web-mcp server executable.
#>
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$BinaryPath
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $BinaryPath)) {
    Write-Host "FAIL: binary not found: $BinaryPath" -ForegroundColor Red
    exit 1
}

function New-Frame {
    param([string]$Json)
    $bodyBytes = [System.Text.Encoding]::UTF8.GetBytes($Json)
    $header    = "Content-Length: $($bodyBytes.Length)`r`n`r`n"
    $headerBytes = [System.Text.Encoding]::ASCII.GetBytes($header)
    $combined = New-Object byte[] ($headerBytes.Length + $bodyBytes.Length)
    [Array]::Copy($headerBytes, 0, $combined, 0, $headerBytes.Length)
    [Array]::Copy($bodyBytes, 0, $combined, $headerBytes.Length, $bodyBytes.Length)
    return ,$combined
}

$initialize = '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"smoke","version":"0.0.0"}}}'
$initialized = '{"jsonrpc":"2.0","method":"notifications/initialized"}'
$toolsList   = '{"jsonrpc":"2.0","id":2,"method":"tools/list"}'

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName               = (Resolve-Path -LiteralPath $BinaryPath).Path
$psi.UseShellExecute        = $false
$psi.RedirectStandardInput  = $true
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError  = $true
$psi.CreateNoWindow         = $true

$proc = [System.Diagnostics.Process]::Start($psi)

try {
    $stdin = $proc.StandardInput.BaseStream

    foreach ($msg in @($initialize, $initialized, $toolsList)) {
        $bytes = New-Frame -Json $msg
        $stdin.Write($bytes, 0, $bytes.Length)
        $stdin.Flush()
    }

    # Read stdout for up to 10 seconds.
    $stdout    = $proc.StandardOutput
    $sb        = New-Object System.Text.StringBuilder
    $deadline  = [DateTime]::UtcNow.AddSeconds(10)
    $needles   = @('web_search', 'web_search_raw', 'web_fetch_raw')
    $found     = @{}

    while ([DateTime]::UtcNow -lt $deadline) {
        if ($stdout.Peek() -ge 0) {
            $line = $stdout.ReadLine()
            if ($null -ne $line) {
                [void]$sb.AppendLine($line)
                foreach ($n in $needles) {
                    if ($line -match [regex]::Escape($n)) { $found[$n] = $true }
                }
            }
        } else {
            Start-Sleep -Milliseconds 100
        }
        if ($found.Count -eq $needles.Count) { break }
        if ($proc.HasExited) { break }
    }

    $missing = @($needles | Where-Object { -not $found.ContainsKey($_) })
    if ($missing.Count -eq 0) {
        Write-Host "PASS: all three tools advertised (web_search, web_search_raw, web_fetch_raw)" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "FAIL: missing tool names: $($missing -join ', ')" -ForegroundColor Red
        Write-Host "--- captured stdout ---" -ForegroundColor DarkGray
        Write-Host $sb.ToString()
        Write-Host "--- captured stderr ---" -ForegroundColor DarkGray
        try { Write-Host $proc.StandardError.ReadToEnd() } catch {}
        exit 1
    }
} finally {
    if (-not $proc.HasExited) {
        try { $proc.Kill() } catch {}
    }
    $proc.Dispose()
}
