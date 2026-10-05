# jellyfin-animex

Jellyfin-Plugin, das Animes aus der Infrastruktur von [animex.one](https://animex.one) als reguläre Jellyfin-Bibliothek bereitstellt. Pro Folge entsteht eine kleine Verweisdatei, kein Videomaterial liegt lokal.

## Status

Implementiert, gebaut, Tests laufen durch. Auf dem NAS noch nicht installiert.

- Installationsanleitung: `docs/installation.md`
- Machbarkeit und Messwerte: `docs/feasibility.md`
- Anforderungen: `docs/prd.md`
- Planung (OpenSpec): `openspec/changes/add-animex-streaming-library/`

## Kurz zur Installation

1. Paket bauen: `./build.sh` (braucht Docker), Ergebnis `dist/Jellyfin.Plugin.Animex_0.1.0.zip`.
2. Auf dem NAS installieren: Dashboard, Plugins, Katalog, „Aus Datei installieren", ZIP wählen, Jellyfin neu starten.
3. Eigenen Ordner für die Bibliothek anlegen, zum Beispiel `/volume1/media/animex-stream`. Getrennt von allen bestehenden Bibliotheken.
4. Neue Bibliothek in Jellyfin anlegen, die nur auf diesen Ordner zeigt, Metadatenleser ausschließlich NFO, externe Anbieter abwählen.
5. Plugin-Einstellungen öffnen: Bibliothekspfad eintragen, Fassung wählen, Trockenlauf zunächst an lassen, speichern.
6. Geplante Aufgaben, Kategorie Animex, Aufgabe „Animex-Katalog abgleichen" starten. Ergebnis im Protokoll prüfen. Danach Trockenlauf abschalten und erneut starten.
7. Bibliothek scannen lassen. Fertig.

Details, Prüfschritte und Rückbau: `docs/installation.md`.

## Wie es funktioniert

1. Katalog aus `graphql.animex.one` lesen (live geprüft: 3283 Titel, Filter greifen), Metadaten über `graphql.anilist.co` anreichern, verknüpft über `anilistId`.
2. Folgen aus `episodeCount`, `subCount` und `dubCount` ableiten, Fassungen getrennt (sub, dub, beide).
3. Pro Folge eine `.strm`-Datei und eine `.nfo` schreiben, dazu `tvshow.nfo` mit Cover, Beschreibung, Genres und Bewertung. Kein Videoinhalt.
4. In der Verweisdatei steht die Adresse des Plugins (`{serverAddress}/Animex/Playback/{id}/{folge}`), nicht die Adresse des Anbieters. Beim Abspielen löst das Plugin neu auf. Dadurch bleibt der Eintrag gültig, auch wenn die ermittelte Wiedergabe-Adresse nur kurz gültig ist.
5. Jellyfin scannt den Ordner und behandelt die Einträge wie eigene Medien. Gesehen-Status, Fortsetzen, Suche und Metadaten laufen über Jellyfin selbst.

Der Auflöser ist ein eigenes Modul, weil die Gegenstelle keine dokumentierte Schnittstelle für Wiedergabe-Adressen anbietet. Ein Bruch dort wirkt nur auf dieses Modul, nicht auf Katalog oder Bibliothek.

## Voraussetzungen

- Jellyfin 10.11.5 auf dem NAS (bestätigt), Plugin-Ziel `TargetAbi` `10.11.0.0`, Zielframework `net9.0`.
- .NET SDK 9 zum Bauen (nicht 8: Jellyfin 10.11 baut auf .NET 9). Der Build über `./build.sh` nutzt dafür Docker.
- Schreibzugriff auf einen eigenen Bibliotheksordner, getrennt von bestehenden Bibliotheken.

## Entwicklung

```bash
./build.sh test    # 23 Einheitentests
./build.sh         # Tests, Build, Installationspaket
```

Aufbau: `Jellyfin.Plugin.Animex/` (Plugin) und `Jellyfin.Plugin.Animex.Tests/` (Tests). Die Anbindung läuft über `Catalogue/` (Katalog und AniList), `Playback/` (Auflöser), `Library/` (Dateien und Bestand), `Sync/` (Ablauf und Task), `Api/` (Wiedergabe-Endpunkt), `Configuration/` (Einstellungen und Oberfläche).

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

Privater Betrieb auf dem eigenen NAS im eigenen Haushalt, keine Weitergabe. Das Repository bleibt privat, das Plugin wird nicht öffentlich beworben. Das Plugin umgeht keine Bezahlschranken und speichert kein Videomaterial.
