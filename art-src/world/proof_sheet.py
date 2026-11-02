"""Proof sheets for Manan: the Blender top-down next to map-B.png, and the top-down with map-B's own coastline drawn over it.
Also the one runnable check: the seabed PNG decodes with a standard decoder (Pillow) and its spot heights are right.
Run after build_island.py:  python art-src/world/proof_sheet.py [proof dir]"""
import os, sys, json
from PIL import Image, ImageDraw, ImageFont
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import island_shape as S

REPO = os.path.normpath(os.path.join(HERE, '..', '..'))
PROOF = sys.argv[1] if len(sys.argv) > 1 else os.path.expanduser(r'~\Desktop\Port Boss\proof\map-b')
FONTS = os.path.join(REPO, 'Assets', 'Game', 'Fonts')
def font(name, size): return ImageFont.truetype(os.path.join(FONTS, name), size)


def check_seabed():
    meta = json.load(open(os.path.join(HERE, 'seabed', 'seabed_2048.json')))
    im = Image.open(os.path.join(HERE, 'seabed', meta['file']))
    a = np.array(im).astype(np.float64)
    assert a.shape == (2048, 2048), a.shape
    h = a / 65535 * (S.HMAX - S.HMIN) + S.HMIN
    def at(mx, my):
        x, y = S.W(mx, my); i = int((x - S.XW) / meta['metres_per_px']); j = int((S.YN - y) / meta['metres_per_px'])
        return h[j, i]
    pier_end = h[int((S.YN - 0) / meta['metres_per_px']), int((S.W(2830, 0)[0] - S.XW) / meta['metres_per_px'])]
    checks = {"open ocean at the sheet corner < -140 m": at(50, 50) < -140,
              "Voss quay = 3.0 m": abs(at(3900, 2600) - 3.0) < 0.05,
              "Saltback Ridge crest 120-155 m": 120 < max(at(3480, y) for y in range(2300, 2800, 20)) < 155,
              "Walt's Cut silted at -0.5 m": abs(at(2615, 3360) + 0.5) < 0.15,
              "the bay bowl 4-16 m deep": 4 < -at(2300, 2800) < 16,
              "berth off Walt's pier >= 2.4 m deep": -pier_end >= 2.4,
              "Gullrock is land": at(2250, 2550) > 2}
    for k, ok in checks.items(): print('CHECK', 'PASS' if ok else 'FAIL', k)
    assert all(checks.values())


def coast_px(pts, scale, n=16, closed=True):
    return [(float(x) * scale, float(y) * scale) for x, y in S.smooth(pts, n, closed)]


def main():
    check_seabed()
    top = Image.open(os.path.join(PROOF, 'topdown.png')).convert('RGB')
    mapb = Image.open(os.path.join(REPO, 'docs', 'plan', 'gdd', 'world', 'maps', 'map-B.png')).convert('RGB')
    P = 1200; a = mapb.crop((0, 0, P, P)); b = top.resize((P, P), Image.LANCZOS)
    head = 120; sheet = Image.new('RGB', (2 * P + 60, P + head + 30), '#FFFBF2'); d = ImageDraw.Draw(sheet)
    d.text((20, 18), 'Crescent Bay: map B (left) vs the Blender blockout at true scale (right)', fill='#3F2C27', font=font('Fraunces-Bold.ttf', 40))
    d.text((20, 72), 'Same 5 x 5 km sheet, north up. Left: docs/plan/gdd/world/maps/map-B.png. Right: art-src/world/build_island.py, top-down render, '
                     'sea coloured by the seabed depth map.', fill='#805C52', font=font('ZenMaruGothic-Medium.ttf', 22))
    sheet.paste(a, (20, head)); sheet.paste(b, (P + 40, head))
    sheet.save(os.path.join(PROOF, 'compare_mapB_vs_blockout.png'))

    # overlay: map-B's exact coastline (white) + its places, on the 2400 px render
    s = 2400 / 5000; ov = top.copy(); d = ImageDraw.Draw(ov)
    for pts, n in [(S.ISLAND, 16), (S.TOWN, 12), (S.VOSS, 12)] + [(S.islet_pts(*v), 8) for v in S.ISLETS.values()]:
        c = coast_px(pts, s, n); d.line(c + [c[0]], fill='#FFFFFF', width=3)
    d.line(coast_px(S.RIDGE, s, 16, False), fill='#6B5B45', width=4)
    for k, (x0, y0, w, h) in S.EXPANSION.items():
        d.rectangle((x0 * s, y0 * s, (x0 + w) * s, (y0 + h) * s), outline='#F2994A', width=3); d.text((x0 * s - 52, y0 * s), k, fill='#F2994A', font=font('ZenMaruGothic-Bold.ttf', 26))
    f = font('ZenMaruGothic-Bold.ttf', 30); fs = font('ZenMaruGothic-Bold.ttf', 22)
    def tag(mx, my, text, col='#FFFFFF', fnt=f, anchor='mm'):
        d.text((mx * s, my * s), text, fill=col, font=fnt, anchor=anchor, stroke_width=4, stroke_fill='#0B3D5C')
    for n, (name, (mx, my)) in S.SHOPS.items():
        d.ellipse((mx * s - 13, my * s - 13, mx * s + 13, my * s + 13), fill='#F2C38E', outline='#3F2C27', width=2)
        d.text((mx * s, my * s), str(n), fill='#3F2C27', font=fs, anchor='mm')
    tag(3250, 2230, 'OLD SALTMOUTH', '#FFFFFF'); tag(3080, 2800, "HALE'S LANDING (Walt's pier)", '#F2994A', anchor='lm')
    tag(1880, 1560, "Dot's lighthouse"); tag(1000, 1610, 'Wreck of the Margaret'); tag(2250, 2700, 'Gullrock')
    tag(1100, 2800, 'Lantern Key'); tag(4100, 1050, 'Sorrel Isle'); tag(4300, 4480, 'Mango Cay')
    tag(4150, 3330, 'VOSS HARBOUR', '#FFB4A8'); tag(4150, 3400, 'buoy line + breakwater', '#FFB4A8', fs)
    tag(2440, 3480, "Walt's Cut (silted)", '#FFFFFF', fs, 'rm'); tag(2800, 3950, 'Breaker Point', '#FFFFFF')
    tag(3330, 1900, 'Saltback Ridge 140 m', '#FFFFFF', fs, 'rm')
    d.rectangle((40, 2250, 600, 2370), fill='#FFFBF2'); d.text((60, 2262), 'White line = map-B.png coastline, exact', fill='#3F2C27', font=fs)
    d.line((60, 2330, 560, 2330), fill='#3F2C27', width=6); d.text((60, 2338), '0                     1 km', fill='#3F2C27', font=fs)
    d.line((60 + 480, 2322, 60 + 480, 2338), fill='#3F2C27', width=4)
    ov.save(os.path.join(PROOF, 'topdown_with_mapB_overlay.png'))
    print('wrote', os.path.join(PROOF, 'compare_mapB_vs_blockout.png'), os.path.join(PROOF, 'topdown_with_mapB_overlay.png'))


main()
