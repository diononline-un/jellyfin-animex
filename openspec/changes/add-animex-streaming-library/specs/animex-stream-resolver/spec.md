## Purpose

Ermittelt zu einem Animex-Titel, einer Episodennummer und einer Fassung die tatsächlich abspielbare Wiedergabe-URL samt Typ und Untertitelspuren. Diese Fähigkeit ist bewusst abgegrenzt, weil die Gegenseite hier keine dokumentierte Schnittstelle anbietet und Änderungen zuerst an dieser Stelle wirken.

## ADDED Requirements

### Requirement: Definierte Auflösungsschnittstelle

Der Auflöser SHALL als eigenständige Einheit mit festgelegter Ein- und Ausgabe vorliegen: Eingabe sind animex-Titel-ID, Episodennummer und Fassung, Ausgabe sind Wiedergabe-URL, Medientyp und Liste der Untertitelspuren. Katalog- und Bibliothekslogik SHALL den Auflöser nur über diese Schnittstelle benutzen.

#### Scenario: Auflösung liefert eine URL

- **GIVEN** eine gültige animex-Titel-ID, eine vorhandene Episodennummer und die Fassung Untertitel
- **WHEN** die Auflösung angefordert wird
- **THEN** liefert sie eine Wiedergabe-URL mit Typangabe
- **AND** die Liste der Untertitelspuren, soweit die Quelle sie anbietet

#### Scenario: Fassung ist nicht vorhanden

- **GIVEN** eine Titel-ID, für die nur eine Fassung existiert, und eine Anfrage für die andere Fassung
- **WHEN** die Auflösung angefordert wird
- **THEN** meldet der Auflöser die fehlende Fassung als eigenes Ergebnis, nicht als allgemeinen Fehler

### Requirement: Ausfall ohne Schaden für die Bibliothek

Schlägt die Auflösung fehl, SHALL der zugehörige Bibliothekseintrag unverändert bestehen bleiben. Der Fehler SHALL dem Titel zugeordnet protokolliert werden, und die Verarbeitung der übrigen Titel SHALL fortgesetzt werden.

#### Scenario: Quelle nicht erreichbar

- **GIVEN** eine unerreichbare Player-Gegenstelle
- **WHEN** ein Synchronisationslauf Folgen auflöst
- **THEN** bleiben bereits vorhandene STRM-Dateien unverändert
- **AND** der Bericht nennt die betroffenen Titel mit Fehlergrund
- **AND** Titel mit erfolgreicher Auflösung werden weiterhin verarbeitet

#### Scenario: Vorhandene URL bleibt nutzbar

- **GIVEN** eine bereits geschriebene STRM-Datei mit einer früher ermittelten URL
- **WHEN** eine spätere Auflösung für denselben Eintrag fehlschlägt
- **THEN** wird die vorhandene STRM-Datei nicht geleert und nicht überschrieben

### Requirement: Entkopplung von der Gegenstelle

Der Auflöser SHALL die Adressen der Player-Infrastruktur über die Konfiguration beziehen können, damit ein Wechsel der Gegenstelle ohne Änderung an Katalog- und Bibliothekslogik möglich ist.

#### Scenario: Adresse wird geändert

- **GIVEN** eine geänderte Adresse der Player-Infrastruktur in der Konfiguration
- **WHEN** die nächste Auflösung angefordert wird
- **THEN** verwendet der Auflöser die neue Adresse
- **AND** Katalog und Bibliotheksbestand bleiben unverändert
