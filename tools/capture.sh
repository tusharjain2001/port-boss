#!/bin/bash
# 75 s trailer capture of a build: capture.sh <PortBoss.exe> <out.mp4>
# The game renders its own frames (-pb-film: title flyovers, then a bot shift; fixed 30 fps game time) and ffmpeg joins them.
# It needs a visible game window for ~3 minutes, so it only runs with WINDOW=1. Never record the desktop (gdigrab): it grabs whatever is on top.
. "$(dirname "$0")/env.sh"
[ "$WINDOW" = 1 ] || { echo "capture.sh opens a game window; rerun with WINDOW=1 when nobody is using the PC"; exit 2; }
EXE=$1; OUT=$2; F="$PB_LOGS/film"; rm -rf "$F"; mkdir -p "$F"
timeout 900 "$EXE" -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -pb-film "$(cygpath -w "$F")" -logFile "$(cygpath -w "$PB_LOGS/film.log")" >/dev/null 2>&1
grep "\[PB\] film done" "$PB_LOGS/film.log"
ffmpeg -y -loglevel error -framerate 30 -i "$F/f_%05d.jpg" -c:v libx264 -crf 20 -pix_fmt yuv420p "$OUT"
ls -la "$OUT"
