#!/bin/bash
# Playtest release: build -> $RELEASE_DIR/<stamp>-MORNING with HOW-TO-PLAY.md, 5 screenshots, test logs.
# Usage: morning.sh            (all runs batchmode: camera-only shots, no menu/HUD drawn)
#        WINDOW=1 morning.sh   (menu, HUD and night-card shots in a game window that pops up briefly)
. "$(dirname "$0")/env.sh"
STAMP=$(date +%Y%m%d-%H%M); OUT="${OUTDIR:-$RELEASE_DIR/$STAMP-MORNING}"
bash "$TOOLS/ubuild.sh" "$OUT" | grep -E "error CS|PBBuild\] build"
cp "$PROJECT/docs/HOW-TO-PLAY.md" "$OUT/"
M="$OUT/screenshots"; T="$PB_LOGS/morning-tmp"; rm -rf "$T"; mkdir -p "$M" "$T" "$OUT/test-logs"
cd "$OUT"
if [ "$WINDOW" = 1 ]; then W="-screen-fullscreen 0 -screen-width 1600 -screen-height 900"; else W="-batchmode"; fi
timeout 120 ./PortBoss.exe $W -pb-uishots "$(cygpath -w "$T/ui")" -pb-quit 45 -logFile "$(cygpath -w "$T/ui.log")" >/dev/null 2>&1
timeout 120 ./PortBoss.exe $W -pb-solo -pb-bot -pb-shots "$(cygpath -w "$T/hud")" 4 -pb-quit 46 -logFile "$(cygpath -w "$T/hud.log")" >/dev/null 2>&1
timeout 120 ./PortBoss.exe $W -pb-solo -pb-night 5 -pb-shots "$(cygpath -w "$T/night")" 9 -pb-quit 20 -logFile "$(cygpath -w "$T/night.log")" >/dev/null 2>&1
timeout 200 ./PortBoss.exe -batchmode -pb-solo -pb-mapshots "$(cygpath -w "$T/map")" -pb-quit 80 -logFile "$(cygpath -w "$T/map.log")" >/dev/null 2>&1
timeout 150 ./PortBoss.exe -batchmode -pb-solo -pb-storm -pb-timescale 3 -pb-shots "$(cygpath -w "$T/storm")" 200 -pb-quit 60 -logFile "$(cygpath -w "$T/storm.log")" >/dev/null 2>&1
cp "$T/ui/title-0.png" "$M/1-menu.png"
cp "$T/hud/shot_003.png" "$M/2-first-boat-hud.png"
cp "$T/map/map-demo-angle.png" "$M/3-our-berth-in-the-harbour.png"
cp "$T/storm/weather-1.0.png" "$M/4-storm.png"
cp "$T/night/shot_000.png" "$M/5-night-card.png"
cp "$T"/*.log "$OUT/test-logs/"
echo "exceptions: $(cat "$T"/*.log | grep -c Exception)   map: $(grep -h '\[PB\] map check: .* placed' "$T/map.log")"
ls "$M"
echo "$OUT"
