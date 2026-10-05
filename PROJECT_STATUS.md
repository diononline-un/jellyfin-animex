---
project: jellyfin-animex
status: in-development
progress: 70
last_updated: 2026-10-05
tags: [jellyfin, plugin, dotnet, anime, streaming, strm]
---

Jellyfin-Plugin, das Animes aus der animex.one-Infrastruktur als Bibliothek ohne lokale Mediendateien bereitstellt (STRM-Verweise plus NFO-Metadaten), in einem eigenen, von den bestehenden Bibliotheken getrennten Ordner.

Fassung 0.1.0 implementiert, gebaut, getestet (23 Einheitentests grün). Katalog, Metadaten, Dateierzeugung, Task und Wiedergabe-Endpunkt stehen. Auf dem NAS noch nicht installiert.

Next steps:
- Paket auf das NAS bringen und Plugin installieren (`docs/installation.md`)
- Eigenen Bibliotheksordner anlegen und als getrennte Bibliothek einbinden
- Ersten Lauf im Trockenlauf, dann mit Schreiben, Bibliothek scannen
- Eine Folge abspielen; greift der Auflöser nicht, externen Auflöser nachziehen
