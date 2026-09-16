# PRD: jellyfin-animex

Jellyfin-Plugin, das Animes aus der Infrastruktur von animex.one als reguläre Bibliothek bereitstellt, ohne Mediendateien lokal zu halten.

Stand: 2026-09-16. Machbarkeit belegt in `docs/feasibility.md`.

## Problem

Der Nutzer betreibt einen Jellyfin-Server auf einem NAS. Animes liegen dort als Dateien, was Speicherplatz kostet und Pflegearbeit bedeutet: besorgen, benennen, einsortieren, aktuell halten. animex.one bietet dieselben Titel mit englischen Untertiteln und Synchronfassungen bereits gestreamt an. Es fehlt die Brücke in Jellyfin, damit nicht zwei Oberflächen nebeneinander existieren.

## Ziel

Animex-Titel erscheinen in Jellyfin wie eigene Medien. Der Nutzer sucht, sieht Metadaten, startet eine Folge, sieht sie als gesehen markiert und kann später fortsetzen. Auf dem NAS liegt dafür kein Videomaterial, nur eine kleine Verweisdatei je Folge.

## Zielgruppe und Nutzung

Ein Betreiber, ein Heimserver, wenige Benutzer im Haushalt. Kein Mehrinstanzbetrieb, keine Veröffentlichung, keine Weitergabe des Plugins.

## Anforderungen

### Katalog und Metadaten

- R1: Das Plugin liest Animes aus dem animex-GraphQL-Backend (`catalogAnime`, `anime`, `searchAnime`).
- R2: Zu jedem Titel reichert es Metadaten über die AniList-GraphQL-API an, verknüpft über `anilistId`. AniList ist die Quelle für Beschreibung, Cover, Banner, Genres, Studios, Bewertung und Erscheinungsdaten.
- R3: Pro Titel entsteht eine Ordnerstruktur mit einer Verweisdatei je Folge. Die Anzahl der Folgen kommt aus `episodeCount` und, je Fassung, aus `subCount` und `dubCount`.
- R4: Titel, Beschreibung, Cover und Episodennummern liegen zusätzlich als NFO beim Eintrag, damit Jellyfin ohne externen Agenten und ohne Netzabgleich korrekt anzeigt.
- R5: Der Nutzer kann den Katalog eingrenzen: Sprache (nur Untertitel, nur Synchron, beides), Genre, Erscheinungsjahr, Mindestbewertung, Höchstzahl der Titel und Ausschluss erwachsener Inhalte. Der Filter wird beim Lauf angewandt, nicht hart in die Bibliothek geschrieben.
- R6: Die Synchronisation läuft als geplanter Jellyfin-Task (Vorgabe: täglich) und ist zusätzlich manuell auslösbar.
- R7: Ein Lauf ist wiederaufsetzbar. Bereits erzeugte Einträge werden nicht erneut geschrieben, solange sich die Zuordnung nicht ändert. Nach einem Abbruch setzt der nächste Lauf dort an, wo er aufgehört hat.

### Wiedergabe

- R8: Jede Folge wird als `.strm`-Datei mit der tatsächlichen Wiedergabe-URL abgelegt. Jellyfin behandelt sie wie jede andere Folge.
- R9: Die Auflösung der Wiedergabe-URL liegt in einem abgegrenzten Modul mit definierter Schnittstelle (Eingabe: animex-Titel-ID, Episodennummer, Fassung; Ausgabe: Wiedergabe-URL, Typ, Untertitelspuren). Ein Bruch nach einem Wechsel der Gegenstelle wird dort repariert, ohne Katalog oder Bibliothekslogik anzufassen.
- R10: Fällt die Auflösung aus, bleibt die Bibliothek unverändert. Die Folge gilt als derzeit nicht abspielbar, der Fehler wird protokolliert, andere Titel laufen weiter.
- R11: Wenn nur eine Fassung existiert (nur Untertitel oder nur Synchron), wird sie ohne Zusatzkennzeichnung verwendet. Existieren beide, sind sie unterscheidbar benannt.

### Bibliotheksverhalten

- R20: Das Plugin SHALL gegen Jellyfin 10.11.5 gebaut werden, mit `TargetAbi` `10.11.0.0` und Zielframework `net9.0`, und SHALL auf der Zielinstanz ohne Plattformwarnung laden.
- R12: Gesehene Folgen, Fortsetzungspositionen, Favoriten und Wiedergabezähler nutzen Jellyfins eigene Mechanismen. Das Plugin speichert keinen Wiedergabestatus.
- R13: Die Bibliothek funktioniert auch, wenn animex nicht erreichbar ist: Anzeige, Suche und Metadaten bleiben, nur die Wiedergabe schlägt fehl.
- R14: Ein Titel, der bei animex verschwindet, wird beim Lauf als verwaist erkannt. Vorgabe: Einträge werden nicht automatisch gelöscht, sondern im Protokoll gemeldet; Löschen ist eine bewusste Einstellung.
- R15: Ändert animex die interne ID eines Titels, bleibt der bestehende Jellyfin-Eintrag erhalten. Das Plugin hält eine Zuordnungstabelle (animex-ID, AniList-ID, Episoden, Verweis-URLs) und schreibt keine doppelten Einträge.

### Betrieb

- R16: Das Plugin wird über die Plugin-Seite konfiguriert: Bibliothekspfad, Filter, Sprache, gewünschte Qualitätsstufe, Nebenläufigkeit, Verhalten bei verwaisten Einträgen, Protokolltiefe.
- R17: Der Bibliothekspfad ist ein eigener Ordner, getrennt von bestehenden Bibliotheken. Ohne konfigurierten Pfad passiert nichts, es gibt keine Schreibversuche ins Leere.
- R18: Läufe protokollieren Anzahl gefundener Titel, erzeugter Einträge, übersprungener Einträge und Fehler pro Titel. Ergebnisse sind auf der Plugin-Seite sichtbar.
- R19: Das Plugin verändert keine bestehende Jellyfin-Bibliothek und keine Systemdateien. Der Rückbau besteht aus Plugin entfernen und Bibliotheksordner löschen.

## Nicht-Ziele

- Kein Umgehen von Bezahlschranken, Konten oder Zugriffsbeschränkungen.
- Kein Herunterladen und kein Vorhalten von Videomaterial auf dem NAS.
- Keine deutschen Synchronfassungen, keine Quelle außer animex.
- Kein Betrieb mehrerer Animex-Konten oder Weitergabe von Zugangsdaten.
- Keine öffentliche Distribution des Plugins.
- Kein Ersatz für Jellyfins Bibliotheksverwaltung, kein eigener Wiedergabestatus.
- Keine Garantie auf Untertitel-Styling (ASS-Formatierung) oder auf bestimmte Qualitätsstufen.

## Erfolgskriterien

1. Nach einem Synchronisationslauf erscheinen mindestens 50 Titel in der neuen Bibliothek, mit korrektem Titel, Cover und Beschreibung.
2. Eine Folge aus dieser Bibliothek startet auf einem Client und läuft mindestens fünf Minuten ohne Abbruch.
3. Nach Abbruch und Neustart der Wiedergabe setzt Jellyfin an der letzten Position fort, und die Folge steht als gesehen markiert in der Benutzerhistorie.
4. Die Suche nach einem Titel innerhalb der Bibliothek liefert den Eintrag.
5. Ein zweiter Lauf erzeugt keine Duplikate und dauert kürzer als der erste.
6. Während eines Neustarts der Jellyfin-Instanz bleibt die Bibliothek konsistent, es entstehen keine halben Einträge.
7. Wird die Gegenstelle unerreichbar gemacht, bleibt die Bibliothek benutzbar und das Protokoll nennt den Fehler pro Titel.

## Annahmen

- Jellyfin auf dem NAS ist Version 10.11.5. Bestätigt durch den Betreiber am 2026-09-16.
- Der Server darf die Player-Domains erreichen (kein DNS- oder Filterproblem, keine Blockade durch Cloudflare).
- Der Betreiber hat Administratorzugriff auf Jellyfin, um Plugin und Bibliothek anzulegen.
- Die Entwicklungsmaschine bekommt Zugang zum NAS oder der Betreiber führt Verifikationsschritte selbst aus.

## Abhängigkeiten und Umgebung

- Plugin-Entwicklung: .NET SDK 9 (Jellyfin 10.11 baut auf .NET 9), Pakete `Jellyfin.Controller` und `Jellyfin.Model` in Version 10.11.5, `TargetAbi` `10.11.0.0`.
- Entwicklungsmaschine: kein .NET-SDK installiert, kein Zugang zum NAS, kein SSH-Zugang. Build und Verifikation brauchen diese Zugänge oder einen Container mit SDK 9.
- Externe APIs: `graphql.animex.one` (Katalog), `graphql.anilist.co` (Metadaten), Player-Infrastruktur für die Wiedergabe.

## Meilensteine

| Nr. | Inhalt | Abschlusskriterium |
|---|---|---|
| M1 | Katalog- und Metadatenleser, Filter, Bericht | Trockenlauf erzeugt eine vollständige Liste mit Metadaten, ohne Dateien zu schreiben |
| M2 | Bibliotheksschreiber mit Verweisdateien und NFO | Jellyfin scannt den Ordner und zeigt Einträge mit korrekten Metadaten |
| M3 | Auflöser für die Wiedergabe-URL, eine Serie | Eine Folge startet auf einem Client, Fortsetzen funktioniert |
| M4 | Geplanter Lauf, Wiederaufsetzen, Aufräumen | Zweiter Lauf ohne Duplikate, verwaiste Titel im Protokoll |
| M5 | Feinschliff: FastSTRM oder Metadaten-Vorbelegung, Nebenläufigkeit, Protokolle | Startzeit je Folge messbar gesunken, Protokolle auswertbar |
