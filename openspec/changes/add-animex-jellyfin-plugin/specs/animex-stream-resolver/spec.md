## ADDED Requirements

### Requirement: Beobachtetes Verhalten der Gegenstelle

Der Auflöser SHALL die Kennung der Folge über die Schnittstelle des Players beziehen (`/api/anmx/{anilistId}/{episode}`) und aus deren Antwort lesen. Der Auflöser SHALL ohne Kopflosen Browser und ohne Auswertung nachgeladener Skripte arbeiten.

#### Scenario: Kennung wird gelesen

- **GIVEN** eine AniList-Kennung und eine Folgennummer, für die die Gegenstelle eine Kennung liefert
- **WHEN** die Auflösung angefordert wird
- **THEN** steht die Kennung im Ergebnis
- **AND** im Protokoll erscheint kein Hinweis auf einen Browser

#### Scenario: Gegenstelle liefert keine Kennung

- **GIVEN** eine nicht erreichbare Player-Schnittstelle
- **WHEN** die Auflösung angefordert wird
- **THEN** wird die Auflösung als nicht erfolgreich gemeldet
- **AND** die Verarbeitung der übrigen Folgen läuft weiter

### Requirement: Ehrliche Meldung bei fehlender Wiedergabe-Adresse

Kann der Auflöser die Playlist-Adresse nicht ermitteln, SHALL er das als Ergebnis mit Begründung melden und nicht als Ausnahme. Die Bibliothek SHALL den Eintrag behalten, der Titel SHALL als derzeit nicht abspielbar gelten.

#### Scenario: Playlist nicht ermittelbar

- **GIVEN** eine Folge, deren Einbettung keine Playlist-Adresse nennt
- **WHEN** die Folge verarbeitet wird
- **THEN** entsteht eine Verweis- und eine Metadatendatei
- **AND** der Bericht nennt die Folge mit Begründung als nicht aufgelöst
- **AND** die Anzahl der Auflösungsfehler erhöht sich um eins

### Requirement: Nachrüstbarer externer Auflöser

Das Plugin SHALL die Auflösung über einen konfigurierbaren externen Befehl zulassen. Der Auftrag SHALL als JSON an die Standardeingabe gehen, die Antwort mit der Wiedergabe-Adresse als JSON von der Standardausgabe kommen. Scheitert der Befehl, SHALL das Plugin das als nicht aufgelöst melden.

#### Scenario: Externer Auflöser liefert eine Adresse

- **GIVEN** ein konfigurierter externer Auflöser, der eine Playlist-Adresse zurückgibt
- **WHEN** eine Folge aufgelöst wird
- **THEN** übernimmt das Plugin die gelieferte Adresse
- **AND** die Verweisdatei verweist weiterhin auf den Plugin-Endpunkt

#### Scenario: Externer Auflöser schlägt fehl

- **GIVEN** ein externer Auflöser, der mit einem Fehlercode endet
- **WHEN** eine Folge aufgelöst wird
- **THEN** meldet das Plugin die Folge als nicht aufgelöst
- **AND** der Fehlercode steht im Protokoll
