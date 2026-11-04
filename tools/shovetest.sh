#!/bin/bash
# M3.1 item 6: the friendly shove (G). A host and one bot rider on this PC over LAN, hidden: the host shoves the rider
# on the jetty, with its line out, fighting a fish, at the rail; each checks its side, pictures.
# usage: shovetest.sh <exe> <name> [extra switches for both]
. "$(dirname "$0")/env.sh"
EXE=$1; NAME=$2; shift 2
D=$PB_LOGS/m1/$NAME; rm -rf "$D"; mkdir -p "$D/shots"; SH=$(cygpath -m "$D/shots")
PORT=$((7600 + RANDOM % 90))
# FILM=<secs>: both copies also film themselves (1280x720 jpg frames in film-host/ and film-rider/; both slowed alike)
FH=(); FR=(); F=(-pb-filmsecs "$FILM" -pb-filmw 1280 -pb-filmh 720)
[ -n "$FILM" ] && { FH=(-pb-film "$(cygpath -m "$D/film-host")" "${F[@]}"); FR=(-pb-film "$(cygpath -m "$D/film-rider")" "${F[@]}"); }
hidden "$EXE" -batchmode -pb-host -pb-port $PORT -pb-name Host -pb-fps 60 "${FH[@]}" -pb-shovetest host -pb-shoveshots "$SH" -pb-quit 240 -pb-voicetest "$@" -logFile "$(cygpath -m "$D/host.txt")" &
sleep 4
hidden "$EXE" -batchmode -pb-join 127.0.0.1:$PORT -pb-name Rider1 -pb-fps 60 "${FR[@]}" -pb-shovetest rider -pb-shoveshots "$SH" -pb-quit 240 -pb-voicetest "$@" -logFile "$(cygpath -m "$D/rider1.txt")" &
wait
for f in "$D"/host.txt "$D"/rider1.txt; do
  echo "== $(basename "$f" .txt): exceptions $(grep -c Exception "$f")"
  grep -E "\[PB\] (shove|m3|RESULT)" "$f" | head -40
done
echo "pictures: $SH"
