# Installation auf dem NAS

Diese Anleitung führt von null bis zur laufenden Bibliothek. Zielsystem ist Jellyfin 10.11.5 auf dem NAS.

## 0. Was am Ende entsteht

- Ein neuer Ordner auf dem NAS, der ausschließlich Verweis- und Metadatendateien enthält.
- Eine neue Jellyfin-Bibliothek, die nur auf diesen Ordner zeigt und von den bestehenden Bibliotheken getrennt bleibt.
- Kein Videomaterial, keine Änderung an bestehenden Bibliotheken oder an der Jellyfin-Konfiguration.

## 1. Paket bauen

Auf der Entwicklungsmaschine (Docker muss laufen):

```bash
cd jellyfin-animex
./build.sh
```

Ergebnis: `dist/Jellyfin.Plugin.Animex_0.1.0.zip`. Der Befehl führt vorher die Tests aus.

Ohne Docker geht es auch mit installiertem .NET SDK 9:

```bash
dotnet publish Jellyfin.Plugin.Animex/Jellyfin.Plugin.Animex.csproj -c Release -o dist/publish
cd dist/publish && zip -r ../Jellyfin.Plugin.Animex_0.1.0.zip .
```

## 2. Plugin auf das NAS bringen

Zwei Wege, beide führen zum selben Ziel:

**Weg A, über die Oberfläche (empfohlen):** Jellyfin-Dashboard öffnen, Seitenleiste, Plugins, Reiter Katalog, dann oben rechts das Menü neben dem Suchfeld, Eintrag „Aus Datei installieren". Die ZIP-Datei auswählen. Jellyfin legt das Plugin selbst ab und fordert einen Neustart an.

**Weg B, über das Dateisystem:** ZIP-Inhalt in das Plugin-Verzeichnis der Instanz entpacken, Unterordner `Animex` anlegen hilft der Übersicht.

| System | Verzeichnis |
|---|---|
| Docker | der Pfad, der im Container auf `/config` gemountet ist, darin `plugins/Animex/` |
| Linux, Paketinstallation | `/var/lib/jellyfin/plugins/Animex/` |
| Windows | `%LOCALAPPDATA%\jellyfin\plugins\Animex\` |
| Synology, DSM-Paket | `/var/packages/Jellyfin/target/var/plugins/Animex/` |

Der Ordner muss heißen, der Name ist beliebig, er darf nur nicht mit einer anderen Plugin-Kennung kollidieren. Danach Jellyfin neu starten.

**Prüfen:** Dashboard, Plugins, Reiter „Meine Plugins". Dort steht „Animex". Steht eine Versions- oder Plattformwarnung daneben, passt die `TargetAbi` nicht zur Instanz; gemeldete Version der Instanz prüfen.

## 3. Bibliotheksordner anlegen

Auf dem NAS, dort wo die Bibliotheken liegen, einen eigenen Ordner anlegen, zum Beispiel:

```bash
mkdir -p /volume1/media/animex-stream
```

Regeln dafür:

- Nicht in einen bestehenden Bibliotheksordner legen. Das Plugin schreibt dort hinein und würde die Bibliothek vermischen. Es prüft das zwar und bricht ab, aber der getrennte Ordner ist der saubere Weg.
- Der Benutzer, unter dem Jellyfin läuft, braucht Schreibrechte darauf.
- Der Ordner sollte auf einem Datenträger liegen, kein Netzlaufwerk, damit die vielen kleinen Dateien schnell geschrieben werden.

## 4. Neue Bibliothek in Jellyfin anlegen

Dashboard, Bibliotheken, „Mediathek hinzufügen":

| Feld | Wert |
|---|---|
| Inhaltstyp | Serien (für fortlaufende Animes) oder Filme, wenn nur Filme aufgenommen werden |
| Anzeigename | zum Beispiel „Animex Stream" |
| Ordner | der in Schritt 3 angelegte Ordner, nur dieser eine |
| Metadatenleser | ausschließlich „NFO" |
| Bilder | „Bilder im Medienordner" nach oben, alle externen Anbieter abwählen |
| Echtzeit-Überwachung | aktiviert lassen, damit neue Dateien von selbst erscheinen |

Wichtig: alle externen Metadaten-Anbieter für diese Bibliothek abschalten. Die Angaben stehen in den NFO-Dateien und sollen nicht überschrieben werden.

## 5. Plugin konfigurieren

Dashboard, Plugins, „Animex", dann Einstellungen:

1. **Bibliothekspfad**: der Ordner aus Schritt 3.
2. **Serveradresse für Verweise**: `{serverAddress}` stehen lassen. Damit setzt Jellyfin die Adresse ein, unter der der jeweilige Client den Server erreicht.
3. **Fassung**: `nur Untertitel` als Startwert.
4. **Höchstzahl der Titel**: für den ersten Lauf klein halten, zum Beispiel 25. Das hält den ersten Durchlauf kurz.
5. **Trockenlauf**: zunächst aktiviert lassen. Der erste Lauf zeigt dann nur, was er tun würde.
6. Speichern.

## 6. Ersten Lauf ausführen

Dashboard, Geplante Aufgaben, Kategorie „Animex", Aufgabe „Animex-Katalog abgleichen", Starten.

Erwartung beim Trockenlauf: Das Ergebnis steht im Serverprotokoll, Kategorie `Animex`, mit einer Zeile wie „Lauf (Trockenlauf) beendet in 3s: 25 Titel im Katalog, 25 verarbeitet, 0 Folgen geschrieben ...".

Dann **Trockenlauf abschalten**, speichern, Aufgabe erneut starten. Jetzt entstehen Dateien. Der Fortschritt läuft auf der Seite der geplanten Aufgabe mit.

**Prüfen:** Im Bibliotheksordner liegen Ordner je Titel, darin je Folge eine `.strm`- und eine `.nfo`-Datei sowie eine `tvshow.nfo`. Im Bibliotheksordner liegt außerdem `.animex-ledger.json`, der Bestand des Plugins.

## 7. Bibliothek scannen lassen

Jellyfin erkennt neue Dateien über die Echtzeit-Überwachung meist innerhalb einer Minute. Sonst: Dashboard, Bibliotheken, beim Eintrag „Animex Stream" das Menü, „Bibliothek scannen". Danach erscheinen die Titel.

## 8. Abspielen und prüfen

Eine Folge starten. Was zu sehen sein sollte:

| Prüfung | Erwartung |
|---|---|
| Wiedergabe | Folge läuft an, Bild und Ton vorhanden |
| Untertitel | im Player zuschaltbar (WebVTT) |
| Fortsetzen | nach Abbruch an derselben Stelle weiter |
| Gesehen-Status | Folge erscheint in der Benutzerhistorie als gesehen |
| Suche | Titel über die Jellyfin-Suche auffindbar |

Läuft die Wiedergabe nicht, steht der Grund im Serverprotokoll: Der Wiedergabe-Endpunkt des Plugins meldet, ob die Auflösung fehlgeschlagen ist und warum.

## 9. Zeitplan

Die Aufgabe läuft ab Installation täglich um 04:00 Uhr. Änderbar unter Geplante Aufgaben, Aufgabe auswählen, Zeitplan bearbeiten.

## Trennung von den bestehenden Bibliotheken

- Das Plugin schreibt ausschließlich in den konfigurierten Bibliothekspfad. Es liest keine andere Bibliothek und ändert keine Datei darin.
- Die neue Bibliothek liegt als eigener Eintrag in Jellyfin. Benutzerfreigaben stellt man getrennt ein: Dashboard, Benutzer, Benutzer wählen, Bibliotheken, dort nur die gewünschten anhaken.
- Die Option „Fremde Medien im Zielordner erkennen und Lauf abbrechen" ist standardmäßig aktiv. Findet das Plugin im Zielordner Videodateien, die es nicht selbst angelegt hat, bricht der Lauf ab, statt sie zu vermischen.

## Rückbau

1. Geplante Aufgabe deaktivieren oder Plugin in der Plugin-Liste deinstallieren.
2. Bibliothek in Jellyfin entfernen (Dashboard, Bibliotheken, Eintrag, Entfernen). Die Option, dabei Dateien zu löschen, ist nicht nötig, wenn Schritt 3 folgt.
3. Den Bibliotheksordner auf dem NAS löschen.
4. Jellyfin neu starten.

Bestehende Bibliotheken, Benutzerkonten und Wiedergabestände bleiben unangetastet, weil das Plugin nur seinen eigenen Ordner beschreibt.

## Bekannte Einschränkungen

- **Auflösung der Wiedergabe-URL, wichtigster Punkt**: Der Anbieter liefert seine Quelle über eine Einbettung mit nachgeladenem, verschleiertem Skript. Ohne Ausführung von JavaScript ist die Wiedergabe-Playlist derzeit nicht automatisch zu ermitteln (Messwerte in `docs/feasibility.md`). Folge: Die Bibliothek entsteht vollständig, Titel, Cover, Beschreibung, Suche und Episodenlisten stimmen, aber ein Abspielversuch endet mit einer Meldung. Ein externer Auflöser ist als Erweiterungspunkt vorhanden (`ExternalResolverCommand` in der Konfigurationsdatei), aber nicht über die Oberfläche. `yt-dlp` hilft hier nicht, dafür gibt es keine Extraktoren.
- **Erster Scan dauert**: Jellyfin prüft jede Verweisadresse per ffprobe über das Netz. Bei einer großen Bibliothek ist der erste Scan langsam.
- **Untertitel-Formatierung**: Es kommen die Spuren an, die der Anbieter liefert (WebVTT). Aufwendige Stile aus ASS-Dateien werden nicht übernommen.
- **Nur englische Fassungen**: Andere Sprachen bietet die Gegenstelle nicht an.
- **Anfragebegrenzung**: Die Katalog-Schnittstelle begrenzt die Anfragen. Das Plugin drosselt die Seitenabfragen auf eine Sekunde und wiederholt bei Überlastung mit Wartezeit. Bei sehr großen Läufen (mehrere tausend Titel) kann ein Lauf trotzdem länger dauern; die Titelobergrenze in der Konfiguration hilft.
