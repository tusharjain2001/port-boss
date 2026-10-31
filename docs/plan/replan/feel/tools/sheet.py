# Contact sheets / crops from animfilm frame folders.
# sheet.py out.jpg cols cellW "label|path|x0,y0,x1,y1(optional crop)" ...
import sys
from PIL import Image, ImageDraw, ImageFont
out, cols, cw = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
cells = []
for spec in sys.argv[4:]:
    parts = spec.split('|')
    im = Image.open(parts[1]).convert('RGB')
    if len(parts) > 2 and parts[2]:
        im = im.crop(tuple(int(v) for v in parts[2].split(',')))
    im = im.resize((cw, int(im.height * cw / im.width)), Image.LANCZOS)
    cells.append((parts[0], im))
ch = max(im.height for _, im in cells)
rows = (len(cells) + cols - 1) // cols
sheet = Image.new('RGB', (cols * cw, rows * ch), (20, 20, 20))
try: font = ImageFont.truetype('arial.ttf', max(12, min(26, cw // 34)))
except OSError: font = ImageFont.load_default()
d = ImageDraw.Draw(sheet)
for i, (lab, im) in enumerate(cells):
    x, y = (i % cols) * cw, (i // cols) * ch
    sheet.paste(im, (x, y))
    d.rectangle([x, y, x + d.textlength(lab, font=font) + 8, y + font.size + 6], fill=(0, 0, 0))
    d.text((x + 4, y + 2), lab, fill=(255, 220, 0), font=font)
sheet.save(out, quality=88)
print(out, sheet.size)
