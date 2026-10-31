#!/bin/bash
# M1b animation checks, one game at a time: 8-direction cast facing (before/after), tiller shots, side-view tour.
# usage: m1animcheck.sh <exe> <outdir>
. "$(dirname "$0")/env.sh"
EXE=$1; OUT=$2; mkdir -p "$OUT"
run() { local n=$1; shift; mkdir -p "$OUT/$n"; hidden "$EXE" -batchmode -pb-quit 240 "$@" -logFile "$(cygpath -w "$OUT/$n/log.txt")"; echo "== $n: exceptions $(grep -c Exception "$OUT/$n/log.txt")"; grep -E "\[PB\] (cast8|RESULT)" "$OUT/$n/log.txt"; }
run cast8-before -pb-cast8 "$(cygpath -w "$OUT/cast8-before")" -pb-oldfacing
run cast8 -pb-cast8 "$(cygpath -w "$OUT/cast8")"
run helm -pb-helmshots "$(cygpath -w "$OUT/helm")"
for d in cast8-before cast8; do ffmpeg -y -loglevel error -pattern_type glob -i "$OUT/$d/*cast8-*.png" -vf "scale=480:-1,tile=4x2" -frames:v 1 "$OUT/$d-grid.jpg"; done
ffmpeg -y -loglevel error -pattern_type glob -i "$OUT/helm/helm-*.png" -vf "scale=640:-1,tile=2x2" -frames:v 1 "$OUT/helm-grid.jpg"
