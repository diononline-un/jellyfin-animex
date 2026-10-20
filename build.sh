#!/usr/bin/env bash
# Baut das Plugin in Containern und packt das Installationspaket.
#
#   ./build.sh          Tests, Build, Paket in dist/
#   ./build.sh test     nur Tests
#
# Auf der Entwicklungsmaschine ist kein .NET SDK installiert; Docker liefert SDK, Node und ZIP.
# Alle Container laufen als aufrufender Benutzer, damit die Dateien nicht root gehören.
set -euo pipefail
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
mode="${1:-build}"
sdk="mcr.microsoft.com/dotnet/sdk:10.0"
node="node:24-alpine"
uid_gid="$(id -u):$(id -g)"
version="$(grep -oP '(?<=<Version>)[^<]+' "$here/Jellyfin.Plugin.Animex/Jellyfin.Plugin.Animex.csproj" | head -1)"
package="Jellyfin.Plugin.Animex_${version}.zip"

dotnet_run() {
  docker run --rm -v "$here:/src" -w /src -u "$uid_gid" \
    -e DOTNET_CLI_TELEMETRY_OPTOUT=1 -e DOTNET_NOLOGO=1 -e HOME=/tmp \
    "$sdk" "$@"
}

# Reste aus früheren Läufen können root gehören. Vorher aufräumen.
docker run --rm -v "$here:/src" alpine:latest sh -c 'rm -rf /src/dist /src/*/bin /src/*/obj' >/dev/null 2>&1 || true

dotnet_run dotnet test jellyfin-animex.sln -c Release
if [ "$mode" = "test" ]; then
  exit 0
fi

dotnet_run dotnet publish Jellyfin.Plugin.Animex/Jellyfin.Plugin.Animex.csproj -c Release -o /src/dist/publish

# Paket bauen. Im Node-Container fehlt das zip-Programm, deshalb archiver nach .ziptool.
# Der Ordner darf nicht unter .tools/ liegen: dort liegt bereits node_modules des OpenSpec-CLI.
docker run --rm -v "$here:/src" -w /src -u "$uid_gid" -e HOME=/tmp -e npm_config_cache=/tmp/npmcache "$node" sh -c \
  "mkdir -p /src/.ziptool && cd /src/.ziptool && npm install --silent --no-fund --no-audit archiver >/dev/null 2>&1 && node /src/tools/zip.mjs /src/dist/publish /src/dist/${package}"

echo
echo "Paket: $here/dist/${package}"
unzip -l "$here/dist/${package}" 2>/dev/null || docker run --rm -v "$here:/src" alpine:latest unzip -l "/src/dist/${package}"
echo
echo "Installation: docs/installation.md"
