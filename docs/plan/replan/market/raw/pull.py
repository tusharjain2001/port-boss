"""Pull live Steam numbers for comparable games. Run: python -I pull.py ids.txt out.json
Sources: Steam appdetails API, Steam store page (English review summary, tags, EA flag),
SteamSpy API (all-language pos/neg, owner band), SteamCharts (all-time peak CCU)."""
import json, re, sys, time, urllib.request, datetime

UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/130 Safari/537.36"
COOKIE = "birthtime=0; wants_mature_content=1; lastagecheckage=1-0-1990; Steam_Language=english"

def get(url):
    req = urllib.request.Request(url, headers={"User-Agent": UA, "Cookie": COOKIE})
    try:
        with urllib.request.urlopen(req, timeout=30) as r:
            return r.read().decode("utf-8", "replace")
    except Exception as e:
        return f"ERR {e}"

def one(appid):
    out = {"appid": appid, "accessed": datetime.date.today().isoformat()}
    d = get(f"https://store.steampowered.com/api/appdetails?appids={appid}&cc=us&l=english")
    try:
        d = json.loads(d)[str(appid)]["data"]
        out["name"] = d.get("name")
        out["dev"] = ", ".join(d.get("developers", []))
        out["pub"] = ", ".join(d.get("publishers", []))
        out["release"] = d.get("release_date", {}).get("date")
        out["coming_soon"] = d.get("release_date", {}).get("coming_soon")
        po = d.get("price_overview") or {}
        out["price_list_usd"] = po.get("initial_formatted") or ("Free" if d.get("is_free") else None)
        out["price_now_usd"] = po.get("final_formatted")
        out["cats"] = [c["description"] for c in d.get("categories", [])]
        out["genres"] = [g["description"] for g in d.get("genres", [])]
    except Exception as e:
        out["appdetails_err"] = str(e)[:80]
    page = get(f"https://store.steampowered.com/app/{appid}/?cc=us&l=english")
    m = re.findall(r'data-tooltip-html="(\d+)% of the ([\d,]+) user reviews in your language', page)
    if m: out["en_pct"], out["en_reviews"] = int(m[0][0]), int(m[0][1].replace(",", ""))
    m = re.findall(r'data-tooltip-html="(\d+)% of the ([\d,]+) user reviews for this game', page)
    if m: out["page_all_pct"], out["page_all_reviews"] = int(m[0][0]), int(m[0][1].replace(",", ""))
    m = re.search(r'All Time:</span>\s*<span class="game_review_summary[^"]*">([^<]+)</span>\s*\((\d+)% of ([\d,]+)\)', page)
    if m: out["alltime_label"], out["alltime_pct"], out["alltime_reviews"] = m.group(1), int(m.group(2)), int(m.group(3).replace(",", ""))
    h = get(f"https://store.steampowered.com/appreviewhistogram/{appid}?l=english&review_score_preference=0")
    try:
        ro = json.loads(h)["results"]["rollups"]
        up = sum(r["recommendations_up"] for r in ro); dn = sum(r["recommendations_down"] for r in ro)
        out["hist_reviews"], out["hist_pct"] = up + dn, (round(100 * up / (up + dn), 1) if up + dn else None)
        out["hist_rollup_type"] = json.loads(h)["results"].get("rollup_type")
    except Exception as e:
        out["hist_err"] = str(e)[:60]
    m = re.search(r'game_review_summary[^"]*"[^>]*>([^<]+)<', page)
    if m: out["summary_label"] = m.group(1).strip()
    out["early_access"] = "early_access_header" in page or "Early Access Release Date" in page
    m = re.search(r'Early Access Release Date:</b>\s*([^<]+)<', page)
    if m: out["ea_date"] = m.group(1).strip()
    out["tags"] = re.findall(r'class="app_tag" style="[^"]*">\s*([^<\t\n]+?)\s*<', page)[:12]
    s = get(f"https://steamspy.com/api.php?request=appdetails&appid={appid}")
    try:
        s = json.loads(s)
        pos, neg = s.get("positive", 0), s.get("negative", 0)
        out["all_reviews"] = pos + neg
        out["all_pct"] = round(100 * pos / (pos + neg), 1) if pos + neg else None
        out["spy_owners"] = s.get("owners")
        out["spy_ccu_yday"] = s.get("ccu")
    except Exception as e:
        out["spy_err"] = str(e)[:80]
    c = get(f"https://steamcharts.com/app/{appid}")
    m = re.search(r'<span class="num">(\d+)</span>\s*<br>all-time peak', c)
    out["peak_ccu_steamcharts"] = int(m.group(1)) if m else None
    n = out.get("alltime_reviews") or out.get("hist_reviews") or out.get("all_reviews") or 0
    out["boxleiter_30_60"] = [n * 30, n * 60]
    return out

if __name__ == "__main__":
    ids = [l.split()[0] for l in open(sys.argv[1]) if l.strip() and not l.startswith("#")]
    res = []
    for i in ids:
        res.append(one(i)); print(json.dumps(res[-1])[:300], flush=True); time.sleep(1.2)
    json.dump(res, open(sys.argv[2], "w"), indent=1)
