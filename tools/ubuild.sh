#!/bin/bash
# usage: ubuild.sh <outdir> [extra args, e.g. -pbDev]   -> runs Setup+Build headless, prints errors + PB lines
. "$(dirname "$0")/env.sh"
LOG=$PB_LOGS/build.log
OUT=$1; shift
"$UNITY" -batchmode -quit -projectPath "$PROJECT" -logFile "$LOG" -executeMethod PortBoss.EditorTools.PortBossBuild.SetupAndBuild -pbOut "$OUT" "$@"
echo "exit $?"
grep -E "error CS|\[PBBuild\]|Exception|Scripts have compiler errors" "$LOG" | sort -u | head -40
