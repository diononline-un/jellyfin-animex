# Projektregeln jellyfin-animex

Jellyfin-Plugin, das Animes aus der animex.one-Infrastruktur als Bibliothek ohne lokale Mediendateien bereitstellt. Globale Regeln aus `~/.dsh/AGENTS.md` gelten weiter.

## Einstieg

- `docs/feasibility.md` — geprüfte Live-Befunde, Risiken, offene Prüfungen. Zuerst lesen.
- `docs/prd.md` — Anforderungen R1 bis R19, Nicht-Ziele, Erfolgskriterien, Meilensteine.
- `openspec/changes/add-animex-streaming-library/` — proposal, specs, design, tasks. Die Specs sind der Verhaltensvertrag.

## Arbeitsweise

- Vor Code-Arbeit: `openspec status --change add-animex-streaming-library` prüfen und den passenden Task aus `tasks.md` abarbeiten.
- Spec-Änderungen laufen über OpenSpec-Deltas, nicht über direktes Umschreiben von `openspec/specs/`.
- Verhaltensänderungen ohne Spec-Anpassung sind ein Fehler. Erst Delta, dann Code.
- Neue Live-Befunde zu animex oder Jellyfin gehören in `docs/feasibility.md`, nicht in den Chat.

## Harte Regeln

- Kein Herunterladen oder Vorhalten von Videomaterial. Nur Verweisdateien.
- Kein Umgehen von Bezahlschranken, Konten oder Zugriffsbeschränkungen.
- Kein Kopfloser Browser als Abhängigkeit des Plugins. Ist eine Auflösung ohne ihn nicht möglich, wird das gemeldet, nicht heimlich nachgerüstet.
- Schreibzugriffe nur im konfigurierten Bibliothekspfad. Keine bestehende Jellyfin-Bibliothek und keine Systemdatei anfassen.
- Kein eigener Wiedergabestatus. Gesehen, Fortsetzen und Favoriten führt Jellyfin.
- Zugangsdaten und Sitzungswerte nie protokollieren, nie committen.
- Keine öffentliche Distribution des Plugins.

## Umgebung

- Entwicklungsmaschine hat kein .NET SDK und keinen Zugang zum NAS. Bauen braucht einen Container mit SDK 8, Verifikation läuft auf dem NAS durch den Betreiber.
- Jellyfin-Version auf dem NAS ist unbekannt und bestimmt die TargetAbi. Vor dem ersten Build feststellen.
- OpenSpec-CLI nicht systemweit installiert (`npm install -g` scheitert an Rechten außerhalb des Workspace). Immer den Wrapper `./opsx` benutzen, CLI liegt in `.tools/` (gitignored). Paket: `@fission-ai/openspec`; das npm-Paket `openspec` ist ein leerer Platzhalter.

## Doku-Pflichten

- Nach jeder abgeschlossenen Coding-Aufgabe Eintrag oben in `CHANGELOG.md`.
- `PROJECT_STATUS.md` klein halten, `next_steps` bei Abschluss abhaken.
- GitHub: `gh` ist auf dieser Maschine nicht authentifiziert. Bis das geklärt ist, bleibt es bei lokalen Commits.
