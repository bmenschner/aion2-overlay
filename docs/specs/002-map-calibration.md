# SPEC-002: Manuelle Kalibrierung einer festen Kartenansicht

Status: in Umsetzung. Stand: 6. Oktober 2026.

## Ziel und Umfang

Schritt 2 des [Projektplans](../../PROJEKTPLAN.md): Eine lokal ausgewählte Referenzkarte auf ein eingefrorenes Aufnahmebild abbilden. Drei Landmarken bestimmen eine affine Abbildung; zwei unabhängige Landmarken prüfen sie. Die geprüfte Kalibrierung kann ausdrücklich als lokale JSON-Datei gespeichert werden.

Dieser Schritt zeigt Zuordnungen und vorhergesagte Prüfpositionen im Kalibrierungsdialog. Live-Cube-Marker, automatische Karten-/Zoom-/Pan-Erkennung, Spielerposition, externe Kartenbeschaffung und das automatische Laden gespeicherter Profile folgen separat. Eine lokale Referenz wird vom Nutzer bereitgestellt; Region oder Datennutzungsrechte werden nicht aus dem Bild abgeleitet.

## Nutzerablauf

1. Aufnahme starten, Gebietskarte öffnen und die Ansicht unverändert lassen.
2. Im Steuerfenster „Karte kalibrieren“ wählen. Der Dialog übernimmt das letzte Bild nur, wenn es jünger als zwei Sekunden ist. Das Bild bleibt im Dialog eingefroren und wird nicht auf die Festplatte geschrieben.
3. Eine lokale PNG-, JPEG- oder BMP-Referenzkarte öffnen. Karten-ID/Gebiet, Global-Spielbuild und eine Beschreibung der festen Ansicht angeben. Die Region ist Europa/Global und wird nicht automatisch verifiziert.
4. Für Landmarken 1–3 zuerst die Referenzkarte, dann dieselbe Stelle im Aufnahmebild anklicken. Punkte sollen weit auseinanderliegen und dürfen nicht nahezu auf einer Linie liegen.
5. Zwei weitere, von den ersten Punkten verschiedene Landmarken als Prüfpunkt 4 und 5 zuordnen. Die Vorhersage wird als Kreis, der tatsächliche Klick als Kreuz dargestellt; Abweichungen werden in Aufnahmepixeln angegeben.
6. Nur bei bestandener Prüfung und vollständigen Angaben ist „Kalibrierung speichern“ aktiv. Ein Dateidialog bestimmt den Speicherort. „Letztes Paar zurück“ und „Neu beginnen“ ermöglichen Korrekturen; eine neue Referenz löscht alte Zuordnungen.

## Verhalten und Grenzen

- Der Dialog verwendet stets denselben eingefrorenen Frame; laufende Vorschauänderungen verschieben keine gesetzten Punkte.
- Klicks außerhalb des dargestellten Bildes (einschließlich Letterbox-Rändern) werden ignoriert. Größenänderungen des Dialogs verändern keine Bildkoordinaten.
- Die Referenz nutzt normierte Bildkoordinaten: Ursprung links oben, x nach rechts, y nach unten, Bereich [0,1]. Das Ziel verwendet physische Pixel des vollständigen WGC-Aufnahmebilds, ebenfalls links oben. Dies ist ausdrücklich noch keine Clientbereich- oder Weltkoordinate.
- Drei Referenz-/Zielpaare bestimmen sechs affine Koeffizienten. Nicht endliche, doppelte und nahezu kollineare Punkte werden abgewiesen. Die normierte doppelte Dreiecksfläche muss auf beiden Bildern größer als 0,0001 sein.
- Prüfpunkte müssen auf der Referenz mindestens 0,01 normierte Bildeinheiten von jedem anderen Punkt entfernt liegen. Sie werden nicht zum Fit benutzt.
- Beide Prüffehler müssen höchstens `8 × Aufnahmehöhe / 1080` Pixel betragen. Das ist eine lokale Prototypprüfung an zwei Punkten, kein Nachweis des im Projektplan vorgesehenen 95. Perzentils an 20 Punkten.
- Bei fehlenden Angaben, ungültiger Geometrie, unvollständigen Paaren oder zu hohem Prüffehler kann kein freigegebenes Profil gespeichert werden. Bild-/Dateifehler erscheinen verständlich im Dialog; vorhandene Zuordnungen bleiben bei gescheitertem Bildladen erhalten.
- Referenzbilder sind auf 64 MiB Dateigröße, 8192 Pixel je Achse und 40 Millionen Pixel begrenzt. Die Anzeige normalisiert Bild-DPI auf 96, sodass Klickkoordinaten das Pixel-Seitenverhältnis verwenden.
- Gespeicherte Profile gelten ausschließlich für die vom Nutzer bezeichnete feste Ansicht. Sie werden in diesem Schritt nie automatisch auf das Live-Overlay angewendet. Nach Zoom, Pan, Karten-/UI-/Auflösungswechsel ist eine neue Kalibrierung erforderlich. Eine spätere Live-Nutzung benötigt eine eigene Prüfung der Aufnahme-zu-Client-Abbildung und der Ansichtsgültigkeit.
- Die Bestätigung des Gebietes/Builds ist eine Nutzerangabe, kein automatischer Global-Kompatibilitätsnachweis.

## Datenmodell

JSON-Schema-Version 1: Region `EuropeGlobal`, Karten-ID, Spielbuild, Ansichtsnotiz, SHA256 und Pixelgröße des Referenzbildes, Pixelgröße/Zeitpunkt des Aufnahmebildes, drei Fit-Paare, zwei Prüfpaare, sechs Koeffizienten, zwei Prüffehler, Toleranz und Speicherzeitpunkt. Weder Aufnahmebilder noch Fenstertitel, Prozess-IDs oder absolute Referenzpfade werden gespeichert. Es werden keine externen Daten heruntergeladen.

## Abnahmekriterien

| ID | Kriterium | Nachweis |
| --- | --- | --- |
| AC-01 | Bekannte affine Abbildungen einschließlich Rotation/Scherung werden rekonstruiert; ungültige und nahezu kollineare Paare abgewiesen | Kernlogiktests |
| AC-02 | Letterbox-Ränder werden ignoriert; Bildpunkte bleiben bei Größenänderung unverändert; verschiedene Bildverhältnisse funktionieren | Geometrietests und Dialogtest |
| AC-03 | Zwei unabhängige Prüfpunkte bestimmen die Freigabe, mit skalierter Pixeltoleranz; ein fehlerhafter Punkt blockiert das Speichern | Kernlogik- und Dialogtest |
| AC-04 | Datei enthält vollständige Metadaten, Referenzhash, Paare und Prüfergebnis; keine Bilder oder Fensterdaten; ungültige Profile werden beim Erzeugen abgewiesen | Profiltest und lokale Testdatei |
| AC-05 | Dialog startet nur aus einer frischen Aufnahme; Referenzwechsel, Rückgängig und Neustart löschen beziehungsweise korrigieren die vorgesehenen Punkte | Implementierungsprüfung und Dialogtest |
| AC-06 | Lokales Windows-x64-Paket baut ohne Fehler; bestehende UI-Farben bleiben lesbar | Build und Live-UI-Prüfung |
| AC-07 | Drei Landmarken und zwei zusätzliche Prüfpunkte auf einer echten EU/Global-Karte ergeben eine bestandene Prüfung; Karte, Build, Auflösung und Ansicht sind dokumentiert | Nutzerprüfung im Zielclient, offen |

## Abhängigkeiten und offene Nachweise

Grundlage: [SPEC-001](001-overlay-capture.md), [Architektur](../architecture.md). Quelle für das Soll-Verhalten ist der bestehende Projektplan, keine externe Koordinaten-API. Die erste echte Referenzkarte, ihre Rechte, die Karten-ID und der konkrete Global-Spielbuild sind noch nicht belegt. Prüfergebnisse stehen in [Validierung SPEC-002](../validation/002-map-calibration.md).
