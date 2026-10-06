# Projektwissen

- [Arbeitsregeln](../AGENTS.md): Spec Driven Development, Markdown-Retrieval und Projektkontext.
- [Projektplan](../PROJEKTPLAN.md): Cube-Overlay, Positionsanzeige, Navigation und Umsetzungsphasen.
- [SPEC-001](specs/001-overlay-capture.md): Fensteraufnahme und transparentes Overlay, aktuell in Umsetzung.
- [SPEC-002](specs/002-map-calibration.md): Bisheriger manueller Prototyp; echte Kartenabnahme offen, als Standardablauf zurückgestellt.
- [SPEC-003](specs/003-auto-map-registration.md): Automatischer Abgleich ohne Landmarkenklicks; erste erfolgreiche Nutzerprüfung mit gespeicherter Zuordnung in v0.3.1, vollständige Kartenabnahme offen.
- [Validierung SPEC-003](validation/003-auto-map-registration.md): Native Runtime, 30 bekannte Warps, 20 Negativbilder, automatischer Dialog und normaler WGC-Ablauf; Grenzen und offene Kriterien.
- [Drittanbieter-Komponenten](../THIRD-PARTY-NOTICES.md): Gepinnte OpenCV-Pakete und mitgelieferte Lizenzhinweise.
- [SPEC-004](specs/004-versionierter-start.md): Fester Starter, unveränderliche Versionspakete und atomare Aktivierung ohne Beenden alter Instanzen.
- [Validierung SPEC-004](validation/004-versionierter-start.md): Parallelprozesse, Dateisperren, fehlgeschlagene Updates, Fehlerfälle und echtes v0.3.1-Paket.
- [SPEC-005](specs/005-live-kartenzuordnung.md): Live-Abgleich, Testpunkt und physische Aufnahme-zu-Overlay-Geometrie; in Umsetzung, Ingame-Abnahme offen.
- [Validierung SPEC-005](validation/005-live-kartenzuordnung.md): Live-Warps, WGC-Wiederaufnahme, fortbestehender Alt-Tab-Fehler in v0.4.1 und v0.4.2 mit vollständiger Erneuerung/Diagnoseexport; Ingame-Behebung offen.
- [ADR-001](decisions/001-automatischer-kartenabgleich.md): Nutzerentscheidung für automatischen Abgleich vor Cube-Markern, Folgen für Reihenfolge und Bedienung.
- [Recherche Kartenabgleich](research/automatischer-kartenabgleich.md): Primärquellen, vorgeschlagenes Verfahren und offene Machbarkeitsnachweise.
- [Gestaltungsreferenz](research/overlay-gestaltung.md): Vom Nutzer gezeigtes kompaktes dunkles Aion-Overlay; visuelle Richtung für einen späteren UI-Entwurf, noch kein implementiertes Redesign.
- [Validierung SPEC-002](validation/002-map-calibration.md): Technische Kalibrierungsprüfung und offene Global-Kartenabnahme.
- [Architektur](architecture.md): Komponenten und Datenfluss des ersten Prototyps.
- [Validierung SPEC-001](validation/001-overlay-capture.md): Build, Aufnahme-/Farbprüfungen und reproduzierter Schließfehler mit v0.2.2-Korrektur; offene Ingame-Abnahme.
- [GitHub-Zugriff](validation/github-zugriff.md): Ergebnis der Zugriffsprüfung über WSL und GitHub CLI vom 6. Oktober 2026.
