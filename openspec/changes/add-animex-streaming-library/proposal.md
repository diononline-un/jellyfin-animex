## Why

Animes liegen auf dem NAS als Dateien und kosten Speicherplatz und Pflege. animex.one bietet dieselben Titel bereits gestreamt mit englischen Untertiteln und Synchronfassungen. Es fehlt der Weg, diesen Katalog in Jellyfin zu benutzen, ohne zwei Oberflächen nebeneinander zu betreiben. Die Machbarkeit ist belegt (`docs/feasibility.md`), die Umsetzung fehlt.

## What Changes

- Neues Jellyfin-Plugin `jellyfin-animex`, das den animex-Katalog liest, Metadaten über AniList anreichert und je Folge eine STRM-Verweisdatei plus NFO in einen eigenen Bibliotheksordner schreibt.
- Neuer Auflöser, der aus animex-Titel-ID, Episodennummer und Fassung eine abspielbare Wiedergabe-URL macht. Klar abgegrenzt, weil diese Stelle bei Änderungen der Gegenstelle zuerst bricht.
- Geplanter Jellyfin-Task mit wiederaufsetzbaren, wiederholbaren Läufen und Bericht über Titel, erzeugte und übersprungene Einträge, Fehler.
- Filter für Sprache, Genre, Jahr, Bewertung und Ausschluss erwachsener Inhalte, angewandt beim Lauf.
- Konfigurationsseite mit Bibliothekspfad, Fassung, Qualitätsstufe, Nebenläufigkeit und Verhalten bei verwaisten Einträgen.
- Keine Änderung an bestehenden Jellyfin-Bibliotheken und keine Speicherung von Wiedergabestatus im Plugin.

## Capabilities

### New Capabilities

- `animex-catalog-sync`: Katalog aus dem animex-Backend lesen, über AniList anreichern, Folgen ableiten, Filter anwenden, STRM- und NFO-Dateien schreiben, Läufe wiederaufsetzbar und wiederholbar halten, verwaiste Einträge melden.
- `animex-stream-resolver`: Aus Titel-ID, Episodennummer und Fassung eine abspielbare URL ermitteln, Ausfälle melden ohne die Bibliothek zu beschädigen.
- `animex-jellyfin-integration`: Verhältnis zur Jellyfin-Instanz: Bibliothek bleibt bei Ausfall der Gegenstelle nutzbar, Wiedergabestatus bleibt bei Jellyfin, Schreibzugriffe bleiben auf den konfigurierten Pfad begrenzt, Rückbau ohne Reste.

### Modified Capabilities

Keine. Es handelt sich um ein neues Projekt ohne bestehende Spezifikationen.

## Impact

- Neues Repository `jellyfin-animex`, bisher kein Code vorhanden.
- Zielsystem: Jellyfin **10.11.5** auf einem NAS (bestätigt), `TargetAbi` `10.11.0.0`, Zielframework `net9.0`. Änderung dort beschränkt auf Plugin-Verzeichnis und eine neue Bibliothek. Betrieb ausschließlich privat im eigenen Haushalt.
- Externe Abhängigkeiten: `graphql.animex.one` (Katalog), `graphql.anilist.co` (Metadaten), Player-Infrastruktur von animex (Wiedergabe).
- Entwicklungsvoraussetzung: .NET SDK 9, auf der Entwicklungsmaschine derzeit nicht vorhanden; NAS-Zugang für Verifikation fehlt ebenfalls.
- Betriebsrisiko: Wartungsaufwand beim Auflöser, weil die Player-Gegenstelle keine dokumentierte API anbietet.
