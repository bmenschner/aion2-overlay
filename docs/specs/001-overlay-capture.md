# SPEC-001: Fensteraufnahme und Overlay-Grundlage

Status: in Umsetzung. Stand: 6. Oktober 2026.

Implementierung vorhanden; technische Prüfung an einem normalen Windows-Testfenster bestanden. Manuelle Eingabe-/DPI-Prüfungen und Aion-2-Abnahme bleiben offen. Nachweise: [Validierung](../validation/001-overlay-capture.md).

## Ziel und Umfang

Schritt 1 aus Abschnitt 10 des [Projektplans](../../PROJEKTPLAN.md): Ein vom Nutzer gewähltes Fenster aufnehmen und ein transparentes Overlay deckungsgleich über dessen Clientbereich positionieren. Ziel ist Aion 2 Europa/Global im randlosen Fenster. Beliebige andere Fenster dienen als technische Testziele; die App wählt kein Fenster automatisch aus.

Die erste Version bietet eine Windows-Oberfläche mit Fensterauswahl, Aktualisieren, Start, Stop, Vorschau und einem optionalen sichtbaren Ausrichtungsrahmen. Der Ausrichtungsrahmen ist ein Testelement, kein Cube-Spot. Standardmäßig ist er aktiviert, damit die Overlay-Geometrie überprüfbar ist. Keine Fundstellen, Kalibrierung oder Positionserkennung in diesem Schritt.

## Verhalten

- Fenster werden über Win32-Toplevel-Handles aufgelistet: sichtbar, nicht minimiert, mit Titel und nutzbarem Clientbereich; eigene Fenster sind ausgeschlossen.
- Aufgenommen wird ausschließlich das gewählte Fenster mit Windows.Graphics.Capture. Die Anwendung liest keinen Prozessspeicher und verändert den Spielclient nicht.
- Das Overlay ist ein separates transparentes, nicht aktivierendes Fenster. Mausaktionen gehen durch das Overlay an die darunterliegende Anwendung.
- Die Aufnahme enthält das gewählte Fenster einschließlich seiner Fenstergrenzen; das Overlay richtet sich am Clientbereich aus. Beide Koordinatenräume werden bewusst getrennt behandelt.
- Geometrie wird in physischen Bildschirmpixeln bestimmt; Windows-DPI wird beim Positionieren berücksichtigt.
- Das Overlay folgt Größen- und Positionsänderungen. Es wird beim Minimieren, ungültigen Fensterhandle und beim Wechsel des Vordergrundfensters ausgeblendet. Die Aufnahme darf im Hintergrund weiterlaufen; ihre Vorschau wird bei ausbleibenden Frames als veraltet markiert.
- Die Vorschau erhält höchstens ungefähr fünf Bildkopien pro Sekunde. GPU-Aufnahmeframes werden zeitnah freigegeben; langsame Vorschauverarbeitung erzeugt keine Warteschlange.
- Start und Stop sind wiederholbar. Fensterwechsel erfolgt nach Stop und erneutem Start. Fensterschließung oder Aufnahmefehler beendet die Sitzung mit verständlicher Meldung und gibt die Ressourcen frei.
- Bei deaktiviertem Ausrichtungsrahmen bleibt die Aufnahme aktiv, das Testoverlay unsichtbar.
- Aufnahmebilder werden nicht automatisch auf die Festplatte geschrieben. Der technische Selbsttest darf sein Ergebnis lokal als JSON speichern; Testdateien werden nicht versioniert.

## Komponenten

`WindowCatalog` liefert Titel, Handle und Clientgeometrie. `WindowCaptureService` liefert unveränderliche BGRA-Frames mit Bildgröße und Zeitstempel. `OverlayWindow` zeichnet Testmarkierungen; `OverlayController` verfolgt Geometrie und Sichtbarkeit. Die Steueroberfläche zeigt Sitzung und Vorschau. Die reine Sichtbarkeitsentscheidung liegt in einem kleinen unabhängig prüfbaren Kern.

Die Erkennung des geöffneten Kartenmodus folgt in einer späteren Spezifikation. In diesem Schritt bleibt der Testrahmen über dem gewählten Vordergrundfenster sichtbar, unabhängig davon, ob dessen Karte geöffnet ist.

## Abnahmekriterien

| ID | Kriterium | Nachweis |
| --- | --- | --- |
| AC-01 | Release-Build für Windows x64 ohne Compilerfehler | Lokaler Build und CI |
| AC-02 | Start einer Aufnahme auf einem normalen Windows-Testfenster liefert BGRA-Daten mit gültiger Größe und Zeitstempel | Technischer Selbsttest |
| AC-03 | Overlay verwendet nicht aktivierende und mausdurchlässige Fensterstile; seine native Position entspricht dem Clientbereich | Technischer Selbsttest; zusätzlich manuelle Klickprüfung |
| AC-04 | Geometrieänderungen werden innerhalb von 250 ms verfolgt; minimierte, geschlossene oder nicht im Vordergrund befindliche Ziele erzeugen kein sichtbares Overlay | Kernlogiktests; manueller Fenstertest |
| AC-05 | Stop und wiederholter Start geben Aufnahme und Overlay frei; ein geschlossenes Aufnahmefenster wird behandelt | Selbsttest und manueller Test |
| AC-06 | Vorschau zeigt Größe, Frische und Status; bei mindestens zwei Sekunden ohne Frame ist sie sichtbar als veraltet markiert | Implementierungsprüfung und manueller Test |
| AC-07 | Europa/Global-Aion-2 im randlosen Fenster ist aufnehmbar; Testrahmen stimmt bei der tatsächlichen UI-/DPI-Konfiguration, blockiert keine Klicks und erscheint nicht im Aufnahmebild | Ingame-Test auf dem Zielclient |

Die Spezifikation bleibt bis zur vollständigen Abnahme „in Umsetzung“. Erfolgreiche Tests an einem gewöhnlichen Fenster ersetzen AC-07 nicht.

## Quellen und offene Fragen

- [Microsoft: Screen capture](https://learn.microsoft.com/en-us/windows/apps/develop/media-authoring-processing/screen-capture), abgerufen am 6. Oktober 2026.
- [Microsoft: CreateForWindow](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.capture.interop/nf-windows-graphics-capture-interop-igraphicscaptureiteminterop-createforwindow), abgerufen am 6. Oktober 2026.
- [Microsoft: SoftwareBitmap.CreateCopyFromSurfaceAsync](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.imaging.softwarebitmap.createcopyfromsurfaceasync), abgerufen am 6. Oktober 2026.
- Aion-2-spezifische Aufnahmeverträglichkeit und geltende Overlay-Regeln sind noch nicht nachgewiesen. Die Software enthält keine Umgehung eines Aufnahmeverbots.
