#!/bin/bash
# Run the M1 build headless with test switches; prints exceptions + [PB] lines.  usage: m1test.sh <exe> <name> [switches...]
# e.g. m1test.sh Builds/m1/PortBoss.exe fish500 -pb-fishtest 500 -pb-timescale 10
. "$(dirname "$0")/env.sh"
EXE=$1; NAME=$2; shift 2
D=$PB_LOGS/m1/$NAME; mkdir -p "$D"
"$EXE" -batchmode -logFile "$(cygpath -m "$D/log.txt")" "$@"
echo "exit $? -> $D/log.txt"
echo "exceptions: $(grep -c Exception "$D/log.txt")"
grep -E "\[PB\] (RESULT|M1|hotbar|boat|fishtest|alert Warning)|Exception|NullReference" "$D/log.txt" | head -40
