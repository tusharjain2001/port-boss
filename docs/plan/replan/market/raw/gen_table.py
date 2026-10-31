"""Build the market.md numbers table from the saved pulls. Run: python -I gen_table.py  -> table.md (UTF-8)."""
import csv, json, os
os.chdir(os.path.dirname(os.path.abspath(__file__)))
S, RJ = {}, {}
for f in ['steam-pull-2026-10-31.json', 'steam-pull2-2026-10-31.json']:
    for r in json.load(open(f, encoding='utf-8')): S[r['appid']] = r
for f in ['raijin-2026-10-31.json', 'raijin2-2026-10-31.json']:
    for r in json.load(open(f, encoding='utf-8')): RJ[r['appid']] = r
TG = {r['appid']: r['trackergg_alltime_peak'] for r in csv.DictReader(open('trackergg-peaks-2026-10-31.csv', encoding='utf-8'))}

# appid, group, team at launch, co-op size, core hook (team/co-op from research-hits.md / research-nearmisses.md / web)
M = [
 ('3241660','A','"small team", Uppsala (n unverified)','1–6','Haul fragile loot with physics; damage = lost money; proximity voice'),
 ('1966720','A','solo (Zeekerss)','1–4','Scrap runs for "the Company" quota; ship leaves at midnight'),
 ('3164500','A','solo core (3 credited)','1–4','Cook and sell drugs, small-time to kingpin'),
 ('3527290','A','7 (jam: 3 Aggro Crab + 4 Landfall)','1–4','Climb a mountain together; stamina + grab physics'),
 ('2881650','A','5','2–4','Film monsters to go viral on SpöökTube; free for 24 h at launch'),
 ('1290000','A','11 at EA start','1–6 (career 2)','Satisfying cleaning, no time pressure'),
 ('2968420','A','FuturLab','1–4 + split screen','Sequel to the above'),
 ('2670630','A','solo dev (own account)','1 → 1–4 (co-op added later)','Run a shop by hand, grow it'),
 ('3070070','A','solo + freelancers','1','Open card packs (the gamble) inside a shop sim'),
 ('1149620','A','DRAGO (studio ≈50)','1','Restore an abandoned gas station'),
 ('1812450','A','Donkey Crew (n unverified)','1–4','Medieval rebellion + town-building'),
 ('3583210','A','Aggro Crab (n unverified)','1–4','Forklift co-op: load trucks before they depart'),
 ('2487300','A','Saber (n unverified)','1','Realistic container-port machinery'),
 ('4001890','B','Dazed Games (n unverified)','1–4','Physics fishing with guns/explosives; sell fish, buy upgrades'),
 ('3949040','B','4 built it in ≈8 weeks (studio 5–10)','1–4','Drive an RV home over awful terrain together'),
 ('4704690','B','solo (n unverified)','online','Hide-and-seek playacting'),
 ('2567870','B','unverified','1–4','Climb out of Hell chained together'),
 ('3097560','B','unverified','4','Bluffing at a bar; loser plays roulette'),
 ('3146520','B','1 credited','1–12','Cosy chat-room fishing'),
 ('3643170','B','Cybernetic Walrus (n unverified)','1–4','Aliens in disguise run a gas station'),
 ('4069520','B','Brimstone (n unverified)','online','Party battle golf'),
 ('2709570','B','unverified','1–16','Free co-op supermarket'),
 ('3716600','B','solo','PvP to 4v4','Shout spells into the mic'),
 ('1532200','C','"small team"','1–4','Build physics rovers, haul awkward cargo on Mars'),
 ('2625420','C','multi-person studio','1–4','Procedural co-op road trip'),
 ('1641700','C','SMG + DevM (Team17)','1–4','Physics house-moving levels'),
 ('1011670','C',"We're Five Games",'1–4','Ragdoll couriers'),
 ('2424010','C','Dansan Digital','1','Parcel job sim, ≈$50 marketing'),
 ('4098030','C','Playstrom (SayGames)','1','Pack parcels by hand, then automate'),
 ('2201940','D','Games Incubator (PlayWay)','online co-op','Scrap ships with heavy tools'),
 ('2582140','D','astragon','1–4 (added Aug 2026)','Realistic ship sim; co-op crew added late'),
 ('4429930','D','DiSolve Studios','co-op','Chaotic co-op on a leaky boat'),
 ('4475470','D','4 friends','1–4','Co-op boat-repair shop comedy'),
 ('3870930','D','C51 Games','1–4','Dockside fish-and-cook sim'),
 ('3806270','D','Tikam Studio','1','Rail container terminal sim'),
 ('3672760','D','NFold Games','1','Harbour traffic control'),
 ('3572770','D','Urban Isotope','1','Idle port tycoon'),
 ('3364470','D','solo','1','Forklift puzzle'),
]
GROUPS = {'A': 'A. The games Kufu named', 'B': 'B. Other 2024–26 co-op breakouts', 'C': 'C. Co-op/physics logistics near-misses', 'D': 'D. Port, ship and harbour games'}

def k(n):
    if n >= 1_000_000: return f"{n/1e6:.1f}M"
    if n >= 10_000: return f"{n/1e3:.0f}K"
    return f"{n:,}"

out, g0 = [], None
hdr = ('| # | Game | EA start | 1.0 / full release | List price | Reviews (all languages) | % positive | All-time peak CCU | Owners: reviews ×30–60 | Raijin est. units | Team at launch | Co-op | Core hook |\n'
       '|---|---|---|---|---|---|---|---|---|---|---|---|---|')
for i, (a, g, team, coop, hook) in enumerate(M, 1):
    if g != g0:
        out += ['', f'**{GROUPS[g]}**', '', hdr]; g0 = g
    r = S[a]
    rev = r.get('alltime_reviews') or r.get('hist_reviews') or 0
    pct = r.get('alltime_pct') if r.get('alltime_reviews') else r.get('hist_pct')
    pk = [x for x in [r.get('peak_ccu_steamcharts'), int(TG[a]) if TG.get(a) else None] if x]
    price = r.get('price_list_usd') or r.get('price_now_usd') or ''
    ea = r.get('ea_date') or '–'
    rel = r.get('release')
    rel = 'still in EA' if rel == ea else rel
    box = f"{k(rev*30)}–{k(rev*60)}" if rev else '–'
    sales = RJ[a]['sales_est']
    name = r['name'].replace('|', '/')
    out.append(f"| {i} | [{name}](https://store.steampowered.com/app/{a}/) | {ea} | {rel} | {price} | {rev:,} | {('%g' % pct) + '%' if pct is not None else '–'} | {f'{max(pk):,}' if pk else '–'} | {box} | {f'{sales:,}' if sales else '–'} | {team} | {coop} | {hook} |")
open('table.md', 'w', encoding='utf-8').write('\n'.join(out) + '\n')
print('rows', len(M))
