## Purpose

Regelt das Verhältnis des Plugins zur Jellyfin-Instanz: Wiedergabestatus bleibt bei Jellyfin, die Bibliothek bleibt bei Ausfall der Gegenstelle nutzbar, Schreibzugriffe bleiben auf den konfigurierten Pfad begrenzt, und der Rückbau hinterlässt keine Reste.

## ADDED Requirements

### Requirement: Wiedergabestatus gehört Jellyfin

Das Plugin SHALL keinen eigenen Wiedergabestatus speichern. Gesehene Folgen, Fortsetzungsposition, Favoriten und Wiedergabezähler SHALL ausschließlich über Jellyfins eigene Mechanismen geführt werden.

#### Scenario: Folge wird als gesehen markiert

- **GIVEN** ein Bibliothekseintrag aus einer STRM-Datei
- **WHEN** ein Benutzer die Folge vollständig ansieht
- **THEN** erscheint sie in der Benutzerhistorie als gesehen
- **AND** das Plugin hat dazu keinen eigenen Zustand geschrieben

#### Scenario: Fortsetzen nach Abbruch

- **GIVEN** eine teilweise angesehene Folge
- **WHEN** der Benutzer die Wiedergabe erneut startet
- **THEN** beginnt die Wiedergabe an der zuletzt gespeicherten Position

### Requirement: Nutzbarkeit bei Ausfall der Gegenstelle

Ist animex nicht erreichbar, SHALL die Bibliothek weiterhin angezeigt, durchsucht und gefiltert werden können. Nur die Wiedergabe SHALL fehlschlagen.

#### Scenario: Gegenstelle nicht erreichbar

- **GIVEN** eine bestehende Bibliothek und eine nicht erreichbare Gegenstelle
- **WHEN** die Bibliothek in Jellyfin geöffnet wird
- **THEN** erscheinen Titel, Cover, Beschreibungen und Episodenlisten
- **AND** ein Wiedergabeversuch endet mit einer Fehlermeldung des Clients

### Requirement: Begrenzte Schreibzugriffe

Das Plugin SHALL ausschließlich innerhalb des konfigurierten Bibliothekspfads schreiben. Es SHALL keine bestehende Jellyfin-Bibliothek, keine Konfigurationsdatei der Instanz und keine Systemdatei ändern.

#### Scenario: Schreibvorgang bleibt im Zielordner

- **GIVEN** ein konfigurierter Bibliothekspfad
- **WHEN** ein Synchronisationslauf schreibt
- **THEN** liegen alle erzeugten Dateien innerhalb dieses Pfads
- **AND** die Zeitstempel bestehender Bibliotheksordner bleiben unverändert

#### Scenario: Pfad zeigt auf eine bestehende Bibliothek

- **GIVEN** ein Bibliothekspfad, der bereits Einträge einer anderen Bibliothek enthält, und aktivierte Schutzprüfung
- **WHEN** der Lauf gestartet wird
- **THEN** verweigert das Plugin den Lauf
- **AND** der Bericht nennt den Grund

### Requirement: Sauberer Rückbau

Das Entfernen des Plugins SHALL die Jellyfin-Instanz funktionsfähig hinterlassen. Die erzeugten Bibliotheksdateien SHALL ohne das Plugin gelöscht werden können, ohne weitere Reste.

#### Scenario: Plugin wird entfernt

- **GIVEN** eine laufende Instanz mit installiertem Plugin und befüllter Bibliothek
- **WHEN** das Plugin entfernt und der Bibliotheksordner gelöscht wird
- **THEN** startet Jellyfin ohne Fehler
- **AND** die übrigen Bibliotheken bleiben vollständig erhalten

### Requirement: Sichtbare Konfiguration und Protokollierung

Das Plugin SHALL über die Plugin-Seite konfigurierbar sein: Bibliothekspfad, Fassung, Filter, Qualitätsstufe, Nebenläufigkeit, Umgang mit verwaisten Einträgen und Protokolltiefe. Läufe SHALL sichtbar protokolliert werden.

#### Scenario: Konfiguration wird übernommen

- **GIVEN** die Plugin-Seite mit geänderten Werten für Bibliothekspfad und Qualitätsstufe
- **WHEN** die Einstellungen gespeichert und ein Lauf gestartet wird
- **THEN** verwendet der Lauf die neuen Werte ohne Neustart der Instanz

#### Scenario: Protokoll einer fehlerhaften Auflösung

- **GIVEN** ein Lauf, bei dem die Auflösung für einen Titel fehlschlägt
- **WHEN** das Protokoll eingesehen wird
- **THEN** enthält der Eintrag den Titel, die Episodennummer und den Fehlergrund
