#!/bin/bash
# M2 step 6: the title menu played by two bots on this PC (hidden, batch mode). One picks "Host on this network"; the other
# picks "Join on this network", types a wrong address first (the menu must come back saying so), then 127.0.0.1. Both
# must end up in the crew, each other in the crew list. Pictures of the menu: -pb-menushots.
# usage: menutest.sh <exe> [name] [extra switches for both]
. "$(dirname "$0")/env.sh"
EXE=$1; NAME=${2:-menu}; shift 2 2>/dev/null || shift $#
D=$PB_LOGS/m1/$NAME; rm -rf "$D"; mkdir -p "$D/shots"
SH=$(cygpath -m "$D/shots")
hidden "$EXE" -batchmode -pb-menutest host -pb-name Tushar -pb-menushots "$SH" -pb-quit 90 -pb-voicetest "$@" -logFile "$(cygpath -m "$D/host.txt")" &
sleep 5
hidden "$EXE" -batchmode -pb-menutest join -pb-menuaddr 127.0.0.1 -pb-name Manan -pb-menushots "$SH" -pb-quit 90 -pb-voicetest "$@" -logFile "$(cygpath -m "$D/join.txt")" &
wait
for f in "$D"/host.txt "$D"/join.txt; do
  echo "== $(basename "$f" .txt): exceptions $(grep -c Exception "$f")"
  grep -E "\[PB\] (menu|RESULT)" "$f"
done
echo "pictures: $SH"
