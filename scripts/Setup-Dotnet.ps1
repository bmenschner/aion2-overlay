param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$sdkVersion = (Get-Content -LiteralPath (Join-Path $projectRoot 'global.json') -Raw | ConvertFrom-Json).sdk.version
$installPath = Join-Path $projectRoot '.tools\dotnet'
$dotnetPath = Join-Path $installPath 'dotnet.exe'
if (Test-Path -LiteralPath $dotnetPath) {
    $installedVersion = & $dotnetPath --version
    if ($installedVersion -eq $sdkVersion) { Write-Output "SDK $sdkVersion bereits vorhanden."; exit 0 }
    throw "Der lokale SDK-Ordner enthält eine andere Version. Bitte einen separaten Installationspfad verwenden."
}
$metadata = Invoke-RestMethod -Uri 'https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json'
$sdk = $metadata.releases | ForEach-Object { $_.sdks } | Where-Object { $_.version -eq $sdkVersion } | Select-Object -First 1
$download = $sdk.files | Where-Object { $_.rid -eq 'win-x64' -and $_.url.EndsWith('.zip') } | Select-Object -First 1
if (-not $download) { throw "Kein offizielles Windows-x64-SDK $sdkVersion gefunden." }
$archive = Join-Path $projectRoot '.tools\dotnet-sdk.zip'
New-Item -ItemType Directory -Path (Split-Path -Parent $archive) -Force | Out-Null
Write-Output "Lade offizielles .NET SDK $sdkVersion..."
Invoke-WebRequest -Uri $download.url -OutFile $archive
$actualHash = (Get-FileHash -LiteralPath $archive -Algorithm SHA512).Hash
if ($actualHash -ne $download.hash) { throw 'Die SHA512-Prüfsumme des SDK-Downloads stimmt nicht.' }
New-Item -ItemType Directory -Path $installPath -Force | Out-Null
Expand-Archive -LiteralPath $archive -DestinationPath $installPath
Remove-Item -LiteralPath $archive
Write-Output "Projektlokales SDK installiert: $dotnetPath"
