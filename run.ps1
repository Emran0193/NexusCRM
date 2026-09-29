#Requires -Version 5.1
<#
.SYNOPSIS
  Build and start NexusCRM locally (classic stack or .NET Aspire).

.PARAMETER Aspire
  Start via NexusCRM.AppHost (dashboard + wired Postgres/Redis/RabbitMQ/Api/Worker).

.PARAMETER Classic
  Explicit classic mode (host Postgres + Docker Redis/Rabbit/OTel + API). Default if -Aspire is omitted.

.EXAMPLE
  .\run.ps1 -Aspire
.EXAMPLE
  .\run.ps1
.EXAMPLE
  .\run.ps1 -Aspire -SkipWeb
#>
[CmdletBinding(DefaultParameterSetName = "Classic")]
param(
    [Parameter(ParameterSetName = "Aspire")]
    [switch]$Aspire,

    [Parameter(ParameterSetName = "Classic")]
    [switch]$Classic,

    [switch]$SkipDocker,
    [switch]$SkipWeb,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$RunDir = Join-Path $Root ".run"
$PidFile = Join-Path $RunDir "pids.json"
$LogDir = Join-Path $RunDir "logs"
$UseAspire = $PSCmdlet.ParameterSetName -eq "Aspire" -or $Aspire.IsPresent

# Classic host Postgres
$PgPort = 5432
$PgUser = "postgres"
$PgPassword = "postgres"
$PgDatabase = "nexuscrm"

# Aspire dashboard (see AppHost launchSettings https profile)
$AspireDashboardHttps = 17117
$AspireDashboardHttp = 15164

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Assert-CommandExists {
    param([string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command not found: $Name"
    }
}

function Wait-TcpPort {
    param(
        [string]$HostName,
        [int]$Port,
        [int]$TimeoutSec = 90,
        [int]$ProcessId = 0,
        [string]$LogPath = ""
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        if ($ProcessId -gt 0 -and -not (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue)) {
            Show-LogTail -LogPath $LogPath
            throw "Process $ProcessId exited before ${HostName}:${Port} opened. See logs above."
        }

        try {
            $client = New-Object System.Net.Sockets.TcpClient
            $iar = $client.BeginConnect($HostName, $Port, $null, $null)
            if ($iar.AsyncWaitHandle.WaitOne(1000) -and $client.Connected) {
                $client.EndConnect($iar)
                $client.Close()
                return
            }
            $client.Close()
        }
        catch {
            # keep waiting
        }
        Start-Sleep -Seconds 1
    }

    Show-LogTail -LogPath $LogPath
    throw "Timed out waiting for ${HostName}:${Port}"
}

function Test-TcpPort {
    param([string]$HostName, [int]$Port)
    try {
        $client = New-Object System.Net.Sockets.TcpClient
        $iar = $client.BeginConnect($HostName, $Port, $null, $null)
        $ok = $iar.AsyncWaitHandle.WaitOne(400) -and $client.Connected
        if ($ok) { $client.EndConnect($iar) }
        $client.Close()
        return $ok
    }
    catch {
        return $false
    }
}

function Show-LogTail {
    param([string]$LogPath)
    if ([string]::IsNullOrWhiteSpace($LogPath)) { return }

    foreach ($suffix in @(".out.log", ".err.log")) {
        $file = "$LogPath$suffix"
        if (-not (Test-Path $file)) { continue }
        $tail = Get-Content $file -ErrorAction SilentlyContinue | Select-Object -Last 40
        if ($tail) {
            Write-Host ""
            Write-Host "----- $(Split-Path $file -Leaf) -----" -ForegroundColor Yellow
            $tail | ForEach-Object { Write-Host $_ }
        }
    }
}

function Start-TrackedProcess {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$CommandLine,
        [Parameter(Mandatory = $true)][string]$WorkingDirectory,
        [Parameter(Mandatory = $true)][string]$LogPath
    )

    # Unique files avoid locks if a previous run's redirect handle is still open.
    $stamp = Get-Date -Format "yyyyMMddHHmmss"
    $stdout = "$LogPath.$stamp.out.log"
    $stderr = "$LogPath.$stamp.err.log"
    New-Item -ItemType File -Path $stdout -Force | Out-Null
    New-Item -ItemType File -Path $stderr -Force | Out-Null

    $arg = "/c $CommandLine > `"$stdout`" 2> `"$stderr`""
    $proc = Start-Process `
        -FilePath "cmd.exe" `
        -ArgumentList $arg `
        -WorkingDirectory $WorkingDirectory `
        -PassThru `
        -WindowStyle Hidden

    return [pscustomobject]@{
        Name    = $Name
        Pid     = $proc.Id
        LogPath = "$LogPath.$stamp"
        Log     = $stdout
    }
}

function Save-RunState {
    param(
        [string]$Mode,
        [System.Collections.IEnumerable]$Tracked
    )

    $processRecords = @()
    foreach ($item in $Tracked) {
        $processRecords += @{
            name = $item.Name
            pid  = $item.Pid
            log  = $item.Log
        }
    }

    $state = @{
        Mode         = $Mode
        StartedAtUtc = [DateTimeOffset]::UtcNow.ToString("o")
        Processes    = $processRecords
    }
    $state | ConvertTo-Json -Depth 5 | Set-Content -Path $PidFile -Encoding UTF8
}

Assert-CommandExists -Name "dotnet"
if (-not $SkipWeb) { Assert-CommandExists -Name "npm" }
if (-not $UseAspire -and -not $SkipDocker) { Assert-CommandExists -Name "docker" }
if ($UseAspire) { Assert-CommandExists -Name "docker" } # Aspire resources need Docker

$stopScript = Join-Path $Root "stop.ps1"
if (Test-Path $PidFile) {
    Write-Step -Message "Stopping previous run"
    & $stopScript -Quiet -KeepDocker
}

New-Item -ItemType Directory -Path $RunDir -Force | Out-Null
New-Item -ItemType Directory -Path $LogDir -Force | Out-Null

if (-not $NoBuild) {
    Write-Step -Message "Building .NET solution"
    Push-Location $Root
    try {
        dotnet build (Join-Path $Root "NexusCRM.slnx") -v q
        if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }
    }
    finally {
        Pop-Location
    }

    if (-not $SkipWeb) {
        $webDir = Join-Path $Root "src/NexusCRM.Web"
        if (-not (Test-Path (Join-Path $webDir "node_modules"))) {
            Write-Step -Message "Installing Angular dependencies (npm install)"
            Push-Location $webDir
            try {
                npm install
                if ($LASTEXITCODE -ne 0) { throw "npm install failed" }
            }
            finally {
                Pop-Location
            }
        }
    }
}

$tracked = New-Object System.Collections.Generic.List[object]
$webLog = Join-Path $LogDir "web"

if ($UseAspire) {
    Write-Host ""
    Write-Host "Mode: Aspire (AppHost orchestrates infra + API + Worker)" -ForegroundColor DarkYellow

    $appHostLog = Join-Path $LogDir "aspire"
    Write-Step -Message "Starting Aspire AppHost (dashboard https://localhost:$AspireDashboardHttps)"
    $appHostCmd = "dotnet run --project `"src\NexusCRM.AppHost\NexusCRM.AppHost.csproj`" --no-build --launch-profile https"
    $appHost = Start-TrackedProcess `
        -Name "aspire" `
        -CommandLine $appHostCmd `
        -WorkingDirectory $Root `
        -LogPath $appHostLog
    $tracked.Add($appHost) | Out-Null

    if (-not $SkipWeb) {
        Write-Step -Message "Starting Angular (http://localhost:4200)"
        $web = Start-TrackedProcess `
            -Name "web" `
            -CommandLine "npm start -- --host 127.0.0.1 --port 4200" `
            -WorkingDirectory (Join-Path $Root "src/NexusCRM.Web") `
            -LogPath $webLog
        $tracked.Add($web) | Out-Null
    }

    Save-RunState -Mode "aspire" -Tracked $tracked

    Write-Step -Message "Waiting for Aspire dashboard"
    try {
        Wait-TcpPort -HostName "127.0.0.1" -Port $AspireDashboardHttps -TimeoutSec 180 -ProcessId $appHost.Pid -LogPath $appHost.LogPath
        Write-Host "Aspire dashboard: https://localhost:$AspireDashboardHttps" -ForegroundColor Green
    }
    catch {
        if (Test-TcpPort -HostName "127.0.0.1" -Port $AspireDashboardHttp) {
            Write-Host "Aspire dashboard: http://localhost:$AspireDashboardHttp" -ForegroundColor Green
        }
        else {
            throw
        }
    }

    # API may keep launch-profile 7148 or use an Aspire-assigned port — prefer 7148 when present.
    if (Test-TcpPort -HostName "127.0.0.1" -Port 7148) {
        Write-Host "API appears on https://localhost:7148" -ForegroundColor Green
    }
    else {
        Write-Host "Open the Aspire dashboard Resources tab for the api HTTPS URL." -ForegroundColor DarkYellow
    }

    if (-not $SkipWeb) {
        Write-Step -Message "Waiting for Angular"
        Wait-TcpPort -HostName "127.0.0.1" -Port 4200 -TimeoutSec 180 -ProcessId $web.Pid -LogPath $web.LogPath
        Write-Host "Angular is listening on http://localhost:4200" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "NexusCRM is up (Aspire)." -ForegroundColor Green
    Write-Host "  Dashboard: https://localhost:$AspireDashboardHttps"
    Write-Host "  SPA:       http://localhost:4200"
    Write-Host "  API:       see Aspire resource 'api' (often https://localhost:7148)"
    Write-Host "  DB:        Aspire Postgres on localhost:5433"
    Write-Host "  Logs:      .run/logs/ (aspire*.log, web*.log)"
    Write-Host "  Stop:      .\stop.ps1"
}
else {
    Write-Host ""
    Write-Host "Mode: Classic (host Postgres + Docker infra + API)" -ForegroundColor DarkYellow

    $hostPg = Get-Service -Name "postgresql*" -ErrorAction SilentlyContinue | Where-Object { $_.Status -eq "Running" }
    if ($hostPg) {
        Write-Host ("Using host PostgreSQL ({0}) at localhost:{1} as {2}/{2}." -f (($hostPg | ForEach-Object Name) -join ", "), $PgPort, $PgUser) -ForegroundColor DarkYellow
    }

    if (-not $SkipDocker) {
        Write-Step -Message "Starting Docker infrastructure (Redis / RabbitMQ / OTel)"
        docker compose -f (Join-Path $Root "deploy/docker-compose.yml") up -d redis rabbitmq otel-collector
        if ($LASTEXITCODE -ne 0) { throw "docker compose up failed" }
    }

    Write-Step -Message "Waiting for PostgreSQL (localhost:$PgPort)"
    Wait-TcpPort -HostName "127.0.0.1" -Port $PgPort

    Write-Step -Message "Ensuring database '$PgDatabase' exists"
    $env:PGPASSWORD = $PgPassword
    $psql = Get-Command psql -ErrorAction SilentlyContinue
    if (-not $psql) {
        $pgBin = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
        if (Test-Path $pgBin) { $psql = Get-Command $pgBin }
    }
    if ($psql) {
        $exists = & $psql.Source -h 127.0.0.1 -p $PgPort -U $PgUser -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = '$PgDatabase'"
        if ($LASTEXITCODE -ne 0) { throw "Cannot connect to PostgreSQL as $PgUser on port $PgPort" }
        if (($exists | Out-String) -notmatch "1") {
            & $psql.Source -h 127.0.0.1 -p $PgPort -U $PgUser -d postgres -v ON_ERROR_STOP=1 -c "CREATE DATABASE $PgDatabase;"
            if ($LASTEXITCODE -ne 0) { throw "Failed to create database $PgDatabase" }
            Write-Host "Created database $PgDatabase"
        }
        else {
            Write-Host "Database $PgDatabase already exists"
        }
    }
    else {
        Write-Host "psql not found - assuming database '$PgDatabase' already exists." -ForegroundColor DarkYellow
    }

    $apiLog = Join-Path $LogDir "api"
    Write-Step -Message "Starting API (https://localhost:7148)"
    $apiCmd = "dotnet run --project `"src\NexusCRM.Api\NexusCRM.Api.csproj`" --no-build --launch-profile https"
    $api = Start-TrackedProcess `
        -Name "api" `
        -CommandLine $apiCmd `
        -WorkingDirectory $Root `
        -LogPath $apiLog
    $tracked.Add($api) | Out-Null

    if (-not $SkipWeb) {
        Write-Step -Message "Starting Angular (http://localhost:4200)"
        $web = Start-TrackedProcess `
            -Name "web" `
            -CommandLine "npm start -- --host 127.0.0.1 --port 4200" `
            -WorkingDirectory (Join-Path $Root "src/NexusCRM.Web") `
            -LogPath $webLog
        $tracked.Add($web) | Out-Null
    }

    Save-RunState -Mode "classic" -Tracked $tracked

    Write-Step -Message "Waiting for API"
    Wait-TcpPort -HostName "127.0.0.1" -Port 7148 -TimeoutSec 120 -ProcessId $api.Pid -LogPath $api.LogPath
    Write-Host "API is listening on https://localhost:7148" -ForegroundColor Green

    if (-not $SkipWeb) {
        Write-Step -Message "Waiting for Angular"
        Wait-TcpPort -HostName "127.0.0.1" -Port 4200 -TimeoutSec 180 -ProcessId $web.Pid -LogPath $web.LogPath
        Write-Host "Angular is listening on http://localhost:4200" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "NexusCRM is up (classic)." -ForegroundColor Green
    Write-Host "  SPA:  http://localhost:4200"
    Write-Host "  API:  https://localhost:7148"
    Write-Host "  DB:   localhost:$PgPort ($PgUser/$PgPassword)"
    Write-Host "  Logs: .run/logs/"
    Write-Host "  Stop: .\stop.ps1"
    Write-Host "  Tip:  .\run.ps1 -Aspire   # dashboard at https://localhost:$AspireDashboardHttps"
}

Write-Host ""
Write-Host "Demo login: admin@nexuscrm.local / ChangeMe!12345"
Write-Host "Tenant:     00000000-0000-0000-0000-000000000001"
