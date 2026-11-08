#!/bin/bash
# M7 (the demo polish): every M7 check, headless and hidden. usage: m7test.sh <exe> [proofdir]
#  1. -pb-storytest: the story driver solo (Walt's texts, journal page 1, the cast and sale beats, Walt's call answered
#     and one declined, the Day 4 letter, the Journal, Act 2 on Day 5), with a still of every card
#  1b. -pb-openingtest -pb-opening -pb-coldstart: a new game's first morning (the cot, the desk letter, Biscuit half full
#     and cold, bail her, three pulls, Walt's text, drive out), with stills
#  1c. -pb-pelicantest: Amara's order end to end at The Pelican (Deal, moor at its jetty, deliver, Walt), with stills
#  2. -pb-storycoop host + rider with -pb-netsim 150 2: Walt welcomes the rider, the rider's rod raises page 1 through the
#     host, the call rings on both, the letter reaches the rider
# (the story's plain rules: tools/companycheck.sh, Company.StoryTest)
. "$(dirname "$0")/env.sh"
EXE=$1; PROOF=${2:-$PB_LOGS/m7}; D=$PB_LOGS/m7; rm -rf "$D"; mkdir -p "$D" "$PROOF/stills"
W() { cygpath -m "$1"; }
sum() { echo "== $1: exceptions $(grep -c Exception "$2"), $(grep -ho '\[PB\] RESULT.*' "$2" | tr '\n' ' ')"; grep -E 'FAIL' "$2" | grep -v RESULT | head -40; }

hidden "$EXE" -batchmode -pb-seed 7 -pb-fps 60 -pb-storytest "$(W "$PROOF/stills")" -pb-quit 300 -logFile "$(W "$D/storytest.txt")"
sum storytest "$D/storytest.txt"
hidden "$EXE" -batchmode -pb-seed 7 -pb-fps 60 -pb-opening -pb-coldstart -pb-openingtest "$(W "$PROOF/opening")" -pb-quit 240 -logFile "$(W "$D/openingtest.txt")"
sum openingtest "$D/openingtest.txt"
hidden "$EXE" -batchmode -pb-seed 7 -pb-fps 60 -pb-pelicantest "$(W "$PROOF/pelican")" -pb-quit 240 -logFile "$(W "$D/pelicantest.txt")"
sum pelicantest "$D/pelicantest.txt"

PORT=$((7900 + RANDOM % 90))
hidden "$EXE" -batchmode -pb-host -pb-port $PORT -pb-name Host -pb-seed 7 -pb-storycoop host -pb-netsim 150 2 -pb-quit 240 -logFile "$(W "$D/coop-host.txt")" &
sleep 4
hidden "$EXE" -batchmode -pb-join 127.0.0.1:$PORT -pb-name Rider1 -pb-storycoop rider -pb-netsim 150 2 -pb-quit 240 -logFile "$(W "$D/coop-rider.txt")" &
wait
sum "storycoop host" "$D/coop-host.txt"; sum "storycoop rider" "$D/coop-rider.txt"

cp "$D"/*.txt "$PROOF/" 2>/dev/null
echo "== logs: $D  proof: $PROOF"
