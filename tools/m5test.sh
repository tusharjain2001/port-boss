#!/bin/bash
# M5 (shops, upgrades, the wardrobe): every M5 check, headless and hidden. usage: m5test.sh <exe> [proofdir]
#  1. -pb-m5test: the shop transaction suite, the set-spot validator, the paint channel test, every shop / installed item /
#     paint job / outfit as a still (day and night, dry and wet)
#  2. -pb-m5coop host + rider with -pb-netsim 150 2: wardrobe uniqueness, concurrent buys, a duplicate transaction, Ask
#     the crew (OK and Not now on the phone), everyone sees every purchase, paint job and outfit
#  3. (unless SKIP_FILM=1) -pb-m5film -pb-film: the M5 film -> m5-film.mp4
. "$(dirname "$0")/env.sh"
EXE=$1; PROOF=${2:-$PB_LOGS/m5}; D=$PB_LOGS/m5; rm -rf "$D"; mkdir -p "$D" "$PROOF/stills"
W() { cygpath -m "$1"; }
sum() { echo "== $1: exceptions $(grep -c Exception "$2"), $(grep -ho '\[PB\] RESULT.*' "$2" | tr '\n' ' ')"; grep -E '\[PB\] m5: FAIL' "$2" | head -40; }

hidden "$EXE" -batchmode -pb-seed 7 -pb-fps 60 -pb-m5test "$(W "$PROOF/stills")" -pb-quit 900 -logFile "$(W "$D/m5test.txt")"
sum m5test "$D/m5test.txt"

PORT=$((7900 + RANDOM % 90)); mkdir -p "$PROOF/coop"
hidden "$EXE" -batchmode -pb-host -pb-port $PORT -pb-name Host -pb-seed 7 -pb-hour 10 -pb-m5coop host -pb-netsim 150 2 -pb-m5shots "$(W "$PROOF/coop")" -pb-quit 400 -logFile "$(W "$D/coop-host.txt")" &
sleep 4
hidden "$EXE" -batchmode -pb-join 127.0.0.1:$PORT -pb-name Rider1 -pb-m5coop rider -pb-netsim 150 2 -pb-m5shots "$(W "$PROOF/coop")" -pb-quit 400 -logFile "$(W "$D/coop-rider.txt")" &
wait
sum "m5coop host" "$D/coop-host.txt"; sum "m5coop rider" "$D/coop-rider.txt"

if [ -z "$SKIP_FILM" ]; then
  F=$D/film; hidden "$EXE" -batchmode -pb-seed 7 -pb-m5film x -pb-film "$(W "$F")" -pb-filmw 1600 -pb-filmh 900 -pb-filmsecs 100 -pb-quit 900 -logFile "$(W "$D/film.txt")"
  sum film "$D/film.txt"
  ffmpeg -loglevel error -y -framerate 30 -i "$F/f_%05d.png" -c:v libx264 -pix_fmt yuv420p -crf 20 "$PROOF/m5-film.mp4" 2>/dev/null \
    || ffmpeg -loglevel error -y -framerate 30 -i "$F/f_%05d.jpg" -c:v libx264 -pix_fmt yuv420p -crf 20 "$PROOF/m5-film.mp4"
  echo "== film: $PROOF/m5-film.mp4"
fi
cp "$D"/*.txt "$PROOF/" 2>/dev/null
echo "== logs: $D  proof: $PROOF"
