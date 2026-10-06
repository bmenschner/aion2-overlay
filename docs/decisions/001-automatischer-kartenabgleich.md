# ADR-001: Automatischer Kartenabgleich vor Cube-Markern

Stand: 6. Oktober 2026. Status: Produktentscheidung getroffen; Bibliotheksauswahl technisch geprüft, echte Kartenabnahme offen.

## Anlass

Die manuelle Kalibrierung aus [SPEC-002](../specs/002-map-calibration.md) ist als Prototyp vorhanden. Der echte Versuch auf dem Altgard-Ausschnitt hat die Prüftoleranz nicht erfüllt. Der Nutzer erklärt, pixelgenaue Klicks seien für ihn kein geeigneter Ablauf, und fordert eine automatische Variante anhand der beiden Kartenbilder. Vergrößerung allein erfüllt diese Anforderung nicht.

## Entscheidung

Der geplante Standardablauf verwendet eine einmal gewählte lokale Referenz und eine aktuelle Fensteraufnahme. Gemeinsame Gelände-/Wegmerkmale bestimmen die Abbildung automatisch; unabhängige Qualitätsprüfungen entscheiden über die Gültigkeit. Kein vorgeschriebener Landmarkenklick, kein wiederholter Screenshot und keine Absenkung der Genauigkeitsanforderung.

Der automatische Abgleich wird vor die Cube-Darstellung gezogen. Zuerst ein geprüftes eingefrorenes Bildpaar, anschließend in einer eigenen Spezifikation laufende Ansichtserkennung und Overlay-Geometrie. Der bisherige manuelle Dialog kann als Entwicklungsdiagnose bestehen bleiben, ist aber kein Abnahmepfad für das neue Feature. Seine bisher offenen Kriterien bleiben offen.

Präzisierung vom 6. Oktober 2026: Der Nutzer möchte die ohnehin vorhandene Spielaufnahme für den Standardablauf verwenden. Ein zusätzlicher eigener Screenshot zur Genauigkeitskontrolle bleibt eine freiwillige Diagnosevariante. Auch die geplante Live-Zuordnung verlangt keinen zusätzlichen Kontrollscreenshot oder manuellen Genauigkeitsfreigabeklick. Automatische Qualitätsgrenzen gelten weiter; externe Genauigkeitsmessungen sind Entwicklungsnachweise und keine Bedienpflicht. Die bereits gewählte Kartenreferenz wird wiederverwendet.

## Technischer Vorschlag und Alternativen

SIFT-Merkmale und robuste Ähnlichkeitsschätzung mit RANSAC bilden den begrenzten ersten Versuch. Maskierung und unabhängige Kontrolle sollen UI-Treffer, ähnliche Landflächen und Überanpassung abweisen. OpenCvSharp4 und die Windows-Slim-Runtime sind auf `4.13.0.20260627` festgelegt; native OpenCV-4.13.0-Aufrufe im eigenständigen Windows-x64/.NET-10-Paket sind geprüft. ECC bleibt eine unimplementierte optionale Verfeinerung. Prüfungen und Grenzen stehen in [Validierung SPEC-003](../validation/003-auto-map-registration.md).

Mehr Handklicks oder nur größere Bilder behalten die vom Nutzer abgelehnte Bedienung. Starres Template-Matching setzt zu ähnliche Ansichten voraus. Ein flexibles perspektivisches Modell erhöht den Spielraum für falsche Fits. Die Recherche und noch offenen Annahmen stehen in [automatischer Kartenabgleich](../research/automatischer-kartenabgleich.md).

## Konsequenzen

- [SPEC-003](../specs/003-auto-map-registration.md) beschreibt Soll-Verhalten und Prüfungen; sie steht in Umsetzung, bis auch die offenen echten Karten- und Fehlerprüfungen nachgewiesen sind.
- Projektplan und Wissensindex führen die neue Reihenfolge. Bisherige Zeitschätzungen sind keine Zusage für diese geänderte Anforderung.
- Der Altgard-Ausschnitt erlaubt nur Aussagen im geprüften gemeinsamen Bereich; globale Kartenabdeckung wird daraus nicht abgeleitet.
- Eine gespeicherte Referenz vereinfacht spätere Starts. Gespeicherte Koeffizienten benötigen stets erneute Prüfung gegen eine frische Aufnahme.
- Bildregistrierung löst weder Cube-Datenbeschaffung noch Spielerposition oder begehbare Navigation.
- Die ursprüngliche Planungsaufgabe enthielt keine Implementierung. Die anschließende Umsetzung in v0.3.0 ergänzt Bildanalyse und den automatischen Standarddialog. Diagnoseaufnahmen erfassen ausschließlich eigene Testfenster; normale Spielfensteraufnahme erfolgt nach ausdrücklicher Auswahl und Start durch den Nutzer.
