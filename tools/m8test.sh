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
#  7. -pb-wavetest: the surf at Breaker Point (a forced 1.5 m face): a set, the breaks, white water, the sand dry; dusk; the
#     Widowmaker at Gullrock (frames)
#  8. -pb-surftest: surfing (the foamie: on, paddle, duck-dive, pop-up, ride, the score, Kai's challenges 2-3, a wipeout,
#     the leash; the bodyboard; the Journal's surf card) with chase-cam frames
#  9. -pb-surfcoop host + rider: a party wave, then a drop-in (both down)
# (the plain rules: tools/companycheck.sh, Company.TackleTest, Company.SurfTest; if Windows Smart App Control blocks the
#  companycheck exe, run the same calls in the editor)
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

hidden "$EXE" -batchmode -pb-seed 7 -pb-fps 60 -pb-wavetest "$(W "$PROOF/waves")" -pb-quit 600 -logFile "$(W "$D/wavetest.txt")"
sum wavetest "$D/wavetest.txt"
hidden "$EXE" -batchmode -pb-seed 7 -pb-fps 60 -pb-surftest "$(W "$PROOF/surf")" -pb-quit 900 -logFile "$(W "$D/surftest.txt")"
sum surftest "$D/surftest.txt"

PORT=$((7900 + RANDOM % 90))
hidden "$EXE" -batchmode -pb-host -pb-port $PORT -pb-name Host -pb-seed 7 -pb-surfcoop host -pb-netsim 150 2 -pb-quit 500 -logFile "$(W "$D/surfcoop-host.txt")" &
sleep 4
hidden "$EXE" -batchmode -pb-join 127.0.0.1:$PORT -pb-name Rider1 -pb-surfcoop rider -pb-surfshots "$(W "$PROOF/surfcoop")" -pb-netsim 150 2 -pb-quit 500 -logFile "$(W "$D/surfcoop-rider.txt")" &
wait
sum "surfcoop host" "$D/surfcoop-host.txt"; sum "surfcoop rider" "$D/surfcoop-rider.txt"

cp "$D"/*.txt "$PROOF/" 2>/dev/null
echo "== logs: $D  proof: $PROOF"
