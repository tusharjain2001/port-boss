#!/bin/bash
# Re-check after fixes, batchmode only. Usage: reg2.sh [outdir] [exe]   (defaults: $PB_LOGS/reg2, Builds/dev/PortBoss.exe)
. "$(dirname "$0")/env.sh"
O=${1:-$PB_LOGS/reg2}; EXE=${2:-$PROJECT/Builds/dev/PortBoss.exe}; rm -rf "$O"; mkdir -p "$O"
run() { local n=$1 q=$2 L="$O/$1/log.txt"; shift 2; mkdir -p "$O/$n"
  timeout $((q+40)) "$EXE" -batchmode -pb-shots "$(cygpath -w "$O/$n")" ${SHOT:-5} -pb-quit $q -logFile "$(cygpath -w "$L")" "$@" >/dev/null 2>&1
  echo "== $n: exceptions=$(grep -c Exception "$L") high=$(grep -c 'high crate' "$L") fast=$(grep -c 'FAST' "$L")"
  grep -E "\[PB\] (swim|wobble|trolley|sold|snap|FAST|storm test: phase=2 level=1)" "$L" | head -6
  grep "\[PB\] t=" "$L" | tail -1 | sed -E 's/listening=True //'; }
run solo 150 -pb-solo -pb-bot   # the market is ~115 m from the jetty
SHOT=0.5 run wobble 16 -pb-solo -pb-wobbletest
SHOT=1.5 run swim 26 -pb-solo -pb-swimtest
run crates 10 -pb-solo -pb-crateshots "$(cygpath -w "$O/crates")"
SHOT=4 run ab 38 -pb-solo
SHOT=4 run storm 60 -pb-solo -pb-storm -pb-timescale 3
# Co-op without Steam: LAN host + direct join on 127.0.0.1.
C="$O/coop"; mkdir -p "$C"; (timeout 190 "$EXE" -batchmode -pb-host-lan -pb-bot -pb-quit 150 -logFile "$(cygpath -w "$C/host.log")" >/dev/null 2>&1 &); sleep 20  # the host needs ~15 s to load headless
timeout 190 "$EXE" -batchmode -pb-join-lan 127.0.0.1 -pb-bot -pb-quit 142 -logFile "$(cygpath -w "$C/client.log")" >/dev/null 2>&1; sleep 10
echo "== coop"; grep -hE "\[PB\] (joined|connected|client)" "$C"/*.log | head -4; grep "\[PB\] t=" "$C/host.log" | tail -1; grep "\[PB\] t=" "$C/client.log" | tail -1; grep -c Exception "$C/host.log" "$C/client.log"
