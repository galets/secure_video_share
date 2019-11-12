#!/bin/bash

set -e
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")"; pwd)"
PROJDIR="$(dirname $DIR)"
PROJ="$PROJDIR/svisha.csproj"

version=$(xmllint --xpath '/Project/PropertyGroup/Version/text()' "$PROJ")
new_version="$(
    echo "$version" | (
        IFS='.' read major minor build extra;
        echo "$major.$minor.$(($build + 1)).$(($(date +%s) % 10000))"
    )
)"

xmllint --shell "$PROJ" >/dev/null << EOF
    cd /Project/PropertyGroup/Version
    set $new_version
    save
EOF

"$PROJDIR/tools/generate_resources.sh"

target="$PROJDIR/builds/linux-x64/$new_version"
mkdir -p "$target"
dotnet-warp --verbose --output "$target/svisha" || rm -rf "$target"


