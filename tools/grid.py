# Tile images into one sheet: python grid.py out.jpg cols width img1 img2 ...
import sys
from PIL import Image
out, cols, w, files = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4:]
ims = [Image.open(f).convert("RGB") for f in files]
h = round(ims[0].height * w / ims[0].width)
rows = (len(ims) + cols - 1) // cols
sheet = Image.new("RGB", (cols * w, rows * h), (20, 20, 24))
for i, im in enumerate(ims):
    sheet.paste(im.resize((w, h), Image.LANCZOS), ((i % cols) * w, (i // cols) * h))
sheet.save(out, quality=88)
