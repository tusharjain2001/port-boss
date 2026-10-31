#!/bin/sh
# Renders map-*.svg to PNG with headless Chrome (fonts load from Google Fonts).
cd "$(dirname "$0")"
CH="/c/Program Files/Google/Chrome/Application/chrome.exe"
for k in A B C; do
  "$CH" --headless=new --disable-gpu --hide-scrollbars --virtual-time-budget=6000 \
    --window-size=1640,1200 --screenshot="$(cygpath -w "$PWD")\map-$k.png" "file:///$(cygpath -m "$PWD")/map-$k.svg" 2>/dev/null
done
ls -la *.png
