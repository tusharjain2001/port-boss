#!/bin/bash
# M3.1 co-op leak test: a host and one bot rider on this PC over LAN, hidden. Biscuit springs a leak; the rider patches it
# (round 1), then the host (round 2); each machine checks the marker, the patcher's kneel-and-hammer pose (its own and the
# crewmate's), the progress and the plank, and takes pictures. usage: leaktest.sh <exe> <name> [extra switches for both]
. "$(dirname "$0")/env.sh"
EXE=$1; NAME=$2; shift 2
D=$PB_LOGS/m1/$NAME; rm -rf "$D"; mkdir -p "$D/shots"; SH=$(cygpath -m "$D/shots")
PORT=$((7600 + RANDOM % 90))
hidden "$EXE" -batchmode -pb-host -pb-port $PORT -pb-name Host -pb-fps 60 -pb-leaktest host -pb-leakshots "$SH" -pb-quit 150 -pb-voicetest "$@" -logFile "$(cygpath -m "$D/host.txt")" &
sleep 4
hidden "$EXE" -batchmode -pb-join 127.0.0.1:$PORT -pb-name Rider1 -pb-fps 60 -pb-leaktest rider -pb-leakshots "$SH" -pb-quit 150 -pb-voicetest "$@" -logFile "$(cygpath -m "$D/rider1.txt")" &
wait
for f in "$D"/host.txt "$D"/rider1.txt; do
  echo "== $(basename "$f" .txt): exceptions $(grep -c Exception "$f")"
  grep -E "\[PB\] (m3|leak|RESULT)" "$f" | head -40
done
echo "pictures: $SH"
