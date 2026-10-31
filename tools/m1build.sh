#!/bin/bash
# M1 (boat + fishing): headless setup + Win64 build.  usage: m1build.sh <outdir> [-pbDev]
. "$(dirname "$0")/env.sh"
LOG=$PB_LOGS/m1build.log
OUT=$1; shift
hidden "$UNITY" -batchmode -nographics -quit -projectPath "$PROJECT" -logFile "$(cygpath -w "$LOG")" -executeMethod PortBoss.EditorTools.M1Build.SetupAndBuild -pbOut "$OUT" "$@"
echo "exit $?"
grep -E "error CS|\[M1Build\]|Exception|Scripts have compiler errors" "$LOG" | sort -u | head -40
