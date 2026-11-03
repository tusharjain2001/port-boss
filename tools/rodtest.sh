#!/bin/bash
# M3.1 item 4: the rod through the catch. A host and one bot rider on this PC over LAN, hidden. The rider fishes
# until it lands one; both check the rod stays in hand the first second, then goes on the back, and take pictures.
# usage: rodtest.sh <exe> <name> [extra switches for both]
. "$(dirname "$0")/env.sh"
EXE=$1; NAME=$2; shift 2
D=$PB_LOGS/m1/$NAME; rm -rf "$D"; mkdir -p "$D/shots"; SH=$(cygpath -m "$D/shots")
PORT=$((7600 + RANDOM % 90))
hidden "$EXE" -batchmode -pb-host -pb-port $PORT -pb-name Host -pb-fps 60 -pb-rodtest host -pb-rodshots "$SH" -pb-quit 400 -pb-voicetest "$@" -logFile "$(cygpath -m "$D/host.txt")" &
sleep 4
hidden "$EXE" -batchmode -pb-join 127.0.0.1:$PORT -pb-name Rider1 -pb-fps 60 -pb-rodtest rider -pb-rodshots "$SH" -pb-quit 400 -pb-voicetest "$@" -logFile "$(cygpath -m "$D/rider1.txt")" &
wait
for f in "$D"/host.txt "$D"/rider1.txt; do
  echo "== $(basename "$f" .txt): exceptions $(grep -c Exception "$f")"
  grep -E "\[PB\] (rod|RESULT)" "$f" | head -40
done
echo "pictures: $SH"
