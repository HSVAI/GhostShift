#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "$0")/.."
apk=Builds/Android/GhostShift.apk
publish_dir=docs
test -s "$apk"
/home/ghtnql/Android/Sdk/build-tools/34.0.0/apksigner verify "$apk"
mkdir -p "$publish_dir"
install -m 644 "$apk" "$publish_dir/GhostShift-0.1.1.apk"
install -m 644 Distribution/index.html "$publish_dir/index.html"
install -m 644 Builds/QA/01-menu.png "$publish_dir/preview.png"
cd "$publish_dir"
sha256sum GhostShift-0.1.1.apk > SHA256SUMS.txt
echo "Published test artifacts to $publish_dir"
