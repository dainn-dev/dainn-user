#!/usr/bin/env bash
# Publish all DainnUser v1.0.0 packages to NuGet.org
# Usage: NUGET_API_KEY=<your-key> ./publish-nuget.sh
#        or: ./publish-nuget.sh <your-api-key>

set -euo pipefail

API_KEY="${NUGET_API_KEY:-${1:-}}"

if [[ -z "$API_KEY" ]]; then
  echo "Error: NuGet API key required."
  echo "  NUGET_API_KEY=<key> ./publish-nuget.sh"
  echo "  or: ./publish-nuget.sh <key>"
  exit 1
fi

NUPKGS_DIR="$(cd "$(dirname "$0")/nupkgs" && pwd)"
SOURCE="https://api.nuget.org/v3/index.json"

packages=(
  "DainnUser.Core.1.0.0.nupkg"
  "DainnUser.Application.1.0.0.nupkg"
  "DainnUser.Infrastructure.1.0.0.nupkg"
  "DainnUser.Web.1.0.0.nupkg"
  "DainnUser.OpenIddict.1.0.0.nupkg"
  "DainnStripe.1.0.0.nupkg"
)

echo "Publishing ${#packages[@]} packages to NuGet.org..."
echo ""

for pkg in "${packages[@]}"; do
  path="$NUPKGS_DIR/$pkg"
  if [[ ! -f "$path" ]]; then
    echo "ERROR: $path not found. Run 'dotnet pack -c Release' first."
    exit 1
  fi
  echo "→ Publishing $pkg"
  dotnet nuget push "$path" \
    --api-key "$API_KEY" \
    --source "$SOURCE" \
    --skip-duplicate
done

echo ""
echo "Done. Packages are live at https://www.nuget.org/profiles/Dainn"
