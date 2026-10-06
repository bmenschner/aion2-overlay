# SPEC-005: Live-Kartenzuordnung und Overlay-Geometrie

Status: in Umsetzung. Stand: 6. Oktober 2026. Vorstufe zu Cube-Markern, kein Cube-Datensatz.

## Ziel und Ablauf

Vorhandene Referenz einmal automatisch abgleichen und „Live-Zuordnung starten“ wählen. Die laufende Aufnahme liefert anschließend neue Spielbilder. Ein grüner Unterstützungsbereich und ein fest in der Referenz verankerter, als Test gekennzeichneter Punkt werden im separaten Overlay dargestellt. Nach Zoom/Verschieben neu zuordnen; unsichere/veraltete Ergebnisse ausblenden. Eine Checkbox im Hauptfenster beendet die Live-Zuordnung. Zusätzliche Kontrollscreenshots und manuelle Genauigkeitsfreigaben sind optional, nie erforderlich.

Die Anzeige bestätigt nur Bildübereinstimmung mit der Referenz. Sie behauptet weder vollständige semantische Kartenmoduserkennung noch Spielerposition oder Cube-Verfügbarkeit. Testpunkt und Bereich sind eine Entwicklungsanzeige. Zurückgestellt: echte Cube-Daten, Navigation, Profilstart ohne neue Prüfung, allgemeine Unterstützung anderer Kartenstile.

Fehlerkorrektur auf Nutzerbericht vom 6. Oktober 2026: Grüner Rahmen und Testtext in v0.4.0 sind im Spiel sichtbar. Nach Alt-Tab fehlen Kreis/Text bei Rückkehr dauerhaft, bis Aufnahme und Abgleich neu gestartet werden. Der genaue native Auslöser im Spiel ist noch nicht gemessen. Die Anwendung muss die bestehende Referenz und Live-Aktivierung über Fokuswechsel erhalten und bei Rückkehr automatisch eine frische Aufnahme samt neuem Fit gewinnen.

Ergänzter Befund: Der Nutzer bestätigt denselben Fehler in v0.4.1 mit „Nur 1–2 gemeinsame Merkmale“. Die Live-Verarbeitung bleibt aktiv, kann das gelieferte Bild aber nicht zuordnen. Das widerspricht einer Erklärung ausschließlich durch pausierte Frames; Aufnahmeinhalt und Verhalten der Karte bei Fokusverlust müssen getrennt untersucht werden. Die Korrektur darf Qualitätsgrenzen nicht senken oder einen alten Fit als aktuell anzeigen.

## Verarbeitung und Koordinaten

- WGC-SystemRelativeTime (Compositor-QPC) liefert das Bildalter; bloßer Empfangszeitpunkt genügt nicht. Jeder Frame erhält eine Sequenz und nach Möglichkeit eine konsistente physische DWM-Fenster-/Clientgeometrie. Bei unbekannter/abweichender Geometrie bleibt die Live-Anzeige gesperrt.
- Ein nativer Abgleich gleichzeitig, höchstens ein neuester wartender Frame. Mindestens 500 ms zwischen Starts; Verwerfen überholter Referenz-/Sitzungsergebnisse. Keine unbegrenzte Warteschlange.
- Ein schneller Fingerabdruck des zentralen Bildbereichs entwertet die alte Zuordnung bei sichtbarer Ansichtsänderung vor dem nächsten vollständigen Fit. Kleine dynamische Symbole dürfen toleriert werden. Dies ist eine zu prüfende Heuristik; kein eigenständiger Genauigkeitsnachweis.
- Ein Fit wird nur angezeigt, wenn seine Qualitätsprüfung aus SPEC-003 besteht, sein Frame höchstens zwei Sekunden alt ist und die aktuelle Ansicht/Geometrie noch passen. Anderenfalls ausblenden und auf neuen Abgleich warten.
- Referenzoriginalpixel → registrierte Aufnahmeoriginalpixel → physischer Clientpunkt unter Abzug von Rahmen-/Titelleistenoffset → WPF-DIPs anhand aktueller Overlay-DPI. Kein pauschales Strecken einer vollständigen Fensteraufnahme auf den Clientbereich. Unterstützung und Clientrechteck begrenzen die Zeichnung.
- Fensterbewegung darf bei gleicher relativer Geometrie mitgeführt werden; Größen-/DPI-Geometrieänderungen erfordern erneute Zuordnung. Fokusverlust/Minimieren/Stop/Referenzwechsel/Schließen entfernen die Anzeige. Das Overlay aktiviert sich nicht und bleibt mausdurchlässig.
- Normale Bilder bleiben im Speicher. Ausführliche Messungen sind Entwicklungsaufgaben; kein zusätzlicher Screenshot vom Nutzer nötig. Diagnose nimmt ausschließlich eigene Testfenster auf.
- Wiederaufnahme: Der Capture-Worker erkennt die Rückkehr des sichtbaren, nicht minimierten Ziel-HWND in den Vordergrund. Er erneuert Framepool/Sitzung im selben Worker und entwertet zuvor gelieferte Live-Frames, bevor neue Ergebnisse angenommen werden. Referenz, Checkbox und Nutzerwahl bleiben erhalten. Bei ausbleibenden frischen Compositor-Frames im Vordergrund erfolgt ein begrenzter neuer Versuch mit mindestens zwei Sekunden Abstand. Keine Änderung alter Zeitstempel, kein Anzeigen ungeprüfter alter Fits. Stop/Schließen unterbinden weitere Wiederaufnahme.
- Erweiterte Wiederaufnahme: GraphicsCaptureItem und D3D-Gerät werden bei Wiederaufnahme ebenfalls erneuert. Schließmeldungen eines ersetzten CaptureItems dürfen die neue Generation nicht beenden. Die bestehende Referenz bleibt erhalten. Dies beseitigt die Wiederverwendung eines möglicherweise gestörten Aufnahmekontexts, beweist für sich aber keine richtige Aufnahme im Spiel.
- Optionale Diagnose: „Diagnose speichern“ exportiert auf ausdrücklichen Klick ein ZIP mit dem letzten tatsächlich analysierten Frame samt zugehöriger Meldung, gegebenenfalls der Live-Referenz und einem Zustandsbericht (Version, Zeit, Bild-/Fenstergeometrie, Zustand und Zähler). Liegt keine abgeschlossene Analyse vor, wird die letzte gelieferte Aufnahme ausdrücklich als solche exportiert. Keine automatische Bildspeicherung; keine Pfade, Fenstertitel oder Zugangsdaten im Bericht. Diagnoseexport ist keine Voraussetzung für die Bedienung. Bei Fehlern des automatischen Abgleichs bleibt aus der Meldung erkennbar, dass der Abgleich weiterläuft.

## Abnahmekriterien

| ID | Verhalten | Nachweis |
| --- | --- | --- |
| AC-01 | Automatischer Dialog startet Live-Zuordnung ohne Kontrollscreenshot; Abschalten/Stop entfernen sie | Echter Hauptfenster-/Dialogablauf mit eigenem WGC-Ziel |
| AC-02 | Zoom/Translation führen Punkt/Bereich nach; kein akzeptierter Prüffall überschreitet die SPEC-003-Pixeltoleranz | Reale Matcher-Aufrufe auf bekannten synthetischen Abbildungen und unabhängige Kontrollpositionen |
| AC-03 | Geänderte/falsche/geschlossene Ansicht entfernt die alte Anzeige spätestens beim nächsten gelieferten Frame; ohne neue Frames nach zwei Sekunden plus 100 ms Timerintervall | Sequenztest und Zeit-/Zustandsprüfungen |
| AC-04 | Rahmen-/Clientoffset, Ultrawide, negative Monitorpositionen und DPI werden korrekt umgerechnet; unklare Geometrie sperrt Anzeige | Kernlogiktests, echte gerahmte/borderlose eigene Aufnahme, 100/150-%-DIP-Rechentests; tatsächlicher Monitorwechsel separat prüfen |
| AC-05 | Ein Worker und ein neuester Frame, kein altes Ergebnis nach Stop/Wechsel; Schließen gibt native Ressourcen frei | Verzögerter Matcher/Zustandsregression und getrennte Prozess-Schließtests |
| AC-06 | Anzeige nur auf ausgewähltem Vordergrundziel, ohne Aktivierung oder Mausblockade; eigene WGC-Aufnahme enthält keine Live-Markierungen | Eigene Fenster-/Stil-/Fokusprüfung; echte menschliche Klickprüfung offenlassen |
| AC-07 | Tatsächliche EU/Global-Nutzerprüfung für Zoom, Verschieben, Schließen/Wiederöffnen, DPI-Deckung | Anzeige von grünem Rahmen/Testtext vom Nutzer bestätigt; weitere Varianten offen. Keine Aussage aus synthetischen Bildern ableiten |
| AC-08 | Nach Fokusverlust über zwei Sekunden und Rückkehr nimmt dieselbe Aufnahme-/Live-Instanz mit derselben Referenz automatisch wieder auf; altes/inzwischen unpassendes Bild bleibt ausgeblendet, Stop beendet Wiederaufnahme | Drei Hintergrund-/Rückkehrfolgen am eigenen echten WGC-Ziel mit expliziter Test-Fokusquelle; Timer-/Drosselungslogik getrennt testen. Echtes Alt-Tab im Spiel bleibt bis Nutzerprüfung offen |
| AC-09 | Expliziter Diagnoseexport enthält das tatsächlich verarbeitete Originalbild, die aktuelle Live-Referenz und Versions-/Zustandsangaben; Fehler meldet er ohne Beenden der Aufnahme. Alte CaptureItem-Generationen können keine neue Sitzung schließen | ZIP-Prüfung mit eigenen Testbildern, Generationsprüfung und erneute native Wiederaufnahme-/Schließtests; keine automatische Spielbildspeicherung |

Ein Entwicklungsprototyp kann mit offenen Ingame-Kriterien bereitgestellt werden; der Status bleibt dann in Umsetzung. Quellen: [Microsoft QPC-Zeit](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframe.systemrelativetime), [physische DWM-Grenzen](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowrect), abgerufen am 6. Oktober 2026. Grenzen und Ergebnisse werden in `docs/validation/005-live-kartenzuordnung.md` dokumentiert.
