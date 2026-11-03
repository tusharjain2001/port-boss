#!/bin/bash
# M2 co-op test (gdd systems §5.2): a host and N bot clients on this PC over LAN, headless and hidden. The riders stand
# on Biscuit's deck while the host drives; then the tiller moves to rider 1 and back; then rider 2 swims and climbs the
# ladder back aboard and rider 3 is knocked over and gets up (M2 step 2), seen by everyone else; then riders 1 and 2
# fish and riders 1 and 3 tangle their lines (step 3), seen by everyone else; then giving, tossing, a race for a deck
# fish and ~10,000 random hotbar requests from all four at once, every hotbar matching the host's book after (step 4);
# then voice (step 5): a made-up test voice (-pb-voicetest, never the mic), heard aboard, fainter at 20 m, gone at 45 m;
# then rider 1 quits and a new copy of it rejoins (step 6): its hotbar, crew member and place come back.
# usage: cooptest.sh <exe> <name> [riders=3] [extra switches for every copy, e.g. -pb-netsim 150 2]
. "$(dirname "$0")/env.sh"
EXE=$1; NAME=$2; N=${3:-3}; shift 3 2>/dev/null || shift $#
D=$PB_LOGS/m1/$NAME; rm -rf "$D"; mkdir -p "$D"
PORT=$((7800 + RANDOM % 100))
hidden "$EXE" -batchmode -pb-host -pb-port $PORT -pb-name Host -pb-cooptest host -pb-coopn $N -pb-quit 320 -pb-voicetest "$@" -logFile "$(cygpath -m "$D/host.txt")" &
sleep 4
for i in $(seq 1 $N); do
  V=""; [ $i = 3 ] && V="-pb-voice off -pb-solofish 1" # (rider 3's mic is off: nothing of it must be heard; it joins holding a fish caught alone)
  hidden "$EXE" -batchmode -pb-join 127.0.0.1:$PORT -pb-name Rider$i -pb-cooptest rider -pb-coopn $N -pb-quit 320 -pb-voicetest $V "$@" -logFile "$(cygpath -m "$D/rider$i.txt")" &
  [ $i = 1 ] && R1=$!
  sleep 1
done
# step 6: rider 1 logs its hotbar and stays connected; a new copy of it (the same name) joins at once, as a player back
# before the old connection timed out (code audit E): the host must retire the old one
until grep -q "rejoin before" "$D/rider1.txt" 2>/dev/null || ! kill -0 $R1 2>/dev/null; do sleep 1; done
hidden "$EXE" -batchmode -pb-join 127.0.0.1:$PORT -pb-name Rider1 -pb-cooptest rejoin -pb-coopn $N -pb-solofish 2 -pb-quit 120 -pb-voicetest "$@" -logFile "$(cygpath -m "$D/rider1-rejoin.txt")" &
wait
for f in "$D"/host.txt "$D"/rider*.txt; do
  echo "== $(basename "$f" .txt): exceptions $(grep -c Exception "$f")"
  grep -E "\[PB\] (coop|RESULT)" "$f" | grep -vE "coop: (rest|bob|fished|toss [0-9]+ lands|.* picked up fish) " | head -40
done
# step 2: the knocked-over rider's hips at rest on its own deck against where every other machine showed them
cat "$D"/*.txt | grep -o "\[PB\] coop: rest .*" | awk '
  $4 == "self" { sx = $5; sy = $6; sz = $7; self = 1; next }
  { n++; x[n] = $5; y[n] = $6; z[n] = $7 }
  END {
    if (!self || n == 0) { print "== knock pose: not measured"; exit }
    m = 0; for (i = 1; i <= n; i++) { d = sqrt((x[i]-sx)^2 + (y[i]-sy)^2 + (z[i]-sz)^2); if (d > m) m = d }
    printf "== knock pose: %d machines showed the hips at rest within %.1f cm of where they lay on their own deck: %s\n", n, m * 100, (m < 0.1 ? "PASS" : "FAIL")
  }'
# step 3: where each rider's float came down against where every other machine showed it (the n-th cast of each)
cat "$D"/*.txt | grep -o "\[PB\] coop: bob .*" | awk '
  { k = $4" "$5; if ($8 == "self") { selfx[k] = $6; selfz[k] = $7 } else { n++; key[n] = k; x[n] = $6; z[n] = $7 } }
  END {
    m = 0; c = 0
    for (i = 1; i <= n; i++) { k = key[i]; if (!(k in selfx)) continue; c++; d = sqrt((x[i]-selfx[k])^2 + (z[i]-selfz[k])^2); if (d > m) m = d }
    if (c == 0) { print "== floats: not measured"; exit }
    printf "== floats: %d sightings of crewmate floats, all within %.1f cm of where they came down: %s\n", c, m * 100, (m < 0.05 ? "PASS" : "FAIL")
  }'
# step 6: rider 1's hotbar and place before it quit against the new copy's after it rejoined
B=$(grep -ho "rejoin before: .*" "$D"/rider1.txt | head -1); A=$(grep -ho "rejoin after: .*" "$D"/rider1-rejoin.txt | head -1)
if [ -z "$B" ] || [ -z "$A" ]; then echo "== rejoin: not measured"; else
  echo "$B|$A" | awk -F'|' '{
    split($1, b, " "); split($2, a, " ");
    d = sqrt((b[5]-a[5])^2 + (b[6]-a[6])^2 + (b[7]-a[7])^2);
    n = (b[3] == "ids=") ? 0 : split(substr(b[3], 5), x, ","); m = (a[3] == "ids=") ? 0 : split(substr(a[3], 5), y, ",");
    for (i = 1; i <= m; i++) has[y[i]] = 1; miss = 0; for (i = 1; i <= n; i++) if (!(x[i] in has)) miss++;
    solo = a[13] + 0; kept = a[15] + 0; ids = (miss == 0 && kept == solo);
    printf "== rejoin: hotbar %s (%d fish, the same ids; %d of %d caught alone while away kept, in the hotbar or on the deck), crew %s -> %s, place back within %.1f cm: %s\n", (ids ? "back" : "DIFFERENT"), n, kept, solo, b[9], a[9], d * 100, (ids && b[9] == a[9] && d < 0.1 ? "PASS" : "FAIL")
  }'
fi
