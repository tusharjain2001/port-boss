#!/bin/bash
# Lightweight compile check of all project scripts (no Unity launch). Needs the .NET SDK and a Library/ the editor has filled once.
. "$(dirname "$0")/env.sh"
D="$PB_LOGS/cc"; mkdir -p "$D"; cp "$TOOLS/cc.csproj" "$D/"
export PB_PROJECT=$(cygpath -m "$PROJECT") UNITY_DATA=$(cygpath -m "$(dirname "$UNITY")/Data")
cd "$D" && dotnet build -nologo -v q 2>&1 | grep -E "error|Build succeeded" | sed 's/\[C:.*csproj\]//' | sort -u | head -30
