# Side by side: the approved Ponyo Coast sheet's hero shot | an M1 shot before | the same shot after.
# python -I vsponyo.py <sheet.jpg> <before.png> <after.png> <out.jpg> [label]
import sys
from PIL import Image, ImageDraw, ImageFont

sheet, before, after, out = sys.argv[1:5]
label = sys.argv[5] if len(sys.argv) > 5 else ""
H = 540
hero = Image.open(sheet).convert("RGB").crop((24, 90, 1224, 765))  # the HERO panel of 4-ponyo-coast-sheet.jpg
tiles = [(hero, "Ponyo Coast sheet (approved)"), (Image.open(before).convert("RGB"), "M1 before"), (Image.open(after).convert("RGB"), "M1b after " + label)]
tiles = [(im.resize((round(im.width * H / im.height), H), Image.LANCZOS), t) for im, t in tiles]
W = sum(im.width for im, _ in tiles) + 8 * (len(tiles) - 1)
sheet_out = Image.new("RGB", (W, H + 36), (24, 24, 30))
d = ImageDraw.Draw(sheet_out)
try:
    font = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 22)
except OSError:
    font = ImageFont.load_default()
x = 0
for im, t in tiles:
    sheet_out.paste(im, (x, 36))
    d.text((x + 10, 6), t, fill=(240, 236, 228), font=font)
    x += im.width + 8
sheet_out.save(out, quality=88)
