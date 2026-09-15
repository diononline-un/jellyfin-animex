# Changelog

## 2026-09-16 — Machbarkeit belegt, PRD und OpenSpec-Change angelegt

- Live geprüft: animex.one ist eine SvelteKit-App mit öffentlichem GraphQL-Backend (`graphql.animex.one`), offener Introspection und leerem `Disallow` in der robots.txt.
- Kernbefund: das öffentliche Schema kennt nur Katalog- und Metadatentypen (17 Typen, `AnimeNode` mit 63 Feldern), aber keine Episoden- und keine Quellenobjekte. Episodenlisten und Stream-URLs brauchen deshalb einen eigenen, abgegrenzten Auflöser.
- Nachweis für die Wiedergabe: Jellyfin unterstützt `.strm`-Dateien nativ, damit entstehen Bibliothekseinträge ohne lokales Medium; Vorbilder sind AniLibriaStrmPlugin und Jellyfin-FastSTRM.
- `docs/feasibility.md` mit allen Messwerten, Risiken und offenen Prüfungen geschrieben.
- `docs/prd.md` mit 19 Anforderungen, Nicht-Zielen, Erfolgskriterien und Meilensteinen M1 bis M5 geschrieben.
- OpenSpec initialisiert (`openspec init --tools agents --language de`), Change `add-animex-streaming-library` mit proposal, drei Capability-Specs, design und tasks angelegt; `openspec validate --strict` läuft durch.
- Offen: Jellyfin-Version auf dem NAS, .NET SDK 8 auf der Entwicklungsmaschine, Praxistest der Auflösung. Implementierung noch nicht begonnen.
