# Aion 2 Overlay

Windows-Prototyp für Aion 2 Europa/Global im randlosen Fenster. Das Projekt soll Hidden-Cube-Spots auf der Ingame-Karte und anschließend Navigation zu ausgewählten Spots anzeigen.

## Aktueller Stand

Implementiert sind Fensterauswahl, Windows-Fensteraufnahme, transparentes Overlay mit Ausrichtungsrahmen und seit v0.3.0 ein automatischer Abgleich einer lokalen Referenz mit der aktuellen Kartenaufnahme. Der Dialog bietet „Automatisch abgleichen“, eine geprüfte Überlagerung und lokale Profilspeicherung. Handpunkte sind dafür nicht erforderlich.

Seit **v0.4.0** führt eine Live-Zuordnung den geprüften Bereich und einen Referenz-Testpunkt im Overlay nach. Der Punkt trägt „TEST · kein Cube“. **Echte Cube-Daten und Spielerposition sind noch nicht implementiert.** Der Nutzer bestätigt Aufnahme, sichtbaren Rahmen und erfolgreichen automatischen Kartenabgleich in v0.3.1. Die Live-Deckung, Klickdurchleitung und vollständige Ingame-Abnahme bleiben offen. Soll-Verhalten und technische Nachweise: [SPEC-005](docs/specs/005-live-kartenzuordnung.md), [Validierung](docs/validation/005-live-kartenzuordnung.md).

Die manuelle Punktwahl hat sich im echten Altgard-Versuch nicht als geeigneter Einrichtungsablauf erwiesen. [SPEC-003](docs/specs/003-auto-map-registration.md) ersetzt sie im Standardablauf. Der automatische Prototyp besteht technische Prüfungen und die erste erfolgreiche Nutzerprüfung mit Ingame-Karteninhalt; zusätzliche echte Varianten und externe Genauigkeitsmessungen bleiben offen. Grenzen und Nachweise stehen in [Validierung SPEC-003](docs/validation/003-auto-map-registration.md).

## Starten

**`Aion2Overlay starten.cmd`** im Projektstamm ist der feste Einstieg. Derselbe Starter liegt zusätzlich im bisherigen Ordner `artifacts/win-x64/`. Per Doppelklick öffnet er das aktuelle geprüfte Paket, auch wenn eine ältere Instanz läuft. Diesen Starter einmal als neuen Startweg verwenden; anschließend bleibt er bei Updates gleich. Direkte alte `Aion2Overlay.exe`-Dateien bleiben an ihre jeweilige Version gebunden.

Die aktuell aktivierte Version ist **v0.4.2**. Pakete liegen unter `artifacts/releases/` mit eingebetteter .NET-Laufzeit; `artifacts/current.json` benennt das vollständig geprüfte Paket. Laufende alte Pakete werden nicht überschrieben, gelöscht oder geschlossen. Starten und Schließen normaler Instanzen übernimmt der Nutzer. Neue Versionen werden über `scripts/Publish-Overlay.ps1` bereitgestellt; erst bei erfolgreicher Prüfung wird der nächste Start umgeschaltet. Build-Artefakte werden nicht in Git gespeichert. Nachweise: [Start-/Updateprüfung](docs/validation/004-versionierter-start.md), [Kartenabgleich](docs/validation/003-auto-map-registration.md), [Live-Zuordnung und Schließen](docs/validation/005-live-kartenzuordnung.md).

Voraussetzungen: Windows x64 mit Unterstützung für Windows.Graphics.Capture und .NET SDK 10.0.401 zum Bauen. Zielsystem ist Windows 11; die technische API-Mindestbasis ist Windows 10 Build 19041. Der Prozess läuft ohne Administratoranforderung.

Falls kein passendes SDK installiert ist, in PowerShell aus dem Projektordner:

```powershell
.\scripts\Setup-Dotnet.ps1
```

Das Skript lädt das offizielle SDK, prüft SHA512 und entpackt es unter `.tools/dotnet`. Es ändert weder PATH noch die systemweite .NET-Installation.

```powershell
.\scripts\Start-Overlay.ps1
```

Das Startskript öffnet das aktive Paket und baut nicht nebenbei. Nur zur Entwicklung/Bereitstellung einer neuen Version:

```powershell
.\scripts\Publish-Overlay.ps1
.\scripts\Start-Overlay.ps1 -ValidateOnly
```

`-ValidateOnly` prüft Version und alle Dateihashes ohne App-Start. `Start-Overlay.ps1 -NoBuild` bleibt als kompatibler Aufruf erhalten; beide Startvarianten verwenden dasselbe aktive Paket. Alte Versionen werden lokal aufbewahrt.

1. Aion 2 im randlosen Fenster starten.
2. Im Overlay auf „Aktualisieren“ klicken und das Spielfenster ausdrücklich auswählen.
3. „Aufnahme starten“ klicken. Die Vorschau zeigt das ausgewählte Fenster.
4. Zum Spiel wechseln: Ein goldener Rahmen und ein Fadenkreuz zeigen den Clientbereich.
5. Zum Steuerfenster zurückkehren und bei Bedarf den Ausrichtungsrahmen abschalten oder die Aufnahme stoppen.

Der Rahmen dient ausschließlich der Ausrichtungsprüfung. Die Steueroberfläche verschwindet nicht automatisch. Bei minimierten Fenstern oder fehlenden Frames kann die Aufnahme aussetzen; die Vorschau kennzeichnet veraltete Daten. Der Windows-Aufnahmerahmen wird nicht unterdrückt.

## Karte automatisch abgleichen (Schritt 2)

1. Aufnahme starten und im Spiel die gewünschte Gebietskarte öffnen. Zoom und Ausschnitt unverändert lassen.
2. Zum Overlay wechseln und „Karte abgleichen“ wählen, solange die Vorschau aktuell ist.
3. Über „Referenzkarte öffnen“ ein lokales Bild derselben Karte wählen. Pfad und Hash werden für spätere Starts lokal gemerkt. Die erste geprüfte Maskierung unterstützt den Ingame-Kartenstil; andere Referenzstile bleiben zu prüfen.
4. „Automatisch abgleichen“ klicken. Der Dialog übernimmt eine frische Aufnahme und friert sie für die Prüfung ein.
5. Bei Erfolg die Überlagerung im grün begrenzten geprüften Bereich ansehen. Bei unsicherer Zuordnung erscheint eine Begründung und Speichern bleibt gesperrt.
6. Optional Angaben zum Gebiet ergänzen und „Zuordnung speichern“ wählen. Buildangaben werden als ungeprüft behandelt; das Profil enthält keine Bilder.

Eine gespeicherte Zuordnung beschreibt das eingefrorene Bild. Ein geladenes Profil benötigt ebenfalls einen frischen Abgleich; es aktiviert die Live-Anzeige nicht von allein. Es gibt keinen Questlog-Download oder bestätigten Export. Der manuelle Dialog bleibt ausschließlich als Entwicklungsdiagnose vorhanden.

Das aktuelle Spielbild kommt direkt aus der Fensteraufnahme. Ein zusätzlich selbst angefertigter Screenshot zur Genauigkeitskontrolle ist optional. Die vorhandene Kartenreferenz wird wiederverwendet; die Anwendung prüft die Zuordnung automatisch. Eine manuelle Genauigkeitsbestätigung ist nicht erforderlich, auch bei der Live-Zuordnung.

## Live-Zuordnung prüfen (v0.4.0)

1. Nach erfolgreichem automatischen Abgleich „Live-Zuordnung starten“ klicken. Profilspeicherung ist dafür nicht nötig.
2. Zum Spiel zurückwechseln. Die Live-Anzeige zeigt einen grünen geprüften Bereich und, sofern im Bereich sichtbar, den Referenzpunkt „TEST · kein Cube“. Der goldene Ausrichtungsrahmen kann separat abgeschaltet werden.
3. Karte zoomen oder verschieben: Die Anwendung ordnet neue Aufnahmebilder automatisch zu. Bei erkannter Änderung verschwindet die bisherige Anzeige während des neuen Abgleichs.
4. Bei unpassender Referenz, unbekannter Geometrie oder veralteten Bildern bleibt die Live-Anzeige ausgeblendet. Ohne frische Bilder läuft die Zuordnung nach zwei Sekunden aus. Eine zuverlässige Erkennung jedes Kartenöffnens/-schließens im Spiel ist noch nicht belegt.
5. Zum Beenden „Live-Zuordnung anzeigen“ abwählen oder die Aufnahme stoppen. Nach dem Abschalten über „Karte abgleichen“ wieder starten. Für eine andere Gebietskarte eine passende Referenz wählen.

Seit v0.4.1 bleibt die Live-Aktivierung beim Nachschlagen per Alt-Tab erhalten. Bei der Rückkehr fordert die Anwendung automatisch frische Aufnahmeframes an und gleicht sie mit der bisherigen Referenz neu ab. Aufnahme und Abgleich müssen dafür nicht manuell gestartet werden. Bis zum erfolgreichen neuen Fit bleibt die Testanzeige ausgeblendet. Bei fehlenden frischen Frames im Vordergrund wird die Aufnahme begrenzt erneut angefordert; ein ausdrücklicher Stop bleibt wirksam.

Der Nutzer meldet weiterhin fehlende Zuordnung in v0.4.1 mit nur 1–2 gemeinsamen Merkmalen. v0.4.2 erneuert bei Rückkehr zusätzlich Aufnahmeobjekt und Grafikgerät. Ob dies den konkreten Fehler im Spiel behebt, ist noch offen. Die Qualitätsprüfung bleibt unverändert; „Kartenbild nicht zugeordnet“ bedeutet, dass automatisch weiter abgeglichen wird.

Zur optionalen Fehleranalyse „Diagnose speichern“ wählen. Das ZIP enthält `aufnahme.png` mit dem letzten tatsächlich analysierten Bild, `referenz.png` mit der Live-Referenz (sofern aktiv) und `zustand.json` mit Version, Zeit, Geometrie und zugehöriger Abgleichmeldung. Ohne abgeschlossene Analyse wird die letzte empfangene Aufnahme entsprechend gekennzeichnet exportiert. Bilder werden ausschließlich nach diesem ausdrücklichen Klick gespeichert. Diese Diagnose ist keine Einrichtungs- oder Genauigkeitsfreigabe.

Es läuft höchstens ein vollständiger Abgleich gleichzeitig, mit mindestens 500 ms zwischen Starts. Kleine Bildänderungen werden heuristisch toleriert; Verhalten bei dynamischen Wolken, Symbolen und kleinen Verschiebungen im echten Spiel ist noch offen. [Nachweise und Grenzen](docs/validation/005-live-kartenzuordnung.md).

## Bauen und testen

Mit einem systemweit verfügbaren passenden SDK:

```powershell
dotnet build Aion2Overlay.slnx --configuration Release
dotnet test tests/Aion2Overlay.Core.Tests/Aion2Overlay.Core.Tests.csproj --configuration Release --no-build
```

Bei projektlokalem SDK statt `dotnet` den Aufruf `& .\.tools\dotnet\dotnet.exe` verwenden.

Der technische Selbsttest erzeugt ein eigenes Testfenster und prüft Fensterstile, Geometrie, Aufnahmegrößen, Größenwechsel, Neustart und Fensterschließung:

```powershell
& .\.tools\dotnet\dotnet.exe .\src\Aion2Overlay.App\bin\Release\net10.0-windows10.0.19041.0\Aion2Overlay.dll --smoke-test --result artifacts\smoke-test.json
```

Der Standardtest platziert das Testfenster außerhalb des sichtbaren Desktops. Windows kann dafür leere Pixel und keine Resize-Frames liefern; Bildinhalt und Größenwechsel sind in diesem eingeschränkten Modus ausdrücklich ungeprüft. Für den vollständigen technischen Test das Argument `--visible-test-window` ergänzen: Ein kleines nicht aktivierendes Testfenster erscheint kurz auf dem Desktop und schließt sich nach dem Test.

Tests der Fensteraufnahme benötigen eine interaktive Windows-Sitzung. CI führt Build und Kernlogiktests aus; sie ersetzt die praktische Overlay-Prüfung nicht.

`Aion2Overlay.exe --registration-smoke-test --result artifacts/registration-test.json` prüft den tatsächlichen nativen Matcher mit bekannten synthetischen Transformationen, Negativfällen, Ultrawide-Laufzeiten und den automatischen Dialog einschließlich normalem WGC-Aufnahmeablauf. Ausschließlich eigene Testfenster werden aufgenommen. Ein Altgard-Bild unter `artifacts/references/Altgard.png` wird, falls vorhanden, nur gegen sich selbst geprüft; das ersetzt keinen echten Bildpaar-Nachweis. Abhängigkeiten und Lizenzen stehen in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

Der Regressionstest für die Textfarben startet über `Aion2Overlay.exe --ui-smoke-test` die echte Steueroberfläche und prüft die Buttons einschließlich Diagnose aktiv/deaktiviert sowie Dropdown-Auswahl und Listeneinträge. Die zwei ausdrücklich synthetischen Fenstereinträge sind als „UI-Test“ gekennzeichnet. Der Test nimmt ausschließlich sein eigenes Fenster und sein Dropdown auf und schließt die Testinstanz wieder. Ergebnisse liegen unter `artifacts/ui-smoke-test.json`, `artifacts/ui-smoke-test-window.png` und `artifacts/ui-smoke-test-dropdown.png`. Dieser Test benötigt eine interaktive Windows-Sitzung und erfasst kein Spielfenster.

`Aion2Overlay.exe --calibration-smoke-test` öffnet den Kalibrierungsdialog mit ausdrücklich synthetischer Referenz und Aufnahme. Er prüft Zuordnung, Letterbox-Ränder, Resize, Rückgängig, Neustart, Referenzwechsel und die Sperre bei hohem Prüffehler. Lokale Ergebnisse und eigene Fensteraufnahme liegen unter `artifacts/calibration-smoke-test*`. Kein Spielfenster wird aufgenommen.

Das zusätzliche Argument `--ultrawide-test` verwendet zwei synthetische Bilder mit 5120 × 1440 Pixeln; die Ergebnisse liegen unter `artifacts/calibration-ultrawide-test*`. Beide Varianten prüfen außerdem unabhängige Vergrößerung, Scrollen, Klickumrechnung und die Grenzen 1×/16×. Die Zoom-Aufnahme wird erst nach Dispatcher-/Compositor-Verarbeitung erstellt, damit sie den tatsächlich dargestellten Zoom zeigt.

`Aion2Overlay.exe --shutdown-smoke-test --scenario idle --result artifacts/shutdown-idle.json` prüft das Beenden durch den Windows-Schließbefehl in einem separaten Diagnostikprozess. Weitere Szenarien: `capture`, `busy`, `dialog`, `repeat`, `live`, `live-unchecked`. Sie verwenden ausschließlich eigene Testfenster und keine Nutzerinstanz; `busy` verzögert die Bestätigung eines Aufnahmeframes, `live` schließt während des nativen Live-Abgleichs und `live-unchecked` direkt nach Abschalten der Live-Anzeige. Der Testprozess muss innerhalb von fünf Sekunden nach einem Schließbefehl enden. Prozess-Exitcode und frischen JSON-Bericht zusammen prüfen; WGC-Szenarien benötigen eine interaktive Windows-Sitzung. CI führt diese interaktiven Tests nicht aus.

`Aion2Overlay.exe --live-map-smoke-test --result artifacts/live-map-test.json` prüft bekannte Live-Zoom-/Verschiebungsfolgen, Ausblenden bei Änderung/Alter, begrenzte Verarbeitung und den normalen Dialog→Live-Ablauf mit eigenem WGC-Ziel. Die Prüfung erzeugt ausschließlich synthetische Fenster und bestätigt keine Ingame-Abnahme. Details zur eingeschränkten Fokusprüfung stehen in der [Validierung](docs/validation/005-live-kartenzuordnung.md).

`Aion2Overlay.exe --resume-smoke-test --result artifacts/resume-test.json` prüft drei Hintergrund-/Rückkehrfolgen mit echter Aufnahme eines eigenen Testfensters. Eine ausdrücklich kontrollierte Fokusquelle und unterdrückte Hintergrundzustellungen simulieren die Pause. Dieselbe Aufnahme-/Live-Instanz muss automatisch weiterlaufen, veränderter Inhalt bleibt zunächst ausgeblendet, Stop verhindert Wiederaufnahme. Der Test bedient keine Nutzerfenster und ersetzt kein echtes Alt-Tab im Spiel.

## Projektwissen

- [AGENTS.md](AGENTS.md): Arbeitsregeln für Spec Driven Development und Markdown-Retrieval.
- [Wissensindex](docs/INDEX.md): Spezifikationen, Entscheidungen und Nachweise.
- [SPEC-001](docs/specs/001-overlay-capture.md): Umfang und Abnahmekriterien für diesen Schritt.
- [SPEC-002](docs/specs/002-map-calibration.md): Kalibrierungsdialog, Koordinatenräume und Abnahmekriterien.
- [SPEC-003](docs/specs/003-auto-map-registration.md): Automatischer Ersatz für Punktwahl, Umsetzungspakete und Qualitätsprüfung.
- [SPEC-004](docs/specs/004-versionierter-start.md): Fester Starter und neue Versionen bei laufender alter Instanz. `scripts/Test-OverlayLauncher.ps1` prüft mit eigenen synthetischen Prozesspaketen; keine Spielaufnahme oder normale Nutzerinstanz.
- [SPEC-005](docs/specs/005-live-kartenzuordnung.md): Laufender Kartenabgleich, Testanzeige und Aufnahme-zu-Client-Geometrie.

## Nächster Schritt

Die Live-Zuordnung im EU/Global-Client bei Zoom, Verschieben, Kartenwechsel und Schließen/Wiederöffnen prüfen; DPI-Deckung und Klickdurchleitung praktisch nachweisen. Die offenen Kriterien aus SPEC-003/SPEC-005 bleiben bestehen. Danach folgen Datenmodell und Darstellung geprüfter Cube-Spots.
