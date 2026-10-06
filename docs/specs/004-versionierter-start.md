# SPEC-004: Fester Starter und versionierte Pakete

Status: umgesetzt. Stand: 6. Oktober 2026. AC-01 bis AC-06 nachgewiesen in [Validierung](../validation/004-versionierter-start.md); tatsächliche Ingame-Abnahmen anderer Features bleiben offen.

## Ziel und Ablauf

Der Nutzer startet immer denselben Starter per Doppelklick. Dieser öffnet das zuletzt erfolgreich bereitgestellte Paket, auch während ältere Overlay-Instanzen laufen. Kein Suchen neuer Versionsordner oder Schließen der alten Instanz erforderlich.

Einmaliger Wechsel: `Aion2Overlay starten.cmd` im Projektstamm oder im bisherigen `artifacts/win-x64/` verwenden. Alte direkte App-EXEs bleiben eigenständige, versionsgebundene Einstiegspunkte. Der neue Starter behält seinen Namen und Ort bei folgenden Updates.

## Verhalten und Schnittstellen

- `Publish-Overlay.ps1` erstellt ein neues Windows-x64-Paket im Staging-Ordner und prüft Version sowie SHA256 aller Dateien. Versionsnummer und Inhalt bestimmen einen unveränderlichen Ordner unter `artifacts/releases/`. Auch Änderungen bei gleicher Versionsnummer bekommen einen anderen Ordner.
- Erst nach vollständiger Prüfung wird `artifacts/current.json` atomar ersetzt. Ein Fehler lässt die bisher aktive Version erhalten. Laufende Pakete werden weder überschrieben noch gelöscht; Prozesse werden nicht beendet oder neu gestartet.
- Aktivmanifest Schema 1: Version, relativer Paketpfad, SHA256 des Paketmanifests. Paketmanifest Schema 1: Version und relative Dateipfade mit SHA256. Keine Fenster-/Prozessdaten; Pakete bleiben außerhalb von Git.
- Der Starter prüft Manifest, Pfad, Version und sämtliche Dateihashes. Fehlende oder veränderte Daten erzeugen einen verständlichen Fehler; kein stiller Start eines alten Standes.
- Start als eigener Prozess mit Projektstamm als Arbeitsverzeichnis, ohne Instanzsperre. Aufnahme bleibt pro Instanz ausdrücklich vom Nutzer zu starten.
- Nutzerstart baut nicht nebenbei in einen gemeinsamen Build-Ordner. `Start-Overlay.ps1 -ValidateOnly` prüft ohne Start; Diagnoseargumente/Wait/PassThru dienen eigenen Tests. `Publish-Overlay.ps1 -PackageSource` prüft/aktiviert ein vorhandenes Paket nach denselben Regeln.
- Zurückgestellt: Online-Download, Updateprüfung im Internet, Löschen alter Pakete, Übernahme laufender Aufnahmen und Änderung fremder Verknüpfungen.

## Abnahmekriterien

| ID | Verhalten | Nachweis |
| --- | --- | --- |
| AC-01 | Derselbe Starter wählt nach Aktivierung das neue Paket; alte Dateien bleiben unverändert | Zwei diagnostische Paketversionen, SHA256-Vergleich |
| AC-02 | Ältere Diagnoseinstanz läuft während Bereitstellung und Start der neueren weiter | Getrennte Prozesse, PID/Version/Prozesspfad, Dateisperre |
| AC-03 | Fehlgeschlagene Bereitstellung lässt aktive Version unverändert | Ungültiges Quellpaket und Manifestvergleich |
| AC-04 | Fehlendes/beschädigtes Manifest oder Paket wird abgewiesen | Negative ValidateOnly-Prüfungen |
| AC-05 | Identische Inhalte überschreiben kein Paket; veränderter Inhalt bei gleicher Version bleibt getrennt | Paketidentität und Dateisperre |
| AC-06 | Fertiges Overlay-Paket wird über den festen Startweg gewählt und als eigener Diagnoseprozess gestartet | Versions-/Hashprüfung und vorhandener UI-Test; keine normale Nutzerinstanz gestartet/geschlossen |

Bezug: [SPEC-001](001-overlay-capture.md) für Anwendungslaufzeit und [SPEC-003](003-auto-map-registration.md) für das Kartenfeature; deren offene Ingame-Kriterien bleiben unverändert.
