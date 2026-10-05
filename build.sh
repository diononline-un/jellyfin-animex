#!/usr/bin/env bash
# Baut das Plugin in Containern und packt das Installationspaket.
#
#   ./build.sh          Tests, Build, Paket in dist/
#   ./build.sh test     nur Tests
#
# Auf der Entwicklungsmaschine ist kein .NET SDK installiert; Docker liefert SDK und zip.
set -euo pipefail
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
mode="${1:-build}"
sdk="mcr.microsoft.com/dotnet/sdk:9.0"
version="$(grep -oP '(?<=<Version>)[^<]+' "$here/Jellyfin.Plugin.Animex/Jellyfin.Plugin.Animex.csproj" | head -1)"

dotnet_run() {
  docker run --rm -v "$here:/src" -w /src \
    -e DOTNET_CLI_TELEMETRY_OPTOUT=1 -e DOTNET_NOLOGO=1 \
    "$sdk" "$@"
}

dotnet_run dotnet test jellyfin-animex.sln -c Release
if [ "$mode" = "test" ]; then
  exit 0
fi

rm -rf "$here/dist/publish"
dotnet_run dotnet publish Jellyfin.Plugin.Animex/Jellyfin.Plugin.Animex.csproj -c Release -o /src/dist/publish

# Paket bauen. Das SDK-Image bringt kein zip mit, deshalb ein schlanker Container.
docker run --rm -v "$here:/src" -w /src/dist/publish alpine:latest sh -c \
  "apk add --no-cache zip >/dev/null && zip -qr /src/dist/Jellyfin.Plugin.Animex_${version}.zip ."

echo
echo "Paket: $here/dist/Jellyfin.Plugin.Animex_${version}.zip"
docker run --rm -v "$here:/src" -w /src alpine:latest unzip -l "/src/dist/Jellyfin.Plugin.Animex_${version}.zip"
echo
echo "Installation: docs/installation.md"
