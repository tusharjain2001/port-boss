#!/bin/bash
# Full regression: dev build, then every scripted test (+ optional A/B shots vs an older build). Usage: regress.sh [outdir]
#   WINDOW=1     run the menu/settings/night/character shots in a game window (IMGUI is only drawn windowed); default batchmode
#   OLD_EXE=...  an older PortBoss.exe to A/B the spawn camera against (skipped when unset)
#   The co-op step finds the host through a Steam lobby: the Steam client must be running and logged in (else use reg2.sh, LAN).
. "$(dirname "$0")/env.sh"
O=${1:-$PB_LOGS/regress}; rm -rf "$O"; mkdir -p "$O"
DEV="$PROJECT/Builds/dev"; EXE="$DEV/PortBoss.exe"
W="-screen-fullscreen 0 -screen-width 1600 -screen-height 900"
B="-batchmode"
run() { # name, seconds, args...   (WIN=1 asks for a window; only honoured with WINDOW=1)
  local n=$1 q=$2 win=${WINDOW:+$WIN} L="$O/$1/log.txt"; shift 2; mkdir -p "$O/$n"
  timeout $((q+40)) "$EXE" ${win:+$W} ${win:-$B} -pb-shots "$(cygpath -w "$O/$n")" ${SHOT:-5} -pb-quit $q -logFile "$(cygpath -w "$L")" "$@" >/dev/null 2>&1
  echo "== $n: exceptions=$(grep -c Exception "$L")"; grep -E "\[PB\] (swim|pier|knock|contract|wobble|trolley|sold|started|snap|carry test|stage test|map check: .* placed)" "$L" | head -12
  grep "\[PB\] t=" "$L" | tail -1 | sed -E 's/listening=True //'
}
bash "$TOOLS/ubuild.sh" "$DEV" -pbDev | grep -E "error|PBBuild\] build"
WIN=1 SHOT=6 run menu 9
WIN=1 SHOT=4 run settings 6 -pb-settings 0
run solo 70 -pb-solo -pb-bot
SHOT=0.5 run wobble 14 -pb-solo -pb-wobbletest
run trolley 20 -pb-solo -pb-trolleytest
SHOT=1.5 run swim 26 -pb-solo -pb-swimtest
SHOT=3 run pier 12 -pb-solo -pb-piertest
SHOT=0.5 run knock 8 -pb-solo -pb-knocktest
SHOT=8 run contract 50 -pb-solo -pb-contracttest
SHOT=6 run day2 70 -pb-solo -pb-bot -pb-day 2 -pb-timescale 4
WIN=1 SHOT=3 run night 26 -pb-solo -pb-bot -pb-night 18
SHOT=4 run storm 60 -pb-solo -pb-storm -pb-timescale 3
SHOT=1 run forklift 20 -pb-solo -pb-forklifttest
WIN=1 run chars 14 -pb-solo -pb-charshots "$(cygpath -w "$O/chars")"
# A/B against an older build: same spawn camera, no bot.
if [ -n "$OLD_EXE" ]; then
  for b in old new; do E=$EXE; [ $b = old ] && E="$OLD_EXE"; mkdir -p "$O/ab-$b"
    timeout 60 "$E" -batchmode -pb-solo -pb-shots "$(cygpath -w "$O/ab-$b")" 4 -pb-quit 38 -logFile "$(cygpath -w "$O/ab-$b/log.txt")" >/dev/null 2>&1; done
  echo "A/B shots: $O/ab-old $O/ab-new (side by side: python tools/pair.py a.png b.png out.png)"
fi
# Co-op: host (LAN mode + Steam lobby) and a client that finds the lobby.
C="$O/coop"; mkdir -p "$C"; (timeout 100 "$EXE" -batchmode -pb-host-lan -pb-bot -pb-quit 60 -logFile "$(cygpath -w "$C/host.log")" >/dev/null 2>&1 &); sleep 6
timeout 100 "$EXE" -batchmode -pb-join-lobby -pb-bot -pb-quit 52 -logFile "$(cygpath -w "$C/client.log")" >/dev/null 2>&1; sleep 10
echo "== coop"; grep -E "\[PB\] (lobby|joined)" "$C"/*.log; grep "\[PB\] t=" "$C/host.log" | tail -1; grep -c Exception "$C/host.log" "$C/client.log"
