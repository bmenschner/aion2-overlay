param([switch]$NoBuild, [switch]$ValidateOnly, [switch]$Wait, [switch]$PassThru,
    [switch]$ShowError, [string[]]$AppArguments = @(), [string]$ArtifactsRoot)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not $ArtifactsRoot) { $ArtifactsRoot = Join-Path $projectRoot 'artifacts' }
. (Join-Path $PSScriptRoot 'Overlay-Package.ps1')
try {
    $package = Get-CurrentOverlayPackage $ArtifactsRoot
    if ($ValidateOnly) { $package; return }
    # Windows argument quoting, including embedded quotes and trailing backslashes.
    $quotedArguments = @($AppArguments | ForEach-Object { '"' + ([regex]::Replace([regex]::Replace($_, '(\\*)"', '$1$1\"'), '(\\+)$', '$1$1')) + '"' })
    $start = @{ FilePath = $package.Executable; WorkingDirectory = $projectRoot; PassThru = $true; WindowStyle = 'Hidden' }
    if ($quotedArguments.Count) { $start.ArgumentList = $quotedArguments }
    $process = Start-Process @start
    if ($Wait) { $process.WaitForExit(); if ($process.ExitCode -ne 0) { throw "Overlay-Diagnose beendet mit Code $($process.ExitCode)." } }
    if ($PassThru) { $process }
} catch {
    if ($ShowError) {
        Add-Type -AssemblyName PresentationFramework
        [void][System.Windows.MessageBox]::Show($_.Exception.Message, 'Aion 2 Overlay konnte nicht gestartet werden', 'OK', 'Error')
    }
    throw
}
