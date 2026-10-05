# Changelog

## 2026-10-05 — Erste lauffähige Fassung des Plugins (0.1.0)

- Plugin für Jellyfin 10.11.5 gebaut: `TargetAbi` 10.11.0.0, Zielframework `net9.0`, Pakete `Jellyfin.Controller` und `Jellyfin.Model` in 10.11.5. Bau und Tests laufen im Container (`./build.sh`), Ergebnis ist ein Installationspaket unter `dist/`.
- Katalogleser gegen `graphql.animex.one` live geprüft: 3283 Titel, Filter für Fassung, Jahr, Bewertung, Genres und erwachsene Inhalte greifen. Bildfelder kommen je nach Titel als Zeichenkette oder als Objekt, beide Formen werden gelesen.
- Anreicherung über AniList, Verknüpfung per `anilistId`, mit Rückfall auf die Animex-Felder.
- Bibliotheksschreiber: je Folge `.strm` plus `.nfo`, je Titel `tvshow.nfo` mit Cover, Beschreibung, Genres, Studios und Bewertung. Die Bestandsdatei `.animex-ledger.json` macht Läufe wiederaufsetzbar, verhindert Duplikate und erkennt verwaiste Titel.
- Wiedergabe-Endpunkt `/Animex/Playback/{id}/{folge}`: In der Verweisdatei steht die Plugin-Adresse, nicht die Anbieteradresse. Beim Abspielen wird neu aufgelöst, dadurch bleiben die Einträge gültig. Zusätzlich `/Animex/Proxy` als Weiterleitung für Playlist- und Segmentadressen, begrenzt auf eine feste Liste erlaubter Wirte.
- Auflöser live vermessen: `GET https://plyr.animex.one/api/anmx/{anilistId}/{folge}` liefert die Folgenkennung; die Wiedergabe-Playlist liegt danach hinter einer Einbettung mit nachgeladenem Skript. Der mitgelieferte Auflöser deckt den direkten Fall ab und meldet sonst „nicht auflösbar"; ein externer Auflöser ist als Erweiterungspunkt vorhanden.
- 23 Einheitentests, alle grün: Adressbildung, Auswertung der Gegenstellen-Antwort, Dateinamen nach Jellyfin-Konvention, Fassungslogik, Bestandsdatei.
- Neue Anleitung `docs/installation.md`: Installation auf dem NAS, eigener Bibliotheksordner, getrennte Bibliothek, Konfiguration, Prüfschritte, Rückbau.
- Offen: Installation und Wiedergabetest auf dem NAS. Dabei entscheidet sich, ob der Auflöser nachgezogen werden muss.

## 2026-09-16 — Zielplattform fixiert: Jellyfin 10.11.5

- Zielinstanz bestätigt: Jellyfin 10.11.5 auf dem NAS, Betrieb ausschließlich privat. Damit entfallen die offenen Punkte zur Version.
- Build-Ziel festgelegt: `TargetAbi` 10.11.0.0, Zielframework `net9.0`, Pakete `Jellyfin.Controller` und `Jellyfin.Model` in 10.11.5 mit `ExcludeAssets=runtime` (Beleg: Plugin-Vorlage und FastSTRM 3.0.0 fahren dieselbe Kombination). Bauvoraussetzung ist damit .NET SDK 9, nicht 8.
- Neue Anforderung R20 und neue Spec-Anforderung „Passende Zielplattform" mit zwei Szenarien ergänzt; `docs/feasibility.md` um den Abschnitt „Zielinstanz" erweitert.
- Aufgaben 1.1 und 1.5 in `tasks.md` als erledigt markiert, 1.2 auf SDK 9 korrigiert.
- Nachtrag: globaler Regelsatz in `~/.dsh/AGENTS.md` enthält jetzt den Abschnitt „OpenSpec in jedem neuen Projekt" (repo-lokale Installation in `.tools/`, Wrapper `opsx`, `validate --strict`).

## 2026-09-16 — Machbarkeit belegt, PRD und OpenSpec-Change angelegt

- Live geprüft: animex.one ist eine SvelteKit-App mit öffentlichem GraphQL-Backend (`graphql.animex.one`), offener Introspection und leerem `Disallow` in der robots.txt.
- Kernbefund: das öffentliche Schema kennt nur Katalog- und Metadatentypen (17 Typen, `AnimeNode` mit 63 Feldern), aber keine Episoden- und keine Quellenobjekte. Episodenlisten und Stream-URLs brauchen deshalb einen eigenen, abgegrenzten Auflöser.
- Nachweis für die Wiedergabe: Jellyfin unterstützt `.strm`-Dateien nativ, damit entstehen Bibliothekseinträge ohne lokales Medium; Vorbilder sind AniLibriaStrmPlugin und Jellyfin-FastSTRM.
- `docs/feasibility.md` mit allen Messwerten, Risiken und offenen Prüfungen geschrieben.
- `docs/prd.md` mit 19 Anforderungen, Nicht-Zielen, Erfolgskriterien und Meilensteinen M1 bis M5 geschrieben.
- OpenSpec initialisiert (`openspec init --tools agents --language de`), Change `add-animex-streaming-library` mit proposal, drei Capability-Specs, design und tasks angelegt; `openspec validate --strict` läuft durch.
- Offen: Jellyfin-Version auf dem NAS, .NET SDK 8 auf der Entwicklungsmaschine, Praxistest der Auflösung. Implementierung noch nicht begonnen.
