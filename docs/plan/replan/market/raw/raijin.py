"""Pull Raijin.gg summary lines (all-time peak CCU, review count/score, sales + wishlist estimates).
Run: python -I raijin.py ids.txt raijin-2026-10-31.json"""
import json, re, sys, time, urllib.request

UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/130 Safari/537.36"

def get(url):
    try:
        with urllib.request.urlopen(urllib.request.Request(url, headers={"User-Agent": UA}), timeout=40) as r:
            return r.read().decode("utf-8", "replace")
    except Exception as e:
        return f"ERR {e}"

def num(pat, t):
    m = re.search(pat, t)
    return int(m.group(1).replace(",", "")) if m else None

res = []
for line in open(sys.argv[1], encoding="utf-8"):
    if not line.strip() or line.startswith("#"):
        continue
    appid = line.split()[0]
    t = get(f"https://raijin.gg/app/{appid}/x").replace('\\"', '"')
    r = {"appid": appid,
         "peak": num(r"peak concurrent players ([\d,]+)", t) or num(r"peak of ([\d,]+)", t),
         "reviews": num(r"positive rating on Steam from ([\d,]+) reviews", t) or num(r"across ([\d,]+) reviews", t),
         "pct": num(r"holds a (\d+)% positive", t),
         "sales_est": num(r'"sales":\{"totalValue":"(\d+)"', t),
         "sales_conf": (re.search(r'"sales":\{"totalValue":"\d+","lastWeekValue":[^,]*,"confidence":"(\w+)"', t) or [None, None])[1],
         "wishlists_est": num(r"Estimated wishlists: ([\d,]+)", t),
         "revenue_line": (re.search(r'"salesRevenue":"([^"]{0,200})', t) or [None, None])[1],
         "url": f"https://raijin.gg/app/{appid}/"}
    res.append(r); print(json.dumps(r, ensure_ascii=True)[:330], flush=True); time.sleep(1.5)
json.dump(res, open(sys.argv[2], "w", encoding="utf-8"), indent=1)
