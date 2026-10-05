---
project: jellyfin-animex
status: in-development
progress: 65
last_updated: 2026-10-05
tags: [jellyfin, plugin, dotnet, anime, streaming, strm]
---

Jellyfin-Plugin, das Animes aus der animex.one-Infrastruktur als Bibliothek ohne lokale Mediendateien bereitstellt (STRM-Verweise plus NFO-Metadaten), in einem eigenen, von den bestehenden Bibliotheken getrennten Ordner.

Fassung 0.1.0 steht: Katalog, AniList-Metadaten, Dateierzeugung, Bestand, geplanter Task, Wiedergabe-Endpunkt, 27 Tests. Gegen eine eigene Jellyfin-Instanz 10.11.5 im Container geprüft: Plugin lädt ohne Plattformwarnung, Task läuft, 3 Titel und 75 Folgen geschrieben, Endpunkte antworten korrekt. PR #1 nach main gemergt.

Offen ist der letzte Schritt: Die Wiedergabe-Playlist ist ohne Ausführung von JavaScript nicht automatisch ermittelbar (Messwerte in `docs/feasibility.md`). Ohne Auflöser entsteht die Bibliothek vollständig, das Abspielen endet aber mit einer Meldung.

Next steps:
- Auflöser nachziehen (Kopfloser Browser als eigener Dienst oder externe Auflösung) oder Bibliothek bewusst ohne Wiedergabe nutzen
- Paket auf das NAS bringen und Plugin installieren (`docs/installation.md`)
- Eigenen Bibliotheksordner anlegen und als getrennte Bibliothek einbinden
- Ersten Lauf im Trockenlauf, dann mit Schreiben, Bibliothek scannen
