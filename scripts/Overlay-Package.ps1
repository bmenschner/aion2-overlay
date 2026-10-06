Set-StrictMode -Version Latest

function Get-OverlayPackage {
    param([Parameter(Mandatory)][string]$Directory)
    $packageRoot = [IO.Path]::GetFullPath($Directory).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $manifestPath = Join-Path $packageRoot 'package.json'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { throw 'Paketmanifest fehlt. Bitte die Version erneut bereitstellen.' }
    try { $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json }
    catch { throw 'Paketmanifest ist beschädigt. Bitte die Version erneut bereitstellen.' }
    if ($manifest.schemaVersion -ne 1 -or $manifest.files.Count -lt 1) { throw 'Paketmanifest ist ungültig.' }
    $seen = @{}
    foreach ($file in $manifest.files) {
        $filePath = [IO.Path]::GetFullPath((Join-Path $packageRoot $file.path))
        if (-not $filePath.StartsWith($packageRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or $seen.ContainsKey($filePath)) { throw 'Ungültiger Dateipfad im Paketmanifest.' }
        $seen[$filePath] = $true
        if (-not (Test-Path -LiteralPath $filePath -PathType Leaf) -or (Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash -ne $file.sha256) { throw "Paketdatei fehlt oder ist verändert: $($file.path). Bitte erneut bereitstellen." }
    }
    if (@(Get-ChildItem -LiteralPath $packageRoot -File -Recurse).Count -ne $manifest.files.Count + 1) { throw 'Paket enthält nicht verzeichnete Dateien. Bitte erneut bereitstellen.' }
    $executable = Join-Path $packageRoot 'Aion2Overlay.exe'
    if (-not $seen.ContainsKey($executable) -or -not $seen.ContainsKey((Join-Path $packageRoot 'Aion2Overlay.dll'))) { throw 'Overlay-Programm fehlt im Paket.' }
    $version = (Get-Item -LiteralPath $executable).VersionInfo.FileVersion
    if ($version -ne $manifest.version) { throw 'Programmversion und Paketmanifest stimmen nicht überein.' }
    [pscustomobject]@{ Directory = $packageRoot; Executable = $executable; Version = $version; ManifestHash = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash; FileCount = $manifest.files.Count }
}

function Get-CurrentOverlayPackage {
    param([Parameter(Mandatory)][string]$ArtifactsRoot)
    $activePath = Join-Path $ArtifactsRoot 'current.json'
    if (-not (Test-Path -LiteralPath $activePath -PathType Leaf)) { throw 'Noch keine Version bereitgestellt. Bitte scripts/Publish-Overlay.ps1 ausführen.' }
    try { $active = Get-Content -LiteralPath $activePath -Raw -Encoding UTF8 | ConvertFrom-Json }
    catch { throw 'Die Angabe der aktuellen Version ist beschädigt. Bitte die Version erneut bereitstellen.' }
    if ($active.schemaVersion -ne 1) { throw 'Aktivmanifest ist ungültig.' }
    $releaseRoot = [IO.Path]::GetFullPath((Join-Path $ArtifactsRoot 'releases')).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $packagePath = [IO.Path]::GetFullPath((Join-Path $ArtifactsRoot $active.packagePath))
    if ([IO.Path]::GetDirectoryName($packagePath) -ne $releaseRoot) { throw 'Aktives Paket liegt außerhalb des Versionsordners.' }
    $package = Get-OverlayPackage $packagePath
    if ($package.ManifestHash -ne $active.packageManifestSha256 -or $package.Version -ne $active.version) { throw 'Aktivmanifest und geprüftes Paket stimmen nicht überein.' }
    $package
}
