#!/bin/bash
# Release build (no Development console) -> $RELEASE_DIR/<stamp>/ with HOW-TO-PLAY.md
. "$(dirname "$0")/env.sh"
STAMP=$(date +%Y%m%d-%H%M); OUT="$RELEASE_DIR/$STAMP"
bash "$TOOLS/ubuild.sh" "$OUT" | grep -E "error|PBBuild\] build"
cp "$PROJECT/docs/HOW-TO-PLAY.md" "$OUT/"
echo "$OUT"
