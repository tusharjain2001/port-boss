#!/bin/bash
# Host a saved world from the title (systems §1 "anyone can host", §3.5, §4.5), hidden, batch mode. 1) -pb-savetest write
# makes a lived-in world; 2) a host bot picks "Host on this network" -> "Which world?" -> that world: the crew must play
# its day and bank; 3) a join bot joins from the title and must see the same bank. usage: hostsavetest.sh <exe> [name]
. "$(dirname "$0")/env.sh"
EXE=$1; NAME=${2:-hostsave}; D="${PB_LOGS:?}/m1/${NAME:?}"
rm -rf "${PB_LOGS:?}/m1/${NAME:?}"; mkdir -p "$D/saves" "$D/joiner" "$D/shots"; W() { cygpath -m "$1"; }
hidden "$EXE" -batchmode -pb-seed 7 -pb-savetest write -pb-savedir "$(W "$D/saves")" -pb-quit 120 -logFile "$(W "$D/save-write.txt")"
CASH=$(grep -ho '"cash":[0-9]*' "$D"/saves/*/meta.json | head -1 | cut -d: -f2)
echo "== the saved world: $(basename "$(ls -d "$D"/saves/w*/ | head -1)") bank \$${CASH:-?} ($(grep -ho '\[PB\] RESULT.*' "$D/save-write.txt"))"
hidden "$EXE" -batchmode -pb-menutest hostsaved -pb-name Tushar -pb-savedir "$(W "$D/saves")" -pb-menushots "$(W "$D/shots")" -pb-quit 90 -pb-voicetest -logFile "$(W "$D/host.txt")" &
sleep 5
hidden "$EXE" -batchmode -pb-menutest join -pb-menuaddr 127.0.0.1 -pb-menucash "${CASH:-0}" -pb-name Manan -pb-savedir "$(W "$D/joiner")" -pb-quit 90 -pb-voicetest -logFile "$(W "$D/join.txt")" &
wait
for f in "$D"/host.txt "$D"/join.txt; do echo "== $(basename "$f" .txt): exceptions $(grep -c Exception "$f")"; grep -E "\[PB\] (menu|RESULT)" "$f"; done
echo "pictures: $D/shots"
