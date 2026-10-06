# SPEC-005: Live-Kartenzuordnung und Overlay-Geometrie

Status: in Umsetzung. Stand: 6. Oktober 2026. Vorstufe zu Cube-Markern, kein Cube-Datensatz.

## Ziel und Ablauf

Vorhandene Referenz einmal automatisch abgleichen und „Live-Zuordnung starten“ wählen. Die laufende Aufnahme liefert anschließend neue Spielbilder. Ein grüner Unterstützungsbereich und ein fest in der Referenz verankerter, als Test gekennzeichneter Punkt werden im separaten Overlay dargestellt. Nach Zoom/Verschieben neu zuordnen; unsichere/veraltete Ergebnisse ausblenden. Eine Checkbox im Hauptfenster beendet die Live-Zuordnung. Zusätzliche Kontrollscreenshots und manuelle Genauigkeitsfreigaben sind optional, nie erforderlich.

Die Anzeige bestätigt nur Bildübereinstimmung mit der Referenz. Sie behauptet weder vollständige semantische Kartenmoduserkennung noch Spielerposition oder Cube-Verfügbarkeit. Testpunkt und Bereich sind eine Entwicklungsanzeige. Zurückgestellt: echte Cube-Daten, Navigation, Profilstart ohne neue Prüfung, allgemeine Unterstützung anderer Kartenstile.

## Verarbeitung und Koordinaten

- WGC-SystemRelativeTime (Compositor-QPC) liefert das Bildalter; bloßer Empfangszeitpunkt genügt nicht. Jeder Frame erhält eine Sequenz und nach Möglichkeit eine konsistente physische DWM-Fenster-/Clientgeometrie. Bei unbekannter/abweichender Geometrie bleibt die Live-Anzeige gesperrt.
- Ein nativer Abgleich gleichzeitig, höchstens ein neuester wartender Frame. Mindestens 500 ms zwischen Starts; Verwerfen überholter Referenz-/Sitzungsergebnisse. Keine unbegrenzte Warteschlange.
- Ein schneller Fingerabdruck des zentralen Bildbereichs entwertet die alte Zuordnung bei sichtbarer Ansichtsänderung vor dem nächsten vollständigen Fit. Kleine dynamische Symbole dürfen toleriert werden. Dies ist eine zu prüfende Heuristik; kein eigenständiger Genauigkeitsnachweis.
- Ein Fit wird nur angezeigt, wenn seine Qualitätsprüfung aus SPEC-003 besteht, sein Frame höchstens zwei Sekunden alt ist und die aktuelle Ansicht/Geometrie noch passen. Anderenfalls ausblenden und auf neuen Abgleich warten.
- Referenzoriginalpixel → registrierte Aufnahmeoriginalpixel → physischer Clientpunkt unter Abzug von Rahmen-/Titelleistenoffset → WPF-DIPs anhand aktueller Overlay-DPI. Kein pauschales Strecken einer vollständigen Fensteraufnahme auf den Clientbereich. Unterstützung und Clientrechteck begrenzen die Zeichnung.
- Fensterbewegung darf bei gleicher relativer Geometrie mitgeführt werden; Größen-/DPI-Geometrieänderungen erfordern erneute Zuordnung. Fokusverlust/Minimieren/Stop/Referenzwechsel/Schließen entfernen die Anzeige. Das Overlay aktiviert sich nicht und bleibt mausdurchlässig.
- Normale Bilder bleiben im Speicher. Ausführliche Messungen sind Entwicklungsaufgaben; kein zusätzlicher Screenshot vom Nutzer nötig. Diagnose nimmt ausschließlich eigene Testfenster auf.

## Abnahmekriterien

| ID | Verhalten | Nachweis |
| --- | --- | --- |
| AC-01 | Automatischer Dialog startet Live-Zuordnung ohne Kontrollscreenshot; Abschalten/Stop entfernen sie | Echter Hauptfenster-/Dialogablauf mit eigenem WGC-Ziel |
| AC-02 | Zoom/Translation führen Punkt/Bereich nach; kein akzeptierter Prüffall überschreitet die SPEC-003-Pixeltoleranz | Reale Matcher-Aufrufe auf bekannten synthetischen Abbildungen und unabhängige Kontrollpositionen |
| AC-03 | Geänderte/falsche/geschlossene Ansicht entfernt die alte Anzeige spätestens beim nächsten gelieferten Frame; ohne neue Frames nach zwei Sekunden plus 100 ms Timerintervall | Sequenztest und Zeit-/Zustandsprüfungen |
| AC-04 | Rahmen-/Clientoffset, Ultrawide, negative Monitorpositionen und DPI werden korrekt umgerechnet; unklare Geometrie sperrt Anzeige | Kernlogiktests, echte gerahmte/borderlose eigene Aufnahme, 100/150-%-DIP-Rechentests; tatsächlicher Monitorwechsel separat prüfen |
| AC-05 | Ein Worker und ein neuester Frame, kein altes Ergebnis nach Stop/Wechsel; Schließen gibt native Ressourcen frei | Verzögerter Matcher/Zustandsregression und getrennte Prozess-Schließtests |
| AC-06 | Anzeige nur auf ausgewähltem Vordergrundziel, ohne Aktivierung oder Mausblockade; eigene WGC-Aufnahme enthält keine Live-Markierungen | Eigene Fenster-/Stil-/Fokusprüfung; echte menschliche Klickprüfung offenlassen |
| AC-07 | Tatsächliche EU/Global-Nutzerprüfung für Zoom, Verschieben, Schließen/Wiederöffnen, DPI-Deckung | Noch offen; keine Aussage aus synthetischen Bildern ableiten |

Ein Entwicklungsprototyp kann mit offenen Ingame-Kriterien bereitgestellt werden; der Status bleibt dann in Umsetzung. Quellen: [Microsoft QPC-Zeit](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframe.systemrelativetime), [physische DWM-Grenzen](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowrect), abgerufen am 6. Oktober 2026. Grenzen und Ergebnisse werden in `docs/validation/005-live-kartenzuordnung.md` dokumentiert.
