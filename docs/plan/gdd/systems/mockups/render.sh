#!/bin/bash
# Renders every NN-*.html in this folder to NN-*.png at 1920x1080 with headless Chrome.
cd "$(dirname "$0")"
CHROME="/c/Program Files/Google/Chrome/Application/chrome.exe"
for f in ${1:-[0-9][0-9]-*.html}; do
  out="${f%.html}.png"
  "$CHROME" --headless=new --disable-gpu --hide-scrollbars --allow-file-access-from-files \
    --window-size=1920,1080 --virtual-time-budget=6000 --screenshot="$(cygpath -w "$PWD/$out")" "file:///$(cygpath -m "$PWD/$f")" >/dev/null 2>&1
  echo "$out $(stat -c %s "$out" 2>/dev/null)"
done
