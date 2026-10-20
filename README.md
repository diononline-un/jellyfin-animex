# jellyfin-animex

Experimentelles Jellyfin-Plugin, das einen externen Anime-Katalog als
Jellyfin-Bibliothek darstellt. Es speichert keine Videodateien. Jellyfin
erhält pro Folge eine kleine Verweisdatei und löst die Wiedergabe beim
Abspielen neu auf.

Privates Projekt, keine offizielle Integration von animex.one, keine
Gewährleistung und kein Support für Dritte.

## Idee

Jellyfin ist eine gute Oberfläche für Bibliotheken, Suche, Fortsetzen,
Gesehen-Status und Clients wie Swiftfin. Der Katalog liegt aber nicht als
lokale Dateisammlung vor, sondern als externer, sich ändernder Bestand.

Das Plugin verbindet beides:

- Jellyfin bleibt die Bibliothek und der Player.
- Der Katalog bleibt die Quelle der Wahrheit.
- Es wird kein Videomaterial vorgehalten oder weitergegeben.
- Die Wiedergabe erfolgt nur auf Anforderung.

## Lösung

1. **Katalog lesen**
   Das Plugin liest Titel und Folgenzahlen aus dem GraphQL-Katalog. Ein
   Ledger hält fest, welche Titel bereits geschrieben wurden. Läufe sind
   damit wiederaufsetzbar.

2. **Metadaten anreichern**
   Titel, Cover, Beschreibung und Genres kommen aus dem Katalog und werden
   über AniList ergänzt. Die Bibliothek erhält lokale NFO-Dateien.

3. **Verweise schreiben**
   Pro Folge entsteht eine `.strm`-Datei, die auf den Plugin-Endpunkt zeigt,
   nicht auf die Anbieteradresse. Dazu kommen `.nfo`-Dateien für Jellyfin.
   Es wird kein Video gespeichert.

4. **Wiedergabe auflösen**
   Beim Abspielen fragt das Plugin die aktuelle Quellen-Schnittstelle des
   Players ab. Kurzlebige Tokens werden dadurch nicht in der Bibliothek
   gespeichert.

5. **HLS durchreichen**
   Der Client erhält eine Playlist mit Proxy-Adressen auf dem Jellyfin-Server.
   Segmente werden bei Bedarf geladen und durchgereicht. Das ist ein
   Reverse Proxy, kein Redirect. Der Client spricht nie direkt mit dem CDN.

6. **Jellyfin indexiert**
   Jellyfin behandelt die Verweise wie normale Medien. Suche, Fortsetzen,
   Gesehen-Status und Client-Kompatibilität laufen über Jellyfin.

## Betrieb

- Zielsystem: Jellyfin 12.2.0, Plugin-Ziel `TargetAbi` 12.2.0.0, `net10.0`.
- Bauen mit `./build.sh`, Ergebnis `dist/Jellyfin.Plugin.Animex_0.1.0.zip`.
- Installation über Jellyfin, Plugins, „Aus Datei installieren“.
- Eigener Bibliotheksordner, getrennt von bestehenden Bibliotheken.
- Bibliothekstyp Serien, Metadatenleser ausschließlich NFO, externe Anbieter
  deaktiviert.
- Trockenlauf zuerst, danach Sync und Jellyfin-Scan.

Details: `docs/installation.md`.

## Grenzen

- **Keine Offline-Medien.** Eine `.strm` ist ein Zeiger. Ohne erreichbare
  Quelle ist die Folge nicht abspielbar.
- **Quellenabhängigkeit.** Anbieter, Tokens, CDN-Regeln und Player-Schnittstellen
  können sich jederzeit ändern.
- **Kein Transcoding durch das Plugin.** Jellyfin kann transkodieren, das
  Plugin liefert HLS.
- **Keine Garantie auf Metadaten.** Katalog und AniList können Lücken haben.
- **Scan-Kosten.** Viele Folgen bedeuten viele kleine Dateien und einen
  langsamen ersten Jellyfin-Scan.
- **Client-Grenzen.** Manche Clients unterstützen `.strm` und HLS nur
  eingeschränkt. Swiftfin ist kein Garant für jede Quelle.
- **Keine native Suche in Jellyfin.** Das Plugin kann keine eigenen Treffer
  in die Jellyfin-Suche einspeisen. Es schreibt Bibliothekseinträge.

## Risiken

- **ToS der Quelle.** Automatisierter Katalogabruf und Auflösung über
  inoffizielle Schnittstellen können gegen Nutzungsbedingungen verstoßen.
- **Urheberrecht.** Das Plugin speichert keine Videos, aber es verweist auf
  externe Quellen. Ob deren Nutzung zulässig ist, hängt von der Quelle und
  der Jurisdiktion ab. Keine Rechtsberatung.
- **Keine Weitergabe.** Das Projekt ist für privaten Betrieb gedacht. Keine
  öffentliche Instanz, keine Keys im Repository, keine kommerzielle Nutzung
  ohne schriftliche Erlaubnis.
- **API-Limits.** Katalog und AniList drosseln. Ein vollständiger Lauf kann
  Tage dauern und jederzeit abbrechen.
- **Sicherheit.** Der Proxy lädt entfernte Inhalte. Erlaubte Wirte sind
  begrenzt, trotzdem bleibt ein Netzwerkdienst mit URL-Eingabe ein Risiko.
- **Keine Gewährleistung.** Fehler, Ausfälle und Datenverlust sind möglich.

## Optimierungen

### Seed-Bibliothek

Ein beim Release mitgeliefertes Archiv aus `.strm`- und `.nfo`-Dateien könnte
den Erstaufbau abkürzen. Statt den gesamten Katalog neu zu schreiben, entpackt
die Installation den Seed und gleicht nur Abweichungen ab. Der Seed enthält
keine Videos und keine Tokens, nur relative Verweise auf den lokalen
Plugin-Endpunkt.

### Delta-Sync und Linkprüfung

Die `.strm`-Links zeigen auf den eigenen Server und bleiben gültig, solange
der Titel im Katalog existiert. Eine Prüfung aller Folgen gegen die Quelle
wäre teuer und würde viele Anfragen erzeugen. Sinnvoller:

- Katalog-IDs und Titel gegen den aktuellen Bestand abgleichen.
- Nur neue, entfernte oder geänderte Titel anfassen.
- Erreichbarkeit stichprobenartig oder erst beim Abspielen prüfen.
- Keine Segment- oder Token-Prüfung im Hintergrund.

### Backfill

Ein Seed plus Ledger erlaubt einen gestaffelten Betrieb: Basis sofort,
ältere Jahrgänge im Hintergrund, neue Titel täglich. Der Ledger verhindert
doppelte Arbeit. Ein Kataloglauf bleibt trotzdem nötig, solange die API
keinen `updatedSince`-Filter anbietet.

### Weitere Punkte

- Abgebrochene Segment-Requests nicht als Fehler loggen.
- Anbieter-Fallback mit kurzem Health-Check.
- Scan-Zeitfenster außerhalb der Wiedergabe.
- Bibliotheksoptionen streng auf NFO begrenzen, keine virtuellen Folgen.

## Entwicklung

```bash
./build.sh test    # Tests
./build.sh         # Tests, Build, ZIP
```

Aufbau:

- `Catalogue/` Katalog und AniList
- `Playback/` Auflösung und Proxy
- `Library/` Dateien und Ledger
- `Sync/` Ablauf und Aufgabe
- `Api/` Wiedergabe-Endpunkt
- `Configuration/` Einstellungen

## Status

Prototyp. Auf einem privaten NAS installiert und mit Jellyfin 12.2.0
getestet. Wiedergabe, Proxy und Bibliothek funktionieren, solange die
Quellen-Schnittstelle erreichbar ist. Der vollständige Katalog ist deutlich
größer als ursprünglich dokumentiert. Ein Teilbestand ist nutzbar, der
restliche Backfill läuft getrennt.

Kein öffentliches Release, keine Zusage für Stabilität oder rechtliche
Zulässigkeit.
