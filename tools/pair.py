# Side-by-side A/B image: python pair.py <a.png> <b.png> <out.png> [label a] [label b]   (needs Pillow)
import sys
from PIL import Image, ImageDraw
a,b,out=sys.argv[1:4]; la=sys.argv[4] if len(sys.argv)>4 else 'BEFORE'; lb=sys.argv[5] if len(sys.argv)>5 else 'AFTER'
A=Image.open(a).convert('RGB').resize((960,540)); B=Image.open(b).convert('RGB').resize((960,540))
o=Image.new('RGB',(1920,540)); o.paste(A,(0,0)); o.paste(B,(960,0))
d=ImageDraw.Draw(o)
for x,t in ((10,la),(970,lb)): d.rectangle((x,10,x+12*len(t)+10,40),fill=(0,0,0)); d.text((x+6,16),t,fill=(255,255,255))
o.save(out)
