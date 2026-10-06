$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$testRoot = Join-Path $projectRoot ('artifacts/launcher-tests-' + [Guid]::NewGuid().ToString('N'))
$activeRoot = Join-Path $testRoot 'active'
[void][IO.Directory]::CreateDirectory($testRoot)
$checks = [Collections.Generic.List[string]]::new()
$oldProcess = $null
$fileLock = $null
function Assert-Launcher([bool]$condition, [string]$message) {
    if (-not $condition) { throw $message }
    $checks.Add($message)
}
function New-ProbePackage([string]$name, [string]$version, [int]$variant) {
    $directory = Join-Path $testRoot $name
    [void][IO.Directory]::CreateDirectory($directory)
    $source = @"
using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
[assembly: AssemblyVersion("$version")]
[assembly: AssemblyFileVersion("$version")]
public static class LauncherProbe {
    public static void Main(string[] args) {
        var version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
        File.WriteAllText(args[0], "{\"version\":\"" + version + "\",\"pid\":" + Process.GetCurrentProcess().Id + ",\"variant\":$variant}");
        Thread.Sleep(Int32.Parse(args[1]));
    }
}
"@
    $sourcePath = Join-Path $directory 'probe.cs'
    [IO.File]::WriteAllText($sourcePath, $source)
    # Framework probe EXEs are only diagnostic fixtures; no game or user instance.
    $compilerPath = Join-Path $directory 'compile.ps1'
    [IO.File]::WriteAllText($compilerPath, 'param($source,$output); Add-Type -Path $source -OutputAssembly $output -OutputType ConsoleApplication -ErrorAction Stop')
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $compilerPath $sourcePath (Join-Path $directory 'Aion2Overlay.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Diagnosepaket konnte nicht gebaut werden.' }
    Remove-Item -LiteralPath $sourcePath,$compilerPath
    Copy-Item -LiteralPath (Join-Path $directory 'Aion2Overlay.exe') -Destination (Join-Path $directory 'Aion2Overlay.dll')
    [IO.File]::WriteAllText((Join-Path $directory 'Aion2Overlay.deps.json'), '{}')
    [IO.File]::WriteAllText((Join-Path $directory 'Aion2Overlay.runtimeconfig.json'), '{}')
    $directory
}
function Expect-Rejected([scriptblock]$operation, [string]$description) {
    $rejected = $false
    try { & $operation | Out-Null } catch { $rejected = $true }
    Assert-Launcher $rejected $description
}
try {
    $oldSource = New-ProbePackage 'old' '0.2.2.0' 1
    $newSource = New-ProbePackage 'new' '0.3.0.0' 1
    $rebuildSource = New-ProbePackage 'rebuild' '0.3.0.0' 2
    $old = & "$PSScriptRoot/Publish-Overlay.ps1" -PackageSource $oldSource -ArtifactsRoot $activeRoot
    $oldHash = (Get-FileHash -LiteralPath $old.Executable).Hash
    $fileLock = [IO.File]::Open((Join-Path $old.Directory 'Aion2Overlay.dll'), 'Open', 'Read', 'Read')
    $oldReport = Join-Path $testRoot 'old probe report.json'
    $oldProcess = & "$PSScriptRoot/Start-Overlay.ps1" -ArtifactsRoot $activeRoot -PassThru -AppArguments @($oldReport, '30000')
    $deadline = [DateTime]::UtcNow.AddSeconds(5)
    while (-not (Test-Path -LiteralPath $oldReport) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 50 }
    Assert-Launcher (Test-Path -LiteralPath $oldReport) 'Alte Diagnoseinstanz gestartet; Argumentpfad mit Leerzeichen korrekt.'
    $new = & "$PSScriptRoot/Publish-Overlay.ps1" -PackageSource $newSource -ArtifactsRoot $activeRoot
    $newReport = Join-Path $testRoot 'new probe report.json'
    $newProcess = & "$PSScriptRoot/Start-Overlay.ps1" -ArtifactsRoot $activeRoot -PassThru -Wait -AppArguments @($newReport, '0')
    $observed = Get-Content -LiteralPath $newReport -Raw | ConvertFrom-Json
    Assert-Launcher ($observed.version -eq '0.3.0.0' -and $observed.pid -eq $newProcess.Id) 'Derselbe Starter öffnet neue Version in eigenem Prozess.'
    Assert-Launcher (-not $oldProcess.HasExited -and $oldProcess.Path -eq $old.Executable) 'Alte Version läuft während Publikation und Neustart weiter.'
    Assert-Launcher ((Get-FileHash -LiteralPath $old.Executable).Hash -eq $oldHash) 'Altes Paket einschließlich gesperrter DLL unverändert.'
    $same = & "$PSScriptRoot/Publish-Overlay.ps1" -PackageSource $newSource -ArtifactsRoot $activeRoot
    Assert-Launcher ($same.Directory -eq $new.Directory) 'Identischer Paketinhalt verwendet bestehenden Ordner ohne Überschreiben.'
    $rebuild = & "$PSScriptRoot/Publish-Overlay.ps1" -PackageSource $rebuildSource -ArtifactsRoot $activeRoot
    Assert-Launcher ($rebuild.Directory -ne $new.Directory -and $rebuild.Version -eq $new.Version) 'Geänderter Build bei gleicher Version erhält eigenen Paketordner.'
    $activePath = Join-Path $activeRoot 'current.json'
    $activeBytes = [IO.File]::ReadAllBytes($activePath)
    $emptySource = Join-Path $testRoot 'incomplete'
    [void][IO.Directory]::CreateDirectory($emptySource)
    Expect-Rejected { & "$PSScriptRoot/Publish-Overlay.ps1" -PackageSource $emptySource -ArtifactsRoot $activeRoot } 'Unvollständige Publikation abgewiesen.'
    Assert-Launcher ([Convert]::ToBase64String([IO.File]::ReadAllBytes($activePath)) -eq [Convert]::ToBase64String($activeBytes)) 'Fehlgeschlagene Publikation lässt Aktivmanifest unverändert.'
    [IO.File]::WriteAllText($activePath, '{broken')
    Expect-Rejected { & "$PSScriptRoot/Start-Overlay.ps1" -ArtifactsRoot $activeRoot -ValidateOnly } 'Beschädigtes Aktivmanifest ohne Rückfall abgewiesen.'
    [IO.File]::WriteAllBytes($activePath, $activeBytes)
    $active = Get-Content -LiteralPath $activePath -Raw | ConvertFrom-Json
    $active.packagePath = '../old'
    [IO.File]::WriteAllText($activePath, ($active | ConvertTo-Json))
    Expect-Rejected { & "$PSScriptRoot/Start-Overlay.ps1" -ArtifactsRoot $activeRoot -ValidateOnly } 'Paketpfad außerhalb des Versionsordners abgewiesen.'
    [IO.File]::WriteAllBytes($activePath, $activeBytes)
    $dllPath = Join-Path $rebuild.Directory 'Aion2Overlay.dll'
    $dllBytes = [IO.File]::ReadAllBytes($dllPath)
    [IO.File]::AppendAllText($dllPath, 'changed')
    Expect-Rejected { & "$PSScriptRoot/Start-Overlay.ps1" -ArtifactsRoot $activeRoot -ValidateOnly } 'Veränderte Paketdatei ohne alten Start abgewiesen.'
    [IO.File]::WriteAllBytes($dllPath, $dllBytes)
    $missingPath = Join-Path $activeRoot 'current.saved'
    Move-Item -LiteralPath $activePath -Destination $missingPath
    Expect-Rejected { & "$PSScriptRoot/Start-Overlay.ps1" -ArtifactsRoot $activeRoot -ValidateOnly } 'Fehlendes Aktivmanifest ohne Rückfall abgewiesen.'
    Move-Item -LiteralPath $missingPath -Destination $activePath
    Assert-Launcher (-not $oldProcess.HasExited) 'Alte Diagnoseinstanz auch nach negativen Prüfungen unverändert aktiv.'
    [ordered]@{ passed=$true; testedAt=[DateTimeOffset]::UtcNow; checks=@($checks); root=$testRoot; oldPid=$oldProcess.Id; newPid=$newProcess.Id; oldPackage=$old.Directory; newPackage=$new.Directory; limitations='Synthetische Framework-Probes. Keine Nutzerinstanz oder Spielaufnahme.' } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $projectRoot 'artifacts/launcher-test.json') -Encoding UTF8
    $checks
} finally {
    if ($fileLock) { $fileLock.Dispose() }
    # Only the exact probe process created above; never enumerate/close user instances.
    if ($oldProcess -and -not $oldProcess.HasExited) { $oldProcess.Kill(); $oldProcess.WaitForExit() }
}
