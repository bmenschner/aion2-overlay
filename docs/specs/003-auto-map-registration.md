# SPEC-003: Automatischer Abgleich zweier Kartenbilder

Status: in Umsetzung. Stand: 6. Oktober 2026. Anlass: Der Nutzer erwartet im Kalibrierungsdialog einen automatischen Abgleich. Umsetzung von Matcher und automatischem Standarddialog begonnen; echte Kartenabnahme bleibt offen. Die bestehenden Abnahmekriterien werden nicht abgeschwächt.

Implementierungsentscheidung: OpenCvSharp4 und Windows-Slim-Runtime `4.13.0.20260627` werden für den begrenzten Versuch fest gepinnt. Die OpenCV-4.13-API entspricht der bisherigen Verfahrensrecherche; ein Major-5-Wechsel ist dafür nicht erforderlich. Eigenes Imaging-Modul, reine Qualitätsprüfung im Kern und separater automatischer Dialog. ECC bleibt optional; eine ohne ECC bestandene unabhängige Prüfung wird nicht als ECC-Nachweis bezeichnet. Unbekannter Build darf lokal als ungeprüft gespeichert werden, bestätigt jedoch kein Cube-Datenpaket.

Änderung der Bereitstellungsreihenfolge vom 6. Oktober 2026: Für die vom Nutzer erwartete automatische Bedienung wird Paket B bereits als begrenzter v0.3.0-Prototyp bereitgestellt, nachdem die native Anbindung und synthetischen Prüfungen aus A bestehen. Der Originalpaar-Nachweis aus A fehlt weiterhin und wird nicht durch die Dialogbereitstellung ersetzt. Die vollständige Abnahme bleibt an unveränderte Kriterien gebunden.

## Ziel und Entscheidung

Eine lokale Referenz und eine frische Aufnahme der geöffneten Ingame-Karte werden automatisch aufeinander abgebildet. Der Nutzer muss keine Landmarken anklicken. Diese Funktion wird vor den Cube-Markern umgesetzt und ersetzt die manuelle Kalibrierung als vorgesehenen Einrichtungsablauf. Grundlage: [Entscheidung ADR-001](../decisions/001-automatischer-kartenabgleich.md), [Recherche](../research/automatischer-kartenabgleich.md) und [bisherige Nutzerprüfung](../validation/002-map-calibration.md).

Erste Zielreferenz ist der vom Nutzer bereitgestellte **Altgard-Ausschnitt**, lokal 5120 × 1440 Pixel. Er ist keine vollständige Gebietskarte. Das zweite Originalbild wird aus der vorhandenen Fensteraufnahme übernommen; das verkleinerte Bild im Dialog-Screenshot genügt nicht als Prüfeingabe.

## Nutzerablauf

1. Einmal eine lokale Referenz wählen; die vorhandene Altgard-Datei kann weiterverwendet werden.
2. Aufnahme starten und im Spiel die Karte öffnen.
3. „Automatisch abgleichen“ wählen. Die Anwendung übernimmt einen frischen Frame und vergleicht die beiden Bilder im Hintergrund.
4. Bei Erfolg erscheint „Kartenbilder passen zusammen“ mit einer überlagerten Vorschau des gemeinsamen Bereichs. Bei Misserfolg erscheint eine konkrete Meldung, etwa „Zu wenig gemeinsamer Kartenbereich – verschiebe die Karte zum Referenzausschnitt“.
5. Eine bestandene Zuordnung kann ausdrücklich lokal gespeichert werden. Beim nächsten Start wird die Referenz wieder angeboten und mit einem neuen Bild abgeglichen. Eine gespeicherte Transformation gilt niemals ungeprüft für die aktuelle Ansicht.

Es gibt keine Pflicht zum Setzen von Punkten, zum Zuschneiden durch den Nutzer oder zum wiederholten Anfertigen von Screenshots. Eine visuelle Kontrolle der Vorschau verlangt keine pixelgenauen Klicks. Das Programm behauptet keine Genauigkeit allein aufgrund einer Nutzerbestätigung.

## Umfang und Grenzen

- Zuerst zwei eingefrorene Ingame-Kartenbilder desselben Gebietes und Kartenstils mit gemeinsam sichtbarem Gelände; unterschiedlicher Ausschnitt und Zoom sind vorgesehen.
- Das Ergebnis beschreibt ausschließlich die Aufnahme, ihren Zeitpunkt und den geprüften gemeinsamen Bereich. Es aktiviert noch keine Live-Marker.
- Zurückgestellt: kontinuierliches Nachführen, Erkennen geöffneter/geschlossener Karte im Spielbetrieb, Aufnahme-zu-Client-Umrechnung für Live-Marker, Cube-Datensatz, Spielerposition und Navigation.
- Questlog-Grafiken können anders aufgebaut sein. Ihre Zuordnung zu Ingame-Bildern benötigt einen separaten Nachweis; API, Export und Nutzungsrechte werden nicht vorausgesetzt.
- Kartenname und Region/Build sind Profilangaben. Bildähnlichkeit bestätigt keinen Global-Spielbuild. Unbekannte Angaben bleiben als ungeprüft gekennzeichnet und sperren später die Freigabe eines Cube-Datenpakets.
- Ein Fehlschlag führt zu einer verständlichen Anleitung für einen anderen sichtbaren Ausschnitt. Manuelle Punktwahl wird kein verpflichtender Rückfallweg.

## Geplantes Verfahren

1. **Kartenfläche isolieren:** In beiden Originalen unabhängig Menüs, Randbedienelemente und Einblendungen maskieren. Gemeinsame UI darf keinen Treffer erzeugen. Merkmale werden auf Gelände, Küsten und Wegen gesucht; Spielerpfeil und dynamische Symbole werden ausgeschlossen. Masken für das unterstützte Layout werden im Prototyp entwickelt und geprüft, nicht als bereits funktionierend angenommen.
2. **Arbeitsbilder erstellen:** Seitenverhältnis erhalten, zunächst längste Seite 1600 Pixel; Graustufen und bei Bedarf begrenzte Kontrastnormalisierung. Crop-Offsets und Skalierung protokollieren. Die Referenzmerkmale werden nach Bildhash und Parametern zwischengespeichert.
3. **Gemeinsame Merkmale finden:** SIFT-Deskriptoren als erster Kandidat; L2-Nachbarsuche, Vergleich des besten mit dem zweitbesten Treffer und gegenseitige Zuordnung. Mehrere tausend Merkmale ersetzen drei Handklicks. Die genaue Parametrisierung wird erst mit dem Prüfdatensatz festgelegt.
4. **Robuste Abbildung schätzen:** RANSAC mit einer Ähnlichkeitstransformation aus Verschiebung, einheitlichem Maßstab und Rotation (`estimateAffinePartial2D`). Scherung und perspektivische Verformung sind im ersten Modell ausgeschlossen. Ein komplexeres Modell wird erst nach einem belegten Bedarf spezifiziert.
5. **Unabhängig prüfen:** Treffer müssen über den gemeinsamen Kartenbereich verteilt sein. Räumlich zurückgehaltene Treffer und zusätzliche lokale Bildausschnitte werden erst nach dem Fit ausgewertet. Vergleichbar gute, geometrisch verschiedene Zuordnungen führen zum Ergebnis „Uneindeutig“.
6. **Feinabgleich untersuchen:** Nur bei bereits belastbarer Startabbildung optional ECC auf höher aufgelösten, maskierten Geländeausschnitten prüfen. Die Verfeinerung darf die unabhängigen Prüfwerte nicht verschlechtern; bei Nichtkonvergenz wird sie verworfen. Die Startabbildung bleibt nur verwendbar, wenn sie alle unveränderten Qualitätsgrenzen erfüllt.
7. **Ergebnis anzeigen:** Überlagerung mit prüfbaren Küsten-/Wegkanten, gemeinsamem Bereich, Qualität und Zeitpunkt. Außerhalb des geprüften Bereichs werden keine Positionen als gültig ausgegeben.

Startwerte für den Versuch, noch keine bewiesenen Zuverlässigkeitsgrenzen: mindestens 25 RANSAC-Inlier bei mindestens 40 vorgefilterten Paaren, Inlier-Anteil mindestens 55 %, Unterstützung in mindestens vier Zellen eines 3×3-Rasters über dem Überlappungsbereich und Inlier-Hülle mindestens 25 % dieser Fläche. Ein konkurrierender, verschiedener Fit mit mindestens 80 % der Unterstützung löst eine Uneindeutigkeitsprüfung aus. Diese Parameter werden vor der abschließenden Prüfung festgeschrieben; Änderungen und Ergebnisse werden dokumentiert. Fit-Restfehler allein sind kein Genauigkeitsnachweis.

## Koordinatenräume und Schnittstellen

- Referenz und Aufnahme besitzen jeweils ursprüngliche physische Bildpixel mit Ursprung links oben; x nach rechts, y nach unten. Windows-DPI und Dialog-Vergrößerung sind keine Eingaben für den Fit.
- Für beide Bilder werden Original-zu-Arbeitsbild-Abbildungen `P_ref` und `P_frame` gespeichert, einschließlich Crop und Resize. Bei Fit `M_work` ergibt sich `T_original = inverse(P_frame) × M_work × P_ref`.
- Eine Ähnlichkeit wird im Pixelraum bestimmt. Die unterschiedliche Normierung von x und y eines breiten Bildes darf das Modell nicht verformen. Für bestehende normierte Referenzkoordinaten wird erst danach die Referenzbreite/-höhe vorgeschaltet.
- Ergebnis: Transformation Referenzoriginal → Aufnahmeoriginal, gültiger Überlappungs-/Unterstützungsbereich, Aufnahmezeitpunkt, Sitzung/Frame-ID und Qualitätsdiagnostik. Sitzung/Frame-ID bleiben intern; Fenster- und Prozessdaten werden nicht ins Profil exportiert.
- Geplant: `IMapRegistrationService` für den Bildabgleich; unabhängiger `RegistrationQualityGate` und `RegistrationResult` im Kern; OpenCV-Anbindung in einem eigenen Windows-x64-Modul. Das existierende `MapCalibration` mit drei Fit- und zwei Prüfpaarklicks wird hierfür nicht umfunktioniert.
- Eine begrenzte Hintergrundaufgabe läuft außerhalb des WPF-Dispatchers. Referenzwechsel, Abbruch oder neue Aufnahmesitzung entwerten alte Ergebnisse; keine unbegrenzte Warteschlange. Native Ressourcen werden deterministisch freigegeben.

## Zustand und Fehlerbehandlung

Zustände: keine Referenz → bereit → Abgleich läuft → zugeordnet / zu wenig Übereinstimmung / uneindeutig / Fehler. Ein verändertes Referenzbild entwertet Cache und gespeicherte Zuordnung. Beschädigte Datei, fehlende native Bibliothek oder abgebrochene Aufnahme erhalten verständliche Meldungen; keine teilweise gültige Transformation wird ausgegeben.

Ein Frame wird nur aus der ausdrücklich gewählten Aufnahmesitzung übernommen und darf beim Start des Abgleichs höchstens zwei Sekunden alt sein. Die Verarbeitung eines eingefrorenen Bilds darf länger dauern; ihre Vorschau trägt den ursprünglichen Zeitpunkt und wird niemals als aktuelle Live-Ansicht bezeichnet. Für spätere Live-Nutzung sind tatsächlicher Aufnahmezeitpunkt, Ansichtsänderung und Kartenmodus separat zu prüfen; die bisherige Frame-Empfangszeit allein genügt dafür nicht.

## Profil und Datenhaltung

Neues Profil mit Schema-Version 2 und `method: automatic`: Karten-ID, Region, Build mit Prüfstatus, Referenzhash/-größe, Aufnahmegröße/-zeitpunkt, Modellart und Koeffizienten, Masken-/Algorithmusversion, Parameter, Prüfergebnisse, gültiger Bereich und Speicherzeitpunkt. Referenzpfad wird bei Bedarf in einer getrennten lokalen Einstellung aufbewahrt; keine absoluten Pfade, Fensterdaten oder Bilder im exportierten Profil. Alte manuelle Schema-1-Profile gelten nicht automatisch als bestätigte automatische Profile.

Normale Verarbeitung bleibt lokal im Speicher. Referenz wird nicht veröffentlicht; Aufnahmebilder werden nur nach ausdrücklichem Diagnoseexport gespeichert. Kein neuer Dienst, Konto oder externer Bildabgleich. Das Wiederladen enthält Hashprüfung und neuen Abgleich; fehlende Referenz führt zur Dateiauswahl, nicht zum Verwenden alter Koeffizienten.

## Abnahmekriterien

Die Kriterien beschreiben die vollständige Abnahme. Der Prototyp hat technische Teilprüfungen bestanden; vollständige Kriterien und echte Kartenabnahme bleiben teilweise offen. Messwerte und Grenzen stehen in [Validierung SPEC-003](../validation/003-auto-map-registration.md). Keine geplante Prüfung wird allein durch die Implementierung als bestanden gewertet.

Entwicklungsbilder und abschließender Prüfsatz werden getrennt. Parameter und Masken werden vor Auswertung des zurückgehaltenen Satzes fixiert; Änderungen danach benötigen einen neuen unabhängigen Prüfsatz. Synthetische Varianten desselben Ausgangsbilds prüfen Geometrie und Störungen, belegen aber keine Übertragbarkeit auf weitere Gebiete.

| ID | Kriterium | Geplanter Nachweis |
| --- | --- | --- |
| AC-01 | Von Referenzwahl bis Ergebnis ist kein Landmarkenklick, manuelles Zuschneiden oder zusätzlicher Screenshot nötig | Echter WPF-Ablauf mit vorhandener Referenz und frischem WGC-Frame |
| AC-02 | UI, Spielerpfeil und dynamische Einblendungen liefern keine alleinige gültige Kartenzuordnung | Negative Bilder mit gleicher UI und anderem Gelände; dokumentierte Maskenprüfung |
| AC-03 | Mindestens 30 synthetische Varianten mit bekannter Wahrheit, Zoom 0,5–2×, Verschiebung, Zuschnitt, mindestens 35 % gemeinsamer maskierter Kartenfläche, Auflösungen 1920×1080 und 5120×1440: mindestens 90 % werden angenommen; kein angenommener Fall überschreitet die Fehlergrenze | Reproduzierbarer Bildsatz; mindestens 20 verteilte, nicht zum Fit verwendete Kontrollpositionen je Fall; 95. Perzentil ≤ `8 × Aufnahmehöhe / 1080` Originalpixel |
| AC-04 | Mindestens 20 negative Fälle werden sämtlich abgewiesen: fehlende Überlappung, falsches Gelände bei gleicher UI, wenige/gehäufte Merkmale, gleich gute wiederholte Muster und geschlossene Karte | Vorab festgelegter negativer Bildsatz mit erwarteten Gründen; keine gültige Transformation |
| AC-05 | Echtes Altgard-Originalpaar plus mindestens fünf frische Varianten unterschiedlicher Ansicht werden ohne Punktwahl abgeglichen; Qualität und unterstützter Bereich nachvollziehbar | Zurückgehaltene Geländeausschnitte und mindestens 20 unabhängige Prüfpositionen je Paar; dieselbe Pixeltoleranz wie AC-03. Entwicklerannotation mit Unsicherheit dokumentieren; bei unzureichender Messgenauigkeit kein Bestehensnachweis |
| AC-06 | Original-/Arbeits-/normierte Koordinaten werden korrekt umgerechnet, einschließlich asymmetrischem Crop und Ultrawide; keine gültige Ausgabe außerhalb der Unterstützung | Geometrietests mit bekannter Abbildung und Randfällen |
| AC-07 | Speicherung/Wiederladen prüft Hash und Schema; neuer Abgleich ist erforderlich; fehlende Referenz oder alte manuelle Profile erzeugen keine gültige aktuelle Zuordnung | Profil- und Zustandsprüfungen |
| AC-08 | Selbstständiges Windows-x64-Paket mit .NET 10 führt den echten Matcher aus; bei 5120×1440 beträgt Abgleichdauer im 95. Perzentil höchstens fünf Sekunden auf dokumentiertem Gerät, UI bleibt bedienbar und Abbruch entwertet jedes spätere Ergebnis | Native-Pakettest, Messung über mindestens 20 Läufe und interaktiver Abbruch-/Referenzwechseltest |
| AC-09 | Bilder bleiben ohne Diagnoseexport im Speicher; Vorschau nennt Aufnahmezeitpunkt und eingefrorenen Zustand; Aufnahme-/Referenzwechsel entwerten ältere Ergebnisse | Dateizugriffs- und Sitzungsprüfung, UI-Prüfung |

## Umsetzung in drei überprüfbaren Paketen

**A – Machbarkeit mit Originalbildern.** Native OpenCV/OpenCvSharp-Anbindung auf .NET 10/x64 prüfen und genaue Version pinnen. Einen Offline-Matcher mit Masken, robustem Fit und Diagnosebericht bauen. Vorhandene Altgard-Referenz verwenden; zweite unveränderte Aufnahme über die vorhandene WGC-Funktion gewinnen. Positiv-/Negativsatz, unabhängige Kontrolle und Laufzeiten prüfen. Ergebnis: belastbare Zuordnung oder dokumentierter Grund, warum das Verfahren bei diesen Bildern nicht trägt. Die verkleinerten Dialogbilder werden nicht als Originale rekonstruiert.

**B – Einfacher Dialog.** Bei erfolgreichem A den Button „Automatisch abgleichen“, überlagerte Vorschau, Fehlertexte, Abbruch, Cache und Schema-2-Speicherung/Wiederladen integrieren. Alle AC-01 bis AC-09 nachweisen; offene Kriterien verhindern den Status „umgesetzt“. B ersetzt den vorgesehenen manuellen Einrichtungsablauf, ohne das bestehende Profilformat stillschweigend umzudeuten.

**C – Anschließende eigene Spezifikation für Live-Marker.** Automatisch bei Öffnen/Zoom/Verschieben neu abgleichen, ungültige/veraltete Ergebnisse ausblenden, Aufnahme-zu-Client-Geometrie einschließlich DPI nachweisen und erst dann geprüfte Cube-Spots zeichnen. Wiederholrate und Live-Latenz werden dort gemessen. Dieses Paket gehört nicht zur Abnahme von SPEC-003 und ist Voraussetzung für tatsächlich folgende Ingame-Marker.

Die ursprüngliche Reihenfolge A→B wurde für die oben dokumentierte Prototypbereitstellung angepasst; die offene echte Kartenprüfung aus A bleibt erforderlich. Eine Aufwandsschätzung wird nach A anhand gemessener Bildqualität und Laufzeiten aktualisiert. Eine erfolgreiche Bildregistrierung findet keine Cubes und liefert keine Spieler- oder Wegdaten.
