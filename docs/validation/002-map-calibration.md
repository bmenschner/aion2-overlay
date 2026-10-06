# Validierung SPEC-002

Stand: 6. Oktober 2026. Bezug: [SPEC-002](../specs/002-map-calibration.md).

## Umgebung

Windows x64, projektlokales .NET SDK 10.0.401, Anwendung v0.2.1 (frühere Nachweise ausdrücklich v0.2.0). Der Live-Dialogtest läuft in der interaktiven Windows-Sitzung außerhalb der Sandbox und nimmt ausschließlich sein eigenes HWND auf. Referenz und Aufnahme sind synthetisch erzeugte Rasterbilder, keine Aion-Karte und keine echten Fundstellen.

## Ergebnisse

| Kriterium | Ergebnis und Grenze |
| --- | --- |
| AC-01 | Bestanden: affine Rotation/Scherung/Translation sowie nicht endliche, degenerierte und nahezu kollineare Eingaben getestet |
| AC-02 | Bestanden an synthetischen Bildern: verschiedene Seitenverhältnisse, Letterbox-Ausschluss und Koordinatenerhalt nach Dialog-Resize geprüft |
| AC-03 | Bestanden: zwei unabhängige Prüfpunkte, 1080p-/2160p-Toleranz, fehlerhafter Punkt und doppelte Prüf-Landmarken getestet; Fehler blockiert Profil und UI-Speichern |
| AC-04 | Profil-Erzeugung und JSON-Roundtrip bestanden; lokale synthetische Profil-Datei geprüft. Der echte Speichern-Dateidialog wurde nicht interaktiv bedient |
| AC-05 | Referenzwechsel, Rückgängig bei vollständigem und unvollständigem Paar sowie Neustart im Dialog bestanden. Frische-/Sitzungsvoraussetzung und eingefrorene Bitmap durch Codeprüfung bestätigt; Benutzerablauf aus der echten Spielaufnahme noch offen |
| AC-06 | Release-Build und Publish v0.2.1 ohne Warnungen/Fehler; 24 Kernlogiktests bestanden. Live-Farbtest bestanden mit acht Button-Zuständen, Auswahl und zwei Dropdown-Einträgen. Kalibrierungsfenster visuell geprüft |
| AC-07 | Nutzerprüfung begonnen, noch nicht bestanden: Altgard-Ausschnitt mit fünf Paaren in v0.2.0 ergibt 216,0 / 347,3 px Fehler bei 10,7 px Grenze. Vollständige Metadaten und erfolgreiche Zuordnung offen |
| AC-08 | v0.2.1 bestanden mit synthetischen Standard- und 5120×1440-Bildern: unabhängiger Zoom, Scrolloffsets, sichtbare Klickumrechnung, Koordinatenerhalt, Grenzen 1×/16× und Rückkehr zur Gesamtansicht geprüft |

`--calibration-smoke-test` liefert `passed: true`, zuletzt am 6. Oktober 2026 um 10:23 UTC. Der Test verwendet eine Referenz mit 800 × 600 Pixeln und ein synthetisches Aufnahmebild mit 1000 × 600 Pixeln. Bekannte Abbildung: x = 100 + 700u, y = 60 + 480v; drei Fit-Paare und zwei unabhängige Paare rekonstruieren sie mit rundungsbedingt weniger als 0,000001 Pixel Fehler. Ein absichtlich um 20 Pixel verschobener Prüfpunkt blockiert die Freigabe.

Der [GitHub-Windows-Build mit Kernlogiktests](https://github.com/bmenschner/aion2-overlay/actions/runs/37449857107) für Implementierungscommit `f3597859d5611da0ad6ce458aa7daeebee700097` wurde am 6. Oktober 2026 ebenfalls erfolgreich abgeschlossen. Die interaktive Dialog- und Ingame-Prüfung ist kein Bestandteil dieses CI-Laufs.

Lokale, nicht versionierte Nachweise: `artifacts/calibration-smoke-test.json`, `artifacts/calibration-smoke-test-profile.json`, `artifacts/calibration-smoke-test-window.png` sowie die bisherigen `artifacts/ui-smoke-test*`-Artefakte. Die eigene Fensteraufnahme wurde visuell geprüft: Metadaten, zwei Kartenbilder, nummerierte Kreuze, Vorhersagekreise und Prüfergebnis sind vollständig lesbar. Diese Prüfung sagt nichts über Fehler auf einer echten Gebietskarte aus.

## Vom Nutzer bereitgestellte Referenz

Am 6. Oktober 2026 hat der Nutzer nach der Anleitung einen Karten-Screenshot bereitgestellt und ausdrücklich präzisiert: Das Bild zeigt einen Ausschnitt der Karte Altgard. Sichtbarer Kartentitel: `Map: Altgard`. Originalbildgröße durch lokalen Bilddecoder geprüft: 5120 × 1440 Pixel. Das unveränderte Bild wurde lokal unter `artifacts/references/Altgard.png` abgelegt; es wird nicht mit Git veröffentlicht. SHA256: `2fcca952743033ef99b92b84a1f7c1e01e2da2028283e69f4b7cf7f810556b7a`.

Dies ist eine echte Nutzerreferenz für einen festen Kartenausschnitt, kein Cube-Datensatz und kein bestätigter Global-Spielbuild. Das Bild wird nicht als vollständige Gebietskarte behandelt; der Versuch belegt keine Zuordnung für außerhalb des Ausschnitts liegende Stellen. Der Nutzer empfand den bisherigen Einrichtungsablauf als zu kompliziert; die praktische Anleitung erfolgt deshalb in einzelnen Schritten.

Der anschließend bereitgestellte Screenshot des vollständigen Dialogs zeigt v0.2.0 mit Referenz und Aufnahme, beide 5120 × 1440 Pixel. Fünf Paare sind gesetzt; Meldung: „Abweichung zu groß“, Fehler 216,0 / 347,3 Pixel, Grenze 10,7 Pixel. Beide Prüfkreise sind rot. Die Bilder sind im Dialog ungefähr 520 Pixel breit; ein Anzeigepixel entspricht damit rund zehn ursprünglichen Bildpixeln. Die Markierungen der ersten drei Paare wirken relativ eng verteilt; abweichende Klickstellen und geringe Präzision der verkleinerten Ansicht sind plausible Ursachen, aber ohne gespeicherte Rohpaare nicht abschließend belegt. Die Fehlergrenze wird nicht verändert.

## Vergrößerung in v0.2.1

Der Dialog bietet pro Bild „+“/„−“, Mausrad-Vergrößerung und Scrollleisten. Dies verändert ausschließlich die Darstellung des eingefrorenen Bildes, nicht die Ingame-Karte oder vorhandene Kalibrierpunkte.

Beide Live-Diagnostikvarianten liefern `passed: true`, zuletzt am 6. Oktober 2026 um 11:30 UTC. Der zusätzliche Ultrawide-Test verwendet zwei synthetische Bilder mit jeweils 5120 × 1440 Pixeln und die bekannte Abbildung x = 400 + 3600u, y = 100 + 1100v. Klicks im tatsächlich gescrollten WPF-Viewport werden in ursprüngliche Bildpixel zurückgerechnet; vorhandene Transformationen bleiben beim unabhängigen 4×-/8×-Zoom unverändert. Fehler bleiben unter 0,000001 Pixel. Ein absichtlich um 20 Pixel falscher Prüfpunkt wird weiterhin abgewiesen. Limits 1×/16× und Referenzwechsel sind geprüft.

Lokale Nachweise: `artifacts/calibration-smoke-test-zoom.png` und `artifacts/calibration-ultrawide-test*`. Die Standard- und Ultrawide-Zoom-Aufnahmen wurden visuell geprüft: Zoomanzeigen, Scrollleisten, vergrößerte Landmarken und deckungsgleicher grüner Prüfkreis sind sichtbar. Die Aufnahme erfolgt nach Dispatcher-Idle und 300 ms Compositor-Zeit; die frühere unmittelbare Aufnahme zeigte noch den vorherigen Darstellungsstand. Kein Nutzerfenster oder Spielfenster wurde von diesen Tests aufgenommen. Der erneute Test mit den echten Altgard-Landmarken bleibt offen.

## Paketabgleich

Das Paket liegt unter `artifacts/win-x64-v0.2.0/`. Die beiden noch laufenden v0.1.3-Instanzen aus `artifacts/win-x64/` wurden regulär geschlossen; der bisherige Startpfad wurde anschließend auf Dateiversion 0.2.0.0 aktualisiert. Das neue Overlay wurde aus diesem Pfad gestartet. Eine Aufnahme wird beim Neustart nicht automatisch begonnen.

v0.2.1 liegt separat unter `artifacts/win-x64-v0.2.1/`. Der offene v0.2.0-Kalibrierungsdialog mit seinen ungespeicherten Paaren bleibt erhalten; der bisherige Startpfad wird währenddessen nicht überschrieben. Für den Zoom-Test ausdrücklich das neue Paket starten.

## Reproduzieren und manuell abnehmen

Aktuelle Produktentscheidung: Der Nutzer erklärt am 6. Oktober 2026 nach dem fehlgeschlagenen Versuch, dass präzise Klicks für ihn kein geeigneter Einrichtungsablauf sind, und fordert eine automatische Variante. Ein weiterer manueller Versuch wird nicht als nächster Nutzerschritt verlangt. [SPEC-003](../specs/003-auto-map-registration.md) plant den Ersatz; die folgenden Schritte bleiben ausschließlich die historische Prüfanleitung für SPEC-002. AC-07 bleibt offen, die technischen Zoom-Tests beweisen keine automatische Registrierung.

`Aion2Overlay.exe --calibration-smoke-test` prüft den synthetischen Kalibrierungsdialog. `--ui-smoke-test` prüft die bestehenden UI-Farben. Beide Tests benötigen eine interaktive Sitzung, öffnen eigene Fenster und schließen ihre Testinstanz wieder.

Für AC-07 die Aufnahme starten, eine unveränderte Gebietskarte öffnen und „Karte kalibrieren“ wählen. Eine lokale Referenz derselben Karte laden; Gebiet/Build/Ansicht angeben. Drei weit verteilte Landmarken und zwei weitere zuordnen. Beide Prüffehler müssen unter der angezeigten Toleranz bleiben. Das Profil über den Dateidialog speichern und dessen Metadaten überprüfen. Bilddateien werden dabei nicht automatisch gespeichert. Nach einer Ansichtsänderung erneut kalibrieren; dieses Profil wird noch nicht für Live-Marker verwendet.

Die Spezifikation bleibt „in Umsetzung“, bis die offenen Kriterien praktisch geprüft sind.
