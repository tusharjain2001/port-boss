#!/bin/bash
# M1-M3 regressions on a build, one at a time (hidden, headless). usage: m4reg.sh [exe=Builds/m1/PortBoss.exe]
cd "$(dirname "$0")/.."; . tools/env.sh
E=${1:-Builds/m1/PortBoss.exe}; S=$PB_LOGS/m4reg; rm -rf "$S"; mkdir -p "$S"; T() { bash tools/m1test.sh $E "$@" | grep -E '^(exceptions|exit)|RESULT|FAIL' | head -8; }
for t in "fish100 -pb-fishtest 100 -pb-timescale 8" "toss -pb-tosstest" "swim -pb-swimtest" "boat -pb-boattest" "knock -pb-knocktest $(cygpath -m $S/knock)" \
         "m3day -pb-daytest $(cygpath -m $S/day) -pb-fps 60 -pb-seed 7" "m3sleep -pb-sleeptest solo -pb-sleepshots $(cygpath -m $S/sleep) -pb-fps 60 -pb-seed 7" \
         "m3storm -pb-stormtest $(cygpath -m $S/storm) -pb-fps 60 -pb-seed 7" "m3weather -pb-weathertest 10000 -pb-seed 7"; do
  echo "##### $t"; T $t; done
echo "##### menutest"; bash tools/menutest.sh $E menu4 | grep -E 'exceptions|RESULT|FAIL|crew' | head -8
echo "##### cooptest"; bash tools/cooptest.sh $E coop4 3 | grep -E 'exceptions|RESULT|FAIL' | head -12
echo "##### sleeptest"; bash tools/sleeptest.sh $E sleep4 | grep -E 'exceptions|RESULT|FAIL' | head -8
echo "##### done"
