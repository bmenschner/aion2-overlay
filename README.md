# Aion 2 Overlay

Windows-Prototyp für Aion 2 Europa/Global im randlosen Fenster. Das Projekt soll Hidden-Cube-Spots auf der Ingame-Karte und anschließend Navigation zu ausgewählten Spots anzeigen.

## Aktueller Stand

Die ersten beiden technischen Schritte aus dem [Projektplan](PROJEKTPLAN.md) sind als Prototyp implementiert: Fensterauswahl, Windows-Fensteraufnahme und transparentes Overlay mit Ausrichtungsrahmen sowie ein Dialog zur manuellen Kartenkalibrierung. Drei Landmarken bestimmen die Abbildung, zwei weitere prüfen sie; geprüfte Profile können lokal gespeichert werden.

**Cube-Daten, Live-Kartenmarker und Spielerposition sind noch nicht implementiert.** Der Nutzer bestätigt Aufnahme und sichtbaren Rahmen im Zielclient. Die genaue DPI-Deckung, Klickdurchleitung und Kalibrierung einer echten EU/Global-Karte bleiben offen. Tests an synthetischen Bildern ersetzen diese Prüfung nicht.

Die manuelle Punktwahl hat sich im echten Altgard-Versuch nicht als geeigneter Einrichtungsablauf erwiesen. Als nächster Schritt ist deshalb ein [automatischer Abgleich beider Kartenbilder](docs/specs/003-auto-map-registration.md) geplant: Referenz einmal wählen, frische Aufnahme übernehmen, gemeinsam sichtbares Gelände automatisch zuordnen und prüfen. Diese Planung ändert die bestehende Anwendung noch nicht.

## Starten

Für den lokal gebauten Stand liegt unter `artifacts/win-x64/Aion2Overlay.exe` ein direkt startbares Paket mit eingebetteter .NET-Laufzeit. Der gesamte Ordner muss zusammenbleiben. Build-Artefakte werden nicht in Git gespeichert.

Die aktuelle Version **v0.2.1** liegt jetzt im bisherigen Startordner `artifacts/win-x64/Aion2Overlay.exe` und zusätzlich unter `artifacts/win-x64-v0.2.1/Aion2Overlay.exe`. Sie ergänzt Vergrößerung und Scrollleisten für beide Kalibrierungsbilder. Die geprüften Textfarben bleiben erhalten: gelbe Buttons weiß, heller Stop-Button sowie Dropdown-Auswahl und Listeneinträge schwarz. Die Version steht in Titelleiste und Fußzeile. Der bisherige Startordner wurde am 6. Oktober 2026 nach dem eigenständigen Schließen durch den Nutzer aktualisiert; Dateiversion und Paket-Hashes sind geprüft. Starten und Schließen übernimmt der Nutzer. Der Paketabgleich ist in der Validierung dokumentiert.

Voraussetzungen: Windows x64 mit Unterstützung für Windows.Graphics.Capture und .NET SDK 10.0.401 zum Bauen. Zielsystem ist Windows 11; die technische API-Mindestbasis ist Windows 10 Build 19041. Der Prozess läuft ohne Administratoranforderung.

Falls kein passendes SDK installiert ist, in PowerShell aus dem Projektordner:

```powershell
.\scripts\Setup-Dotnet.ps1
```

Das Skript lädt das offizielle SDK, prüft SHA512 und entpackt es unter `.tools/dotnet`. Es ändert weder PATH noch die systemweite .NET-Installation.

```powershell
.\scripts\Start-Overlay.ps1
```

1. Aion 2 im randlosen Fenster starten.
2. Im Overlay auf „Aktualisieren“ klicken und das Spielfenster ausdrücklich auswählen.
3. „Aufnahme starten“ klicken. Die Vorschau zeigt das ausgewählte Fenster.
4. Zum Spiel wechseln: Ein goldener Rahmen und ein Fadenkreuz zeigen den Clientbereich.
5. Zum Steuerfenster zurückkehren und bei Bedarf den Ausrichtungsrahmen abschalten oder die Aufnahme stoppen.

Der Rahmen dient ausschließlich der Ausrichtungsprüfung. Die Steueroberfläche verschwindet nicht automatisch. Bei minimierten Fenstern oder fehlenden Frames kann die Aufnahme aussetzen; die Vorschau kennzeichnet veraltete Daten. Der Windows-Aufnahmerahmen wird nicht unterdrückt.

## Karte kalibrieren (Schritt 2)

Die folgende Anleitung beschreibt den vorhandenen manuellen Prototyp. Sie ist kein erforderlicher nächster Nutzerschritt; der geplante Standardablauf ersetzt sie durch [SPEC-003](docs/specs/003-auto-map-registration.md).

1. Aufnahme starten und im Spiel die gewünschte Gebietskarte öffnen. Zoom und Ausschnitt unverändert lassen.
2. Zum Overlay wechseln und „Karte kalibrieren“ wählen, solange die Vorschau aktuell ist.
3. Ein lokales Referenzbild derselben Karte öffnen. Karten-ID/Gebiet, Global-Spielbuild und feste Ansicht angeben.
4. Drei weit verteilte Landmarken jeweils zuerst links auf der Referenz, dann rechts im eingefrorenen Aufnahmebild anklicken.
   Für genaue Klicks die Bilder über „+“ oder das Mausrad vergrößern. Mit den Scrollleisten die gewünschte Stelle ins Bild bewegen. „−“ kehrt zur Gesamtansicht zurück; diese Dialog-Vergrößerung verändert die Karte im Spiel nicht.
5. Zwei weitere Landmarken zuordnen. Die Kreise zeigen die vorhergesagten Stellen, die Kreuze deine Prüfklicks. Beide Fehler müssen innerhalb der angezeigten Grenze liegen.
6. Bei bestandener Prüfung das Profil über „Kalibrierung speichern“ als JSON speichern. Das Aufnahmebild wird dabei nicht gespeichert.

Das Profil gilt nur für diese feste Ansicht und wird noch nicht automatisch auf das Live-Overlay angewendet. Nach Zoom-, Karten- oder UI-Änderung neu kalibrieren. Die Referenzkarte wird lokal vom Nutzer ausgewählt; es gibt keinen Questlog-Download oder bestätigten Export in diesem Prototyp.

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

Der Regressionstest für die Textfarben startet über `Aion2Overlay.exe --ui-smoke-test` die echte Steueroberfläche und prüft alle drei Buttons aktiv/deaktiviert sowie Dropdown-Auswahl und Listeneinträge. Die zwei ausdrücklich synthetischen Fenstereinträge sind als „UI-Test“ gekennzeichnet. Der Test nimmt ausschließlich sein eigenes Fenster und sein Dropdown auf und schließt die Testinstanz wieder. Ergebnisse liegen unter `artifacts/ui-smoke-test.json`, `artifacts/ui-smoke-test-window.png` und `artifacts/ui-smoke-test-dropdown.png`. Dieser Test benötigt eine interaktive Windows-Sitzung und erfasst kein Spielfenster.

`Aion2Overlay.exe --calibration-smoke-test` öffnet den Kalibrierungsdialog mit ausdrücklich synthetischer Referenz und Aufnahme. Er prüft Zuordnung, Letterbox-Ränder, Resize, Rückgängig, Neustart, Referenzwechsel und die Sperre bei hohem Prüffehler. Lokale Ergebnisse und eigene Fensteraufnahme liegen unter `artifacts/calibration-smoke-test*`. Kein Spielfenster wird aufgenommen.

Das zusätzliche Argument `--ultrawide-test` verwendet zwei synthetische Bilder mit 5120 × 1440 Pixeln; die Ergebnisse liegen unter `artifacts/calibration-ultrawide-test*`. Beide Varianten prüfen außerdem unabhängige Vergrößerung, Scrollen, Klickumrechnung und die Grenzen 1×/16×. Die Zoom-Aufnahme wird erst nach Dispatcher-/Compositor-Verarbeitung erstellt, damit sie den tatsächlich dargestellten Zoom zeigt.

## Projektwissen

- [AGENTS.md](AGENTS.md): Arbeitsregeln für Spec Driven Development und Markdown-Retrieval.
- [Wissensindex](docs/INDEX.md): Spezifikationen, Entscheidungen und Nachweise.
- [SPEC-001](docs/specs/001-overlay-capture.md): Umfang und Abnahmekriterien für diesen Schritt.
- [SPEC-002](docs/specs/002-map-calibration.md): Kalibrierungsdialog, Koordinatenräume und Abnahmekriterien.
- [SPEC-003](docs/specs/003-auto-map-registration.md): Automatischer Ersatz für Punktwahl, Umsetzungspakete und Qualitätsprüfung.

## Nächster Schritt

Paket A aus SPEC-003: Die beiden unveränderten Kartenbilder automatisch vergleichen und Genauigkeit, Fehlzuordnungen und Laufzeit prüfen. Bei belastbarem Ergebnis folgt ein einfacher Dialog mit „Automatisch abgleichen“. Kontinuierliches Nachführen und die Umrechnung zum Live-Overlay benötigen anschließend eigene Nachweise; erst danach werden geprüfte Cube-Spots dargestellt. Technische Annahmen und Prüfergebnisse werden in den Markdown-Dateien festgehalten.
