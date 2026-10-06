param([switch]$NoBuild)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$localDotnet = Join-Path $projectRoot '.tools\dotnet\dotnet.exe'
$dotnet = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { 'dotnet' }
Push-Location $projectRoot
try {
    $env:DOTNET_CLI_HOME = Join-Path $projectRoot '.tools\cli'
    $env:NUGET_PACKAGES = Join-Path $projectRoot '.tools\nuget'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
    if (-not $NoBuild) {
        & $dotnet build 'src/Aion2Overlay.App/Aion2Overlay.App.csproj' --configuration Release
        if ($LASTEXITCODE -ne 0) { throw 'Der Build ist fehlgeschlagen.' }
    }
    & $dotnet 'src/Aion2Overlay.App/bin/Release/net10.0-windows10.0.19041.0/Aion2Overlay.dll'
    if ($LASTEXITCODE -ne 0) { throw "Overlay beendet mit Code $LASTEXITCODE." }
} finally { Pop-Location }
