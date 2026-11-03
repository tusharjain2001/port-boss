#!/bin/bash
# M3.1 invite test: a LAN host presses I (the invite list with the address), then plays a Steam crew with made-up
# friends and Steam's overlay off (as on Manan's PC): the list, the left note saying why, inviting one, the crew code in
# the pause menu, the crew code's round trip. usage: invitetest.sh <exe> [name]
. "$(dirname "$0")/env.sh"
EXE=$1; NAME=${2:-invite}
D=$PB_LOGS/m1/$NAME; rm -rf "$D"; mkdir -p "$D/shots"
hidden "$EXE" -batchmode -pb-host -pb-port $((7700 + RANDOM % 90)) -pb-name Manan -pb-fps 60 -pb-invitetest "$(cygpath -m "$D/shots")" -pb-quit 60 -pb-voicetest -logFile "$(cygpath -m "$D/host.txt")"
echo "== exceptions $(grep -c Exception "$D/host.txt")"
grep -E "\[PB\] (invite|RESULT|coop: invite)" "$D/host.txt"
echo "pictures: $D/shots"
