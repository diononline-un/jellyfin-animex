## Why

Der Change `add-animex-streaming-library` hat die Machbarkeit geklärt und die Anforderungen festgehalten. Jetzt liegt die erste lauffähige Fassung vor. Dieser Change hält fest, was tatsächlich gebaut wurde, wo die Umsetzung von der Planung abweicht und welche Einschränkungen bleiben. Ohne ihn beschreibt `openspec/specs/` einen Stand, den es nicht gibt.

## What Changes

- Plugin `Jellyfin.Plugin.Animex` für Jellyfin 10.11.5 (`TargetAbi` 10.11.0.0, `net9.0`) mit Katalogleser, AniList-Anreicherung, Bibliotheksschreiber, Bestandsdatei, geplantem Task und Wiedergabe-Endpunkt.
- Abweichung von der Planung: In der Verweisdatei steht nicht die Anbieteradresse, sondern die Adresse des Plugins (`/Animex/Playback/{id}/{folge}`). Der Grund steht im Design.
- Zusätzlich ein Weiterleitungs-Endpunkt `/Animex/Proxy` für Playlist- und Segmentadressen, begrenzt auf eine feste Liste erlaubter Wirte.
- Neue Einschränkung als Anforderung festgehalten: Kann der Auflöser die Wiedergabe-Adresse nicht ermitteln, bleibt der Bibliothekseintrag bestehen und der Titel gilt als derzeit nicht abspielbar; der Grund wird protokolliert.
- Erweiterungspunkt für einen externen Auflöser, damit die Auflösung ohne Änderung am Plugin nachgerüstet werden kann.
- Bauweg über Container (`./build.sh`), weil auf der Entwicklungsmaschine kein .NET SDK liegt.
- Anleitung für Installation, getrennte Bibliothek, Konfiguration und Rückbau (`docs/installation.md`).

## Capabilities

### New Capabilities

Keine. Die drei Fähigkeiten aus dem ersten Change decken den Umfang ab.

### Modified Capabilities

- `animex-catalog-sync`: Ergänzungen zu Bildfeldern, Folgenableitung und Trockenlauf, dazu die neuen Anforderungen an den Wiedergabe-Endpunkt und an die Weiterleitung.
- `animex-stream-resolver`: Ergänzung um das tatsächlich beobachtete Verhalten der Gegenstelle, um die Meldung „nicht auflösbar" und um den externen Auflöser.
- `animex-jellyfin-integration`: Ergänzungen zu getrennter Bibliothek, Weiterleitung mit Wirteliste und Prüfpfad.

## Impact

- Neuer Code: `Jellyfin.Plugin.Animex/` mit sechs Bereichen, `Jellyfin.Plugin.Animex.Tests/` mit 23 Einheitentests, `build.sh`, `build.yaml`.
- Zielsystem: Jellyfin 10.11.5 auf dem NAS. Änderung dort beschränkt auf das Plugin-Verzeichnis und einen neuen Bibliotheksordner.
- Neue Dateien im Bibliotheksordner: `.strm`, `.nfo`, `tvshow.nfo`, `.animex-ledger.json`.
- Offen: Installation und Wiedergabetest auf dem NAS. Erst danach ist die Abnahme nach `docs/prd.md` belegbar.
