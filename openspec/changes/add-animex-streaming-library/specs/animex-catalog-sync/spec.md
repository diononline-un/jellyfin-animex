## Purpose

Liest den Anime-Katalog aus dem animex-Backend, reichert ihn über AniList an und legt daraus eine Jellyfin-Bibliothek an: pro Folge eine STRM-Verweisdatei mit den zugehörigen NFO-Metadaten, ohne Videomaterial lokal zu halten.

## ADDED Requirements

### Requirement: Katalogaufnahme aus dem animex-Backend

Das Plugin SHALL den Anime-Katalog über die GraphQL-Schnittstelle von animex (`catalogAnime`, `anime`, `searchAnime`) beziehen. Der Bezug SHALL ohne Browser und ohne Ausführung von JavaScript auf der Gegenseite auskommen.

#### Scenario: Katalog wird vollständig gelesen

- **GIVEN** eine erreichbare animex-GraphQL-Schnittstelle und eine Titelgrenze von 200
- **WHEN** ein Synchronisationslauf gestartet wird
- **THEN** liegen bis zu 200 Katalogeinträge mit animex-Titel-ID vor
- **AND** der Lauf meldet die Anzahl gelesener Einträge

#### Scenario: Schnittstelle antwortet nicht

- **GIVEN** die animex-GraphQL-Schnittstelle ist nicht erreichbar
- **WHEN** ein Synchronisationslauf gestartet wird
- **THEN** bricht der Lauf mit einer Fehlermeldung ab
- **AND** bereits vorhandene Bibliothekseinträge bleiben unverändert

### Requirement: Anreicherung über AniList

Zu jedem Katalogeintrag SHALL das Plugin Metadaten über die AniList-GraphQL-API beziehen und dabei über `anilistId` verknüpfen. AniList SHALL die Quelle für Beschreibung, Cover, Banner, Genres, Studios, Bewertung und Erscheinungsdaten sein.

#### Scenario: Titel mit AniList-Verknüpfung

- **GIVEN** ein Katalogeintrag mit gesetztem `anilistId`
- **WHEN** der Eintrag angereichert wird
- **THEN** enthält der Bibliothekseintrag Beschreibung, Cover-Adresse, Genres und Erscheinungsjahr aus AniList

#### Scenario: Titel ohne AniList-Verknüpfung

- **GIVEN** ein Katalogeintrag ohne `anilistId`
- **WHEN** der Eintrag verarbeitet wird
- **THEN** verwendet das Plugin die Felder aus dem animex-Katalog als Rückfall
- **AND** der Lauf vermerkt den Eintrag als nicht angereichert

### Requirement: Ableitung der Folgen

Das Plugin SHALL die Folgenanzahl aus `episodeCount` sowie je Fassung aus `subCount` und `dubCount` ableiten und daraus je Folge einen Bibliothekseintrag erzeugen.

#### Scenario: Titel mit beiden Fassungen

- **GIVEN** ein Titel mit `subCount` 12 und `dubCount` 12
- **WHEN** die Folgen abgeleitet werden
- **THEN** entstehen je Folge zwei unterscheidbar benannte Einträge, einer für Untertitel und einer für Synchron

#### Scenario: Titel mit nur einer Fassung

- **GIVEN** ein Titel mit `subCount` 12 und `dubCount` 0
- **WHEN** die Folgen abgeleitet werden
- **THEN** entstehen 12 Einträge ohne Fassungskennzeichnung im Namen

### Requirement: Bibliotheksartefakte je Folge

Pro Folge SHALL das Plugin eine `.strm`-Datei mit der Wiedergabe-URL sowie eine NFO-Datei mit Titel, Beschreibung, Cover-Adresse und Episodennummer schreiben. Das Plugin SHALL kein Videomaterial speichern.

#### Scenario: Dateien werden angelegt

- **GIVEN** ein konfigurierter Bibliothekspfad und ein Titel mit 12 Folgen
- **WHEN** der Schreibvorgang abgeschlossen ist
- **THEN** existieren 12 STRM-Dateien und 12 NFO-Dateien
- **AND** keine Datei enthält Videodaten

#### Scenario: Kein Bibliothekspfad konfiguriert

- **GIVEN** kein Bibliothekspfad ist konfiguriert
- **WHEN** ein Synchronisationslauf gestartet wird
- **THEN** schreibt das Plugin keine Datei
- **AND** der Lauf endet mit einem Hinweis auf die fehlende Konfiguration

### Requirement: Filterbare Synchronisation

Das Plugin SHALL folgende Eingrenzungen anwenden: Sprache (nur Untertitel, nur Synchron, beides), Genre, Erscheinungsjahr, Mindestbewertung, Höchstzahl der Titel und Ausschluss erwachsener Inhalte. Die Filter SHALL bei jedem Lauf auf den Katalog angewandt werden.

#### Scenario: Filter grenzt den Lauf ein

- **GIVEN** ein Filter auf Genre Action und Erscheinungsjahr ab 2020
- **WHEN** ein Synchronisationslauf läuft
- **THEN** enthält die Bibliothek nur Titel, die beide Bedingungen erfüllen

#### Scenario: Geänderter Filter wirkt beim nächsten Lauf

- **GIVEN** ein Lauf mit Filter auf einen Bestand von 30 Titeln
- **WHEN** der Filter auf einen Bestand von 50 Titeln erweitert und erneut gestartet wird
- **THEN** enthält die Bibliothek 50 Titel ohne Duplikate

### Requirement: Wiederholbare und wiederaufsetzbare Läufe

Ein Lauf SHALL wiederholbar sein, ohne Duplikate zu erzeugen. Nach einem Abbruch SHALL der nächste Lauf die bereits erledigten Einträge überspringen und die offenen abschließen.

#### Scenario: Zweiter Lauf erzeugt keine Duplikate

- **GIVEN** ein abgeschlossener Lauf mit 50 Titeln
- **WHEN** derselbe Lauf ohne Katalogänderung erneut läuft
- **THEN** bleibt die Anzahl der Bibliothekseinträge gleich
- **AND** der Lauf meldet die Einträge als übersprungen

#### Scenario: Abbruch mitten im Lauf

- **GIVEN** ein Lauf, der nach 20 von 50 Titeln abgebrochen wurde
- **WHEN** der Lauf erneut gestartet wird
- **THEN** werden die 20 vorhandenen Einträge nicht erneut geschrieben
- **AND** die verbleibenden 30 Titel werden angelegt

### Requirement: Umgang mit verwaisten Einträgen

Verschwindet ein Titel aus dem animex-Katalog, SHALL das Plugin den zugehörigen Bibliothekseintrag im Bericht als verwaist melden. Das Löschen SHALL nur bei ausdrücklich aktivierter Einstellung erfolgen.

#### Scenario: Titel verschwindet aus dem Katalog

- **GIVEN** eine Bibliothek mit einem Titel, der im aktuellen Katalog fehlt, und deaktiviertem Löschen
- **WHEN** ein Lauf ausgeführt wird
- **THEN** bleibt der Bibliothekseintrag bestehen
- **AND** der Bericht nennt ihn als verwaist

#### Scenario: Löschen ist aktiviert

- **GIVEN** aktiviertes Löschen verwaister Einträge und ein fehlender Titel
- **WHEN** ein Lauf ausgeführt wird
- **THEN** entfernt das Plugin die zugehörigen STRM- und NFO-Dateien
- **AND** der Bericht nennt die Anzahl entfernter Einträge

### Requirement: Nachvollziehbarer Laufbericht

Jeder Lauf SHALL die Anzahl gefundener Titel, erzeugter Einträge, übersprungener Einträge und Fehler je Titel festhalten. Der Bericht SHALL auf der Plugin-Seite einsehbar sein.

#### Scenario: Bericht nach einem Lauf

- **GIVEN** ein abgeschlossener Lauf mit einem fehlerhaften Titel
- **WHEN** die Plugin-Seite geöffnet wird
- **THEN** sind Zeitpunkt, Anzahl gefundener Titel, erzeugter Einträge, übersprungener Einträge und der fehlerhafte Titel mit Fehlergrund sichtbar

#### Scenario: Geplanter Lauf

- **GIVEN** ein aktivierter Zeitplan mit täglicher Ausführung
- **WHEN** der Zeitpunkt erreicht ist
- **THEN** läuft die Synchronisation ohne weitere Eingabe
- **AND** das Ergebnis erscheint im Bericht
