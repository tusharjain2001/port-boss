#!/bin/bash
# M3 rule check without Unity (needs the .NET 8 SDK): clock, sleep hours, lighting blend, rot, weather Markov + forecast.
# usage: daycheck.sh [days]   (default 10000)
. "$(dirname "$0")/env.sh"
D="$PB_LOGS/daycheck"; mkdir -p "$D"; cp "$TOOLS/daycheck/"* "$D/"
export PB_PROJECT=$(cygpath -m "$PROJECT")
cd "$D" && dotnet run -c Release -v q -- "$@"
