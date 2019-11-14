#!/bin/bash

set -e
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")"; pwd)"
PORT=8172
SVISHA_PATH=~/Videos/svisha
VIDEO_ID=

while (( "$#" )); do
  case "$1" in
    -s|--storage)
      SVISHA_PATH="$2"
      shift 2
      ;;
    *) # preserve positional arguments
      VIDEO_ID="$1"
      shift
      ;;
  esac
done

if [ "$VIDEO_ID" == "" ] ; then
    echo "Usage:"
    echo "   launch_video [--storage path] video-id"
    exit 1
fi

if [ ! -f "$SVISHA_PATH/database.json" ] ; then
    echo "Can not find $SVISHA_PATH/database.json"
    exit 1
fi

key=$(jq --arg v "$VIDEO_ID" --raw-output '.entries[] | select(.id == $v) | .key' <"$SVISHA_PATH/database.json")
if [ "$key" == "" ] ; then
    echo "Video with id $VIDEO_I not foind"
    exit 1
fi

(sleep 2; firefox --private-window "http://127.0.0.1:$PORT/index.html#$key") &
http-server "$SVISHA_PATH/$VIDEO_ID" -p $PORT -a 127.0.0.1 --gzip
# httpid=$!
# trap exit "kill -2 $httpid"
