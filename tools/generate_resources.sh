#!/bin/bash

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")"; pwd)"
PROJDIR="$(dirname $DIR)"

target="$PROJDIR/Resources.cs"

(
    echo "using System.Collections.Generic;"
    echo ""

    echo "public class Resources {"
    echo "    public static readonly Dictionary<string, string> R = new Dictionary<string, string>() {"

    for f in "$PROJDIR/Resources"/* ; do

        name=$(basename "$f")
        contents=$(<"$f")
        echo "{ @\"${name//\"/\\\"}\", @\"${contents//\"/\"\"}\" },"

    done

    echo "  };"
    echo "}"
) >$target