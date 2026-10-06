# Recherche: automatischer Kartenabgleich

Stand/Abrufdatum: 6. Oktober 2026. Ziel: Windows x64, .NET 10, Aion 2 Europa/Global gemäß Nutzerkontext. Konkreter Global-Build und vollständige echte Kartenregistrierung bleiben ungeprüft; der automatische Prototyp hat technische Nachweise. Bezug: [SPEC-003](../specs/003-auto-map-registration.md), [Validierung](../validation/003-auto-map-registration.md).

## Belastbarer Projektstand

Die Fensteraufnahme funktioniert laut Nutzer im Zielclient. Der lokale Altgard-Screenshot zeigt ausdrücklich einen Ausschnitt, 5120×1440 Pixel; Hash und Herkunft stehen in [Validierung SPEC-002](../validation/002-map-calibration.md). Der anschließende Dialog-Screenshot zeigt dieselbe Gebietsansicht in beiden Panels, aber nicht beide unveränderten Originaldateien. Das zweite Rohbild und die ursprünglichen Punktkoordinaten liegen für diesen Versuch nicht als Nachweis vor.

Die manuelle Prüfung meldet 216,0/347,3 Originalpixel Fehler bei 10,7 Pixel Grenze. Verkleinerte Darstellung und Punktverteilung sind mögliche Ursachen, keine abschließend belegte Diagnose. Der Nutzer lehnt am 6. Oktober 2026 präzise Landmarkenklicks als Einrichtung ab und fordert die Planung anhand beider Kartenbilder. Die Fehlergrenze bleibt bestehen.

## Primärquellen und Folgerungen

| Quelle / dokumentierter Stand | Beleg | Folgerung für den Entwurf |
| --- | --- | --- |
| [OpenCV 4.13.0: SIFT](https://docs.opencv.org/4.13.0/da/df5/tutorial_py_sift_intro.html) | Beschreibt Merkmale in mehreren Maßstäben, Orientierung und lokale Deskriptoren | Kandidat für unterschiedliche Zoomstufen; kein Beleg für Treffer auf Aion-Karten |
| [OpenCV: Feature Matching mit FLANN](https://docs.opencv.org/4.x/d5/d6f/tutorial_feature_flann_matcher.html) | Deskriptortyp bestimmt Distanz; Verhältnisprüfung, gegenseitige Treffer und geometrische Prüfung filtern Zuordnungen | SIFT mit L2, mehrstufiges Filtern; Schwellenwerte am Projektbildsatz prüfen. Versionsbeweglicher Link, beim Implementieren erneut prüfen |
| [OpenCV 4.13.0: robuste Transformationen](https://docs.opencv.org/4.13.0/d9/d0c/group__calib3d.html) | `estimateAffinePartial2D` schätzt eine vierparametrige Abbildung für Rotation, einheitlichen Maßstab und Verschiebung; RANSAC liefert Inlier-Maske; Schätzung kann scheitern | Für eine flache Karte zunächst begrenztes Modell statt perspektivischer Verformung; Inlier-Restfehler ersetzt keine unabhängige Kontrolle |
| [OpenCV 4.13.0: ECC](https://docs.opencv.org/4.13.0/dc/d6b/group__video__track.html) | Intensitätsbasierter Abgleich braucht bei deutlichen Änderungen eine geeignete Startabbildung und kann bei Nichtkonvergenz fehlschlagen | Optionaler Feinabgleich nach Merkmalszuordnung; keine Rettung eines uneindeutigen Fits |
| [OpenCvSharp: Projekt und Paketübersicht](https://github.com/shimat/opencvsharp) | README unterscheidet aktive OpenCvSharp5/OpenCV5-Entwicklung und OpenCvSharp4/OpenCV4.13-Wartung; Windows-Runtime- und Slim-Pakete werden beschrieben | .NET-Anbindung ist verfügbarer Kandidat. Native Module, konkrete API, x64-Publish und .NET-10-Kompatibilität durch ausführbaren Versuch prüfen; noch keine Paketversion beschlossen |

Die OpenCV-4.13-Dokumentation belegt die beschriebenen Verfahren. Sie belegt nicht die identische API einer noch auszuwählenden OpenCvSharp5-Version. Der begrenzte Integrationsversuch prüft SIFT, robuste Ähnlichkeit und optional ECC, pinnt kompatible Paket-/Runtime-Versionen und dokumentiert deren Lizenz-/Auslieferungsdateien. Es wird nicht bereits für diese Planungsaufgabe eine Bibliothek installiert.

## Hypothesen, Alternativen und Grenzen

- **Hypothese:** Gelände, Küsten und Wege des Altgard-Ausschnitts enthalten genug unterscheidbare Merkmale. Zu prüfen mit beiden unveränderten Originalen, variierendem Ausschnitt und Zoom.
- **Hypothese:** Eine Ähnlichkeitsabbildung reicht im unterstützten Kartenmodus. Falls UI-Skalierung oder Kartenverformung davon abweicht, Ursache zuerst messen; kein stillschweigender Wechsel zu einem flexibleren Modell.
- **Offen:** Automatische Maskierung des konkreten UI-Layouts. Gleiche Menüs und Rasterlinien können falsche Zuordnungen liefern, obwohl das Gelände nicht passt. Negative Fälle mit gleicher UI sind deshalb verpflichtend.
- **Offen:** Laufzeit und Genauigkeit bei 5120×1440. Kleine Arbeitsbilder beschleunigen den Versuch; Abnahme erfolgt in ursprünglichen Bildpixeln, bei Bedarf mit lokalem Feinabgleich.
- **Alternative ORB:** Kandidat für eine spätere Laufzeitoptimierung mit passender Hamming-Distanz. Erst prüfen, wenn SIFT nachweislich zu teuer ist; keine parallele große Implementierung ohne Befund.
- **Nicht ausreichend als erster Ansatz:** Starres Template-Matching bei geändertem Ausschnitt/Zoom oder ausschließlich ECC ohne robuste Startabbildung. Ein perspektivisches Modell könnte Ausreißer plausibel erscheinen lassen und wird zunächst nicht benötigt.
- **Abgrenzung:** Ein Questlog-Bild kann andere Symbole und Kartengrafik verwenden. Aus ähnlichem Gebietsnamen folgt keine pixelgenaue Koordinatenübertragung. Externe Datenbeschaffung bleibt separat.

## Historischer Planungsnachweis

Paket A aus [SPEC-003](../specs/003-auto-map-registration.md): Originalpaar, synthetische Varianten mit bekannter Transformation, negative Paare, räumlich unabhängige Prüfungen und Laufzeitmessung. Erst danach ist eine Aussage über die praktische Eignung des Verfahrens zulässig. In dieser Planungsaufgabe wurden Dokumentation und Quellen geprüft; kein automatischer Matcher wurde ausgeführt.

Planprüfung am 6. Oktober 2026: Projektplan, SPEC-002, bisherige Validierung, Architektur und README auf die neue Reihenfolge abgeglichen. Lokale Markdown-Links in 13 Wissens-/Projektdateien ohne fehlendes Ziel geprüft; `git diff --check` ohne Whitespace-Fehler. Keine Code-, Paket- oder Laufzeitänderung; keine neuen Bildabgleich-Ergebnisse und deshalb kein behauptetes Bestehen von SPEC-003.

## Implementierungsnachweis v0.3.0

Für den begrenzten Versuch wurde OpenCvSharp4 plus `OpenCvSharp4.runtime.win.slim` auf `4.13.0.20260627` gepinnt. [Offizielle Paketübersicht](https://www.nuget.org/packages/OpenCvSharp4.Windows.Slim/4.13.0.20260627), abgerufen am 6. Oktober 2026: Die Slim-Runtime enthält unter anderem Bildverarbeitung, Merkmale und robuste Geometrie; .NET-8-Zielbasis ist angegeben. Kompatibilität mit dem konkreten .NET-10/x64-Paket wurde anschließend ausführbar geprüft; die native Bibliothek meldet OpenCV 4.13.0. Die Wahl hält die bisher recherchierte 4.13-API konsistent; neue Major-5-APIs werden nicht vorausgesetzt.

SIFT, L2-Matching und robuste Ähnlichkeit liefen im echten Windows-Paket. Original-/Arbeitskoordinaten, räumlicher Holdout und Intensitätskontrolle sind implementiert; ECC bleibt optional und ungetestet. Der automatische Standarddialog, Schema-2-Profil und Abbruch/Referenzwechsel wurden mit eigenen Testfenstern geprüft. Die Messergebnisse werden ausschließlich in [Validierung SPEC-003](../validation/003-auto-map-registration.md) gepflegt. Eine Identitätsprobe des Altgard-Originals ersetzt kein unabhängiges Originalpaar. Lizenzhinweise: [Drittanbieter-Komponenten](../../THIRD-PARTY-NOTICES.md).
