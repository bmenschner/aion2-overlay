# Architektur: erster Prototyp

Stand: 6. Oktober 2026. Bezug: [SPEC-001](specs/001-overlay-capture.md) und [SPEC-002](specs/002-map-calibration.md).

## Projekte

- `Aion2Overlay.Core`: Plattformunabhängige physische Rechtecke, Fenstersnapshot und Entscheidung zur Overlay-Sichtbarkeit.
- Der Kern enthält außerdem affine Kartenkalibrierung, die Uniform-Bildgeometrie und die Erstellung geprüfter Kalibrierungsprofile.
- `Aion2Overlay.App`: C#/.NET 10, Windows-API-Projektionen und WPF-Oberfläche. Der Prozess ist DPI-aware pro Monitor und läuft als x64 ohne Administratoranforderung.
- `Aion2Overlay.Core.Tests`: Regressionstests für Fokuswechsel, Minimierung, geschlossene Ziele, ungültige Geometrie und deaktiviertes Overlay.

## Datenfluss

`WindowCatalog` enumeriert geeignete HWNDs. Die Nutzerwahl liefert Handle, Prozess-ID und Titel. `WindowCaptureService` erzeugt für genau dieses Handle ein `GraphicsCaptureItem`, ein D3D11-Gerät und einen freilaufenden Capture-Framepool. Ein Worker kopiert ungefähr fünf Frames pro Sekunde in CPU-BGRA-Daten. Der Verbraucher wird abgewartet, sodass keine unbegrenzte Vorschauwarteschlange wächst.

Die Vorschau wird auf dem WPF-Dispatcher aktualisiert. Keine Aufnahme wird im normalen Betrieb gespeichert. Beim Stop wird der Worker abgewartet, bevor Sitzung, Framepool und Gerät freigegeben werden. Ein neues Starten erzeugt eine neue Sitzung.

Bei einer Größenänderung werden nach Freigabe des alten Frames Sitzung und Framepool neu angelegt. Ein statisches Fenster liefert nach bloßer Änderung des Framepools möglicherweise kein weiteres Bild, solange sein Inhalt unverändert bleibt. Die neue Sitzung fordert eine vollständige Aufnahme an; der Regressionstest prüft diesen Fall ausdrücklich.

`OverlayController` fragt alle 100 ms den physischen Clientbereich und den Fensterzustand ab. Er prüft auch die Prozess-ID, damit ein wiederverwendetes Handle nicht einfach zum neuen Ziel wird. `OverlayWindow` wird nativ ohne Aktivierung ausgerichtet; Layered-/Transparent-/NoActivate-Stile und Hit-Test-Antworten unterbinden Eingaben an den Testrahmen. Nur das ausgewählte Vordergrundfenster erhält einen sichtbaren Rahmen.

## Kalibrierungsdialog

`MainWindow` öffnet `CalibrationWindow` nur aus einer weniger als zwei Sekunden alten Aufnahme. Die bereits eingefrorene BitmapSource und ihr Zeitstempel werden übernommen; der Dialog folgt keinen weiteren Frames. Eine lokale Referenzkarte wird mit SHA256 identifiziert, auf 96 DPI normalisiert und neben der Aufnahme angezeigt. `ImageViewport` bildet Klicks und Markierungen konsistent auf die tatsächlich dargestellten Uniform-Bildrechtecke ab; Letterbox-Ränder werden ausgeschlossen.

`MapCalibration` rechnet normierte Referenzbildkoordinaten auf physische Pixel des vollständigen Aufnahmebilds um. Drei Fit-Paare und zwei unabhängige Prüfpaare liefern eine Transformation mit Fehlerwerten und höhenabhängiger Toleranz. Erst bei bestandener Prüfung und vollständigen Metadaten erzeugt `CalibrationProfile.Create` das lokale Profil. Die Speicherung erfolgt ausschließlich über den ausdrücklich verwendeten Dateidialog. Das Profil enthält keine Bilder oder Fensterdaten und wird noch nicht geladen oder auf das Live-Overlay angewendet.

## Grenzen der aktuellen Integration

Fensteraufnahme und Clientgeometrie sind verschiedene Koordinatenräume. Die Kalibrierung liefert Aufnahmebildkoordinaten; deren Umrechnung in den Clientbereich für Live-Marker ist noch nicht implementiert oder geprüft. Die Karte selbst wird noch nicht erkannt. Die echte Mausdurchlässigkeit und genaue DPI-Deckung müssen auf der Zielkonfiguration praktisch geprüft werden.

Es gibt kein Prozessspeicherlesen, keine Injektion und keine Netzwerkpaketerfassung. Es gibt keine zusätzlichen Dienste, Nutzerkonten oder Datenbanken. Für den Build werden Windows-SDK-.NET-Projektionen über NuGet bereitgestellt.
