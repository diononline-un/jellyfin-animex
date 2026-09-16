## Context

Gründe und Umfang stehen in `proposal.md`, die geprüften Live-Befunde in `docs/feasibility.md`. Für dieses Design sind davon vier Tatsachen maßgeblich:

1. Jellyfin behandelt `.strm`-Dateien als native Medien-Quelle. Der Inhalt ist eine URL, das Item verhält sich danach wie jede andere Folge.
2. Das animex-GraphQL-Schema enthält nur 17 Typen und keine Episoden- oder Quellenobjekte. Katalog und Metadaten sind stabil beziehbar, Episodenlisten und Stream-URLs nicht.
3. Die Serienseite `https://animex.one/anime/{id}` ist serverseitig gerendert und enthält JSON-LD sowie Episodenverweise im Muster `/watch/{slug}-{animexId}-episode-{n}`.
4. Die Entwicklungsmaschine hat kein .NET-SDK und keinen Zugriff auf den Zielserver. Bauen und Verifizieren brauchen einen Container oder Zugang. Zielinstanz ist Jellyfin 10.11.5 auf einem NAS, privater Betrieb.

## Goals / Non-Goals

**Goals:**

- Bibliothek, die ohne lokales Videomaterial vollständig wie eine Jellyfin-Bibliothek benutzbar ist.
- Klare Trennung zwischen stabilen Teilen (Katalog, Metadaten, Dateierzeugung) und der wartungsanfälligen Stelle (Auflösung der Wiedergabe-URL).
- Läufe, die wiederholbar, wiederaufsetzbar und ohne Duplikate sind.
- Rückbau in zwei Schritten: Plugin entfernen, Ordner löschen.

**Non-Goals:**

- Eigener Wiedergabestatus, eigene Benutzerverwaltung, eigenes Frontend.
- Vorhalten oder Zwischenspeichern von Videomaterial.
- Zugriff auf kostenpflichtige oder beschränkte Angebote, Umgehung von Zugriffsbeschränkungen.
- Öffentliche Distribution als Community-Plugin.
- Vollständige Untertitel-Formatierungstreue (ASS-Styling).
- Parallele Unterstützung mehrerer Animex-Konten.

## Decisions

### D1: STRM-Dateien als Grundlage, kein eigener MediaSourceProvider

Jellyfin bekommt pro Folge eine STRM-Datei, die Bibliothek entsteht durch einen normalen Scan. Alternative wäre ein Plugin, das `IMediaSourceProvider` implementiert und Quellen zur Laufzeit liefert, ohne Dateien anzulegen. Dagegen spricht: dieser Weg verzichtet auf Jellyfins eigene Scan-, Änderungs- und Aufräumlogik, ist an mehr interne Schnittstellen gebunden und bricht bei Jellyfin-Aktualisierungen häufiger. Für den STRM-Weg existieren zudem zwei gepflegte Vorbilder (AniLibriaStrmPlugin, FastSTRM).

Konsequenz: Dieser Weg nimmt die ffprobe-Prüfung über das Netz in Kauf. Gegenmaßnahme ist D5.

### D2: animex GraphQL als Katalog, AniList als Metadatenquelle

Der Katalog kommt aus `graphql.animex.one`, die Beschreibungs- und Bildfelder aus `graphql.anilist.co`, verknüpft über `anilistId`. Alternative wäre HTML-Kratzen der Serienseite für alles. Dagegen spricht: HTML ist formgebunden und bricht bei jedem Frontend-Deployment, GraphQL liefert typisierte Felder.

Rückfall: Fehlt `anilistId`, verwendet das Plugin die animex-Felder (`titleEnglish`, `titleRomaji`, `coverImage`, `description`, `genres`), die inhaltlich ebenfalls aus AniList stammen.

### D3: Episodenliste aus Zählfeldern, nicht aus der Seite

Die Zahl der Folgen kommt aus `subCount` und `dubCount`, die Nummerierung ist fortlaufend ab 1. Die Episoden-URL wird nach dem bekannten Muster gebildet. Alternative wäre, die Episodenliste aus der gerenderten Serienseite zu lesen; das bringt echte Episodentitel, kostet aber pro Titel eine zusätzliche Anfrage und hängt an der Seitenstruktur.

Entscheidung: Zählfelder sind die Quelle, der Seitentext ist eine optionale Verfeinerung (eigener Task, standardmäßig aus).

### D4: Metadaten in NFO-Dateien, kein externer Agent

Jellyfin liest NFO lokal. Damit sind Titel, Beschreibung und Episodennummern auch ohne Internetzugriff und ohne Metadaten-Agent korrekt, und der Scan hängt nicht an der Erreichbarkeit von AniList. Alternative wäre, Jellyfin die Metadaten über einen eigenen Provider-Plugin-Teil ziehen zu lassen; das ist mehr Code für weniger Kontrolle.

### D5: Auflöser als eigenes Modul mit austauschbaren Adressen

Der Auflöser ist die einzige Stelle, die von Änderungen der Gegenseite betroffen ist. Er bekommt eine schmale Schnittstelle (Titel-ID, Episodennummer, Fassung hinein; URL, Typ, Untertitelspuren heraus) und eine eigene Adresskonfiguration. Katalog-, Bibliotheks- und Task-Logik kennen nur diese Schnittstelle.

Zur Auflösung selbst, nach heutigem Kenntnisstand: Die Serienseite wird geladen, der Player-Verweis extrahiert, dem Verweis gefolgt und die resultierende Medienadresse samt Untertitelspuren gelesen. Ein Kopfloser Browser ist ausdrücklich nicht vorgesehen; wenn die Auflösung ohne ihn nicht möglich ist, wird das als Ergebnis gemeldet statt eine Abhängigkeit auf Chromium im Plugin zu erzeugen.

Parallel dazu: Vorhandene STRM-Dateien werden bei fehlgeschlagener Auflösung nicht überschrieben.

### D6: Zuordnungstabelle als Gedächtnis der Läufe

Das Plugin hält eine Datei im Bibliotheksordner mit der Zuordnung animex-ID, AniList-ID, Fassung, Episodenzahl und geschriebener Datei. Sie macht Läufe wiederaufsetzbar, verhindert Duplikate und lässt verwaiste Einträge erkennen, ohne das Dateisystem zu durchsuchen. Alternative wäre, den Bestand ausschließlich aus dem Dateisystem abzuleiten; das verliert die Herkunft und erschwert Umbenennungen.

### D7: Bibliotheksaufbau flach je Titel, Fassungen als getrennte Einträge

Ein Ordner je Titel, darin Dateien nach Jellyfin-Konvention `{Titel} S{Staffel}E{Nummer}.strm`. Existieren beide Fassungen, wird die Fassung im Dateinamen geführt, damit Jellyfin zwei unterscheidbare Einträge anlegt statt einen zu überschreiben.

Alternative wäre, die Fassung als Untertitelspur zu behandeln. Das geht nicht, weil die Gegenstelle getrennte Streams liefert und Jellyfin eine Datei nicht zwei Quellen zuordnen kann.

### D8: Keine Episode-übergreifende Wiedergabelogik

Kein Fortschritts-Sharing zwischen Fassungen, kein automatischer Wechsel. Jellyfin führt den Status je Item; alles andere wäre eigener Zustand.

### D9: Zielplattform Jellyfin 10.11.5 mit TargetAbi 10.11.0.0 und Zielframework net9.0

Bestätigt: die Zielinstanz läuft auf Jellyfin 10.11.5. Daraus folgen `TargetAbi` `10.11.0.0`, Zielframework `net9.0` und die Paketverweise `Jellyfin.Controller` sowie `Jellyfin.Model` in Version 10.11.5 (mit `ExcludeAssets=runtime`, weil die Laufzeitumgebung die Instanz liefert). .NET 9 ist damit die Bauvoraussetzung, nicht .NET 8.

Der Bau erfolgt gegen genau diese Fassung. Die offizielle Plugin-Vorlage und FastSTRM 3.0.0 führen dieselbe Kombination (`net9.0`, `10.11.0.0`), der Weg ist also belegt.

### D10: Entwicklungs- und Bauumgebung getrennt von der Zielumgebung

Gebaut wird in einem Container mit .NET-SDK, die Verifikation findet auf dem NAS statt. Die Entwicklungsmaschine hat weder SDK noch Zugang, deshalb listet `tasks.md` die Beschaffung dieser Zugänge als eigene, zuerst zu erledigende Aufgaben.

## Risks / Trade-offs

- Auflösung der Wiedergabe-URL bricht nach Änderungen der Gegenseite → Auflöser isoliert (D5), Fehler je Titel protokolliert, vorhandene STRM-Dateien bleiben bestehen; ein Bruch kostet dann Wartung an einer Datei, nicht an der Bibliothek.
- Kein Kopfloser Browser bedeutet möglicherweise, dass einzelne Titel nicht auflösbar sind → Ergebnis wird als „nicht auflösbar" gemeldet und die Bibliothek bleibt nutzbar; Nachrüstung eines externen Auflösers ist über die Adresskonfiguration vorbereitet.
- ffprobe über das Netz verzögert Scan und Wiedergabestart → Nebenläufigkeit begrenzen, optionaler Metadaten-Vorbelegungsschritt (FastSTRM-Muster) als Folgeschritt.
- Sehr große Kataloge erzeugen viele kleine Dateien → Titelgrenze und Filter in der Konfiguration, Lauf protokolliert die Mengen.
- Cloudflare kann den Server ausschließen → vor der Implementierung prüfen; bei Blockade ist die gesamte Wiedergabe unmöglich, nicht nur einzelne Titel.
- Untertitel-Styling weicht ab → ausdrücklich Nicht-Ziel, im Bericht als bekannte Einschränkung geführt.
- Änderungen der animex-IDs führen zu Waisen statt zu Umbenennungen → Zuordnungstabelle (D6) hält die Herkunft, Waisen werden gemeldet statt automatisch gelöscht.
- Zwei Fassungen verdoppeln die Episodenzahl und damit die Dateien → Fassungswahl pro Lauf konfigurierbar, Vorgabe „beide nur wenn vorhanden".

## Migration Plan

1. Plugin auf dem NAS installieren: DLL in das Plugin-Verzeichnis der Instanz, Instanz neu starten.
2. Neuen Bibliotheksordner anlegen und in Jellyfin als Bibliothek vom Typ „Filme" oder „Serien" mit Metadatenleser „NFO" hinzufügen.
3. Plugin konfigurieren: Bibliothekspfad, Fassung, Filter, Qualitätsstufe, Nebenläufigkeit.
4. Trockenlauf ohne Schreiben (Vorgabe der ersten Ausbaustufe), Ergebnis prüfen.
5. Ersten Synchronisationslauf mit Schreiben ausführen, Bibliotheksscan beobachten, eine Folge abspielen und Fortsetzung prüfen.
6. Geplanten Task aktivieren.

Rückbau: Plugin deinstallieren, Bibliotheksordner löschen, Bibliothek aus Jellyfin entfernen. Bestehende Bibliotheken und Benutzerdaten bleiben unberührt, weil das Plugin weder Jellyfins Datenbank noch deren Ordner beschreibt.

## Open Questions

- Ob die Auflösung ohne Kopflosen Browser für alle Titel gelingt oder nur für einen Teil: wird beim ersten Praxistest gemessen und im Bericht festgehalten.
- Ob FastSTRM als zweiter Schritt nötig ist oder die begrenzte Nebenläufigkeit genügt: abhängig von gemessenen Startzeiten.
- Wie stark die Untertitelspuren in der Praxis abweichen: nach dem ersten Praxistest zu beurteilen.
- Ob die Zielinstanz die Player-Domains überhaupt erreicht: vor der Implementierung geprüft; bei Blockade ist der Change gestoppt.
