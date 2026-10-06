# ADR-001: Automatischer Kartenabgleich vor Cube-Markern

Stand: 6. Oktober 2026. Status: Produktentscheidung getroffen; technische Auswahl vorläufig bis zum Machbarkeitsnachweis.

## Anlass

Die manuelle Kalibrierung aus [SPEC-002](../specs/002-map-calibration.md) ist als Prototyp vorhanden. Der echte Versuch auf dem Altgard-Ausschnitt hat die Prüftoleranz nicht erfüllt. Der Nutzer erklärt, pixelgenaue Klicks seien für ihn kein geeigneter Ablauf, und fordert eine automatische Variante anhand der beiden Kartenbilder. Vergrößerung allein erfüllt diese Anforderung nicht.

## Entscheidung

Der geplante Standardablauf verwendet eine einmal gewählte lokale Referenz und eine aktuelle Fensteraufnahme. Gemeinsame Gelände-/Wegmerkmale bestimmen die Abbildung automatisch; unabhängige Qualitätsprüfungen entscheiden über die Gültigkeit. Kein vorgeschriebener Landmarkenklick, kein wiederholter Screenshot und keine Absenkung der Genauigkeitsanforderung.

Der automatische Abgleich wird vor die Cube-Darstellung gezogen. Zuerst ein geprüftes eingefrorenes Bildpaar, anschließend in einer eigenen Spezifikation laufende Ansichtserkennung und Overlay-Geometrie. Der bisherige manuelle Dialog kann als Entwicklungsdiagnose bestehen bleiben, ist aber kein Abnahmepfad für das neue Feature. Seine bisher offenen Kriterien bleiben offen.

## Technischer Vorschlag und Alternativen

SIFT-Merkmale, robuste Ähnlichkeitsschätzung mit RANSAC und optionaler ECC-Feinabgleich bilden den begrenzten ersten Versuch. Maskierung und unabhängige Kontrolle sollen UI-Treffer, ähnliche Landflächen und Überanpassung abweisen. OpenCV/OpenCvSharp ist eine zu prüfende lokale Bibliotheksanbindung; genaue Version und Runtime werden erst nach Windows-x64/.NET-10-Test festgelegt.

Mehr Handklicks oder nur größere Bilder behalten die vom Nutzer abgelehnte Bedienung. Starres Template-Matching setzt zu ähnliche Ansichten voraus. Ein flexibles perspektivisches Modell erhöht den Spielraum für falsche Fits. Die Recherche und noch offenen Annahmen stehen in [automatischer Kartenabgleich](../research/automatischer-kartenabgleich.md).

## Konsequenzen

- [SPEC-003](../specs/003-auto-map-registration.md) beschreibt Soll-Verhalten und Prüfungen; sie bleibt Entwurf, bis die Machbarkeit geklärt ist.
- Projektplan und Wissensindex führen die neue Reihenfolge. Bisherige Zeitschätzungen sind keine Zusage für diese geänderte Anforderung.
- Der Altgard-Ausschnitt erlaubt nur Aussagen im geprüften gemeinsamen Bereich; globale Kartenabdeckung wird daraus nicht abgeleitet.
- Eine gespeicherte Referenz vereinfacht spätere Starts. Gespeicherte Koeffizienten benötigen stets erneute Prüfung gegen eine frische Aufnahme.
- Bildregistrierung löst weder Cube-Datenbeschaffung noch Spielerposition oder begehbare Navigation.
- Keine Implementierung, neue Abhängigkeit oder automatische Spielfensteraufnahme wurde durch diese Planungsaufgabe durchgeführt.
