#!/bin/bash
# M4 money check without Unity (needs the .NET 8 SDK): Mari, the market, June's shelf, the bank and Ledger, the save JSON.
# usage: companycheck.sh [ops]   (default 20000)
. "$(dirname "$0")/env.sh"
D="$PB_LOGS/companycheck"; mkdir -p "$D"; cp "$TOOLS/companycheck/"* "$D/"
export PB_PROJECT=$(cygpath -m "$PROJECT")
cd "$D" && dotnet run -c Release -v q -- "$@"
