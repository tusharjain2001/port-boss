#!/bin/bash
# M6 (workers): every M6 check, headless and hidden. usage: m6test.sh <exe> [proofdir]
#  1. companycheck: the wage / IOU suite (Company.CrewTest: Rosie's board, hiring, wages at 07:00, the IOU rule, one boat
#     per worker, the save, 4000 seeded shifts per skill against the model) + the M4/M5 money suites, without Unity
#  2. (unless SKIP_EDIT=1) the same suite as EditMode in the editor: -executeMethod M6Tests.RunAll
#  3. -pb-workertest: the A* sea grid (berth -> every spot, 40 random sea pairs: never over land or the Voss line), Rosie's
#     board, a live shift near the player, 20 seeded shifts within ±10% of the economy model, wages, the IOU rule; stills
#  4. -pb-m6coop host + rider with -pb-netsim 150 2: the rider hires and assigns from the phone, the host runs the shift,
#     both see the boat sail, the texts and the catch
. "$(dirname "$0")/env.sh"
EXE=$1; PROOF=${2:-$PB_LOGS/m6}; D=$PB_LOGS/m6; rm -rf "$D"; mkdir -p "$D" "$PROOF/stills"
W() { cygpath -m "$1"; }
sum() { echo "== $1: exceptions $(grep -c Exception "$2"), $(grep -ho '\[PB\] RESULT.*' "$2" | tr '\n' ' ')"; grep -E '\[PB\] m6(test)?: FAIL' "$2" | head -40; }

bash "$TOOLS/companycheck.sh" 4000 > "$D/companycheck.txt" 2>&1; echo "== companycheck: $(tail -1 "$D/companycheck.txt")"; grep -E "M6|FAIL" "$D/companycheck.txt"

if [ -z "$SKIP_EDIT" ]; then
  hidden "$UNITY" -batchmode -nographics -projectPath "$PROJECT" -logFile "$(W "$D/edit.txt")" -executeMethod PortBoss.M1.M6Tests.RunAll
  grep -E '\[PB\] (m6test|RESULT)' "$D/edit.txt"; echo "== edit: compile errors $(grep -c 'error CS' "$D/edit.txt")"
fi

hidden "$EXE" -batchmode -pb-seed 7 -pb-fps 60 -pb-workertest "$(W "$PROOF/stills")" -pb-quit 900 -logFile "$(W "$D/workertest.txt")"
sum workertest "$D/workertest.txt"

PORT=$((7900 + RANDOM % 90)); mkdir -p "$PROOF/coop"
hidden "$EXE" -batchmode -pb-host -pb-port $PORT -pb-name Host -pb-seed 7 -pb-hour 10 -pb-m6coop host -pb-netsim 150 2 -pb-m5shots "$(W "$PROOF/coop")" -pb-quit 500 -logFile "$(W "$D/coop-host.txt")" &
sleep 4
hidden "$EXE" -batchmode -pb-join 127.0.0.1:$PORT -pb-name Rider1 -pb-m6coop rider -pb-netsim 150 2 -pb-m5shots "$(W "$PROOF/coop")" -pb-quit 500 -logFile "$(W "$D/coop-rider.txt")" &
wait
sum "m6coop host" "$D/coop-host.txt"; sum "m6coop rider" "$D/coop-rider.txt"
cp "$D"/*.txt "$PROOF/" 2>/dev/null
echo "== logs: $D  proof: $PROOF"
