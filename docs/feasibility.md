# Machbarkeit: Animex-Inhalte als Jellyfin-Bibliothek streamen

Stand: 2026-09-16. Alle Befunde live gegen die echten Endpunkte geprüft, keine Annahmen aus zweiter Hand.

## Fragestellung

Kann ein Jellyfin-Plugin Animes nicht von der Festplatte, sondern aus der Infrastruktur von animex.one abspielen, und sich dabei wie eine normale Jellyfin-Bibliothek verhalten: Suche, Metadaten, gesehene Episoden geloggt, Fortsetzen an der letzten Position.

## Antwort

Ja. Der Weg ist technisch belegt, die Metadaten- und Katalogseite ist offen dokumentierbar, und der einzige echte Wartungspunkt liegt bei der Auflösung der Stream-URL. Zwei Bausteine tragen das:

1. Jellyfin unterstützt `.strm`-Dateien als Bibliotheks-Items. Eine STRM-Datei enthält nur eine URL. Jellyfin legt daraus einen normalen Bibliothekseintrag an und spielt die URL ab. Kein Medium auf Platte. Gesehen-Status, Position, Suche, Collections und Benutzerverwaltung laufen danach über Jellyfins eigene Mechanismen, ohne Sonderweg.
2. animex.one betreibt ein öffentliches GraphQL-Backend mit vollständigen Anime-Metadaten und einer ID-Verknüpfung zu AniList. Damit lässt sich der Katalog maschinell lesen statt HTML zu kratzen.

Bekannte Vorbilder, die den STRM-Weg produktiv nutzen: [AniLibriaStrmPlugin](https://github.com/queukat/AniLibriaStrmPlugin) (STRM-, NFO- und EDL-Erzeugung für eine russische Fansub-Seite) und [Jellyfin-FastSTRM](https://github.com/mmyysnd/Jellyfin-FastSTRM) (überspringt die ffprobe-Fernabfrage, damit STRM-Items schnell starten).

## Geprüfte Fakten

### animex.one

| Prüfung | Befund |
|---|---|
| Technik | SvelteKit-App, ausgeliefert über Cloudflare (`server: cloudflare`, `cf-ray`) |
| Roboterzugriff | `https://animex.one/robots.txt` = `User-agent: *` / `Disallow:` (leer), Crawling nicht untersagt |
| Catalog-API | `POST https://graphql.animex.one/graphql`, Introspection offen, `GET /` antwortet `{"message":"we alive bois"}` |
| Query-Root | `anime(id, anilistId, malId, kitsuId, tmdbId, thetvdbId, anidbId, imdbId, subDubFilter)`, `searchAnime(query, limit, offset, includeAdult)`, `catalogAnime(filter, sort, limit, offset)` |
| Datentypen | 17 Typen, u. a. `AnimeNode` (63 Felder), `AnimeCatalogFilterInput`, `AnimeSortInput`, `SubDubFilter`, `LinkedAnimeNode` |
| AnimeNode-Felder | `id`, `anilistId`, `malId`, `titleRomaji`, `titleEnglish`, `titles`, `synonyms`, `coverImage`, `bannerImage`, `description`, `episodeCount`, `subCount`, `dubCount`, `duration`, `genres`, `studios`, `tags`, `characters`, `relations`, `seasons`, `averageScore`, `popularity`, `status`, `format`, `seasonYear`, `isAdult`, `trailerId`, `nextAiringEpisode` |
| ID-Beziehung | Am Beispiel `anime/21` liefert animex `anilistId` 21 (ONE PIECE). Die interne ID ist in diesem Fall identisch, die Verknüpfung ist aber über das Feld `anilistId` sauber auslesbar |
| Metadaten-Herkunft | animex selbst zieht Titel und Bilder aus AniList (`graphql.anilist.co`, `s4.anilist.co`, `img.anili.st`, `artworks.thetvdb.com`) |
| Serienseite | `https://animex.one/anime/{id}` ist serverseitig gerendert, 241 KB, enthält JSON-LD (`@type: TVSeries`, `numberOfEpisodes: 1176`) und Episodenverweise |
| Episoden-URL-Muster | `/watch/{slug}-{animexId}-episode-{n}`, z. B. `/watch/one-piece-21-episode-1` |
| Player | `plyr.animex.one`, `megaplay.buzz`, getrennte SUB-/DUB-Spuren |
| Weitere Infrastruktur | `auth.animex.one` (Login), `pp.animex.one` (`{"status":"ok"}`), `ws.animex.one` (Watch-Party), `serveproxy.com` (Stream-Proxy) |

Entscheidender Negativbefund: das öffentliche GraphQL-Schema enthält **keine** Episoden- und **keine** Quellenobjekte. Es gibt nur `AnimeNode` und Verbindungs-/Filtertypen. Episodenlisten und Stream-URLs liegen also nicht in der dokumentierten API. Damit ist klar getrennt, was stabil ist (Katalog und Metadaten) und was Wartungsarbeit wird (Quellenauflösung).

### Jellyfin-Seite

- `.strm` ist ein natives Medienformat der Bibliothek. Jellyfin liest den Inhalt als URL und übergibt sie an den Client oder an den Transcoder.
- NFO-Dateien werden lokal gelesen, damit funktionieren Titel, Beschreibung, Poster und Episodennummern ohne externen Metadaten-Agenten und ohne Internetzugriff beim Scan.
- Fernabruf-URLs werden per ffprobe geprüft. Das kostet beim ersten Scan und beim Playback-Start Zeit, deshalb existiert FastSTRM. Für den ersten Ausbau genügt es, den Scan zu drosseln; die Beschleunigung ist ein eigener, späterer Schritt.
- Fortsetzen und Gesehen-Status hängen an der Jellyfin-Item-ID, nicht an einer Datei. Ein STRM-Eintrag verhält sich hier wie jede andere Folge.

### Zielinstanz

Bestätigt durch den Betreiber am 2026-09-16: Jellyfin **10.11.5** auf einem NAS, ausschließlich privater Betrieb im eigenen Haushalt.

Daraus folgt für den Bau:

| Punkt | Wert | Herkunft |
|---|---|---|
| Zielversion | 10.11.5 | Angabe des Betreibers |
| TargetAbi | `10.11.0.0` | Versionsschema der Plugin-Verwaltung |
| Zielframework | `net9.0` | Jellyfin 10.11 baut auf .NET 9; die offizielle Plugin-Vorlage und FastSTRM 3.0.0 führen `net9.0` |
| Paketversionen | `Jellyfin.Controller` und `Jellyfin.Model` in 10.11.5, `ExcludeAssets=runtime` | Plugin-Vorlage des Projekts |
| Benötigtes SDK zum Bauen | .NET SDK 9 (nicht 8) | folgt aus `net9.0` |

Damit ist der letzte offene Punkt zur Version geschlossen. Zwei Aufgaben aus `tasks.md` sind damit erledigt: Version festgestellt, Zielplattform festgelegt.

## Was funktioniert

| Anforderung | Bewertung | Begründung |
|---|---|---|
| Titel erscheint in der Bibliothek ohne Medium auf Platte | machbar | STRM-Eintrag mit URL |
| Suche über Jellyfin | machbar | Item existiert in der Datenbank, Suche arbeitet darauf |
| Metadaten (Titel, Beschreibung, Cover, Genres) | machbar | animex GraphQL plus AniList GraphQL, Bilder direkt verlinkbar |
| Episodenliste, Staffelstruktur, Nummerierung | machbar | `episodeCount`, `subCount`, `dubCount` aus AnimeNode, Episoden-URL-Muster bekannt |
| Gesehene Episoden werden geloggt | machbar | Jellyfin-Standardverhalten, kein Plugin-Anteil nötig |
| Fortsetzen an letzter Position | machbar | dito, sofern der Stream seekbar ist (HLS mit Byte-Range oder Segmenten) |
| Mehrere Benutzer | machbar | betrifft nur die Bibliotheksfreigabe, Plugin bleibt benutzerneutral |
| Betrieb ohne Internet | eingeschränkt | Bibliothek bleibt lesbar, Abspielen unmöglich; kein Datenverlust |
| Ausfall von animex | eingeschränkt | bestehende Einträge bleiben, Wiedergabe schlägt fehl, Neusynchronisation überschreibt nichts |

## Risiken

1. **Auflösung der Stream-URL.** Größtes Risiko. Die Quellen kommen über die Player-Infrastruktur, nicht über eine dokumentierte API. Der Auflöser muss in einem klar abgegrenzten Modul liegen, damit ein Bruch an einer Stelle repariert wird. Erwartung: Wartung in unregelmäßigen Abständen, nicht dauerhaft.
2. **Cloudflare.** Der Jellyfin-Server muss die Player-Domains erreichen dürfen. Auf dem Zielsystem zu prüfen: HTTP-Status und Ladezeit von `megaplay.buzz` und der ausgelieferten Playlist aus Sicht des Servers, nicht aus Sicht des Browsers.
3. **Startlatenz und Scan-Dauer.** ffprobe über das Netz pro Folge. Bei einer großen Bibliothek wird der erste Scan lang. Gegenmaßnahme im Design: begrenzte Parallelität, optional FastSTRM als zweiter Schritt.
4. **Untertitel.** HLS-Untertitel sind meist WebVTT und werden vom Client gerendert. Erzwungene Untertitel und ASS-Styling sind Glückssache. Das ist ein Qualitäts-, kein Machbarkeitsthema.
5. **Animex-IDs können sich ändern.** Deshalb speichert das Plugin die Zuordnung animex-ID zu AniList-ID in einer eigenen Datei und erzeugt bei Konflikten neue Einträge statt bestehende zu überschreiben.
6. **Recht und Verbreitung.** Geklärt: privater Betrieb auf dem eigenen NAS, keine Weitergabe. Damit bleibt das Risiko beim Betreiber und die Verbreitung fremder Inhalte ist kein Thema. Das Repository bleibt privat, das Plugin wird nicht öffentlich beworben. Unverändert gilt: keine Umgehung von Bezahlschranken, kein Vorhalten von Videomaterial.

## Offene Prüfungen vor der Implementierung

1. Erreichbarkeit der Player-Domains vom NAS heraus, mit Statuscodes und Antwortzeiten.
2. Ein echter Auflösungsversuch für eine Folge, um zu sehen, welche URL-Form am Ende herauskommt (Playlist mit Segmenten oder Einzeldatei) und ob die Wiedergabe im Client seekbar ist.
3. .NET SDK 9 auf der Entwicklungsmaschine, sonst kein Build gegen `net9.0`.

## Empfehlung

Ausbauen, aber in dieser Reihenfolge: erst Katalog und Metadaten bis zur lauffähigen Bibliothek, dann die Quellenauflösung für eine einzelne Serie, dann Massensynchronisation. So steht nach dem ersten Schritt eine sichtbare, nützliche Bibliothek und die riskante Stelle lässt sich isoliert bewerten.
