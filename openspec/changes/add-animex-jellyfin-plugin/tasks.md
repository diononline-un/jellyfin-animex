## 1. Gerüst und Bauweg

- [x] 1.1 Projektmappe mit Plugin- und Testprojekt angelegt (`jellyfin-animex.sln`), Ziel `net9.0`, `TargetAbi` 10.11.0.0. Verifikation: `./build.sh test` läuft durch.
- [x] 1.2 Bauweg über Container eingerichtet (`build.sh`: SDK-Container für Bau und Tests, Alpine-Container für das ZIP), weil kein SDK auf der Maschine installiert ist. Verifikation: Paket `dist/Jellyfin.Plugin.Animex_0.1.0.zip` enthält DLL, Abhängigkeitsdatei und Symboldatei.
- [x] 1.3 Plugin-Beschreibung für die Plugin-Verwaltung angelegt (`build.yaml`). Verifikation: Datei nennt Kennung, Version, TargetAbi und Zielframework.

## 2. Katalog und Metadaten

- [x] 2.1 GraphQL-Client für den Katalog mit Paging, Titelfilter und Fehlerabbildung. Verifikation: Abfrage live gegen `graphql.animex.one` geprüft, 3283 Titel, Filter greifen.
- [x] 2.2 Auswertung der Katalogantwort mit Rückfall bei Bildfeldern (Zeichenkette oder Objekt) und bei der Folgenzahl. Verifikation: Live-Antworten beider Formen verarbeitet.
- [x] 2.3 AniList-Anreicherung über `anilistId` mit Rückfall auf die Animex-Felder. Verifikation: Abfrage liefert Beschreibung, Cover, Genres, Bewertung, Jahr.
- [x] 2.4 Filter für Fassung, Jahr, Mindestbewertung, Genres und erwachsene Inhalte. Verifikation: Filter live angewandt, Ergebnis eingegrenzt.
- [x] 2.5 Einheitentests für Fassungs- und Folgenlogik. Verifikation: `./build.sh test` grün, 23 Tests.

## 3. Auflöser

- [x] 3.1 Schnittstelle und Ergebnisobjekt des Auflösers. Verifikation: Einheitentests prüfen Adressbildung und Auswertung der Antwort.
- [x] 3.2 Auflösungskette live vermessen und dokumentiert (`docs/feasibility.md`). Verifikation: Player-Schnittstelle liefert die Folgenkennung, Playlist liegt hinter der Einbettung, Messung mit Container-Browser belegt.
- [x] 3.3 Ehrliches Ergebnis bei nicht ermittelbarer Playlist statt Ausnahme. Verifikation: Bibliothekseintrag entsteht, Bericht nennt den Grund, Zähler für Auflösungsfehler steigt.
- [x] 3.4 Externer Auflöser als Erweiterungspunkt (JSON hinein, JSON heraus). Verifikation: Fehlerfälle werden als nicht aufgelöst gemeldet, Rückgabecode im Protokoll.

## 4. Bibliothek

- [x] 4.1 Ordner- und Dateinamenschema nach Jellyfin-Konvention, unzulässige Zeichen ersetzt. Verifikation: Einheitentests für beide Fassungsfälle und für Sonderzeichen.
- [x] 4.2 Verweisdateien schreiben. Verifikation: Datei enthält genau eine Adresse, keine Mediendaten.
- [x] 4.3 Metadatendateien schreiben (`tvshow.nfo` je Titel, `.nfo` je Folge) mit Titel, Beschreibung, Cover, Genres und Nummer. Verifikation: Einheitentests der Namensbildung, Aufbau beim Praxistest zu prüfen.
- [x] 4.4 Bestandsdatei mit Herkunft und Folgen. Verifikation: Einheitentests für Anlegen, Wiederverwenden, Speichern und Lesen, dazu Verhalten bei beschädigter Datei.
- [x] 4.5 Schutzprüfung gegen fremde Medien im Zielordner. Verifikation: Prüfung bricht den Lauf mit Begründung ab.
- [x] 4.6 Verwaiste Titel melden, Löschen nur bei aktivierter Einstellung. Verifikation: im Ablauf umgesetzt, Bericht nennt die Anzahl.

## 5. Plugin, Oberfläche und Task

- [x] 5.1 Plugin-Grundgerüst mit Konfiguration und Konfigurationsseite. Verifikation: Seite bietet Bibliothekspfad, Serveradresse, Fassung, Filter, Trockenlauf, Schutzprüfung und Anbieter.
- [x] 5.2 Geplanter Task mit täglicher Vorgabe, manuell auslösbar, mit Fortschritt. Verifikation: Task in der Kategorie Animex, Vorgabe 04:00 Uhr.
- [x] 5.3 Laufbericht als Datei und im Protokoll. Verifikation: Bericht nennt Zeitpunkt, Mengen, Fehler und Notizen.
- [x] 5.4 Wiedergabe-Endpunkt und Weiterleitung. Verifikation: Endpunkt löst neu auf, weist fremde Wirte mit 403 ab, schreibt Playlists um.

## 6. Verifikation auf dem Zielsystem

- [ ] 6.1 Paket auf das NAS bringen und Plugin installieren. Verifikation: Plugin erscheint in der Liste ohne Plattformwarnung.
- [ ] 6.2 Eigenen Bibliotheksordner anlegen und als getrennte Bibliothek einbinden. Verifikation: neue Bibliothek erscheint, bestehende bleiben unverändert.
- [ ] 6.3 Ersten Lauf im Trockenlauf, danach mit Schreiben. Verifikation: mindestens 25 Titel erscheinen mit Titel, Cover und Beschreibung.
- [ ] 6.4 Eine Folge abspielen. Verifikation: Wiedergabe läuft mindestens fünf Minuten, Untertitel zuschaltbar.
- [ ] 6.5 Fortsetzen und Gesehen-Status prüfen. Verifikation: Wiedergabe setzt fort, Folge steht in der Historie.
- [ ] 6.6 Zweiten Lauf ausführen. Verifikation: keine Duplikate, übersprungene Einträge im Bericht.
- [x] 6.7 Entscheiden, ob der Auflöser nachgezogen werden muss; erledigt 2026-10-05: ja, sonst ist keine Wiedergabe möglich. Messwerte und Grund stehen in `docs/feasibility.md`.

## 7. Abschluss

- [x] 7.1 Ergebnisse in `docs/feasibility.md` und `docs/installation.md` nachgetragen; erledigt 2026-10-05: Abschnitte „Nachbau in C#: gemessener Stand" und „Lokale Ende-zu-Ende-Prüfung" mit Messwerten, bekannte Einschränkungen in der Anleitung.
- [ ] 7.2 `PROJECT_STATUS.md` auf `done` setzen, Fortschritt 100. Verifikation: Frontmatter stimmt. Offen bis zur Wiedergabe auf dem NAS.
