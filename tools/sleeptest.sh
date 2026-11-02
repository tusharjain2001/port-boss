#!/bin/bash
# M3 co-op sleep test (gdd systems §5.2 -pb-sleeptest): a host and one bot rider on this PC over LAN, headless and hidden.
# The host sleeps in Walt's cot at 21:00 on Days 1-4 while the rider is (1) fighting a fish (the skip waits for it),
# (2) swimming (wakes on a ladder), (3) driving (Biscuit anchors itself, wakes at the tiller), (4) on the deck; then (5)
# both are up at 02:00 and pass out together (one skip). Each machine checks where it woke; the clocks must agree.
# usage: sleeptest.sh <exe> <name> [extra switches for both, e.g. -pb-netsim 150 2]
. "$(dirname "$0")/env.sh"
EXE=$1; NAME=$2; shift 2
D=$PB_LOGS/m1/$NAME; rm -rf "$D"; mkdir -p "$D"
PORT=$((7900 + RANDOM % 90))
hidden "$EXE" -batchmode -pb-host -pb-port $PORT -pb-name Host -pb-seed 7 -pb-hour 20.5 -pb-sleeptest host -pb-quit 260 "$@" -logFile "$(cygpath -m "$D/host.txt")" &
sleep 4
hidden "$EXE" -batchmode -pb-join 127.0.0.1:$PORT -pb-name Rider1 -pb-sleeptest rider -pb-quit 260 "$@" -logFile "$(cygpath -m "$D/rider1.txt")" &
wait
for f in "$D"/host.txt "$D"/rider1.txt; do
  echo "== $(basename "$f" .txt): exceptions $(grep -c Exception "$f")"
  grep -E "\[PB\] (m3|sleep|RESULT|coop: day)" "$f" | head -60
done
# both machines' clocks after each round (in-game minutes; within 1 minute)
paste <(grep -ho "clock after round [0-9]: Day [0-9]* [0-9.]*" "$D/host.txt" | awk '{print $7}') <(grep -ho "clock after round [0-9]: Day [0-9]* [0-9.]*" "$D/rider1.txt" | awk '{print $7}') | awk '{ d = $1 - $2; if (d < 0) d = -d; if (d > m) m = d; n++ } END { printf "== clocks: %d rounds, host and rider within %.2f in-game min: %s\n", n, m, (n == 5 && m < 1 ? "PASS" : "FAIL") }'
