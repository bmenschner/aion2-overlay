# Architektur: Aufnahme, Bildabgleich und Live-Prototyp

Stand: 6. Oktober 2026, v0.4.0. Bezug: [SPEC-001](specs/001-overlay-capture.md), [SPEC-002](specs/002-map-calibration.md), [SPEC-003](specs/003-auto-map-registration.md) und [SPEC-005](specs/005-live-kartenzuordnung.md).

## Projekte

- `Aion2Overlay.Core`: Plattformunabhängige physische Rechtecke, Fenstersnapshot und Entscheidung zur Overlay-Sichtbarkeit.
- Der Kern enthält außerdem affine Kartenkalibrierung, die Uniform-Bildgeometrie und die Erstellung geprüfter Kalibrierungsprofile.
- `Aion2Overlay.Imaging`: Nativer OpenCV-Abgleich; der Kern enthält Qualitätsgate, registrierte Bildräume, Live-Frische, Bildfingerabdruck und Aufnahme-/Client-/DIP-Geometrie.
- `Aion2Overlay.App`: C#/.NET 10, Windows-API-Projektionen und WPF-Oberfläche. Der Prozess ist DPI-aware pro Monitor und läuft als x64 ohne Administratoranforderung.
- `Aion2Overlay.Core.Tests`: Regressionstests für Fokuswechsel, Minimierung, geschlossene Ziele, ungültige Geometrie und deaktiviertes Overlay.

## Datenfluss

`WindowCatalog` enumeriert geeignete HWNDs. Die Nutzerwahl liefert Handle, Prozess-ID und Titel. `WindowCaptureService` erzeugt für genau dieses Handle ein `GraphicsCaptureItem`, ein D3D11-Gerät und einen freilaufenden Capture-Framepool. Ein Worker kopiert ungefähr fünf Frames pro Sekunde in CPU-BGRA-Daten. Der Verbraucher wird abgewartet, sodass keine unbegrenzte Vorschauwarteschlange wächst.

Die Vorschau wird auf dem WPF-Dispatcher aktualisiert. Seit v0.4.0 stammt der Framezeitpunkt aus `Direct3D11CaptureFrame.SystemRelativeTime` relativ zur Compositor-QPC-Uhr; reine Empfangszeit würde alte wiederholte Bilder als frisch behandeln. Physische DWM-Rahmengrenzen und Clientgrenzen werden vor/nach dem Kopieren geprüft. Bei inkonsistenter oder nicht zur Aufnahmegröße passender Geometrie werden keine Live-Punkte gezeichnet. Keine Aufnahme wird im normalen Betrieb gespeichert. Beim Stop wird der Worker abgewartet, bevor Sitzung, Framepool und Gerät freigegeben werden. Ein neues Starten erzeugt eine neue Sitzung.

Bei einer Größenänderung werden nach Freigabe des alten Frames Sitzung und Framepool neu angelegt. Ein statisches Fenster liefert nach bloßer Änderung des Framepools möglicherweise kein weiteres Bild, solange sein Inhalt unverändert bleibt. Die neue Sitzung fordert eine vollständige Aufnahme an; der Regressionstest prüft diesen Fall ausdrücklich.

`OverlayController` fragt alle 100 ms den physischen Clientbereich und den Fensterzustand ab. Er prüft auch die Prozess-ID, damit ein wiederverwendetes Handle nicht einfach zum neuen Ziel wird. `OverlayWindow` wird nativ ohne Aktivierung ausgerichtet; Layered-/Transparent-/NoActivate-Stile und Hit-Test-Antworten unterbinden Eingaben an den Testrahmen. Nur das ausgewählte Vordergrundfenster erhält einen sichtbaren Rahmen.

## Anwendungslaufzeit und Schließen

Seit v0.3.1 ist der feste Einstieg `Aion2Overlay starten.cmd` → `scripts/Start-Overlay.ps1`. Der Starter liest `artifacts/current.json`, prüft das aktive Paketmanifest und sämtliche Dateihashes und öffnet dessen EXE als eigenen Prozess. `Publish-Overlay.ps1` legt geprüfte Pakete in unveränderliche Versions-/Inhaltsordner unter `artifacts/releases/` und ersetzt ausschließlich die Aktivdatei atomar. Ein älterer Prozess behält seinen Ordner und läuft weiter; nachfolgende Starts verwenden den neuen Stand. Keine Instanzsperre oder automatische Prozessbeendigung. Fehler führen zu einer Meldung statt stillem Rückfall. [SPEC-004](specs/004-versionierter-start.md), [Validierung](validation/004-versionierter-start.md).

Seit v0.2.2 bindet `App` die normale Anwendung an das Hauptfenster (`OnMainWindowClose`). `MainWindow` merkt einen Windows-Schließwunsch vor und deaktiviert weitere Aktionen. Falls Start/Stop bereits laufen, plant deren Abschluss das Beenden; andernfalls wird es sofort über den Dispatcher eingeplant. Das ursprüngliche `Closing`-Ereignis ist vor dem Aufräumen beendet. Eine einmalige Aufgabe stoppt Timer, Dialog, Overlay und Aufnahme und beendet die Anwendung auch bei einem Aufräumfehler. Ein zweiter Klick ist nicht nötig. Das Kalibrierungsdialog-„X“ schließt ausschließlich den Dialog. Nachweis: [Schließtests SPEC-001](validation/001-overlay-capture.md).

## Manueller Kalibrierungsdialog (Entwicklungsdiagnose SPEC-002)

Bis v0.2.2 öffnete `MainWindow` den manuellen `CalibrationWindow` aus einer weniger als zwei Sekunden alten Aufnahme. Seit v0.3.0 ist der automatische Dialog Standard; der alte Dialog bleibt im eigenständigen `--calibration-smoke-test` als Entwicklungsdiagnose. Seine eingefrorene BitmapSource und ihr Zeitstempel folgen keinen weiteren Frames. Eine lokale Referenzkarte wird mit SHA256 identifiziert, auf 96 DPI normalisiert und neben der Aufnahme angezeigt. `ImageViewport` bildet Klicks und Markierungen konsistent auf die tatsächlich dargestellten Uniform-Bildrechtecke ab; Letterbox-Ränder werden ausgeschlossen.

`MapCalibration` rechnet normierte Referenzbildkoordinaten auf physische Pixel des vollständigen Aufnahmebilds um. Drei Fit-Paare und zwei unabhängige Prüfpaare liefern eine Transformation mit Fehlerwerten und höhenabhängiger Toleranz. Erst bei bestandener Prüfung und vollständigen Metadaten erzeugt `CalibrationProfile.Create` das lokale Profil. Die Speicherung erfolgt ausschließlich über den ausdrücklich verwendeten Dateidialog. Das Profil enthält keine Bilder oder Fensterdaten und wird noch nicht geladen oder auf das Live-Overlay angewendet.

## Grenzen der aktuellen Integration

Seit v0.2.1 liegt jedes Dialogbild in einem eigenen ScrollViewer. Die Bildfläche wächst entsprechend der Dialog-Vergrößerung (1× bis 16×); `ImageViewport` rechnet weiterhin auf das ursprüngliche Bild. Mauspositionen werden vom ScrollViewer in dessen Bildfläche transformiert, sodass Scrolloffsets berücksichtigt werden. Vergrößerung und Scrollen ändern weder gespeicherte Paare noch den Ingame-Kartenausschnitt.

Fensteraufnahme und Clientgeometrie sind verschiedene Koordinatenräume. Seit v0.4.0 werden registrierte Aufnahmeoriginalpixel über den physischen Rahmen-/Clientoffset in Clientpixel und über aktuelle Overlay-DPI in DIPs umgerechnet. Ein gerahmtes eigenes WGC-Ziel und synthetische Ultrawide-/DPI-Fälle sind geprüft; tatsächlicher Monitorwechsel und Ingame-Deckung bleiben offen. Bildübereinstimmung ersetzt keine vollständig geprüfte semantische Kartenmoduserkennung. Die echte Mausdurchlässigkeit muss auf der Zielkonfiguration praktisch geprüft werden.

Es gibt kein Prozessspeicherlesen, keine Injektion und keine Netzwerkpaketerfassung. Es gibt keine zusätzlichen Dienste, Nutzerkonten oder Datenbanken. Für den Build werden Windows-SDK-.NET-Projektionen über NuGet bereitgestellt.

## Automatischer Bildabgleich v0.3.0

[SPEC-003](specs/003-auto-map-registration.md) ersetzt seit v0.3.0 die manuelle Punktwahl im normalen Nutzerablauf. `MainWindow` öffnet `AutomaticRegistrationWindow` und liefert beim Klick auf „Automatisch abgleichen“ eine höchstens zwei Sekunden alte Aufnahme. Diese bleibt während des Vergleichs eingefroren. `Aion2Overlay.Imaging` implementiert `IMapRegistrationService` mit OpenCvSharp4 und nativer Windows-Slim-Runtime `4.13.0.20260627`; `RegistrationQualityGate` und Ergebnis-/Profilgeometrie liegen unabhängig im Kern. Der manuelle Dialog besteht ausschließlich als bisherige Entwicklungsdiagnose.

Der implementierte Datenfluss lautet: Referenzhash/Original + Frame/Original → getrennte Layout-/Gelände-Masken → maximal 1600 Pixel lange Arbeitsbilder → SIFT-/gegenseitige L2-Zuordnung → RANSAC-Ähnlichkeit → räumlich zurückgehaltene Merkmale und lokale Intensitätskontrolle → Transformation in ursprünglichen Aufnahmepixeln plus begrenzter Supportbereich → eingefrorene Überlagerungsvorschau. Resize-Geometrie berücksichtigt tatsächliche Achsenskalierung und Pixelzentren; Crop-Offsets sind im Kernmodell prüfbar. Ein komplexeres Modell und ECC sind nicht implementiert.

Referenzcache und Schema-2-Profil ersetzen keinen frischen Abgleich. Referenzwechsel, Abbruch und Dialogschließen entwerten laufende Ergebnisse über Generation/Token; eine Semaphore serialisiert native Aufträge. Der Dispatcher bleibt bedienbar; Abbruch entwertet das Ergebnis sofort, während ein bereits laufender nativer Aufruf bis zur nächsten Tokenprüfung enden kann. Schließen wartet auf die Aufträge und gibt Cache-/Mat-Ressourcen frei. Bilder bleiben ohne ausdrücklichen Diagnoseexport im Speicher. Der ausdrücklich gewählte Referenzpfad/Hash wird getrennt in AppData gemerkt; Profile exportieren keine Pfade/Bilder. Nachweise: [Validierung](validation/003-auto-map-registration.md); Entscheidung/Quellen: [ADR-001](decisions/001-automatischer-kartenabgleich.md), [Recherche](research/automatischer-kartenabgleich.md).

## Live-Zuordnung v0.4.0

„Live-Zuordnung starten“ übernimmt Referenzoriginal, Hash und einen festen Referenzanker aus dem erfolgreichen Dialog. Der eingefrorene Fit wird nicht als Live-Ergebnis weiterverwendet. `LiveMapController` erstellt einen eigenen Matcher und prüft frische Frames erneut. Der Referenzanker bleibt gleich; ein neu berechneter Unterstützungsbereich begrenzt seine Anzeige. `OverlayWindow.DrawLive` zeichnet den Bereich und „TEST · kein Cube“, wenn die Transformation den Anker im geprüften Bereich abbildet. Es gibt noch keinen Cube-Datensatz.

Ein Dispatcher-Timer von 100 ms prüft Alter und neue Arbeit. Höchstens ein Auftrag läuft; nur der neueste Frame wartet, mindestens 500 ms liegen zwischen Starts. Ein Fingerabdruck aus 96×36 Grauproben im zentralen Bildbereich verwirft auffällige Ansichtsänderungen vor dem vollständigen Fit. Kleine Veränderungen werden toleriert; diese Heuristik muss an dynamischem Spielinhalt geprüft werden. Nur bestandene, weniger als zwei Sekunden alte, noch zur aktuellen Bild-/Fenstergeometrie passende Ergebnisse werden veröffentlicht.

Der Overlay-Controller hält die Anzeige am physischen Clientbereich und blendet sie bei Fokusverlust/Minimierung sowie ungeeigneter Geometrie aus. Das Abschalten entkoppelt den Live-Controller sofort, entfernt seine Anzeige und wartet auf die native Bereinigung. Auch ein anschließendes Windows-Schließen wartet auf diese bereits begonnene Bereinigung. Stop/Sitzungsende versteckt das alte Overlay vor dem Warten; Ergebnisse einer abgeschalteten Sitzung dürfen nichts erneut zeichnen. Nachweise und offene Ingame-Eigenschaften: [SPEC-005-Validierung](validation/005-live-kartenzuordnung.md).
