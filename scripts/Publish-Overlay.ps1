param([string]$PackageSource, [string]$ArtifactsRoot)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not $ArtifactsRoot) { $ArtifactsRoot = Join-Path $projectRoot 'artifacts' }
$ArtifactsRoot = [IO.Path]::GetFullPath($ArtifactsRoot)
. (Join-Path $PSScriptRoot 'Overlay-Package.ps1')
$releaseRoot = Join-Path $ArtifactsRoot 'releases'
[void][IO.Directory]::CreateDirectory($releaseRoot)
$staging = Join-Path $ArtifactsRoot ('staging-' + [Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($staging)
if ($PackageSource) {
    $source = (Resolve-Path -LiteralPath $PackageSource).Path
    Copy-Item -Path (Join-Path $source '*') -Destination $staging -Recurse
} else {
    $localDotnet = Join-Path $projectRoot '.tools/dotnet/dotnet.exe'
    $dotnet = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { 'dotnet' }
    $env:DOTNET_CLI_HOME = Join-Path $projectRoot '.tools/cli'
    $env:NUGET_PACKAGES = Join-Path $projectRoot '.tools/nuget'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    & $dotnet publish (Join-Path $projectRoot 'src/Aion2Overlay.App/Aion2Overlay.App.csproj') --configuration Release --runtime win-x64 --self-contained true --output $staging
    if ($LASTEXITCODE -ne 0) { throw 'Publish fehlgeschlagen. Die bisher aktive Version bleibt erhalten.' }
}
foreach ($required in @('Aion2Overlay.exe', 'Aion2Overlay.dll', 'Aion2Overlay.deps.json', 'Aion2Overlay.runtimeconfig.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $staging $required) -PathType Leaf)) { throw "Quellpaket unvollständig: $required fehlt. Die bisher aktive Version bleibt erhalten." }
}
$version = (Get-Item -LiteralPath (Join-Path $staging 'Aion2Overlay.exe')).VersionInfo.FileVersion
if ($version -notmatch '^\d+\.\d+\.\d+\.\d+$') { throw 'Quellpaket besitzt keine gültige Versionsnummer.' }
$files = @(Get-ChildItem -LiteralPath $staging -File -Recurse | Where-Object { $_.FullName -ne (Join-Path $staging 'package.json') } | Sort-Object FullName | ForEach-Object {
    [ordered]@{ path = $_.FullName.Substring($staging.Length + 1).Replace('\', '/'); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
$manifest = [ordered]@{ schemaVersion = 1; version = $version; files = $files } | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText((Join-Path $staging 'package.json'), $manifest, [Text.UTF8Encoding]::new($false))
$package = Get-OverlayPackage $staging
$packageName = 'v' + $version + '-' + $package.ManifestHash.ToLowerInvariant()
$target = Join-Path $releaseRoot $packageName
if (Test-Path -LiteralPath $target) {
    $existing = Get-OverlayPackage $target
    if ($existing.ManifestHash -ne $package.ManifestHash) { throw 'Vorhandenes Versionspaket stimmt nicht überein.' }
    if ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($staging)) -ne $ArtifactsRoot -or [IO.Path]::GetFileName($staging) -notlike 'staging-*') { throw 'Staging-Pfad liegt außerhalb des vorgesehenen Ordners.' }
    Remove-Item -LiteralPath $staging -Recurse
} else {
    if ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($target)) -ne [IO.Path]::GetFullPath($releaseRoot)) { throw 'Ziel liegt außerhalb des Versionsordners.' }
    Move-Item -LiteralPath $staging -Destination $target
}
$package = Get-OverlayPackage $target
$starterSource = Join-Path $projectRoot 'Aion2Overlay starten.cmd'
$mainDirectory = Join-Path $ArtifactsRoot 'win-x64'
[void][IO.Directory]::CreateDirectory($mainDirectory)
$starterTarget = Join-Path $mainDirectory 'Aion2Overlay starten.cmd'
if (-not (Test-Path -LiteralPath $starterTarget) -or (Get-FileHash -LiteralPath $starterTarget).Hash -ne (Get-FileHash -LiteralPath $starterSource).Hash) {
    Copy-Item -LiteralPath $starterSource -Destination $starterTarget
}
$active = [ordered]@{ schemaVersion = 1; version = $package.Version; packagePath = 'releases/' + $packageName; packageManifestSha256 = $package.ManifestHash } | ConvertTo-Json
$activePath = Join-Path $ArtifactsRoot 'current.json'
$temporaryPath = Join-Path $ArtifactsRoot ('current-' + [Guid]::NewGuid().ToString('N') + '.tmp')
[IO.File]::WriteAllText($temporaryPath, $active, [Text.UTF8Encoding]::new($false))
try {
    if (Test-Path -LiteralPath $activePath) { [IO.File]::Replace($temporaryPath, $activePath, [NullString]::Value) }
    else { [IO.File]::Move($temporaryPath, $activePath) }
} finally { if (Test-Path -LiteralPath $temporaryPath) { Remove-Item -LiteralPath $temporaryPath } }
$package
