#!/bin/bash
# M1 animation proof: films the tour (-pb-tour: walk the deck, 2 casts to the catch, tiller + drive) headless, makes
# an MP4 and a contact sheet of the first cast at 0.25 s steps.  usage: m1tour.sh <exe> <outdir> [-pb-side]
. "$(dirname "$0")/env.sh"
EXE=$1; OUT=$2; shift 2; F="$OUT/frames"; rm -rf "$F"; mkdir -p "$F"
hidden "$EXE" -batchmode -pb-tour "$(cygpath -w "$F")" -pb-quit 200 "$@" -logFile "$(cygpath -w "$OUT/log.txt")"
echo "exceptions: $(grep -c Exception "$OUT/log.txt")"; grep -E "\[PB\] (tour|film|RESULT)" "$OUT/log.txt"
ffmpeg -y -loglevel error -framerate 30 -i "$F/f_%05d.jpg" -c:v libx264 -crf 21 -pix_fmt yuv420p "$OUT/tour.mp4"
# the first cast: from the charge to 6 s later, a frame every 0.25 s (every 8th... 7.5 frames: use fps filter)
S=$(sed -n 's/.*film start \([0-9.]*\).*/\1/p' "$OUT/log.txt"); C=$(sed -n 's/.*tour cast 1 at \([0-9.]*\).*/\1/p' "$OUT/log.txt")
SS=$(python -c "print(max(0,$C-$S-0.2))")
# 24 frames left to right, top to bottom, 0.25 s apart, from just before the charge
ffmpeg -y -loglevel error -ss "$SS" -t 6 -i "$OUT/tour.mp4" -vf "fps=4,scale=320:-1,tile=6x4" -frames:v 1 "$OUT/cast-sheet.jpg"
# walking the deck (from the first step) and the tiller (sitting down, then motoring off), same 0.25 s steps
W=$(sed -n 's/.*tour walk \([0-9.]*\).*/\1/p' "$OUT/log.txt"); H=$(sed -n 's/.*tour helm \([0-9.]*\).*/\1/p' "$OUT/log.txt")
ffmpeg -y -loglevel error -ss "$(python -c "print(max(0,$W-$S))")" -t 6 -i "$OUT/tour.mp4" -vf "fps=4,scale=320:-1,tile=6x4" -frames:v 1 "$OUT/walk-sheet.jpg"
ffmpeg -y -loglevel error -ss "$(python -c "print(max(0,$H-$S+0.5))")" -t 6 -i "$OUT/tour.mp4" -vf "fps=4,scale=320:-1,tile=6x4" -frames:v 1 "$OUT/drive-sheet.jpg"
rm -rf "$F" # the frames are big; the mp4 keeps them
ls -la "$OUT"/*.mp4 "$OUT"/*.jpg
