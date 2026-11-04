#!/bin/bash
# M3.1 item 8: what a joiner sees while joining. A host and one bot rider on this PC over LAN, hidden. The rider joins
# fresh, goes aboard and dies without a goodbye; comes back 2 s later (the host still holds the old connection), quits
# properly; comes back 12 s later. Each join checks no frame shows it before it is itself, put back where it was, and
# saves frames. usage: rejointest.sh <exe> <name> [extra switches for the rider, e.g. -pb-oldjoin for the before]
. "$(dirname "$0")/env.sh"
EXE=$1; NAME=$2; shift 2
D=$PB_LOGS/m1/$NAME; rm -rf "$D"; mkdir -p "$D/shots"; SH=$(cygpath -m "$D/shots")
PORT=$((7600 + RANDOM % 90))
hidden "$EXE" -batchmode -pb-host -pb-port $PORT -pb-name Host -pb-fps 60 -pb-rejointest host -pb-rejoinshots "$SH" -pb-quit "${HOSTQUIT:-90}" $HOSTX -pb-voicetest -logFile "$(cygpath -m "$D/host.txt")" &
sleep 4
for c in fresh quick late; do
  [ $c = late ] && sleep 12 || { [ $c = quick ] && sleep "${QGAP:-2}" || sleep 2; }   # QGAP=35: the quick one comes after the host timed out the dead one
  hidden "$EXE" -batchmode -pb-join 127.0.0.1:$PORT -pb-name Rider1 -pb-fps 60 -pb-rejointest $c -pb-rejoinshots "$SH" -pb-quit 40 -pb-voicetest "$@" -logFile "$(cygpath -m "$D/rider-$c.txt")"
done
wait
for f in "$D"/host.txt "$D"/rider-*.txt; do
  echo "== $(basename "$f" .txt): exceptions $(grep -c Exception "$f")"
  grep -E "\[PB\] (rejoin|m3|RESULT)|join view|back where|is back|retiring|left:" "$f" | head -20
done
echo "pictures: $SH"
