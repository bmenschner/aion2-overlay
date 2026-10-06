# GitHub-Zugriffsprüfung

Prüfdatum: 6. Oktober 2026, Europe/Berlin.

Ziel: Erreichbarkeit und gemeldete Berechtigungen für [bmenschner/aion2-overlay](https://github.com/bmenschner/aion2-overlay) über WSL und `gh` prüfen. Der Nutzer hat diesen Zugriff und die Nutzung des Repositorys zur Projektverfolgung autorisiert.

## Ergebnisse

| Prüfung | Ergebnis |
| --- | --- |
| WSL-Umgebung | Ubuntu unter WSL 2 erreichbar |
| `gh auth status` | Erfolgreich bei github.com als `bmenschner` angemeldet; Git-Protokoll in gh auf SSH eingestellt |
| `gh repo view bmenschner/aion2-overlay --json nameWithOwner,url,isPrivate,defaultBranchRef,viewerPermission,isEmpty,description` | Erfolgreich |
| Sichtbarkeit | Öffentlich (`isPrivate: false`) |
| Gemeldete Repository-Berechtigung | `ADMIN` |
| Repository-Inhalt | Leer (`isEmpty: true`); kein benannter Standardbranch zurückgegeben |
| Lokaler Projektordner | Zum Prüfzeitpunkt noch kein Git-Repository |

## Grenzen und Umgebung

Die Prüfung hat lesend auf GitHub zugegriffen. Es wurden keine Commits, Branches, Issues oder sonstigen GitHub-Inhalte angelegt oder geändert. Administratorrechte wurden über die API gemeldet; ein tatsächlicher Push und der SSH-Git-Transport wurden nicht getestet.

Der erste WSL-Aufruf innerhalb der Sandbox scheiterte mit `E_ACCESSDENIED`. Mit genehmigter Ausführung außerhalb der Sandbox war WSL erreichbar. Die installierte gh-Version unterstützt das JSON-Feld `visibility` nicht; die Prüfung verwendete stattdessen das unterstützte Feld `isPrivate`.

Anmeldedaten werden nicht in den Projektdateien gespeichert. Berechtigungen und Repository-Zustand können sich nach diesem Prüfdatum ändern.
