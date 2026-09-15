---
project: jellyfin-animex
status: in-development
progress: 10
last_updated: 2026-09-16
tags: [jellyfin, plugin, dotnet, anime, streaming, strm]
---

Jellyfin-Plugin, das Animes aus der animex.one-Infrastruktur als Bibliothek ohne lokale Mediendateien bereitstellt (STRM-Verweise plus NFO-Metadaten).

Machbarkeit belegt (`docs/feasibility.md`), PRD und OpenSpec-Change geschrieben, keine Implementierung begonnen.

Next steps:
- Jellyfin-Version auf dem NAS feststellen (TargetAbi für das Plugin)
- .NET SDK 8 auf der Entwicklungsmaschine bereitstellen
- Katalogleser gegen animex-GraphQL und AniList implementieren (M1)
- Praxistest Stream-Auflösung für eine Folge, Ergebnis dokumentieren
