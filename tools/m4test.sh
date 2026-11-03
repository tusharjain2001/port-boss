#!/bin/bash
# M4 (phone, the first income, selling, save): every M4 check, headless and hidden. usage: m4test.sh <exe> [proofdir]
#  1. EditMode suites (contract + money rules, fish ledger, save migration) in the editor: -executeMethod M4Tests.RunAll
#  2. -pb-firstday: the practised first three days (economy §2.3) through the real shops/phone/nights + every screen shot
#  3. -pb-firstday -pb-film: the same, filmed at 30 fps -> firstday.mp4
#  4. -pb-savetest write, then read in a second process (+ the schema 1 fixture): the world hash must match
#  5. -pb-banktest host + rider over LAN: one company bank, the rider's sale/buy/accept, the rent, the morning copy
# Set SKIP_EDIT=1 to skip 1 (the editor), SKIP_FILM=1 to skip 3.
. "$(dirname "$0")/env.sh"
EXE=$1; PROOF=${2:-$PB_LOGS/m4}; D=$PB_LOGS/m4; rm -rf "$D"; mkdir -p "$D" "$PROOF"
W() { cygpath -m "$1"; }
sum() { echo "== $1: exceptions $(grep -c Exception "$2"), $(grep -ho '\[PB\] RESULT.*' "$2" | tr '\n' ' ')"; grep -E '\[PB\] m4(test)?: (FAIL|firstday \|)' "$2" | head -40; }

if [ -z "$SKIP_EDIT" ]; then
  hidden "$UNITY" -batchmode -nographics -projectPath "$PROJECT" -logFile "$(W "$D/edit.txt")" -executeMethod PortBoss.M1.M4Tests.RunAll
  grep -E '\[PB\] m4test' "$D/edit.txt"; echo "== edit: compile errors $(grep -c 'error CS' "$D/edit.txt")"
fi

hidden "$EXE" -batchmode -pb-seed 7 -pb-firstday "$(W "$PROOF/phone")" -pb-quit 400 -logFile "$(W "$D/firstday.txt")"
sum firstday "$D/firstday.txt"
hidden "$EXE" -batchmode -pb-seed 7 -pb-firstday - -pb-grades game -pb-quit 400 -logFile "$(W "$D/firstday-game.txt")"
sum "firstday (the game's own grade odds)" "$D/firstday-game.txt"

if [ -z "$SKIP_FILM" ]; then
  F=$D/film; hidden "$EXE" -batchmode -pb-seed 7 -pb-firstday - -pb-hold 1.6 -pb-film "$(W "$F")" -pb-filmw 1280 -pb-filmh 720 -pb-filmsecs 240 -pb-quit 900 -logFile "$(W "$D/film.txt")"
  sum film "$D/film.txt"
  ffmpeg -loglevel error -y -framerate 30 -i "$F/f_%05d.jpg" -c:v libx264 -pix_fmt yuv420p -crf 22 "$PROOF/firstday.mp4" && echo "== film: $PROOF/firstday.mp4"
fi

S=$D/saves; mkdir -p "$S"
hidden "$EXE" -batchmode -pb-seed 7 -pb-savetest write -pb-savedir "$(W "$S")" -pb-quit 120 -logFile "$(W "$D/save-write.txt")"
sum "savetest write" "$D/save-write.txt"
hidden "$EXE" -batchmode -pb-savetest read -pb-savedir "$(W "$S")" -pb-fixture "$(W "$PROJECT/tests/fixtures/save-v1.json")" -pb-quit 120 -logFile "$(W "$D/save-read.txt")"
sum "savetest read" "$D/save-read.txt"
diff -q "$S/world1.json" "$S/world2.json" && echo "== the world JSON after the reload is byte-identical"

PORT=$((7900 + RANDOM % 90)); H=$D/bank-host; R=$D/bank-rider; mkdir -p "$H" "$R"
hidden "$EXE" -batchmode -pb-host -pb-port $PORT -pb-name Host -pb-seed 7 -pb-hour 10 -pb-banktest host -pb-savedir "$(W "$H")" -pb-quit 200 -logFile "$(W "$D/bank-host.txt")" &
sleep 4
hidden "$EXE" -batchmode -pb-join 127.0.0.1:$PORT -pb-name Rider1 -pb-banktest rider -pb-savedir "$(W "$R")" -pb-quit 200 -logFile "$(W "$D/bank-rider.txt")" &
wait
sum "banktest host" "$D/bank-host.txt"; sum "banktest rider" "$D/bank-rider.txt"

cp "$D"/*.txt "$PROOF/" 2>/dev/null; cp "$S/world1.json" "$S/world2.json" "$PROOF/" 2>/dev/null
echo "== logs: $D  proof: $PROOF"
