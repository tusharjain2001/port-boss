#!/bin/bash
# M8 (surfing + fishing gear): every M8 check, headless and hidden. usage: m8test.sh <exe> [proofdir]
#  1. -pb-tackletest: June's Tackle tab (reels, lines, hooks, the Sight float, Use a hook), the strike window and the
#     label; the Barbed hook's retry (miss then hit = Rare, 2 stops); a heavy fish with the Barbed: 3 stops then the snap
#  2. -pb-tacklecoop host + rider with -pb-netsim 150 2: the rider buys tackle through the host and picks a hook; the
#     host's record follows; the rider's 2-stop fight checks out on the host
#  3. -pb-holdertest: rod holders + trolling solo (Biscuit's holder; put, the bell, taken struck and landed; the bell
#     missed and re-baited; taken back; "needs a second rod"; trolling with sardine at ~3 m/s from the tiller, its bite
#     landed; worms at speed: dragged in, the rod rests, taken)
#  4. -pb-holdercoop host + rider with -pb-netsim 150 2: the rider's holder rod on the host's screen, its roll kept aside
#     while the rider casts a second line, the holder bite taken and landed: checked out on the host
#  5. -pb-netcoop host + rider with -pb-netsim 150 2: the landing net (the rider readies it beside the host's fight: one
#     more stop and a wider green; a Rare, the rider scoops it)
#  6. -pb-surfshoptest: Swell Surf Co. (Act 2, Kai's hello, his shelf: the free foamie, the longboard, the wetsuit, the
#     shortboard locked, Carry; a storm shuts him; the jetty placed, Biscuit moored at it; the Forecast's surf line; the
#     wetsuit's stamina; his dawn report)
# (the plain rules: tools/companycheck.sh, Company.TackleTest, Company.SurfTest)
. "$(dirname "$0")/env.sh"
EXE=$1; PROOF=${2:-$PB_LOGS/m8}; D=$PB_LOGS/m8; rm -rf "$D"; mkdir -p "$D" "$PROOF"
W() { cygpath -m "$1"; }
sum() { echo "== $1: exceptions $(grep -c Exception "$2"), $(grep -ho '\[PB\] RESULT.*' "$2" | tr '\n' ' ')"; grep -E 'FAIL' "$2" | grep -v RESULT | head -40; }

hidden "$EXE" -batchmode -pb-seed 7 -pb-fps 60 -pb-tackletest "$(W "$PROOF/tackle")" -pb-quit 300 -logFile "$(W "$D/tackletest.txt")"
sum tackletest "$D/tackletest.txt"

PORT=$((7900 + RANDOM % 90))
hidden "$EXE" -batchmode -pb-host -pb-port $PORT -pb-name Host -pb-seed 7 -pb-tacklecoop host -pb-netsim 150 2 -pb-quit 240 -logFile "$(W "$D/tacklecoop-host.txt")" &
sleep 4
hidden "$EXE" -batchmode -pb-join 127.0.0.1:$PORT -pb-name Rider1 -pb-tacklecoop rider -pb-netsim 150 2 -pb-quit 240 -logFile "$(W "$D/tacklecoop-rider.txt")" &
wait
sum "tacklecoop host" "$D/tacklecoop-host.txt"; sum "tacklecoop rider" "$D/tacklecoop-rider.txt"

hidden "$EXE" -batchmode -pb-seed 7 -pb-fps 60 -pb-holdertest "$(W "$PROOF/holder")" -pb-quit 400 -logFile "$(W "$D/holdertest.txt")"
sum holdertest "$D/holdertest.txt"

PORT=$((7900 + RANDOM % 90))
hidden "$EXE" -batchmode -pb-host -pb-port $PORT -pb-name Host -pb-seed 7 -pb-holdercoop host -pb-netsim 150 2 -pb-quit 300 -logFile "$(W "$D/holdercoop-host.txt")" &
sleep 4
hidden "$EXE" -batchmode -pb-join 127.0.0.1:$PORT -pb-name Rider1 -pb-holdercoop rider -pb-netsim 150 2 -pb-quit 300 -logFile "$(W "$D/holdercoop-rider.txt")" &
wait
sum "holdercoop host" "$D/holdercoop-host.txt"; sum "holdercoop rider" "$D/holdercoop-rider.txt"

PORT=$((7900 + RANDOM % 90))
hidden "$EXE" -batchmode -pb-host -pb-port $PORT -pb-name Host -pb-seed 7 -pb-netcoop host -pb-netsim 150 2 -pb-quit 240 -logFile "$(W "$D/netcoop-host.txt")" &
sleep 4
hidden "$EXE" -batchmode -pb-join 127.0.0.1:$PORT -pb-name Rider1 -pb-netcoop rider -pb-netshots "$(W "$PROOF/net")" -pb-netsim 150 2 -pb-quit 240 -logFile "$(W "$D/netcoop-rider.txt")" &
wait
sum "netcoop host" "$D/netcoop-host.txt"; sum "netcoop rider" "$D/netcoop-rider.txt"

hidden "$EXE" -batchmode -pb-seed 7 -pb-fps 60 -pb-surfshoptest "$(W "$PROOF/surfshop")" -pb-quit 400 -logFile "$(W "$D/surfshoptest.txt")"
sum surfshoptest "$D/surfshoptest.txt"

cp "$D"/*.txt "$PROOF/" 2>/dev/null
echo "== logs: $D  proof: $PROOF"
