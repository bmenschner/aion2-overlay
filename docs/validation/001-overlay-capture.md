# Validierung SPEC-001

Stand: 6. Oktober 2026. Bezug: [Spezifikation](../specs/001-overlay-capture.md).

## Umgebung

- Windows x64, OS-Version 10.0.26300.0.
- Projektlokales .NET SDK 10.0.401; Download anhand der offiziellen Microsoft-SHA512 geprüft.
- Windows-Aufnahmetest in einer interaktiven Sitzung außerhalb der Sandbox. Die Sandbox blockierte den Aufnahmedienst mit `0x80070424`.
- Eigene WPF-Testfenster; kein Aion-2-Fenster wurde aufgenommen.

## Ergebnisse

| Kriterium | Stand | Nachweis und Grenze |
| --- | --- | --- |
| AC-01 | Lokal bestanden | Release-Build ohne Warnungen oder Fehler; CI prüft nach Push separat |
| AC-02 | Bestanden am Testfenster | Echter WGC-Frame 640 × 360, BGRA-Länge/Zeitstempel geprüft, erwartete Hintergrundfarbe erkannt |
| AC-03 | Teilweise bestanden | Layered-/Transparent-/NoActivate-Stile und native Ausrichtung geprüft; echte Klickdurchleitung muss manuell getestet werden |
| AC-04 | Teilweise bestanden | Elf Kernlogiktests bestanden. Testfenster 640 × 360 auf 720 × 420 vergrößert; Overlay folgt und bleibt im Hintergrund unsichtbar. Manuelle Fokus-, Minimierungs-, DPI- und Timingprüfung offen |
| AC-05 | Bestanden am Testfenster | Aufnahme gestoppt, neue Sitzung erfolgreich gestartet, Schließen des Ziels erkannt und Overlay ausgeblendet |
| AC-06 | Implementiert, manuelle Prüfung offen | Vorschau liefert Bildgröße, wartet auf Erstbild und markiert Frames ab zwei Sekunden als veraltet |
| AC-07 | Aufnahme und sichtbarer Rahmen durch Nutzer bestätigt | Am 6. Oktober 2026 bestätigt der Nutzer Fenstererkennung, funktionierende Aufnahme und sichtbare Umrandung des aktiven Fensters. Exakte Deckung/DPI, Klickdurchleitung und Ausschluss eigener Marker im Aufnahmebild bleiben ungeprüft |
| AC-08 | Live-Fensterprüfung v0.2.0 bestanden; vorherige Farbanpassung vom Nutzer akzeptiert | Gelbe Buttons aktiv weiß, Stoppen schwarz; deaktivierte Buttons mit hellem Hintergrund schwarz. Acht Zustandsprüfungen einschließlich Kalibrierungsbutton und Aufnahme des geöffneten Fensters bestanden. Manueller Hover-/Fokustest offen |
| AC-09 | Live-Dropdown-Prüfung v0.2.0 bestanden; vorherige Farbanpassung vom Nutzer akzeptiert | Schwarze Auswahl und zwei schwarze Listeneinträge mit ausdrücklich synthetischen Daten geprüft; eigenes Dropdown-HWND aufgenommen |
| AC-10 | Bestanden an isolierten Testprozessen v0.2.2 | Fünf Windows-Schließszenarien mit tatsächlichem Prozessende, Exitcode 0, abgeschlossenem Aufnahme-Worker und keiner verbleibenden WPF-Fensterinstanz; tatsächlicher Nutzerablauf noch nicht beobachtet |

Der vollständige technische Selbsttest mit sichtbarem Testfenster lieferte `passed: true`. Rohberichte liegen lokal unter `artifacts/` und werden nicht versioniert. Der Test ist mit den in der README angegebenen Befehlen reproduzierbar. Der Selbsttest ohne sichtbares Fenster liefert nur einen eingeschränkten Nachweis: Bildinhalt und Aufnahmegrößenwechsel sind dort ausdrücklich ungeprüft.

## Relevanter Befund

Nutzerrückmeldung vom 6. Oktober 2026: Fenstererkennung funktioniert, Button-Schrift ist schlecht lesbar. Die globale TextBlock-Farbe konnte sich auf die Button-Beschriftungen auswirken; der Stop-Button hatte zusätzlich eine explizit helle Schrift auf dunklem Hintergrund. Die Button-Textvorlage setzt die Schrift nun lokal auf Schwarz; Stoppen verwendet einen hellen Hintergrund.

Die korrigierte Oberfläche wurde ohne Zugriff auf fremde Fenster aus den WPF-Elementen gerendert (`artifacts/button-preview.png`, lokal und nicht versioniert). Weil zwei Instanzen des bisherigen Pakets liefen, wird das aktualisierte Paket getrennt unter `artifacts/win-x64-button-fix/` bereitgestellt. Die laufenden Programme wurden nicht beendet.

Eine weitere Nutzerrückmeldung mit Screenshot zeigte weiterhin helle Beschriftungen. Die bisherige Prüfung des unhosteten Inhalts war deshalb kein ausreichender Nachweis für das laufende Programm. Welche Paketversion der Screenshot zeigt, ist nicht belegt. Die frühere Aussage zur erfolgreichen Korrektur wird durch diese Rückmeldung eingeschränkt.

Version 0.1.2 bindet einen benannten Button-Stil ausdrücklich an alle drei Buttons und ersetzt die Standard-ControlTemplate durch eine eigene Textdarstellung mit schwarzer Caption. Disabled-/Hover-/Pressed-Trigger ändern nur den Hintergrund; Tastaturfokus erhält eine sichtbare Umrandung. Version 0.1.2 steht in Titelleiste, Fußzeile und Dateiversion.

Neuer Nachweis: `--ui-smoke-test` öffnet die tatsächliche Steueroberfläche, nimmt ausschließlich dieses eigene HWND auf und prüft anschließend die sichtbaren Captions im aktiven/deaktivierten Zustand. `artifacts/ui-smoke-test.json` lieferte `passed: true` mit sechs erfolgreichen Zustandsprüfungen. `artifacts/ui-smoke-test-window.png` wurde visuell geprüft; die Aufnahme enthält schwarze Beschriftungen bei Aktualisieren, Aufnahme starten und deaktiviertem Stoppen. Diese lokalen Artefakte werden nicht versioniert. Das Paket liegt unter `artifacts/win-x64-v0.1.2/`.

Grundlage der WPF-Umsetzung: [Microsoft: Dependency property value precedence](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/properties/dependency-property-value-precedence) und [Microsoft: Control templates](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/how-to-create-apply-template), abgerufen am 6. Oktober 2026. Die konkreten Ursachen des Unterschieds zum Nutzer-Screenshot sind damit nicht abschließend nachgewiesen.

Nutzerpräzisierung vom 6. Oktober 2026: Weiße Schrift auf den gelben Buttons war gewünscht; gemeint waren die unlesbaren Dropdown-Inhalte und die helle Beschriftung des weißen Stop-Buttons. Das ersetzt die vorherige Vorgabe „alle Buttons schwarz“. In v0.1.3 bindet die Button-Caption die jeweilige Foreground-Farbe: gelbe Buttons weiß, Stoppen schwarz, deaktivierte Buttons schwarz auf hellem Hintergrund. Dropdown-Auswahl und Einträge verwenden eine eigene Textvorlage mit lokaler schwarzer Schrift; DisplayMemberPath wurde durch diese ItemTemplate ersetzt.

Der v0.1.3-Testdatensatz enthält ausschließlich `AION 2 · UI-Test` und `Zweites Fenster · UI-Test`, jeweils mit PID 0 und ungültigem Handle. Diese Einträge werden nur im UI-Testmodus verwendet und dienen nicht als echte Aufnahmeziele. Der Live-Test lieferte `passed: true`: sechs Button-Zustände, geschlossene Auswahl sowie zwei geöffnete Listeneinträge. Aufnahme des eigenen Fensters und Dropdowns liegen unter `artifacts/ui-smoke-test-window.png` und `artifacts/ui-smoke-test-dropdown.png`; beide wurden visuell geprüft. Kein Spielfenster wurde aufgenommen. Die neue EXE liegt unter `artifacts/win-x64-v0.1.3/`.

Beim außerhalb des Desktops platzierten Fenster lieferte der Windows-Compositor leere Pixel und keine zuverlässigen Resize-Frames. Deshalb wurde der Bildinhalt mit einem eigenen sichtbaren, nicht aktivierenden Fenster geprüft. Ein zweiter Befund betraf statische Fenster: Nach dem ersten Resize-Frame blieb die Aufnahme bei bloßer Framepool-Änderung stehen. Der Dienst erneuert jetzt Sitzung und Framepool gemeinsam; Größenwechsel, Stop und Neustart bestanden danach.

## Paketabgleich nach erneuter Dropdown-Rückmeldung

Nach Start der aktualisierten Version meldet der Nutzer „sehr gut“ und bestätigt funktionierende Aufnahme sowie sichtbare Umrandung des aktiven Fensters. Die bisherigen Farbanpassungen sind damit vom Nutzer akzeptiert. Diese Rückmeldung ersetzt keine separate DPI-, Fokus- oder Klickdurchleitungsprüfung.

Am 6. Oktober 2026 meldete der Nutzer erneut helle Dropdown-Schrift. Der bislang verwendete Pfad `artifacts/win-x64/Aion2Overlay.exe` enthielt noch Dateiversion 0.1.2.0; die Korrektur lag im separaten Paket v0.1.3. Beim früheren Kopieren war der Hauptordner wegen laufender Instanzen übersprungen worden. Das erklärt, warum die Bereitstellung der Korrektur im bisherigen Startpfad noch nicht wirksam war.

Nachdem keine Overlay-Instanz mehr lief, wurde der Hauptordner auf Dateiversion 0.1.3.0 aktualisiert. Der UI-Test wurde unmittelbar aus `artifacts/win-x64/Aion2Overlay.exe` gestartet und lieferte am 6. Oktober 2026 um 09:41 UTC `passed: true`: schwarze geschlossene Auswahl, zwei schwarze synthetische Dropdown-Einträge und alle sechs Button-Zustände gemäß AC-08/AC-09. Die Aufnahme des eigenen Dropdowns wurde visuell geprüft. Die erneute Nutzerprüfung und die Ingame-Kriterien bleiben offen. Es war keine weitere Änderung der Farbvorlagen erforderlich.

## Schließfehler und Korrektur v0.2.2

Am 6. Oktober 2026 meldet der Nutzer eine weiterlaufende Instanz nach Nutzung des Windows-Schließen-Buttons. Zum Beginn dieser Untersuchung wurde kein laufender Aion2Overlay-Prozess gefunden; der genaue Zustand der früher beobachteten Instanz lässt sich rückwirkend nicht feststellen. Der bisherige Quellcode enthält jedoch zwei reproduzierte Fehler im Hauptfenster-Schließablauf:

- Ohne aktive Aufnahme beendet `StopSessionAsync` synchron. Der anschließende `Close()`-Aufruf innerhalb des noch laufenden `Closing`-Ereignisses wirft `InvalidOperationException`; der explizite Shutdown wird nicht erreicht. Lokaler Vorher-Nachweis: `artifacts/shutdown-before-idle.json`, 12:43 UTC, v0.2.1, Fehler im bisherigen `MainWindow.OnClosing`.
- Bei laufender Aktion verwirft `OnClosing` den Schließwunsch mit `e.Cancel = true` und `return`. Im isolierten Versuch mit gezielt verzögertem Aufnahme-Stop bleibt der Prozess nach einem Windows-Schließbefehl mindestens fünf Sekunden offen. Vorher-Nachweis: `artifacts/shutdown-before-busy.json`, 12:43 UTC, v0.2.1. Diese Prüfung verwendet echte WGC-Aufnahme eines eigenen synthetischen Testfensters und eine verzögerte Consumer-Bestätigung; sie simuliert keine Spielaufnahme.

v0.2.2 merkt den Schließwunsch vor. Der Abschluss jeder Start-/Stop-Aktion plant bei Bedarf das Beenden ein. Eine einmalige Dispatcher-Aufgabe räumt nach Rückkehr aus dem ersten Schließereignis auf und ruft danach `Application.Shutdown` auf. Aufnahmefreigabe erfolgt auch bei vorherigen Fehlern im Dialog-/Overlay-Aufräumen über `finally`; ein Fehler im Shutdown-Aufräumen führt zum Exitcode 1. Für normale Steuerfenster gilt außerdem `OnMainWindowClose`, sodass unsichtbare Zusatzfenster die Hauptanwendung nicht weiterlaufen lassen. Die früh gestarteten eigenständigen Aufnahmetests behalten ihren expliziten Shutdown-Modus.

Grundlage: [Microsoft: Window.Closing](https://learn.microsoft.com/en-us/dotnet/api/system.windows.window.closing?view=windowsdesktop-10.0) dokumentiert Windows-„X“, abbrechbares Schließen und die Ausnahme beim erneuten Close im Schließereignis. [Microsoft: Application.ShutdownMode](https://learn.microsoft.com/en-us/dotnet/api/system.windows.application.shutdownmode?view=windowsdesktop-10.0) beschreibt explizites Beenden sowie die Bindung an das Hauptfenster. Abrufdatum 6. Oktober 2026, WPF/.NET 10.

Endgültige Paketprüfung am 6. Oktober 2026, 12:48 UTC, Windows x64, eigenständiges Paket v0.2.2:

| Szenario | Ergebnis |
| --- | --- |
| `idle` | Prozess endet mit Code 0; ungefähr 12 ms bis Exit-Ereignis |
| `capture` | Eigener WGC-Frame vorhanden; Worker abgeschlossen, Prozess Code 0; ungefähr 22 ms |
| `busy` | Ein Schließbefehl während Stop; Consumer nach 300 ms freigegeben; Prozess Code 0, Worker abgeschlossen; ungefähr 327 ms |
| `dialog` | Windows-Schließbefehl am Kalibrierungsdialog erhält Hauptfenster und Aufnahme; anschließend Hauptfenster-Schließbefehl beendet Prozess mit Code 0; ungefähr 21 ms |
| `repeat` | Zwei Windows-Schließbefehle erzeugen keine Reentranz-Ausnahme; Prozess Code 0; ungefähr 21 ms |

Alle fünf frischen Berichte liegen unter `artifacts/shutdown-v022-<scenario>.json` und wurden zusammen mit dem tatsächlichen Exitcode des jeweiligen Prozesses geprüft. Zum Exit ist die WPF-Fensterliste leer; bei Aufnahme ist der tatsächliche Worker abgeschlossen. Die Dauer misst Schließbefehl bis Exit-Ereignis im Testprozess, keine allgemeine Latenzgarantie auf allen Geräten. Release-Build/Publish ohne Warnungen/Fehler, 24 Kernlogiktests bestanden. Farbregression aus dem v0.2.2-Paket: `artifacts/ui-smoke-test.json`, `passed: true`, Exitcode 0, acht Button-Zustände, Auswahl und zwei Dropdown-Einträge. Nur eigene Diagnostikprozesse wurden gestartet und geschlossen; normale Nutzerinstanzen wurden nicht gesteuert.

Grenzen: Kein erneuter Ingame-Test; die konkrete vorherige Nutzerinstanz ist nicht mehr untersuchbar. Das Beenden bei tatsächlichem Geräte-/Treiberfehler während der Ressourcenfreigabe ist durch Codeprüfung abgesichert, aber nicht praktisch nachgewiesen. Eine versuchte Ausnahmeinjektion über ein natives Fenster-Closed-Ereignis erzeugte stattdessen eine Dispatcher-Ausnahme und zählt nicht als Nachweis für diesen Aufräumpfad. Die fünf normalen Schließszenarien sind davon getrennt nachgewiesen.

Bereitstellung: Alle 404 Paketdateien mit dem Staging-Paket per SHA256 verglichen, alter Startordner als lokales Backup erhalten, `artifacts/win-x64/Aion2Overlay.exe` auf Dateiversion `0.2.2.0` aktualisiert und EXE-/DLL-Hashes erneut geprüft. Lokaler Nachweis: `artifacts/package-update-0.2.2.json`. Keine normale Anwendung gestartet oder beendet. Automatischer Kartenabgleich bleibt geplant.

## Manuelle Abnahme im Zielclient (weiterhin offen)

1. Global-Spielbuild, Auflösung, Monitor-DPI und UI-Skalierung festhalten.
2. Aion-2-Fenster auswählen, Aufnahme starten und Bildvorschau auf echten Inhalt prüfen.
3. Zum Spiel wechseln: Rahmen und Mittelpunkt mit dem Clientbereich vergleichen.
4. Unter dem Rahmen im Spiel klicken; keine Eingabe darf vom Overlay abgefangen werden.
5. Alt-Tab, Minimieren und Fenster-/Monitorwechsel prüfen; der Rahmen darf nicht über fremden Anwendungen stehen bleiben.
6. Mit sichtbarem Rahmen prüfen, dass dieser nicht in der Aufnahmevorschau erscheint.
7. Aufnahme stoppen, erneut starten und das Spielfenster schließen; UI und Ressourcenverhalten prüfen.

Bis diese Prüfungen durchgeführt sind, bleibt die Spezifikation „in Umsetzung“.
