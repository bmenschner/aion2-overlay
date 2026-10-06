# Validierung SPEC-005: Live-Kartenzuordnung v0.4.0

Prüfdatum: 6. Oktober 2026. Bezug: [SPEC-005](../specs/005-live-kartenzuordnung.md). Status: technischer Entwicklungsprototyp bereitgestellt, Ingame-Abnahme offen; Spezifikation bleibt in Umsetzung. Ein zusätzlicher Kontrollscreenshot oder manuelle Genauigkeitsfreigabe ist keine Bedienvoraussetzung.

## Umgebung und Paket

Windows x64, Windows-Version `10.0.26300.0`, .NET SDK 10.0.401. Gerät wie in [SPEC-003-Validierung](003-auto-map-registration.md): Intel Core i7-13700KF, 24 logische Prozessoren. OpenCvSharp4 und native Windows-Slim-Runtime `4.13.0.20260627`. Keine Aion-/Nutzerinstanz wurde für diese Prüfungen gestartet, aufgenommen oder geschlossen. Diagnoseprozesse nutzen ausschließlich eigene synthetische Fenster.

`scripts/Publish-Overlay.ps1` hat das eigenständige Paket aktiviert; anschließend bestätigt `scripts/Start-Overlay.ps1 -ValidateOnly` Dateiversion `0.4.0.0` und alle 410 Dateien. Manifest-SHA256: `98D9D71FB75470DB9EA2B36494E9D2AAF356B4C6F07ED5B785D57B4D92E3CB71`. Fester Einstieg bleibt `Aion2Overlay starten.cmd`; ältere laufende Pakete werden nicht ersetzt oder beendet. Live-, Schließ-, Farb- und Aufnahmeprüfungen unten wurden über diesen Starter im finalen Paket ausgeführt.

## Ergebnisse nach Abnahmekriterium

| Kriterium | Ergebnis und Grenze |
| --- | --- |
| AC-01 | Technisch bestanden: echte Hauptoberfläche → laufende WGC-Aufnahme → automatischer Dialog → erfolgreicher Fit → „Live-Zuordnung starten“ → neue Live-Prüfung. Abschalten und Stop entfernen Anzeige. Keine Handpunkte, kein Kontrollscreenshot, kein Pflicht-Profil. |
| AC-02 | Technisch bestanden für vier bekannte synthetische Live-Abbildungen: Maßstab 1/0,7/1,3/1 und Translation. Je 132–239 unabhängige Kontrollpunkte innerhalb des Supports, maximaler wahrer P95-Fehler 0,147 Originalpixel gegenüber 6,667 Pixel Toleranz bei 1600×900. Das ist kein Nachweis für reale Ingame-Zoomfolgen. |
| AC-03 | Teilweise belegt: getestete deutliche Bildänderungen und leeres Bild entfernen die Anzeige sofort bei Framezustellung; ohne neue Frames läuft sie aus. Universelle Erkennung kleiner Verschiebungen, dynamischer Wolken/Symbole oder Kartenöffnen/-schließen im Spiel ist offen. |
| AC-04 | Teilweise belegt: Kernlogik prüft 5120×1440, negative Bildschirmkoordinaten, Rahmen-/Clientoffset und 100/150-%-DIPs sowie Ablehnung unbekannter/inkompatibler Geometrie. Echtes gerahmtes WGC-Ziel: Frame 986×643, DWM-Grenzen (117,100,986,643), Client (118,131,984,611); Testpunkt korrekt umgerechnet bei DPI 1. Zusätzlicher borderloser Aufnahme-/Resize-Test besteht, aber kein vollständiger Live-Test bei tatsächlichem DPI-/Monitorwechsel. |
| AC-05 | Technisch bestanden: verzögerter Matcher erhält bei 26 angebotenen Frames genau einen laufenden Aufruf; nach Abschalten kein verspätetes Ergebnis, native Bereinigung abgeschlossen. Schließen während echter nativer Live-Arbeit und direkt nach Abwählen besteht als separater Prozesscheck. Referenz-/Sitzungswechsel beendet den bisherigen Controller vor neuem Start. |
| AC-06 | Teilweise belegt: Transparent-/Layered-/NoActivate-Stile, Hintergrund-Ausblendung und korrekt umgerechnete Testanzeige. Aufnahme des eigenen Ziel-HWND enthält den darüber gezeichneten grünen Testmarker nicht. Die Live-Diagnose durfte ihr Ziel per `Activate()` nicht in den Vordergrund holen (`foregroundProved=false`); nur für die Rückkopplungsprüfung wurde ihr eigenes Overlay kurz sichtbar gemacht. Keine erfolgreiche Live-Fokus-/menschliche Klickprüfung daraus ableiten. |
| AC-07 | Teilweise durch Nutzerbeobachtung belegt: Grüner Rahmen und Testtext in v0.4.0 werden im Spiel ausgegeben. Nach Alt-Tab fehlen Kreis/Text bei Rückkehr dauerhaft. Weitere reale Karten-/Zoom-/Panfolgen, Schließen/Wiederöffnen, tatsächliche DPI-Deckung und Klickdurchleitung bleiben offen. |

Das Overlay des Diagnoseziels wurde als `artifacts/live-map-v040-final.overlay.png` gerendert und visuell geprüft: grüner Supportbereich, begrenzter Testpunkt mit „TEST · kein Cube“, optionaler goldener Rahmen. Das Bild zeigt nur das separate Overlay und belegt keine Deckung über Aion.

## Regression und lokale Berichte

- Release-Build und Paketveröffentlichung erfolgreich; 35 Kernlogiktests bestanden. Fünf neue Tests sichern Originalpixel-/Client-/DIP-Umrechnung, inkompatible Geometrie, Frische/Generation und Bildänderungsheuristik.
- `artifacts/live-map-v040-final.json`: zehn Prüfungen bestanden; echte Matcher auf den vier Live-Abbildungen, Entwertung/Alter, begrenzter Worker und normaler WGC→Dialog→Live-Ablauf. Zeitstempel `2026-10-06T15:12:39Z`.
- `artifacts/registration-v040.json`: 54 vorhandene Matcher-/Dialogprüfungen bestanden, 30 bekannte synthetische Warps mit maximalem wahrem P95-Fehler 1,227 Pixel, 20 Negativfälle; native Laufzeit und normaler WGC-Dialogablauf. Auf v0.4.0 nach der letzten produktiven Änderung ausgeführt; die danach ergänzte Schließdiagnose ändert den Matcher nicht.
- Sieben separate Windows-Schließtests, alle Prozess-Exitcode 0, keine verbleibenden WPF-Fenster und Capture-/Live-Worker vor Exit beendet. Berichte `artifacts/shutdown-v040-<scenario>-final.json`: `idle` 15 ms, `capture` 22 ms, `busy` 380 ms, `dialog` 38 ms, `repeat` 23 ms, `live` 152 ms, `live-unchecked` 54 ms. Das Titel-X wird über denselben `SC_CLOSE`-Befehl ausgelöst. Kein Nutzerprozess wird angesprochen.
- `artifacts/ui-smoke-test.json`: elf Farbprüfungen bestanden im finalen v0.4.0-Hauptfenster. Weiß auf aktiven gelben Buttons, Schwarz auf weißen/deaktivierten Buttons und auf Dropdown-Auswahl/-Einträgen. Eigene Fensteraufnahme visuell geprüft.
- `artifacts/capture-v040-final.json`: echte eigene borderlose Aufnahme 640×360 → 720×420, Zielinhalt, Overlay-Position/Stile, Hintergrund-Ausblendung, Stop, Neustart und geschlossenes Ziel bestanden.

Berichte und PNGs liegen lokal im ignorierten Artefaktordner. Diese Markdown-Datei hält die reproduzierbaren Ergebnisse und ihre Grenzen im Repository fest. Interaktive Diagnosen benötigen eine echte Windows-Sitzung; CI übernimmt nur Build/Kernlogik.

## Implementierungsgrenzen und nächste praktische Prüfung

Frische basiert auf WGC-Compositor-QPC statt Frameempfang. Live-Ergebnisse sind höchstens zwei Sekunden alt; der Timer läuft alle 100 ms. Es läuft ein nativer Auftrag gleichzeitig mit mindestens 500 ms zwischen Starts und nur einem neuesten wartenden Bild. Dies belegt weder das 500-ms-P95-Ziel des Projektplans noch CPU-/Speicherbudgets oder eine zweistündige Sitzung.

Die Änderungsheuristik vergleicht 96×36 Grauproben im zentralen Bildbereich: mittlere Differenz höchstens 3 und höchstens 4 % Proben mit Differenz über 16. Sie toleriert kleine Änderungen und kann damit auch kleine Kartenverschiebungen bis zum nächsten Fit übersehen. Dynamischer Spielinhalt kann umgekehrt häufiges Ausblenden auslösen. Diese Schwellen sind Prototypparameter; sie wurden nicht als zuverlässig für alle Ingame-Ansichten freigegeben.

Nutzerablauf: v0.4.0 selbst über den festen Starter öffnen → Aion wählen/Aufnahme starten → Karte öffnen → „Karte abgleichen“ und vorhandene Referenz automatisch zuordnen → „Live-Zuordnung starten“ → zum Spiel zurück. Der feste Testpunkt soll beim Zoomen/Verschieben an derselben Referenzstelle bleiben. Unpassende/geschlossene Karte darf keine fortgesetzte Testanzeige erzeugen. Bei Problemen sichtbaren Zustand und angezeigten Grund berichten; zusätzliche Screenshots helfen optional bei der Diagnose. Echte Cube-Spots, Spielerposition, Meterangaben und Navigation sind weiterhin nicht implementiert.

## Nutzerbericht und Wiederaufnahme v0.4.1

Beobachtung vom 6. Oktober 2026, Nutzerumgebung Aion 2 EU/Global im randlosen Fenster: Initiale Testanzeige funktioniert. Nach einmaligem Alt-Tab und Rückkehr fehlen Kreis/Text, bis Aufnahme und Abgleich neu gestartet werden. Keine Aufzeichnung des nativen Frame-/Fokusverlaufs liegt vor; genaue Ursache im Spiel ist deshalb nicht abschließend bewiesen.

Codebefund v0.4.0: Veraltete Fits werden korrekt entwertet, aber es gibt weder eine Aufnahmeerneuerung bei Vordergrundrückkehr noch einen begrenzten Versuch bei ausbleibenden frischen Frames. Der vorhandene Resize-Neustart hilft bei einem Fokuswechsel ohne Größenänderung nicht. Das ist ein konkreter fehlender Wiederaufnahmeweg; es beweist keinen bestimmten WGC-Fehler im Spiel.

Korrektur v0.4.1 gemäß AC-08: Derselbe Capture-Worker erneuert Framepool und Sitzung bei Rückkehr des sichtbaren, nicht minimierten Ziel-HWND in den Vordergrund. Ohne frische Compositor-Bilder erfolgen im Vordergrund begrenzte Erneuerungen mit mindestens zwei Sekunden Abstand. Der Dispatcher entwertet letzte Aufnahme und Live-Generation vor dem Neustart. Referenz und Live-Aktivierung bleiben erhalten; nur ein neuer erfolgreicher Fit zeichnet wieder. Wiederholte alte Zeitstempel werden nicht künstlich verjüngt. Stop beendet den Worker und verhindert spätere Wiederaufnahme.

Der eigene Wiederaufnahmetest verwendet echte WGC-Aufnahme und den normalen Hauptfenster→Dialog→Live-Ablauf. Er gibt ausschließlich für sein eigenes Ziel eine kontrollierte Fokusquelle vor und unterdrückt Hintergrundzustellungen für jeweils 2,6 Sekunden. Drei Rückkehrfolgen müssen frische Frames und einen neuen Fit mit derselben Aufnahme-/Live-Instanz liefern; einmal wird zwischendurch unpassender Inhalt angezeigt. Dieser bleibt ausgeblendet, bis passende Referenzinhalte wieder erscheinen. Nach Stop im Hintergrund darf keine Wiederaufnahme erfolgen. Dies belegt die technische Integration, aber kein echtes OS-Alt-Tab oder die Rückkehr im Spiel. Die Nutzerprüfung der Korrektur bleibt offen.

Nachweise am finalen v0.4.1-Paket, 6. Oktober 2026:

- Release-Build ohne Warnungen/Fehler und 38 Kernlogiktests bestanden. Drei zusätzliche Tests prüfen Hintergrund/Rückkehr, auf zwei Sekunden begrenzte Wiederholung und Ablehnung alter/wiederholter/zukünftiger Compositor-Zeitstempel als Frischequelle.
- Veröffentlichung und `Start-Overlay.ps1 -ValidateOnly`: Dateiversion `0.4.1.0`, alle 410 Dateihashes bestätigt; Manifest-SHA256 `C53C330DD11184BD75DF3BE7231EBB03FCFC8E0A03BAAE5D59C2AAB3CF2086C4`. Alte Pakete und Nutzerinstanzen bleiben erhalten.
- `artifacts/resume-v041-final.json`, Zeitpunkt `2026-10-06T17:52:50Z`: fünf Prüfungen bestanden, Exitcode 0. Drei Rückkehrfolgen mit je genau einer Aufnahmeerneuerung und unveränderten Capture-/Live-Instanzen. Rückkehr bis erfolgreicher Zuordnung 0,512 / 1,569 / 0,420 Sekunden; der mittlere Versuch enthält zusätzlich unpassenden Inhalt und dessen Wiederherstellung. Diese Werte gelten für das eigene 1000×650-Testfenster, nicht für Aion oder Ultrawide. Unpassender Inhalt blieb ausgeblendet; Stop im Hintergrund unterband Wiederaufnahme.
- `artifacts/live-map-v041-final.json`: alle zehn vorhandenen Live-Prüfungen bestanden. `artifacts/capture-v041-final.json`: sichtbares eigenes borderloses Ziel, Aufnahmeinhalt, Resize, Stop/Neustart und geschlossenes Ziel bestanden.
- Sieben Schließregressionen `artifacts/shutdown-v041-<scenario>.json`: `idle`, `capture`, `busy`, `dialog`, `repeat`, `live`, `live-unchecked` bestanden, jeweils Prozess-Exitcode 0 und abgeschlossene Ressourcenbereinigung. Maximale gemessene Schließdauer 325 ms (`busy`).

AC-08 ist damit technisch belegt für die kontrollierte Fokusquelle und echte eigene Aufnahme. Im Spiel erneut v0.4.1 über den festen Starter starten, einmal abgleichen/Live aktivieren, per Alt-Tab nachschlagen und zurückkehren. Die erneute Einrichtung soll entfallen; die tatsächliche Zielclient-Wiederaufnahme bleibt bis zum Nutzerergebnis offen.
