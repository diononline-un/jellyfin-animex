# jellyfin-animex

Jellyfin-Plugin, das Animes aus der Infrastruktur von [animex.one](https://animex.one) als reguläre Jellyfin-Bibliothek bereitstellt. Pro Folge entsteht eine kleine Verweisdatei, kein Videomaterial liegt lokal.

## Status

Planung abgeschlossen, Implementierung nicht begonnen.

- Machbarkeit belegt: `docs/feasibility.md`
- Anforderungen: `docs/prd.md`
- Planungsartefakte (OpenSpec): `openspec/changes/add-animex-streaming-library/`

## Wie es funktionieren soll

1. Katalog aus `graphql.animex.one` lesen, Metadaten über `graphql.anilist.co` anreichern (Verknüpfung über `anilistId`).
2. Folgen ableiten aus `episodeCount`, `subCount`, `dubCount`.
3. Pro Folge eine `.strm`-Datei mit der Wiedergabe-URL und eine `.nfo` mit Titel, Beschreibung, Cover und Nummer in einen eigenen Bibliotheksordner schreiben.
4. Jellyfin scannt den Ordner und behandelt die Einträge wie eigene Medien. Gesehen-Status, Fortsetzen, Suche und Metadaten laufen über Jellyfin selbst.

Der Auflöser für die Wiedergabe-URL ist bewusst ein eigenes Modul, weil die Player-Seite der Gegenstelle keine dokumentierte Schnittstelle anbietet. Änderungen dort wirken nur auf dieses Modul.

## Voraussetzungen

- Jellyfin 10.10 oder neuer auf dem Zielserver (genaue Version noch festzustellen, sie bestimmt die TargetAbi).
- .NET SDK 8 zum Bauen.
- Schreibzugriff auf einen eigenen Bibliotheksordner, getrennt von bestehenden Bibliotheken.

## OpenSpec benutzen

Die Planung liegt als OpenSpec-Change vor. Das Projekt bringt die Agent-Skills in `.agents/skills/` mit. Das CLI ist nicht systemweit installiert; lokal aufrufbar ist es über:

```bash
cd /tmp/osi && ./node_modules/.bin/openspec status --change add-animex-streaming-library
```

Dauerhaft installieren: `npm install -g @fission-ai/openspec@latest` (Desktop-Paket `openspec` auf npm ist ein leeres Platzhalterpaket).

## Rechtliches und Verbreitung

Privater Betrieb auf dem eigenen Server. Eine Veröffentlichung als allgemein installierbares Plugin ist nicht vorgesehen, weil sie die Verbreitung fremder Inhalte mitübernimmt. Das Plugin umgeht keine Bezahlschranken und speichert kein Videomaterial.
