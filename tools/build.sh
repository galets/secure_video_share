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

for rid in win-x64 linux-x64 ; do
    target="$PROJDIR/bin/Release/netcoreapp3.0/$rid/publish"
    dotnet publish --configuration Release --runtime $rid --output "$target"
    cp "$PROJDIR/Platform/$rid/"* "$target/"
    zip -j "$PROJDIR/builds/svisha_${rid}_${new_version}.zip" "$target"/*
done
