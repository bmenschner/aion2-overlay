# Gestaltungsreferenz für das Overlay

Stand: 6. Oktober 2026. Quelle: vom Nutzer im Chat bereitgestellter Screenshot eines anderen Aion-Overlays, ausdrücklich als schön gestaltetes Beispiel bezeichnet. Produktname und Version sind unbekannt. Diese Datei hält die visuelle Richtung fest; ein Redesign ist noch nicht implementiert.

## Beobachtungen aus dem Screenshot

- Kompaktes dunkles Panel mit abgerundeten Ecken und einem dünnen, zurückhaltenden Rand.
- Kleine Kopfzeile mit Titel, Status und gleichmäßig angeordneten Symbolaktionen.
- Heller Haupttext und gedämpfte Nebeninformationen; wichtige Werte durch Gewicht und Ausrichtung hervorgehoben.
- Dicht angeordnete Zeilen mit klaren Abständen, Icons und farbigen Hintergrundflächen.
- Gelb als gezielter Hinweisakzent, Türkis und weitere Farben für unterscheidbare Zustände.
- Statusleiste und einklappbar wirkende Hinweise integrieren Informationen auf wenig Raum. Das tatsächliche Bedienverhalten lässt sich aus dem Bild nicht prüfen.

## Übertragung auf unser Projekt

Als Entwurfsrichtung bietet sich ein kompaktes Karten-/Cube-Panel mit dunklen Flächen, feinen Rändern und klarer Typografie an. Eine Kopfzeile kann Gebiet und Zuordnungsstatus aufnehmen; spätere Cube-Zeilen können Symbol, Fundstellenname und Suchstatus zeigen. Aufnahme-/Abgleichfunktionen und die große Vorschau benötigen weiterhin Platz in der Einrichtung. Der genaue Aufbau wird in einer eigenen UI-Spezifikation konkretisiert, sobald der Gestaltungsauftrag umgesetzt wird.

Vorhandene ausdrückliche Lesbarkeitsanforderungen bleiben Grundlage: weiße Schrift auf aktiven gelben Buttons, schwarze Schrift auf weißen Buttons und im hellen Dropdown. Status muss auch durch Text oder Symbol erkennbar sein. Die Referenz liefert keine Farbcodes, Nutzungsnachweise oder neue Funktionen; abgebildete Netzwerk-/Kampfdaten gehören nicht zum daraus abgeleiteten Projektumfang.

Offen für einen UI-Entwurf: Breite und Informationsumfang des kompakten Panels, Verhältnis zwischen Einrichtung und Ingame-Anzeige, konkrete Farben sowie eigene verständliche Symbolaktionen. Aktueller Funktionsstand: [SPEC-005](../specs/005-live-kartenzuordnung.md) und [Architektur](../architecture.md).
