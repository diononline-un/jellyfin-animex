## Context

Der erste Change (`add-animex-streaming-library`) hat Machbarkeit und Anforderungen festgehalten. Bei der Umsetzung kamen drei Erkenntnisse hinzu, die Entscheidungen nötig machten:

1. Die Wiedergabe-Adresse der Gegenstelle trägt ein zeitlich begrenztes Merkmal. Eine Adresse, die beim Synchronisieren ermittelt und fest in die Verweisdatei geschrieben wird, ist beim nächsten Abspielen womöglich abgelaufen.
2. Die Playlist verweist ihre Segmente relativ und setzt eine Herkunftsbeschränkung auf den Player. Ein Client, der die Adresse direkt aufruft, kann daran scheitern.
3. Das öffentliche GraphQL-Schema liefert Bildfelder in zwei Formen (Zeichenkette und Objekt) und die Folgenzahl nicht immer in `episodeCount`.

## Goals / Non-Goals

**Goals:**

- Bibliothekseinträge, die unabhängig vom Ablauf einzelner Adressen gültig bleiben.
- Wiedergabe, die ohne fremde Herkunftsprobleme über die Jellyfin-Instanz läuft.
- Ehrliches Verhalten, wenn die Gegenstelle die Wiedergabe-Adresse nicht herausgibt.
- Nachrüstbarkeit der Auflösung ohne Änderung am Plugin.

**Non-Goals:**

- Kein Kopfloser Browser im Betrieb, auch nicht als Umweg. Bleibt ausgeschlossen.
- Kein Zwischenspeichern von Videomaterial, auch nicht als Zwischenspeicher für die Wiedergabe.
- Keine Umgehung von Zugriffsbeschränkungen der Gegenstelle.
- Kein automatisches Auslösen eines Bibliotheksscans aus dem Plugin heraus.

## Decisions

### D1: Verweisdatei zeigt auf den Plugin-Endpunkt, nicht auf den Anbieter

Die Verweisdatei enthält `{serverAddress}/Animex/Playback/{animexId}/{folge}?language=sub`. Der Endpunkt ermittelt beim Aufruf die aktuelle Wiedergabe-Adresse. Alternative wäre, die beim Lauf ermittelte Adresse direkt zu schreiben: kürzer, aber die Einträge verfallen mit dem Merkmal und müssten ständig neu geschrieben werden. Zusätzlich wäre die Bibliothek dauerhaft falsch, sobald die Gegenstelle ihre Adressen umstellt.

Nebeneffekt: Die Bibliothek bleibt stabil, auch wenn die Auflösung zeitweise nicht funktioniert. Es ändert sich nur das Verhalten beim Abspielen.

### D2: Weiterleitung mit fester Wirteliste

`/Animex/Proxy` leitet Playlist- und Segmentadressen weiter, aber nur für eine feste Liste von Wirtsendungen der Gegenstelle. Playlists werden beim Durchleiten umgeschrieben, Verweise auf den eigenen Endpunkt gesetzt. Alternative wäre, die Adressen unverändert an den Client zu geben: spart Arbeit, scheitert aber an der Herkunftsbeschränkung und bei relativen Verweisen. Die Wirteliste verhindert, dass der Endpunkt als offener Weiterleiter missbraucht wird.

### D3: Bildfelder und Folgenzahl mit Rückfall lesen

Bildfelder werden als Zeichenkette und als Objekt gelesen, die größte vorhandene Größe gewinnt. Die Folgenzahl kommt aus `episodeCount`, mit `subCount` und `dubCount` als Rückfall. Beides ist live beobachtet, nicht vermutet: Bei „Attack on Titan" liefert die Gegenstelle das Bild als Objekt, bei „Demon Slayer" ebenfalls, und `episodeCount` 25 gegen `subCount` 33 zeigt, dass die Zählfelder nicht gleichbedeutend sind.

### D4: Externe Auflösung als Erweiterungspunkt

Ein konfigurierbarer Befehl bekommt den Auftrag als JSON auf der Standardeingabe und antwortet mit der Adresse als JSON. Alternative wäre, die Auflösung fest im Plugin zu erweitern: langsamer im Wartungsfall und an einen Neubau gebunden. Der Erweiterungspunkt ist bewusst nicht Teil der Konfigurationsseite, damit er nicht versehentlich gefüllt wird.

### D5: Kein automatischer Bibliotheksscan aus dem Plugin

Der Task schreibt Dateien, Jellyfin erkennt sie über die Überwachung des Ordners. Alternative wäre, den Bibliotheksdienst aus dem Plugin aufzurufen: koppelt das Plugin an eine interne Schnittstelle und ist für den Nutzen nicht nötig.

### D6: Bau über Container

`build.sh` baut und testet in Containern, weil auf der Entwicklungsmaschine kein .NET SDK liegt und ein systemweites Nachinstallieren an Rechten scheitert. Das Paket entsteht als ZIP mit DLL, Abhängigkeitsdatei und Symboldatei; die Jellyfin-Laufzeitdateien sind bewusst nicht enthalten.

## Risks / Trade-offs

- Auflösung gelingt nur teilweise, weil die Einbettung die Playlist-Adresse nicht im Text nennt → Der Fall ist als Ergebnis vorgesehen, nicht als Fehler. Der Nutzer sieht den Titel, kann ihn aber nicht abspielen, bis ein externer Auflöser nachgezogen ist. Das ist die ehrliche Variante gegenüber einer Behauptung, die nicht hält.
- Weiterleitung leitet fremden Verkehr weiter, wenn die Wirteliste zu weit gefasst wird → Liste bewusst eng, Ablehnung mit 403, Protokolleintrag.
- Der Plugin-Endpunkt ist ohne Anmeldung erreichbar → Bewusste Entscheidung, weil Clients die Adresse ohne Sitzung aufrufen. Er gibt nur weiter, was ohnehin öffentlich abrufbar ist, und nur für freigegebene Wirte.
- Wiedergabe über die Instanz kostet Serverbandbreite → Für einen Heimserver unkritisch; Alternative wäre Direct Play mit Herkunftsproblemen.
- Erster Scan langsam, weil jede Folge geprüft wird → Bekannt, im ersten Change als eigener späterer Schritt vermerkt.

## Migration Plan

1. Paket bauen: `./build.sh`, Ergebnis `dist/Jellyfin.Plugin.Animex_0.1.0.zip`.
2. Auf dem NAS installieren (Dashboard, Plugins, Aus Datei installieren), Instanz neu starten.
3. Eigenen Bibliotheksordner anlegen, neue Bibliothek mit Metadatenleser NFO darauf zeigen lassen.
4. Plugin konfigurieren, zuerst mit Trockenlauf.
5. Lauf mit Schreiben, Bibliothek scannen, eine Folge abspielen.

Rückbau: Plugin deinstallieren, Bibliothek entfernen, Ordner löschen. Bestehende Bibliotheken bleiben unberührt, weil das Plugin sie nie beschreibt.

## Open Questions

- Ob die Einbettung die Playlist-Adresse in weiteren Fällen direkt nennt oder ob die Regel lautet, dass es nie der Fall ist: erst nach Praxistests mehrerer Titel zu beurteilen.
- Ob FastSTRM als zweiter Schritt nötig ist: abhängig von den gemessenen Startzeiten auf dem NAS.
- Ob die Weiterleitung für alle liefernden Wirte ausreicht: die Liste wächst mit den Messungen.
