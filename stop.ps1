#Requires -Version 5.1
<#
.SYNOPSIS
  Stop NexusCRM processes started by run.ps1 (classic or Aspire) and free ports.
#>
[CmdletBinding()]
param(
    [switch]$KeepDocker,
    [switch]$Quiet
)

$ErrorActionPreference = "Continue"
$Root = $PSScriptRoot
$RunDir = Join-Path $Root ".run"
$PidFile = Join-Path $RunDir "pids.json"

function Write-Step([string]$Message) {
    if (-not $Quiet) {
        Write-Host "==> $Message" -ForegroundColor Cyan
    }
}

function Stop-ProcessTree([int]$ProcessId) {
    if ($ProcessId -le 0) { return }
    try {
        & taskkill.exe /PID $ProcessId /T /F 2>$null | Out-Null
    }
    catch {
        # ignore
    }
}

function Stop-ListenersOnPort([int]$Port) {
    try {
        $conns = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
        foreach ($c in $conns) {
            if ($c.OwningProcess -and $c.OwningProcess -gt 0) {
                Stop-ProcessTree -ProcessId $c.OwningProcess
            }
        }
    }
    catch {
        $lines = netstat -ano | Select-String ":$Port\s+.*LISTENING"
        foreach ($line in $lines) {
            $parts = ($line.ToString() -split "\s+") | Where-Object { $_ }
            $procId = [int]$parts[-1]
            if ($procId -gt 0) {
                Stop-ProcessTree -ProcessId $procId
            }
        }
    }
}

$mode = "classic"
Write-Step "Stopping tracked NexusCRM processes"

if (Test-Path $PidFile) {
    try {
        $state = Get-Content -Path $PidFile -Raw | ConvertFrom-Json
        if ($state.Mode) { $mode = [string]$state.Mode }
        foreach ($p in @($state.Processes)) {
            if ($p.pid) {
                if (-not $Quiet) {
                    Write-Host ("  killing {0} (pid {1})" -f $p.name, $p.pid)
                }
                Stop-ProcessTree -ProcessId ([int]$p.pid)
            }
        }
    }
    catch {
        if (-not $Quiet) {
            Write-Host "  Could not parse pid file; continuing with port cleanup." -ForegroundColor Yellow
        }
    }
    Remove-Item -Path $PidFile -Force -ErrorAction SilentlyContinue
}

# Classic API/SPA + Aspire dashboard / AppHost ports
Write-Step "Freeing ports (API, SPA, Aspire dashboard)"
foreach ($port in 7148, 5078, 4200, 17117, 15164, 21174, 22259, 19151, 20266) {
    Stop-ListenersOnPort -Port $port
}

Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
    Where-Object {
        ($_.CommandLine -and (
            $_.CommandLine -match 'NexusCRM\.AppHost' -or
            $_.CommandLine -match 'NexusCRM\.Api' -or
            $_.CommandLine -match 'NexusCRM\.Worker' -or
            $_.CommandLine -match 'NexusCRM\.Web' -or
            ($_.Name -eq 'node.exe' -and $_.CommandLine -match 'ng serve|@angular[/\\]cli')
        ))
    } |
    ForEach-Object {
        if (-not $Quiet) {
            Write-Host ("  killing leftover {0} (pid {1})" -f $_.Name, $_.ProcessId)
        }
        Stop-ProcessTree -ProcessId ([int]$_.ProcessId)
    }

# Aspire also starts containers via Docker; stop compose infra unless KeepDocker.
# AppHost-owned containers are typically removed when the AppHost process exits.
if (-not $KeepDocker) {
    $compose = Join-Path $Root "deploy/docker-compose.yml"
    if ((Test-Path $compose) -and (Get-Command docker -ErrorAction SilentlyContinue)) {
        Write-Step "Stopping Docker compose infrastructure"
        docker compose -f $compose stop | Out-Null
    }
}

if (-not $Quiet) {
    Write-Host ""
    Write-Host ("NexusCRM stopped (was: {0})." -f $mode) -ForegroundColor Green
    Write-Host "  Tip: .\stop.ps1 -KeepDocker  leaves compose Redis/Rabbit/OTel running."
    Write-Host "  Aspire: .\run.ps1 -Aspire"
}
