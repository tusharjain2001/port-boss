#!/bin/bash
# Imports the (paid, gitignored) Toon Harbor Pack headless into Assets/ThirdParty/ToonHarbor, where the build looks for it.
# Usage: import-toon-harbor.sh "<path to Toon Harbor Pack.unitypackage>"   (keep that file outside the repo; never commit or upload it)
# The pack file only wraps two packages (Built-in, URP); this pulls out the URP one, imports it, then moves the folder.
# Unity must be closed for this project (batchmode can't open a project the editor has open).
. "$(dirname "$0")/env.sh"
PKG=$1
[ -f "$PKG" ] || { echo "usage: import-toon-harbor.sh <Toon Harbor Pack.unitypackage>  (not found: '$PKG')"; exit 1; }
X="$PB_LOGS/th_extract"; rm -rf "$X"; mkdir -p "$X"
python -I - "$PKG" "$X" <<'PY' || exit 1
import sys, tarfile, shutil, os
pkg, out = sys.argv[1:3]
with tarfile.open(pkg) as t:
    for m in t.getmembers():
        if not m.name.endswith('/pathname'): continue
        path = t.extractfile(m).read().decode().splitlines()[0]
        if os.path.basename(path).startswith('URP_') and path.endswith('.unitypackage'):
            with t.extractfile(m.name[:-len('pathname')] + 'asset') as src, open(os.path.join(out, 'URP.unitypackage'), 'wb') as dst:
                shutil.copyfileobj(src, dst)
            print('extracted', path); break
    else: sys.exit('no URP_*.unitypackage inside ' + pkg)
PY
"$UNITY" -batchmode -quit -projectPath "$PROJECT" -importPackage "$(cygpath -w "$X/URP.unitypackage")" -logFile "$PB_LOGS/th_import.log"; echo "import exit $?"
"$UNITY" -batchmode -quit -projectPath "$PROJECT" -executeMethod PortBoss.EditorTools.ToonHarborTools.MoveToThirdParty -logFile "$PB_LOGS/th_move.log"
grep "\[TH\] move" "$PB_LOGS/th_move.log"
ls "$PROJECT/Assets/ThirdParty/ToonHarbor" | head
