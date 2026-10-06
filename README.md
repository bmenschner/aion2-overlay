# Aion 2 Overlay

Windows-Prototyp für Aion 2 Europa/Global im randlosen Fenster. Das Projekt soll Hidden-Cube-Spots auf der Ingame-Karte und anschließend Navigation zu ausgewählten Spots anzeigen.

## Aktueller Stand

Der erste technische Schritt aus dem [Projektplan](PROJEKTPLAN.md) ist implementiert: Fensterauswahl, Windows-Fensteraufnahme, Vorschau und transparentes Overlay mit Ausrichtungsrahmen. Der Rahmen folgt dem Clientbereich des gewählten Fensters und wird beim Wechsel zu einer anderen Anwendung ausgeblendet.

**Cube-Daten, Kartenkalibrierung und Spielerposition sind noch nicht implementiert.** Die praktische Abnahme im EU/Global-Spiel steht aus. Der Test mit normalen Windows-Fenstern bestätigt keine Freigabe oder Verträglichkeit durch Aion 2.

## Starten

Für den lokal gebauten Stand liegt unter `artifacts/win-x64/Aion2Overlay.exe` ein direkt startbares Paket mit eingebetteter .NET-Laufzeit. Der gesamte Ordner muss zusammenbleiben. Build-Artefakte werden nicht in Git gespeichert.

Die aktuelle Farbanpassung ist als **v0.1.3** unter `artifacts/win-x64-v0.1.3/Aion2Overlay.exe` bereitgestellt: weiße Beschriftungen auf gelben Buttons, schwarze Beschriftung auf dem hellen Stop-Button sowie schwarze Dropdown-Auswahl und Listeneinträge. Die Version steht in Titelleiste und Fußzeile. Ältere Paketordner können noch frühere Versionen enthalten.

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

## Projektwissen

- [AGENTS.md](AGENTS.md): Arbeitsregeln für Spec Driven Development und Markdown-Retrieval.
- [Wissensindex](docs/INDEX.md): Spezifikationen, Entscheidungen und Nachweise.
- [SPEC-001](docs/specs/001-overlay-capture.md): Umfang und Abnahmekriterien für diesen Schritt.

## Nächster Schritt

Die erste feste Kartenansicht anhand von drei Landmarken kalibrieren und an weiteren Punkten prüfen. Anschließend zehn im Global-Client geprüfte Cube-Spots einbinden. Technische Annahmen und Prüfergebnisse werden vor einer Erweiterung in den Markdown-Dateien festgehalten.
