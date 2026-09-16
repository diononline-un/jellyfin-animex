---
project: jellyfin-animex
status: in-development
progress: 15
last_updated: 2026-09-16
tags: [jellyfin, plugin, dotnet, anime, streaming, strm]
---

Jellyfin-Plugin, das Animes aus der animex.one-Infrastruktur als Bibliothek ohne lokale Mediendateien bereitstellt (STRM-Verweise plus NFO-Metadaten). Privater Betrieb auf dem NAS.

Machbarkeit belegt (`docs/feasibility.md`), PRD und OpenSpec-Change geschrieben, Implementierung nicht begonnen. Zielplattform fixiert: Jellyfin 10.11.5, `TargetAbi` 10.11.0.0, `net9.0`.

Next steps:
- .NET SDK 9 auf der Entwicklungsmaschine bereitstellen (Container genügt)
- Erreichbarkeit der Player-Domains vom NAS prüfen
- Praxistest Stream-Auflösung für eine Folge, Ergebnis in `docs/feasibility.md`
- Katalogleser gegen animex-GraphQL und AniList implementieren (Meilenstein M1)
