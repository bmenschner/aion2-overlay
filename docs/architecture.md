# Architektur: erster Prototyp

Stand: 6. Oktober 2026. Bezug: [SPEC-001](specs/001-overlay-capture.md).

## Projekte

- `Aion2Overlay.Core`: Plattformunabhängige physische Rechtecke, Fenstersnapshot und Entscheidung zur Overlay-Sichtbarkeit.
- `Aion2Overlay.App`: C#/.NET 10, Windows-API-Projektionen und WPF-Oberfläche. Der Prozess ist DPI-aware pro Monitor und läuft als x64 ohne Administratoranforderung.
- `Aion2Overlay.Core.Tests`: Regressionstests für Fokuswechsel, Minimierung, geschlossene Ziele, ungültige Geometrie und deaktiviertes Overlay.

## Datenfluss

`WindowCatalog` enumeriert geeignete HWNDs. Die Nutzerwahl liefert Handle, Prozess-ID und Titel. `WindowCaptureService` erzeugt für genau dieses Handle ein `GraphicsCaptureItem`, ein D3D11-Gerät und einen freilaufenden Capture-Framepool. Ein Worker kopiert ungefähr fünf Frames pro Sekunde in CPU-BGRA-Daten. Der Verbraucher wird abgewartet, sodass keine unbegrenzte Vorschauwarteschlange wächst.

Die Vorschau wird auf dem WPF-Dispatcher aktualisiert. Keine Aufnahme wird im normalen Betrieb gespeichert. Beim Stop wird der Worker abgewartet, bevor Sitzung, Framepool und Gerät freigegeben werden. Ein neues Starten erzeugt eine neue Sitzung.

Bei einer Größenänderung werden nach Freigabe des alten Frames Sitzung und Framepool neu angelegt. Ein statisches Fenster liefert nach bloßer Änderung des Framepools möglicherweise kein weiteres Bild, solange sein Inhalt unverändert bleibt. Die neue Sitzung fordert eine vollständige Aufnahme an; der Regressionstest prüft diesen Fall ausdrücklich.

`OverlayController` fragt alle 100 ms den physischen Clientbereich und den Fensterzustand ab. Er prüft auch die Prozess-ID, damit ein wiederverwendetes Handle nicht einfach zum neuen Ziel wird. `OverlayWindow` wird nativ ohne Aktivierung ausgerichtet; Layered-/Transparent-/NoActivate-Stile und Hit-Test-Antworten unterbinden Eingaben an den Testrahmen. Nur das ausgewählte Vordergrundfenster erhält einen sichtbaren Rahmen.

## Grenzen

Fensteraufnahme und Clientgeometrie sind verschiedene Koordinatenräume. Der Prototyp rechnet noch keine Kartenmarker in Aufnahmekoordinaten um. Die Karte selbst wird noch nicht erkannt. Die transparente WPF-Darstellung und echte Mausdurchlässigkeit müssen auf der Zielkonfiguration praktisch geprüft werden.

Es gibt kein Prozessspeicherlesen, keine Injektion und keine Netzwerkpaketerfassung. Es gibt keine zusätzlichen Dienste, Nutzerkonten oder Datenbanken. Für den Build werden Windows-SDK-.NET-Projektionen über NuGet bereitgestellt.
