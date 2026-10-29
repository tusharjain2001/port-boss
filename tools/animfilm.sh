#!/bin/bash
# Films the player's animations (-pb-animfilm, batchmode) and turns each clip into an MP4, a GIF and a frame sheet.
# Usage: tools/animfilm.sh [outdir] [exe]   (default $PB_LOGS/animfilm, Builds/dev/PortBoss.exe)
. "$(dirname "$0")/env.sh"
OUT="${1:-$PB_LOGS/animfilm}"; EXE="${2:-$PROJECT/Builds/dev/PortBoss.exe}"
rm -rf "$OUT"; mkdir -p "$OUT"
"$EXE" -batchmode -pb-solo -pb-animfilm "$(cygpath -w "$OUT")" -pb-quit 400 -logFile "$(cygpath -w "$OUT/log.txt")"
grep -E "\[PB\] (gait|animfilm)" "$OUT/log.txt"; echo "exceptions: $(grep -c Exception "$OUT/log.txt")"
for d in "$OUT"/*/; do
  n=$(basename "$d")
  ffmpeg -loglevel error -y -framerate 30 -i "$d/f_%04d.jpg" -c:v libx264 -pix_fmt yuv420p -crf 20 "$OUT/$n.mp4"
  ffmpeg -loglevel error -y -framerate 30 -i "$d/f_%04d.jpg" -vf "fps=15,scale=480:-1:flags=lanczos,split[a][b];[a]palettegen[p];[b][p]paletteuse" "$OUT/$n.gif"
  # Frame sheet: every 3rd frame, 6 across.
  ffmpeg -loglevel error -y -framerate 30 -i "$d/f_%04d.jpg" -vf "select='not(mod(n\,3))',scale=320:-1,tile=6x4" -frames:v 1 "$OUT/$n-sheet.jpg"
done
ls "$OUT"/*.mp4
