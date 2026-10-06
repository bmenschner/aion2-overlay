# Validierung SPEC-003: automatischer Kartenabgleich

Stand: 6. Oktober 2026. Bezug: [SPEC-003](../specs/003-auto-map-registration.md). Prototyp v0.3.0; Status weiterhin in Umsetzung, keine vollständige Ingame-Abnahme.

## Umgebung und Umfang

Windows x64, .NET SDK 10.0.401, Intel Core i7-13700KF mit 24 logischen Prozessoren. OpenCvSharp4 und Windows-Slim-Runtime fest auf `4.13.0.20260627`; native Laufzeit meldet OpenCV `4.13.0`. Das eigenständige Windows-Paket führt den tatsächlichen SIFT-/RANSAC-Matcher aus; keine simulierten Abgleichergebnisse.

Quelle der Positiv-/Negativbilder: reproduzierbar erzeugtes synthetisches Gelände mit festem Seed, keine Aion-Karte. Separat das lokal vorhandene Altgard-Original ausschließlich gegen sich selbst geprüft. Keine unabhängige aktuelle Aufnahme des Spiels wurde durch die Diagnostik angefertigt. Referenzbild und Testergebnisse werden nicht in Git veröffentlicht.

## Ergebnisse

| Kriterium | Ergebnis und Grenze |
| --- | --- |
| AC-01 | Technischer Ablauf bestanden: normales Hauptfenster nimmt ein eigenes synthetisches Kartenfenster mit WGC auf; „Karte abgleichen“ öffnet den automatischen Dialog, Referenzwahl/Abgleich ohne Punktklick ergibt eine speicherbare Zuordnung. Echte Nutzerprüfung offen |
| AC-02 | Teilweise: Layout-/Farbmasken im echten Matcher; falsches Gelände bei gleichem synthetischen UI-Rand abgewiesen. Ausschluss aller tatsächlichen Spieler-/UI-Symbole im Zielclient offen |
| AC-03 | 30/30 synthetische bekannte Abbildungen angenommen und innerhalb der Pixeltoleranz: Zoom 0,5/0,7/1/1,3/2, Verschiebung, Zuschnitt, 1920×1080 und 5120×1440. Mindestens 20 unabhängige geometrische Kontrollpositionen pro Fall; größtes gemessenes 95. Perzentil 1,23 Originalpixel. Die Überlappungsquote wurde nicht separat quantifiziert; insbesondere starke Vergrößerung prüft auch kleinere Ausschnitte. Vollständiger Nachweis der geforderten Überlappungs-/Prüfsatzbedingungen bleibt offen |
| AC-04 | 20/20 Negativfälle abgewiesen: zehn unabhängig erzeugte Geländeansichten bei gleichem UI-Rand und zehn leere graue Ansichten. Zusätzliche vorgeschriebene Cluster-, Wiederholungs-/Mehrdeutigkeits-, Nichtüberlappungs- und echte Kartenmodusfälle stehen aus; kein vollständiges Bestehen behauptet |
| AC-05 | Offen: Kein unabhängiges Altgard-Originalpaar plus fünf echte Varianten vorhanden. Identitätsprobe des vorhandenen Originalbildes bestanden, aber kein Nachweis für einen anderen Ausschnitt/Zoom im Spiel |
| AC-06 | Kernlogiktests bestehen: asymmetrische Crop-Offsets, gerundete Resize-Dimensionen/Pixelzentren, ursprüngliche Pixelkoordinaten, Supportpolygon und Verweigerung außerhalb des geprüften Bereichs. Ultrawide-Matcher zusätzlich mit bekannter Wahrheit geprüft |
| AC-07 | Schema-2-Erzeugung/JSON-Roundtrip, Hash-/Schema-Prüfung im Dialog und Sperre nach Wiederladen bestanden. Altes Schema-1-Profil abgewiesen. Geladene Koeffizienten werden nie als aktueller Fit übernommen. Dateidialoge/Referenzwiederherstellung nach echtem App-Neustart noch nicht interaktiv abgenommen; numerische Verfahrensparameter stehen derzeit in Code/Dokumentation und noch nicht vollständig im Profil |
| AC-08 | Eigenständiges x64-Paket, tatsächliche native Funktionen, Abbruch und Referenzwechsel bestanden. 20 Identitätsabgleiche bei 5120×1440 einschließlich erster/wiederholter Referenzberechnung unter fünf Sekunden im 95. Perzentil; gemessenen Wert siehe lokaler Bericht. Keine allgemeine Leistungsgarantie für andere PCs oder reale Karten |
| AC-09 | Implementierungsprüfung: Verarbeitung im Speicher, Speichern nur ausdrücklich über Dateidialog; Zeitpunkt/eingefrorener Zustand sichtbar; Referenzwechsel/Abbruch entwerten laufende Ergebnisse. Neuer Klick holt in der normalen Aufnahme eine höchstens zwei Sekunden alte Bitmap, die anschließend eingefroren bleibt. Vollständige Dateizugriffs-/Sitzungsabnahme im Zielclient offen |

Release-Build/Publish ohne Warnungen oder Fehler; **30 Kernlogiktests bestanden**. Das fertige Paket besteht außerdem die fünf Schließregressionen (`idle`, `capture`, `busy`, `dialog`, `repeat`) und die vorhandene Farbenprüfung. Der Dialog-Schließtest verwendet jetzt den automatischen Dialog. Normale Nutzerinstanzen wurden weder gestartet noch geschlossen.

## Tatsächliche Prüfungen und Artefakte

- `artifacts/registration-v030-complete.json`: abschließender Paketlauf, `passed: true`, Exitcode 0, 54 Prüfeinträge einschließlich normalem WGC→Hauptfenster→automatischem Dialog. Die unabhängigen Prüftreffer dieses WGC-Falls ergeben ungefähr 0,58 Pixel im 95. Perzentil; 1206/1217 Inlier, neun räumliche Zellen, 74 zusätzliche Geländeprüfungen.
- 30 bekannte Warps: Kontrollpositionen werden nicht zum Fit oder zur Auswahl der SIFT-Paare benutzt; ursprüngliche Pixelwahrheit wird mit der bekannten erzeugten Transformation verglichen. Die Bilder stammen vom selben synthetischen Ausgangsbild; Übertragbarkeit auf weitere Karten ist damit nicht belegt.
- Altgard-Identitätsprobe: 1218 passende Merkmale, 488 räumlich zurückgehaltene Prüftreffer, 38 unabhängige Intensitätsausschnitte. Dies beweist nur, dass das originale Bild in dieser Runtime analysierbar ist.
- `artifacts/registration-v030-complete.live.png`: echte WGC-Aufnahme ausschließlich des eigenen Testdialogs, visuell geprüft. „Automatisch abgleichen“, Überlagerung, grüne Supportgrenze, Fehlertoleranz und „Zuordnung speichern“ sind sichtbar und lesbar. Karte ausdrücklich synthetisch gekennzeichnet.
- `artifacts/shutdown-v030-*.json` und `artifacts/ui-smoke-test.json`: bestehende Schließ-/Farbfunktionen bestanden, jeweils Prozess-Exitcode 0. Keine Aufnahme des Spiels.

Die erste kombinierte WGC-Prüfung verwendete einen anfänglich leeren Compositor-Frame und wurde vom Matcher korrekt ohne Zuordnung abgewiesen. Die abschließende Diagnostik wartet deshalb auf nachweislich gezeichnetes Gelände im eigenen Testfenster. Eine erste UI-Aufnahme zeigte noch den vorherigen Darstellungsstand; Aufnahme nach Dispatcher-Idle und Compositor-Verarbeitung wurde visuell nachgeprüft.

## Parameter und Implementierungsgrenzen

Arbeitsbild maximal 1600 Pixel an der längsten Seite; SIFT maximal 3500 Merkmale; L2-Zweiernachbarn mit Verhältnis 0,72 und gegenseitiger Zuordnung. RANSAC-Ähnlichkeit mit 2 Arbeitsbildpixeln Schwelle, 2000 Iterationen, Konfidenz 0,99 und zehn Verfeinerungsschritten. Der ursprüngliche Maßstab muss zwischen 0,2 und 5 liegen, Rotation maximal 15 Grad. Qualitätsgrenzen stehen unverändert im Gate und in SPEC-003; die Diagnostik hat sie nicht abgesenkt.

Die Maske unterstützt zunächst Screenshots des erwarteten Ingame-Kartenlayouts: zentraler Kartenbereich x=13–96 %, y=10–92 %, farbiges Gelände; sehr helle/farbige Symbole werden erweitert ausgeschlossen. Dies ist eine zu prüfende Heuristik für diesen Kartenstil. Graue externe Karten, andere Layouts und verdeckte/schwach texturierte Karten sind nicht freigegeben. ECC wurde nicht implementiert oder geprüft; die bestandenen Versuche verwenden SIFT, robuste Ähnlichkeit und lokale Intensitätskontrolle.

Die räumlichen Zellen werden derzeit über das gesamte Referenzarbeitsbild bestimmt; die Hüllenquote nutzt die maskierte Überlappung. Die geforderte Zellverteilung ausdrücklich über dem Überlappungsbereich ist daher noch nicht vollständig nachgewiesen. Exportierte Profile enthalten Algorithmus-/Maskenversion und Prüfergebnisse, aber noch keinen vollständigen numerischen Parametersatz.

Referenzpfad und Hash werden getrennt im lokalen AppData gespeichert, wenn der Nutzer eine Datei auswählt. Der normale Klick auf „Automatisch abgleichen“ friert eine frische Aufnahme ein. Ein Profil enthält keine Bilder, Fenstertitel, Prozessdaten oder absoluten Referenzpfade; Buildangaben bleiben ungeprüft. Cache und resultierende Profile ersetzen keinen neuen Abgleich. Live-Kartenmarker/Spielerposition bleiben außerhalb dieses Prototyps.

## Bereitstellung und nächste Abnahme

Paket: `artifacts/win-x64-v0.3.0/Aion2Overlay.exe`. 410 Dateien einschließlich Imaging-Modul, nativer Runtime und Lizenzhinweisen wurden zum vorbereiteten Paket per SHA256 verglichen. Eine noch laufende Nutzerinstanz im bisherigen Ordner wird nicht beendet; ein vorbereiteter Austausch wartet auf eigenständiges Schließen. Erfolgreicher Austausch wird separat unter `artifacts/package-update-0.3.0.json` protokolliert. Ohne diesen Nachweis enthält der bisherige Startordner weiterhin seine alte Version.

Nutzerablauf zur echten Abnahme: v0.3.0 selbst starten → Aion-Fenster wählen/Aufnahme starten → Gebietskarte öffnen → „Karte abgleichen“ → Referenz öffnen → „Automatisch abgleichen“. Zusammenpassende Bilder und resultierende Überlagerung prüfen; fehlende Zuordnung mit Gebiet, Auflösung, sichtbarem Ausschnitt und angezeigtem Grund dokumentieren. Keine Handpunkte setzen. Ingame-Eigenschaften und die oben offenen Kriterien bleiben bis zum Nachweis offen.
