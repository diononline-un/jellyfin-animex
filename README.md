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

Die Planung liegt als OpenSpec-Change vor. Das Projekt bringt die Agent-Skills in `.agents/skills/` mit und ruft das CLI über den Wrapper `./opsx` auf. Das CLI liegt in `.tools/` (nicht versioniert), weil ein systemweiter `npm install -g` auf dieser Maschine an fehlenden Rechten scheitert.

```bash
./opsx status --change add-animex-streaming-library
./opsx show add-animex-streaming-library
./opsx validate add-animex-streaming-library --strict
```

Fehlt `.tools/`, einmalig nachinstallieren:

```bash
npm install --prefix .tools @fission-ai/openspec@latest
```

Das npm-Paket `openspec` ist ein leerer Platzhalter, das echte Paket heißt `@fission-ai/openspec`.

## Rechtliches und Verbreitung

Privater Betrieb auf dem eigenen Server. Eine Veröffentlichung als allgemein installierbares Plugin ist nicht vorgesehen, weil sie die Verbreitung fremder Inhalte mitübernimmt. Das Plugin umgeht keine Bezahlschranken und speichert kein Videomaterial.
