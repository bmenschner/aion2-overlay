# Validierung SPEC-004: fester Starter und Updates

Stand: 6. Oktober 2026. Bezug: [SPEC-004](../specs/004-versionierter-start.md).

## Umgebung

Windows x64, PowerShell 7 und der vom Doppelklick-Starter verwendete Windows PowerShell 5.1. Eigene synthetische Framework-Probeprogramme mit tatsächlichen PE-Dateiversionen `0.2.2.0` und `0.3.0.0`, zusätzlich veränderter Build bei gleicher Version. Diese Fixtures sind ausdrücklich keine Aion-Overlay-Versionen. Sie melden PID/Version und laufen getrennt; die ältere Probe bleibt während aller Updateprüfungen aktiv. Keine Nutzerinstanz gestartet oder geschlossen, keine Spielaufnahme.

## Nachweise

| Kriterium | Ergebnis |
| --- | --- |
| AC-01 | Bestanden: derselbe Startskriptpfad öffnet nach Aktivierung Version 0.3.0.0; PID aus dem gestarteten Prozess stimmt mit dem Bericht überein, alte EXE bleibt identisch |
| AC-02 | Bestanden: alter Probeprozess bleibt bei Publikation und neuem Start aktiv; sein Prozesspfad bleibt der alte Paketordner. Zusätzlich ist seine DLL für Schreiben/Löschen gesperrt, dennoch gelingt das Update |
| AC-03 | Bestanden: unvollständiges Quellpaket erzeugt Fehler; Aktivmanifest bleibt bytegenau unverändert |
| AC-04 | Bestanden: fehlendes/beschädigtes Aktivmanifest, Pfad außerhalb der Releases und veränderte DLL werden ohne Rückfall abgewiesen |
| AC-05 | Bestanden: identischer Inhalt verwendet denselben Ordner; anderer Build bei gleicher Version erhält eigenen Ordner. Vorhandene/laufende Pakete bleiben unverändert |
| AC-06 | Bestanden: über den festen Startskriptpfad geprüftes echtes v0.3.1-Paket startet den vorhandenen UI-Diagnosemodus und endet mit Exitcode 0. Bericht bestätigt Titel v0.3.1 und korrekte Farben; aufgenommen wird ausschließlich das eigene Testfenster |

`scripts/Test-OverlayLauncher.ps1` besteht lokal unter PowerShell 7 und Windows PowerShell 5.1: 13 Prüfaussagen, `artifacts/launcher-test.json` meldet `passed: true`. Prüfung von Argumentpfaden mit Leerzeichen enthalten. Der Test beendet im Aufräumen ausschließlich den genauen eigenen Probeprozess. Windows-CI wurde um diesen unabhängigen Prozesstest ergänzt; ein abgeschlossener Remote-CI-Lauf wird hier nicht behauptet.

Release-Publish v0.3.1 ohne Compilerwarnungen/-fehler, 30 Kernlogiktests bestanden. `Start-Overlay.ps1 -ValidateOnly` bestätigt 410 Paketdateien und Version `0.3.1.0`. Manifest: `artifacts/current.json`, aktives Paket `artifacts/releases/v0.3.1.0-19d3ec88d9a8234b02f90f3879e1db08d2024b0e6e6f1ab233decc1d688cd2ed/`. Paketmanifest-SHA256 `19D3EC88D9A8234B02F90F3879E1DB08D2024B0E6E6F1AB233DECC1D688CD2ED`.

`artifacts/ui-smoke-test.json`: erfolgreich am 6. Oktober 2026 um 14:18:48 UTC, eigenes WPF-Fenster v0.3.1, Testprozess PID 4784, Exitcode 0. Ein erster Lauf in der isolierten Sandbox konnte den WGC-Dienst nicht erreichen (`0x80070424`); der Wiederholungslauf in der interaktiven Windows-Sitzung besteht. Eine erste lokale Publish-Ausführung scheiterte am gesperrten NuGet-Netzzugriff und ließ das vorherige Aktivmanifest erhalten; mit Zugriff auf die Paketmetadaten gelang Publish. Im ersten Prozesstest wurde ein PowerShell-Unterschied bei `File.Replace` gefunden und mit explizitem `[NullString]::Value` korrigiert; die abschließenden Läufe prüfen die korrigierte atomare Aktivierung.

## Bereitstellung und Grenzen

Fester Starter im Projektstamm und im bisherigen `artifacts/win-x64/`: `Aion2Overlay starten.cmd`. Beide verwenden denselben Startskriptpfad. Das bisherige `artifacts/win-x64/Aion2Overlay.exe` bleibt v0.3.0 und ist ein alter direkter Einstieg; es wird nicht für neue Versionen überschrieben. Die einmalige Umstellung auf den neuen Starter ist erforderlich. Bei künftigen Änderungen ist `Publish-Overlay.ps1` der verbindliche Bereitstellungsweg laut AGENTS.md.

Ein Versionsstart wird nicht auf eine schon laufende Instanz umgeleitet. Jede neue Instanz muss ihr Spielfenster selbst verbinden; keine Aufnahme wird automatisch übernommen. Alte Versionen/Staging-Dateien werden nicht automatisch bereinigt. Online-Updates und die offenen echten Karten-/Overlay-Abnahmen aus SPEC-001/SPEC-003 sind nicht Bestandteil dieses Nachweises.
