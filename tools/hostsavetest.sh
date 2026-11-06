#!/bin/bash
# Host a saved world from the title (systems §1 "anyone can host", §3.5, §4.5; economy §8 a joiner's own things back),
# hidden, batch mode. 1) -pb-savetest write makes a lived-in world. 2) Round 1: a host bot picks "Host on this network" ->
# "Which world?" -> that world (the crew must play its day and bank); a join bot joins from the title holding 3 fish, sees
# the same bank and leaves; the host's save must still hold the joiner's 3 fish, and it saves on quit. 3) Round 2: the
# same world hosted again; the joiner comes back with an empty hotbar (a fresh PC) and must get its own 3 fish back.
# usage: hostsavetest.sh <exe> [name]
. "$(dirname "$0")/env.sh"
EXE=$1; NAME=${2:-hostsave}; D="${PB_LOGS:?}/m1/${NAME:?}"
rm -rf "${PB_LOGS:?}/m1/${NAME:?}"; mkdir -p "$D/saves" "$D/joiner1" "$D/joiner2" "$D/shots"; W() { cygpath -m "$1"; }
hidden "$EXE" -batchmode -pb-seed 7 -pb-savetest write -pb-savedir "$(W "$D/saves")" -pb-quit 120 -logFile "$(W "$D/save-write.txt")"
CASH=$(grep -ho '"cash":[0-9]*' "$D"/saves/*/meta.json | head -1 | cut -d: -f2)
echo "== the saved world: $(basename "$(ls -d "$D"/saves/w*/ | head -1)") bank \$${CASH:-?} ($(grep -ho '\[PB\] RESULT.*' "$D/save-write.txt"))"
round() { # <n> <host switches> -- <join switches>
  local n=$1; shift; local h=(); while [ "$1" != "--" ]; do h+=("$1"); shift; done; shift
  hidden "$EXE" -batchmode -pb-menutest hostsaved -pb-name Tushar -pb-savedir "$(W "$D/saves")" -pb-quit 90 -pb-voicetest "${h[@]}" -logFile "$(W "$D/host$n.txt")" &
  sleep 5
  hidden "$EXE" -batchmode -pb-menutest join -pb-menuaddr 127.0.0.1 -pb-menucash "${CASH:-0}" -pb-name Manan -pb-savedir "$(W "$D/joiner$n")" -pb-quit 90 -pb-voicetest "$@" -logFile "$(W "$D/join$n.txt")" &
  wait
}
round 1 -pb-menushots "$(W "$D/shots")" -pb-menukeep 3 -- -pb-menufish 3
round 2 -- -pb-menuwantfish 3
for f in "$D"/host1.txt "$D"/join1.txt "$D"/host2.txt "$D"/join2.txt; do echo "== $(basename "$f" .txt): exceptions $(grep -c Exception "$f")"; grep -E "\[PB\] (menu|RESULT)|from the save|brought" "$f"; done
echo "pictures: $D/shots"
