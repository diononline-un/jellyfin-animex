# Changelog

## 2026-10-17 — Wiedergabe über die Quellen-Schnittstelle reparieren

- Auflöser auf die aktuelle Quellen-Schnittstelle des Players umgestellt: `pp.animex.one/rest/api/sources` liefert mit der Animex-Serienkennung (Slug plus ID) und den Anbietern `nero`, `yuki`, `zuna` direkt eine HLS-Adresse. Die bisherige Einbettung von `megaplay.buzz` antwortet nur noch mit einer Fehlerseite, ein Browser ist für die Auflösung nicht nötig.
- Animex-Kennung wird unverändert als Serienkennung verwendet. Die alte Bildung aus Titel und ID erzeugte bei bereits vollständigen IDs einen doppelten Slug und damit „No anime found“.
- Proxy erkennt Playlists jetzt am Inhalt (`#EXTM3U`) statt an Dateiendung oder Inhaltstyp. Varianten-Playlists des Anbieters heißen `.txt` und wurden vorher unverändert ausgeliefert.
- Segmente in Playlists, die als `file:///i/...` bezeichnet werden, werden als absoluter Pfad auf dem Playlist-Wirt aufgelöst; `Uri` hatte sie vorher als lokale Datei interpretiert.
- Segmente werden vollständig gepuffert. Der geteilte HttpClient komprimiert Antworten, der bisherige Streaming-Pfad lief dadurch in eine `ObjectDisposedException`.
- Referer und Origin werden an die CDN-Anfragen gesetzt. Erlaubte Wirte um die aktuellen CDN-Domains ergänzt.
- Live auf dem NAS geprüft: Master-Playlist, Varianten-Playlist und Segment für „Attack on Titan“ und „Death Note“ liefern HTTP 200, Segmente sind gültige MPEG-TS-Daten. 27 Einheitentests grün.
- Offen: kein automatischer Wiedergabetest in Jellyfin selbst; Player- und CDN-Verhalten kann sich ändern. Der Anbieter `anmx` ist nicht mehr verfügbar, Vorgabe ist jetzt `nero`.

## 2026-10-05 — Erste lauffähige Fassung des Plugins (0.1.0)

- Plugin für Jellyfin 10.11.5 gebaut: `TargetAbi` 10.11.0.0, Zielframework `net9.0`, Pakete `Jellyfin.Controller` und `Jellyfin.Model` in 10.11.5. Bau und Tests laufen im Container (`./build.sh`), Ergebnis ist ein Installationspaket unter `dist/`.
- Katalogleser gegen `graphql.animex.one` live geprüft: 3283 Titel, Filter für Fassung, Jahr, Bewertung, Genres und erwachsene Inhalte greifen. Bildfelder kommen je nach Titel als Zeichenkette oder als Objekt, beide Formen werden gelesen.
- Anreicherung über AniList, Verknüpfung per `anilistId`, mit Rückfall auf die Animex-Felder.
- Bibliotheksschreiber: je Folge `.strm` plus `.nfo`, je Titel `tvshow.nfo` mit Cover, Beschreibung, Genres, Studios und Bewertung. Die Bestandsdatei `.animex-ledger.json` macht Läufe wiederaufsetzbar, verhindert Duplikate und erkennt verwaiste Titel.
- Wiedergabe-Endpunkt `/Animex/Playback/{id}/{folge}`: In der Verweisdatei steht die Plugin-Adresse, nicht die Anbieteradresse. Beim Abspielen wird neu aufgelöst, dadurch bleiben die Einträge gültig. Zusätzlich `/Animex/Proxy` als Weiterleitung für Playlist- und Segmentadressen, begrenzt auf eine feste Liste erlaubter Wirte.
- Auflöser live vermessen: `GET https://plyr.animex.one/api/anmx/{anilistId}/{folge}` liefert die Folgenkennung. Die Wiedergabe-Playlist liegt danach hinter einer Einbettung mit nachgeladenem, verschleiertem Skript und ist ohne Ausführung von JavaScript nicht erreichbar. Der mitgelieferte Auflöser meldet das als „nicht auflösbar" mit Begründung; ein externer Auflöser ist als Erweiterungspunkt vorhanden.
- Zwei Fehler bei der Prüfung gegen eine eigene Jellyfin-Instanz 10.11.5 gefunden und behoben: Der Wiedergabe-Endpunkt las den Katalog seitenweise (hunderte Anfragen je Abspielen, dazu HTTP 429 der Gegenstelle), jetzt Einzelabfrage über den Kennungsfilter. Die Bestandsdatei wurde in gemischter Schreibweise gespeichert und war beim nächsten Lauf unlesbar, jetzt einheitlich klein. Beide Fälle haben einen Test.
- Katalogabfragen gegen die Anfragebegrenzung gehärtet: eine Sekunde Pause zwischen Seiten, Wiederholung mit der von der Gegenstelle genannten Wartezeit (HTTP 429, `retry_after`).
- Bauweg gehärtet: `./build.sh` räumt Reste früherer Läufe auf und läuft als aufrufender Benutzer, damit keine root-Dateien entstehen. Das ZIP entsteht über `tools/zip.mjs`, weil das zip-Programm in den Containern fehlt.
- 27 Einheitentests, alle grün: Adressbildung, Auswertung der Gegenstellen-Antwort, Dateinamen nach Jellyfin-Konvention, Fassungslogik, Bestandsdatei, Wartezeitberechnung.
- Neue Anleitung `docs/installation.md`: Installation auf dem NAS, eigener Bibliotheksordner, getrennte Bibliothek, Konfiguration, Prüfschritte, Rückbau, bekannte Einschränkungen.
- Offen: Auflöser für die Wiedergabe, dann Installation und Wiedergabetest auf dem NAS.

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
