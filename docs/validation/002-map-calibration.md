# Validierung SPEC-002

Stand: 6. Oktober 2026. Bezug: [SPEC-002](../specs/002-map-calibration.md).

## Umgebung

Windows x64, projektlokales .NET SDK 10.0.401, Anwendung v0.2.0. Der Live-Dialogtest läuft in der interaktiven Windows-Sitzung außerhalb der Sandbox und nimmt ausschließlich sein eigenes HWND auf. Referenz und Aufnahme sind synthetisch erzeugte Rasterbilder, keine Aion-Karte und keine echten Fundstellen.

## Ergebnisse

| Kriterium | Ergebnis und Grenze |
| --- | --- |
| AC-01 | Bestanden: affine Rotation/Scherung/Translation sowie nicht endliche, degenerierte und nahezu kollineare Eingaben getestet |
| AC-02 | Bestanden an synthetischen Bildern: verschiedene Seitenverhältnisse, Letterbox-Ausschluss und Koordinatenerhalt nach Dialog-Resize geprüft |
| AC-03 | Bestanden: zwei unabhängige Prüfpunkte, 1080p-/2160p-Toleranz, fehlerhafter Punkt und doppelte Prüf-Landmarken getestet; Fehler blockiert Profil und UI-Speichern |
| AC-04 | Profil-Erzeugung und JSON-Roundtrip bestanden; lokale synthetische Profil-Datei geprüft. Der echte Speichern-Dateidialog wurde nicht interaktiv bedient |
| AC-05 | Referenzwechsel, Rückgängig bei vollständigem und unvollständigem Paar sowie Neustart im Dialog bestanden. Frische-/Sitzungsvoraussetzung und eingefrorene Bitmap durch Codeprüfung bestätigt; Benutzerablauf aus der echten Spielaufnahme noch offen |
| AC-06 | Release-Build und Publish ohne Warnungen/Fehler; 23 Kernlogiktests bestanden. Live-Farbtest bestanden mit acht Button-Zuständen, Auswahl und zwei Dropdown-Einträgen. Kalibrierungsfenster visuell geprüft |
| AC-07 | Offen: erste echte Global-Karte, Referenzrechte, Gebiet, Spielbuild, Auflösung, feste Ansicht und fünf Landmarken noch nicht belegt |

`--calibration-smoke-test` liefert `passed: true`, zuletzt am 6. Oktober 2026 um 10:23 UTC. Der Test verwendet eine Referenz mit 800 × 600 Pixeln und ein synthetisches Aufnahmebild mit 1000 × 600 Pixeln. Bekannte Abbildung: x = 100 + 700u, y = 60 + 480v; drei Fit-Paare und zwei unabhängige Paare rekonstruieren sie mit rundungsbedingt weniger als 0,000001 Pixel Fehler. Ein absichtlich um 20 Pixel verschobener Prüfpunkt blockiert die Freigabe.

Der [GitHub-Windows-Build mit Kernlogiktests](https://github.com/bmenschner/aion2-overlay/actions/runs/37449857107) für Implementierungscommit `f3597859d5611da0ad6ce458aa7daeebee700097` wurde am 6. Oktober 2026 ebenfalls erfolgreich abgeschlossen. Die interaktive Dialog- und Ingame-Prüfung ist kein Bestandteil dieses CI-Laufs.

Lokale, nicht versionierte Nachweise: `artifacts/calibration-smoke-test.json`, `artifacts/calibration-smoke-test-profile.json`, `artifacts/calibration-smoke-test-window.png` sowie die bisherigen `artifacts/ui-smoke-test*`-Artefakte. Die eigene Fensteraufnahme wurde visuell geprüft: Metadaten, zwei Kartenbilder, nummerierte Kreuze, Vorhersagekreise und Prüfergebnis sind vollständig lesbar. Diese Prüfung sagt nichts über Fehler auf einer echten Gebietskarte aus.

## Paketabgleich

Das Paket liegt unter `artifacts/win-x64-v0.2.0/`. Die beiden noch laufenden v0.1.3-Instanzen aus `artifacts/win-x64/` wurden regulär geschlossen; der bisherige Startpfad wurde anschließend auf Dateiversion 0.2.0.0 aktualisiert. Das neue Overlay wurde aus diesem Pfad gestartet. Eine Aufnahme wird beim Neustart nicht automatisch begonnen.

## Reproduzieren und manuell abnehmen

`Aion2Overlay.exe --calibration-smoke-test` prüft den synthetischen Kalibrierungsdialog. `--ui-smoke-test` prüft die bestehenden UI-Farben. Beide Tests benötigen eine interaktive Sitzung, öffnen eigene Fenster und schließen ihre Testinstanz wieder.

Für AC-07 die Aufnahme starten, eine unveränderte Gebietskarte öffnen und „Karte kalibrieren“ wählen. Eine lokale Referenz derselben Karte laden; Gebiet/Build/Ansicht angeben. Drei weit verteilte Landmarken und zwei weitere zuordnen. Beide Prüffehler müssen unter der angezeigten Toleranz bleiben. Das Profil über den Dateidialog speichern und dessen Metadaten überprüfen. Bilddateien werden dabei nicht automatisch gespeichert. Nach einer Ansichtsänderung erneut kalibrieren; dieses Profil wird noch nicht für Live-Marker verwendet.

Die Spezifikation bleibt „in Umsetzung“, bis die offenen Kriterien praktisch geprüft sind.
