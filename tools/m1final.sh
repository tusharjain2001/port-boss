#!/bin/bash
# M1b proof run, one game at a time, every launch hidden: shots, cast facing x8 (before/after), tiller, idle rod,
# the tour (game camera = the capture; side camera; heavy fish), scroll test, fishtest at 1x.
# usage: m1final.sh <exe> <outdir> [fishtest casts at 1x, default 40]
. "$(dirname "$0")/env.sh"
EXE=$1; OUT=$2; N=${3:-40}; mkdir -p "$OUT"; T="$(dirname "$0")"
w() { cygpath -w "$1"; }
run() { local n=$1; shift; mkdir -p "$OUT/$n"; hidden "$EXE" -batchmode "$@" -logFile "$(w "$OUT/$n/log.txt")"; echo "== $n: exceptions $(grep -c Exception "$OUT/$n/log.txt")"; grep -E "\[PB\] (RESULT|cast8 aim|helm clearance|scroll )" "$OUT/$n/log.txt"; }
sheet() { # <mp4> <start s> <secs> <out> : frames 0.25 s apart, 6 across
  ffmpeg -y -loglevel error -ss "$2" -t "$3" -i "$1" -vf "fps=4,scale=320:-1,tile=6x$(python -c "import math;print(math.ceil($3*4/6))")" -frames:v 1 "$4"; }

run shots -pb-quit 300 -pb-shots "$(w "$OUT/shots")"
run cast8-before -pb-quit 240 -pb-cast8 "$(w "$OUT/cast8-before")" -pb-oldfacing
run cast8 -pb-quit 240 -pb-cast8 "$(w "$OUT/cast8")"
run helm -pb-quit 120 -pb-helmshots "$(w "$OUT/helm")"
run idle -pb-quit 150 -pb-idleshots "$(w "$OUT/idle")"
run scroll -pb-quit 150 -pb-scrolltest -pb-icons "$(w "$OUT/scroll/icons")"
python -I "$T/grid.py" "$OUT/cast8-before.jpg" 4 480 "$OUT"/cast8-before/*.png
python -I "$T/grid.py" "$OUT/cast8-after.jpg" 4 480 "$OUT"/cast8/*.png
python -I "$T/grid.py" "$OUT/tiller-4-angles.jpg" 2 800 "$OUT"/helm/helm-*.png
python -I "$T/grid.py" "$OUT/idle-rod-3-distances.jpg" 3 640 "$OUT"/idle/*.png

for v in game side heavy; do
  case $v in game) a="";; side) a="-pb-side";; heavy) a="-pb-forceheavy -pb-fightheavy";; esac
  echo "== tour $v"; bash "$T/m1tour.sh" "$EXE" "$OUT/tour-$v" $a | grep -E "exceptions|RESULT|tour fish"
  S=$(sed -n 's/.*film start \([0-9.]*\).*/\1/p' "$OUT/tour-$v/log.txt")
  M=$(sed -n 's/.*tour beat Minigame at \([0-9.]*\).*/\1/p' "$OUT/tour-$v/log.txt" | head -1)
  B=$(sed -n 's/.*tour beat Bite at \([0-9.]*\).*/\1/p' "$OUT/tour-$v/log.txt" | head -1)
  [ -n "$M" ] && sheet "$OUT/tour-$v/tour.mp4" "$(python -c "print(max(0,$M-$S-1.5))")" 6 "$OUT/tour-$v/minigame-sheet.jpg"
  [ -n "$B" ] && sheet "$OUT/tour-$v/tour.mp4" "$(python -c "print(max(0,$B-$S-0.5))")" 9 "$OUT/tour-$v/bite-strike-reel-reveal-sheet.jpg"
done

run fishtest-1x -pb-quit 1500 -pb-fishtest "$N" -pb-csv "$(w "$OUT/fishtest-1x/fishtest.csv")"
echo "== done"; df -h /c | tail -1
