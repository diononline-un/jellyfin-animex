## 1. Voraussetzungen und Zugänge

- [ ] 1.1 Jellyfin-Version auf dem NAS feststellen (Dashboard, Info) und notieren; Ergebnis in `docs/feasibility.md` unter „Offene Prüfungen" eintragen. Verifikation: Version und TargetAbi stehen im Dokument.
- [ ] 1.2 .NET SDK 8 auf der Entwicklungsmaschine bereitstellen (nativ oder Container) und mit `dotnet --list-sdks` belegen. Verifikation: Kommandoausgabe zeigt SDK 8.
- [ ] 1.3 Erreichbarkeit der Player-Domains vom NAS aus prüfen (Statuscode und Antwortzeit für die Player-Adresse und eine Playlist). Verifikation: Messwerte stehen im Dokument; bei Blockade wird der Change gestoppt und gemeldet.
- [ ] 1.4 Repository-Grundgerüst anlegen: Projektmappe, Plugin-Projekt, Testprojekt, `.gitignore`, `README.md`. Verifikation: `dotnet build` läuft durch und erzeugt eine Plugin-DLL.
- [ ] 1.5 Plugin-Zielplatform festlegen (TargetAbi passend zur NAS-Version) und in der Projektdatei setzen. Verifikation: `dotnet build` ohne Plattformwarnung.

## 2. Katalogleser und Anreicherung

- [ ] 2.1 GraphQL-Client mit Zeitlimit, Wiederholung mit wachsender Wartezeit und Fehlerabbildung implementieren. Verifikation: Einheitentest mit nachgebildeter Gegenstelle deckt Erfolg, Zeitüberschreitung und Fehlerstatus ab.
- [ ] 2.2 Katalogabfrage (`catalogAnime` mit Paging) und Einzelabfrage (`anime`) implementieren. Verifikation: Einheitentest gegen aufgezeichnete Antwort liefert erwartete Titel-IDs.
- [ ] 2.3 AniList-Anreicherung über `anilistId` implementieren, mit Rückfall auf animex-Felder bei fehlender Verknüpfung. Verifikation: Einheitentest deckt beide Fälle ab.
- [ ] 2.4 Filter (Fassung, Genre, Jahr, Mindestbewertung, Titelgrenze, Ausschluss erwachsener Inhalte) implementieren. Verifikation: Einheitentest prüft jede Bedingung einzeln und in Kombination.
- [ ] 2.5 Ableitung der Folgen aus `episodeCount`, `subCount`, `dubCount` implementieren, inklusive Namensregel für eine oder zwei Fassungen. Verifikation: Einheitentest für beide Fälle mit erwarteter Folgenzahl.
- [ ] 2.6 Trockenlauf-Modus ohne Schreibzugriff implementieren. Verifikation: Testlauf gegen die echte Gegenstelle erzeugt eine vollständige Liste mit Metadaten und keine Datei im Zielordner.

## 3. Auflöser

- [ ] 3.1 Schnittstelle und Ergebnisobjekt des Auflösers festlegen (Titel-ID, Episodennummer, Fassung hinein; URL, Typ, Untertitelspuren heraus, dazu getrennte Ergebnisse für „Fassung fehlt" und „nicht auflösbar"). Verifikation: Einheitentest prüft alle drei Ergebnisformen.
- [ ] 3.2 Auflösung gegen die echte Gegenstelle für eine Serie implementieren, Ergebnis protokolliert. Verifikation: Handlauf für eine Folge liefert eine URL, die im Browser abspielbar ist; Ausgabe im Bericht festgehalten.
- [ ] 3.3 Fehlerbehandlung ohne Schaden für bestehende Einträge implementieren. Verifikation: Einheitentest mit absichtlich fehlschlagender Gegenstelle zeigt unveränderte STRM-Datei und Fehlereintrag im Bericht.
- [ ] 3.4 Adressen der Gegenstelle konfigurierbar machen. Verifikation: Einheitentest mit geänderter Adresse verwendet die neue Adresse.

## 4. Bibliotheksschreiber

- [ ] 4.1 Ordner- und Dateinamenschema nach Jellyfin-Konvention implementieren (Ordner je Titel, `S{Staffel}E{Nummer}`, Fassung im Namen bei zwei Fassungen). Verifikation: Einheitentest prüft erzeugte Pfade für beide Fassungsfälle.
- [ ] 4.2 STRM-Dateien schreiben. Verifikation: Nach dem Lauf enthält jede Datei genau eine URL und keine Mediendaten.
- [ ] 4.3 NFO-Dateien mit Titel, Beschreibung, Cover-Adresse und Episodennummer schreiben. Verifikation: Jellyfin-Scan zeigt Titel, Beschreibung und Cover korrekt an.
- [ ] 4.4 Zuordnungstabelle (animex-ID, AniList-ID, Fassung, Folgenzahl, Dateipfade) schreiben und lesen. Verifikation: Zweiter Lauf liest die Tabelle und schreibt keine vorhandenen Einträge erneut.
- [ ] 4.5 Wiederaufsetzen nach Abbruch implementieren. Verifikation: Test mit künstlichem Abbruch nach 20 von 50 Titeln; der nächste Lauf legt nur die fehlenden 30 an.
- [ ] 4.6 Schutzprüfung für den Zielordner (kein Überschreiben fremder Bibliotheken, Verweigerung bei belegtem Pfad ohne eigene Zuordnungstabelle). Verifikation: Einheitentest mit fremd belegtem Ordner bricht den Lauf mit Begründung ab.
- [ ] 4.7 Behandlung verwaister Einträge: standardmäßig melden, Löschen nur bei aktivierter Einstellung. Verifikation: Einheitentest für beide Einstellungen.

## 5. Plugin, Konfiguration und Task

- [ ] 5.1 Plugin-Grundgerüst mit Konfigurationsobjekt und Konfigurationsseite implementieren (Bibliothekspfad, Fassung, Filter, Qualitätsstufe, Nebenläufigkeit, Protokolltiefe, Umgang mit Waisen). Verifikation: Werte lassen sich auf der Plugin-Seite speichern und werden nach einem Neustart der Instanz beibehalten.
- [ ] 5.2 Geplanten Task mit täglicher Vorgabe implementieren, zusätzlich manuell auslösbar. Verifikation: Manueller Start im Dashboard läuft durch; Zeitplan erscheint in den geplanten Aufgaben.
- [ ] 5.3 Laufbericht auf der Plugin-Seite darstellen (Zeitpunkt, gefundene Titel, erzeugt, übersprungen, Fehler je Titel mit Grund). Verifikation: Nach einem Lauf mit einem absichtlich fehlerhaften Titel zeigt die Seite den Fehler mit Titel und Grund.
- [ ] 5.4 Protokollierung mit Rücksicht auf Zugangsdaten implementieren. Verifikation: Protokolldatei enthält keine Kopfzeilen mit Sitzungswerten oder Schlüsseln.

## 6. Verifikation auf dem Zielsystem

- [ ] 6.1 Plugin auf dem NAS installieren und Bibliotheksordner anlegen, Bibliothek in Jellyfin hinzufügen (Metadatenleser NFO). Verifikation: Bibliothek erscheint und ist leer bis zum ersten Lauf.
- [ ] 6.2 Ersten Synchronisationslauf ausführen. Verifikation: mindestens 50 Titel erscheinen mit korrektem Titel, Cover und Beschreibung.
- [ ] 6.3 Eine Folge abspielen. Verifikation: Wiedergabe läuft mindestens fünf Minuten auf einem Client ohne Abbruch.
- [ ] 6.4 Fortsetzen und Gesehen-Status prüfen. Verifikation: Nach Abbruch setzt die Wiedergabe an der letzten Position fort; die Folge steht in der Benutzerhistorie.
- [ ] 6.5 Zweiten Lauf ausführen. Verifikation: Anzahl der Einträge unverändert, Lauf meldet übersprungene Einträge, Dauer kürzer als beim ersten Lauf.
- [ ] 6.6 Ausfallverhalten prüfen (Gegenstelle unerreichbar gemacht). Verifikation: Bibliothek bleibt durchsuchbar, Wiedergabe schlägt fehl, Bericht nennt die betroffenen Titel.
- [ ] 6.7 Startzeit je Folge messen und festhalten. Verifikation: Messwerte stehen in `docs/feasibility.md`; daraus wird entschieden, ob ein FastSTRM-Schritt nötig ist.

## 7. Abschluss

- [ ] 7.1 `docs/feasibility.md` um Messergebnisse und den tatsächlich gangbaren Auflösungsweg ergänzen. Verifikation: Dokument nennt Messwerte und offene Einschränkungen.
- [ ] 7.2 `README.md` mit Installation, Konfiguration, Rückbau und bekannten Einschränkungen schreiben. Verifikation: Ein Leser kann das Plugin allein anhand der Datei installieren und entfernen.
- [ ] 7.3 `PROJECT_STATUS.md` und `CHANGELOG.md` aktualisieren. Verifikation: Status steht auf `done`, Fortschritt 100, Änderungen im Changelog oben eingetragen.
