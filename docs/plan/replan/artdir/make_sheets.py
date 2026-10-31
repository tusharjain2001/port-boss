# Builds one comparison sheet per art direction + one overview grid (1920x1080 each) from the renders and Grok concepts.
# Run: python -I make_sheets.py   (from anywhere; paths are relative to this file)
import os
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
R, C, OUT = os.path.join(HERE, "renders"), os.path.join(HERE, "concepts"), os.path.join(HERE, "sheets")

DIRS = [
    ("1-golden-hour", "1  Golden Hour Adventure", "Premium stylised realism: warm low sun, cool shade, haze, rich clear water.",
     ["2F6DC0", "E9DCC8", "FFDDB0", "8C9BD6", "3FD1C2", "0B3D5C", "EBD5B5", "F2C38E"]),
    ("2-miniature", "2  Miniature Harbour", "Soft matte diorama: airy light, big soft AO, pastel water, tilt-shift.",
     ["78B4E8", "DCEAF0", "FFF3E0", "B8BCE0", "A6EBD8", "52B4C0", "EEE8DF", "FFF6E6"]),
    ("3-ink-cel", "3  Ink & Cel", "Bold ink outlines, two flat tones, hard shadows, saturated toon sea.",
     ["2E8BFF", "BFE8FF", "FFF8EC", "8C80C0", "3FC4E8", "2462C4", "1A1428", "FFD23C"]),
    ("4-ponyo-coast", "4  Ponyo Coast", "Ghibli-ish soft cel, painted blue-violet shade, towering clouds, luminous sea.",
     ["3A86E0", "CFE6F5", "FFF3DC", "98A6EA", "46D6C8", "2160B8", "805C52", "FFFBF2"]),
    ("5-glossy-toybox", "5  Glossy Toybox", "Saturated glossy plastic, strong highlights, candy water, bright clean light.",
     ["4FA9FF", "DDF3FF", "FFFFFF", "B4A6E6", "4EE6F0", "1B8FE0", "FF4F6A", "FFC83C"]),
    ("6-lantern-dusk", "6  Lantern Dusk", "Graphic late light: gradient fog layers, banded shade, teal sea, warm lamps.",
     ["2D4A7A", "F7A86B", "FF9E60", "6B669E", "2E6F73", "0E2733", "E89A6E", "FFB060"]),
]


def font(size, bold=False):
    for f in (("seguisb.ttf" if bold else "segoeui.ttf"), ("arialbd.ttf" if bold else "arial.ttf")):
        try:
            return ImageFont.truetype(os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts", f), size)
        except OSError:
            pass
    return ImageFont.load_default()


def tile(path, w, h):
    if not os.path.exists(path):
        im = Image.new("RGB", (w, h), (40, 40, 46))
        ImageDraw.Draw(im).text((12, 12), "missing: " + os.path.basename(path), fill=(200, 120, 120), font=font(18))
        return im
    im = Image.open(path).convert("RGB")
    s = max(w / im.width, h / im.height)  # cover-crop
    im = im.resize((round(im.width * s), round(im.height * s)), Image.LANCZOS)
    x, y = (im.width - w) // 2, (im.height - h) // 2
    return im.crop((x, y, x + w, y + h))


def label(d, xy, text):
    x, y = xy
    f = font(17, True)
    tw = d.textlength(text, font=f)
    d.rectangle((x, y, x + tw + 16, y + 28), fill=(0, 0, 0))
    d.text((x + 8, y + 3), text, fill=(255, 255, 255), font=f)


def palette(d, x, y, w, h, hexes, labels=True):
    sw = w / len(hexes)
    for i, hx in enumerate(hexes):
        c = tuple(int(hx[k:k + 2], 16) for k in (0, 2, 4))
        d.rectangle((x + i * sw, y, x + (i + 1) * sw - 2, y + h), fill=c)
        if labels:
            lum = 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2]
            d.text((x + i * sw + 6, y + h - 24), "#" + hx, fill=(20, 20, 20) if lum > 140 else (240, 240, 240), font=font(15))


def sheet(did, name, pitch, hexes):
    im = Image.new("RGB", (1920, 1080), (22, 23, 28))
    d = ImageDraw.Draw(im)
    d.text((24, 18), name, fill=(255, 255, 255), font=font(40, True))
    d.text((24 + d.textlength(name, font=font(40, True)) + 24, 34), pitch, fill=(190, 194, 205), font=font(22))
    im.paste(tile(os.path.join(R, did, "1-hero.png"), 1200, 675), (24, 90)); label(d, (34, 100), "HERO (Toon Harbor pack, in-engine)")
    im.paste(tile(os.path.join(R, did, "2-gameplay.png"), 672, 378), (1236, 90)); label(d, (1246, 100), "GAMEPLAY 3/4")
    im.paste(tile(os.path.join(C, did + "-concept.png"), 672, 378), (1236, 480)); label(d, (1246, 490), "CHARACTER CONCEPT (Grok, concept only)")
    im.paste(tile(os.path.join(R, did, "3-closeup.png"), 480, 270), (24, 783)); label(d, (34, 793), "CLOSE-UP")
    im.paste(tile(os.path.join(R, did, "4-dusk-hero.png"), 480, 270), (516, 783)); label(d, (526, 793), "DUSK")
    palette(d, 1008, 783, 900, 150, hexes)
    d.text((1008, 948), "Palette: sky top / horizon / sun / shadow tint / water shallow / water deep / accent / highlight", fill=(160, 164, 175), font=font(17))
    d.text((1008, 980), "Renders: Unity 6.5 URP, free tools only (own shaders + URP post, SSAO, full-screen outline).", fill=(160, 164, 175), font=font(17))
    d.text((1008, 1008), "Spec + recommendation: README.md", fill=(160, 164, 175), font=font(17))
    im.save(os.path.join(OUT, did + "-sheet.jpg"), quality=92)


def overview():
    im = Image.new("RGB", (1920, 1080), (22, 23, 28))
    d = ImageDraw.Draw(im)
    d.text((24, 14), "Port Boss: 6 art directions on the Toon Harbor pack", fill=(255, 255, 255), font=font(34, True))
    for i, (did, name, pitch, hexes) in enumerate(DIRS):
        x, y = 24 + (i % 3) * 632, 70 + (i // 3) * 505
        im.paste(tile(os.path.join(R, did, "1-hero.png"), 616, 300), (x, y))
        im.paste(tile(os.path.join(R, did, "2-gameplay.png"), 304, 152), (x, y + 306))
        im.paste(tile(os.path.join(C, did + "-concept.png"), 306, 152), (x + 310, y + 306))
        palette(d, x, y + 464, 616, 14, hexes, labels=False)
        label(d, (x + 8, y + 8), name)
    im.save(os.path.join(OUT, "overview-grid.jpg"), quality=92)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for args in DIRS:
        sheet(*args)
    overview()
    print("sheets ->", OUT)
