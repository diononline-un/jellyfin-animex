## ADDED Requirements

### Requirement: Getrennte Bibliothek

Das Plugin SHALL ausschließlich in einen eigenen Bibliotheksordner schreiben, der von allen bestehenden Bibliotheken getrennt ist. Findet es dort Videodateien, die es nicht selbst angelegt hat, SHALL es den Lauf abbrechen, solange die Schutzprüfung aktiviert ist.

#### Scenario: Eigener Ordner wird befüllt

- **GIVEN** ein Bibliothekspfad außerhalb aller bestehenden Bibliotheken
- **WHEN** ein Lauf schreibt
- **THEN** liegen alle Dateien in diesem Ordner
- **AND** die Ordner der bestehenden Bibliotheken bleiben unverändert

#### Scenario: Fremde Medien im Zielordner

- **GIVEN** ein Bibliothekspfad, in dem bereits Videodateien liegen, die das Plugin nicht angelegt hat
- **WHEN** ein Lauf gestartet wird
- **THEN** bricht das Plugin den Lauf mit Begründung ab
- **AND** es wird keine Datei geschrieben

### Requirement: Nachweisbarer Bestand

Das Plugin SHALL den geschriebenen Bestand in einer eigenen Datei im Bibliotheksordner führen, mit Kennung, AniList-Kennung, Titel, Ordnername, Folgenzahl und je Folge Dateiname und ermittelte Adresse.

#### Scenario: Bestand wird geführt

- **GIVEN** ein abgeschlossener Lauf mit einem Titel und zwölf Folgen
- **WHEN** der Bibliotheksordner eingesehen wird
- **THEN** enthält die Bestandsdatei den Titel mit zwölf Folgen
- **AND** jede Folge nennt Dateiname und Auflösungszeitpunkt

#### Scenario: Bestand ist unlesbar

- **GIVEN** eine beschädigte Bestandsdatei
- **WHEN** ein Lauf gestartet wird
- **THEN** beginnt das Plugin mit einem leeren Bestand, statt abzubrechen
- **AND** vorhandene Dateien werden nicht gelöscht
