# Drittanbieter-Komponenten

Stand: 6. Oktober 2026. Das Windows-Paket v0.3.0 enthält neben der .NET-Laufzeit die folgenden fest gepinnten Komponenten für lokalen Bildabgleich:

| Komponente | Version | Quelle / Lizenz |
| --- | --- | --- |
| OpenCvSharp4 | 4.13.0.20260627 | [Projekt](https://github.com/shimat/opencvsharp), [NuGet-Paket](https://www.nuget.org/packages/OpenCvSharp4/4.13.0.20260627), Apache-2.0 |
| OpenCvSharp4.runtime.win.slim | 4.13.0.20260627, native OpenCV 4.13.0 | [NuGet-Paket](https://www.nuget.org/packages/OpenCvSharp4.runtime.win.slim/4.13.0.20260627), Apache-2.0; weitere enthaltene Komponenten gemäß [OpenCV-Drittanbieterhinweisen](https://github.com/opencv/opencv/tree/4.13.0/3rdparty) |

Die Komponenten bleiben lokal; es werden keine Bilder an einen Dienst gesendet. Der Matcher nutzt Merkmale, Bildverarbeitung und robuste 2D-Geometrie. Ein GPU-, Video- oder Deep-Learning-Modul wird für diese Funktion nicht benötigt. Die native Windows-Slim-Laufzeit wird mit dem Paket ausgeliefert.

Apache-2.0-Lizenztext: `docs/licenses/apache-2.0.txt`. Die Namens-/Copyright-Hinweise der Anbieter und ihrer Quellen bleiben maßgeblich. Projektlizenz und Datenrechte für Kartenbilder werden dadurch nicht ersetzt.
