#!/bin/bash

target=Resources.cs

(
    echo "using System.Collections.Generic;"
    echo ""

    echo "public class Resources {"
    echo "    public static readonly Dictionary<string, string> R = new Dictionary<string, string>() {"

    for f in Resources/* ; do

        name=$(basename $f)
        contents=$(<$f)
        echo "{ @\"${name//\"/\\\"}\", @\"${contents//\"/\"\"}\" },"

    done

    echo "  };"
    echo "}"
) >$target