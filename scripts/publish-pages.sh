#!/usr/bin/env bash
# Publishes src/Web for static hosting under a base path.
# Usage: scripts/publish-pages.sh <base-path> <output-dir> [version]
#   base-path  "/" for a root host, "/Fitness-App/" for a GitHub Pages project site
#   output-dir receives the site (index.html, _framework, 404.html, .nojekyll, ...)
#   version    build identifier shown in the app (default: dev)
set -euo pipefail

base="${1:?base path required, e.g. /Fitness-App/}"
out="${2:?output dir required}"
version="${3:-dev}"

# Normalise to /segment/ form ("/" stays "/").
base="/${base#/}"
base="${base%/}/"
[ "$base" = "//" ] && base="/"

# Number of path segments in the base: 0 for root, 1 for a project site.
trimmed="${base#/}"; trimmed="${trimmed%/}"
if [ -z "$trimmed" ]; then segments=0; else segments=$(awk -F/ '{print NF}' <<<"$trimmed"); fi

root="$(cd "$(dirname "$0")/.." && pwd)"
site="$out/wwwroot"
rm -rf "$out"
dotnet publish "$root/src/Web" -c Release -o "$out" -p:InformationalVersion="$version"

hash_of() { echo "sha256-$(openssl dgst -sha256 -binary "$1" | openssl base64 -A)"; }

old_hash="$(hash_of "$site/index.html")"
sed -i "s|<base href=\"/\" />|<base href=\"$base\" />|" "$site/index.html"
grep -q "<base href=\"$base\" />" "$site/index.html" || { echo "base href rewrite failed" >&2; exit 1; }
new_hash="$(hash_of "$site/index.html")"

# The service worker fetches index.html with an integrity check against this manifest,
# so the edited file's hash must replace the original or the offline install fails.
# The precompressed copies are stale and GitHub Pages doesn't use them: drop them.
grep -q "$old_hash" "$site/service-worker-assets.js" || { echo "index.html hash not found in service-worker-assets.js" >&2; exit 1; }
sed -i "s|$old_hash|$new_hash|" "$site/service-worker-assets.js"
rm -f "$site/index.html.br" "$site/index.html.gz" "$site/service-worker-assets.js.br" "$site/service-worker-assets.js.gz"

sed "s|__SEGMENT_COUNT__|$segments|" "$root/deploy/404.template.html" > "$site/404.html"
touch "$site/.nojekyll"
echo "Published to $site with base $base (404 segment count $segments)"
