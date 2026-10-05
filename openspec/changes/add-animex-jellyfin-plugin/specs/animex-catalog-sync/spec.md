## ADDED Requirements

### Requirement: Robuste Auswertung der Katalogantwort

Das Plugin SHALL Bildfelder der Katalogantwort sowohl als Zeichenkette als auch als Objekt mit mehreren Größen lesen. Die Folgenzahl SHALL aus `episodeCount` stammen, mit `subCount` und `dubCount` als Rückfall, wenn die Hauptangabe fehlt oder null ist.

#### Scenario: Bildfeld kommt als Objekt

- **GIVEN** ein Katalogeintrag, dessen Bildfeld als Objekt mit den Größen `large` und `medium` geliefert wird
- **WHEN** der Eintrag verarbeitet wird
- **THEN** verwendet das Plugin die größte vorhandene Adresse als Cover

#### Scenario: Folgenzahl fehlt

- **GIVEN** ein Katalogeintrag ohne `episodeCount`, aber mit `subCount` 33 und `dubCount` 25
- **WHEN** die Folgen abgeleitet werden
- **THEN** verwendet das Plugin 25 Folgen als Grundlage

### Requirement: Aussagekräftiger Trockenlauf

Der Trockenlauf SHALL den Katalog vollständig lesen, Filter anwenden und je Titel die vorgesehene Folgenzahl und Fassungen berichten, ohne eine Datei zu schreiben.

#### Scenario: Trockenlauf schreibt nichts

- **GIVEN** ein konfigurierter Bibliothekspfad und aktivierter Trockenlauf
- **WHEN** ein Lauf ausgeführt wird
- **THEN** bleibt der Bibliotheksordner unverändert
- **AND** der Bericht nennt je Titel die vorgesehene Folgenzahl und die Fassungen

### Requirement: Wiedergabe-Endpunkt des Plugins

In der Verweisdatei SHALL die Adresse des Plugins stehen, nicht die Adresse des Anbieters. Der Endpunkt SHALL beim Aufruf die Wiedergabe-Adresse neu ermitteln und die Playlist an den Client ausliefern.

#### Scenario: Verweis bleibt gültig

- **GIVEN** eine Verweisdatei mit der Adresse `/Animex/Playback/{id}/{folge}`
- **WHEN** die Folge Wochen später abgespielt wird
- **THEN** ermittelt das Plugin die Wiedergabe-Adresse erneut
- **AND** die Wiedergabe beginnt, ohne dass die Bibliothek neu geschrieben werden musste

#### Scenario: Auflösung schlägt fehl

- **GIVEN** eine erreichbare Jellyfin-Instanz und eine nicht ermittelbare Wiedergabe-Adresse
- **WHEN** die Folge abgespielt wird
- **THEN** antwortet der Endpunkt mit dem Status 503 und einer Begründung
- **AND** die Bibliothek bleibt unverändert

### Requirement: Weiterleitung für Playlist und Segmente

Das Plugin SHALL relative Verweise einer Playlist auf einen eigenen Weiterleitungs-Endpunkt umschreiben. Die Weiterleitung SHALL ausschließlich Adressen der freigegebenen Wirte annehmen und andere mit dem Status 403 abweisen.

#### Scenario: Relative Segmentadresse wird umgeschrieben

- **GIVEN** eine Playlist, die Segmente relativ verweist
- **WHEN** der Endpunkt die Playlist ausliefert
- **THEN** verweisen die Segmentadressen auf den Weiterleitungs-Endpunkt
- **AND** der Client lädt die Segmente über die Jellyfin-Instanz

#### Scenario: Fremder Wirt wird abgewiesen

- **GIVEN** eine Weiterleitungsanfrage mit einer Adresse außerhalb der freigegebenen Wirte
- **WHEN** die Anfrage den Endpunkt erreicht
- **THEN** antwortet das Plugin mit dem Status 403
- **AND** die Anfrage wird nicht weitergeleitet
