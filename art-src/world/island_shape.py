"""Crescent Bay (map B) as numbers: the coastline, heights, seabed depth, sailing routes. Pure numpy, no Blender.
One source of truth: the Blender blockout (build_island.py), the seabed PNG and the route measures all sample heightfield().

Map coordinates are make_maps.py's (metres, x east, y SOUTH, the 5 x 5 km sheet 0..5000), so every control point below is
copied 1:1 from map_b() and the picture wins (HANDOVER rule 42). World coordinates: X east, Y north, Z up, metres, origin
= the island's bounding-box centre (map 2840, 2550), as gdd world 8.5 asks ("the island's centre is world (0,0)").

Run alone:  python art-src/world/island_shape.py [outdir]  -> seabed PNG + JSON + preview, and prints the measures.
"""
import math, os, sys, json, zlib, struct, heapq
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
OX, OY = 2840.0, 2550.0            # map point that is world (0, 0)
SIZE, N = 5000.0, 2048             # seabed texture = exactly map-B.png's 5 x 5 km sheet, 2.44 m per pixel
XW, YN = -OX, OY                   # the sheet's west edge (world X) and north edge (world Y): X -2840..2160, Y -2450..2550
HMIN, HMAX = -160.0, 240.0         # 16-bit encoding range: h = v / 65535 * 400 - 160 (6.1 mm steps; a 220 m ridge fits)


def W(mx, my): return (mx - OX, OY - my)          # map -> world
def Wl(pts): return [W(*p) for p in pts]


# ---------------------------------------------------------------- map B control points (make_maps.py map_b, 31 Oct)
ISLAND = [(1900, 1700), (2300, 1450), (2900, 1400), (3400, 1650), (3700, 2100), (3780, 2600), (3650, 3150),
          (3300, 3550), (2800, 3700), (2250, 3600), (1900, 3350), (2150, 3200), (2550, 3250), (2850, 3050),
          (3000, 2650), (2950, 2200), (2700, 1900), (2300, 1880), (2050, 1950)]
TOWN = [(2880, 2450), (2990, 2400), (3030, 2560), (2960, 2700), (2870, 2640)]          # Old Saltmouth's tan blob
VOSS = [(3700, 2350), (3980, 2300), (4080, 2600), (3980, 2900), (3720, 2850)]          # Voss Harbour's grey quay
RIDGE = [(2950, 1550), (3400, 2100), (3500, 2700), (3200, 3350)]                       # Saltback Ridge, 140 m
CUT = [(2580, 3230), (2700, 3680)]                                                     # Walt's Cut, silted
BREAKWATER = [(4250, 2150), (4380, 2600), (4250, 3050)]
BUOYLINE = [(4300, 2050), (4520, 2600), (4320, 3200)]
SHOPS = {1: ("Hook, Line & Sinker", (2900, 2480)), 2: ("Doyle Boatworks", (2880, 2620)), 3: ("Plank & Post", (2960, 2650)),
         4: ("The Mooring Line", (2950, 2510)), 5: ("Fish Market", (3000, 2570)), 6: ("Swell Surf Co.", (2600, 3260)),
         7: ("Post office + Nell's", (2990, 2440))}
PLACES = dict(lighthouse=(1880, 1640), wreck=(1000, 1700), dock=(2750, 2550))
EXPANSION = {"P1": (2700, 2300, 180, 110), "P2": (2650, 2080, 200, 140), "P3": (2620, 2700, 180, 180)}  # x0, y0, w, h
ISLETS = dict(gullrock=(2250, 2550, 120, 90), lantern=(1100, 2600, 220, 120), sorrel=(4100, 900, 120, 90),
              mango=(4300, 4300, 150, 110))
# gdd world 8.0's route end points and its measured lengths (make_maps.py, 31 Oct), to compare with the real water paths
TARGETS = {"Bay Shallows": ((2500, 2700), 300), "Gullrock": ((2380, 2560), 400), "Lantern Key": ((1320, 2660), 1700),
           "Wreck of the Margaret": ((1100, 1780), 1900), "Breaker Point surf": ((2600, 3900), 2700),
           "Sorrel Isle": ((4000, 1000), 4200), "Mango Cay": ((4150, 4250), 4300),
           "Voss Harbour (round the south horn)": ((3950, 3000), 4500)}
CUT_TARGET = ("Voss Harbour via Walt's Cut (dredged)", (3950, 3000), 2900)


def islet_pts(x, y, rx, ry):      # make_maps.py Map.islet(), the same 12 points
    return [(x + rx * math.cos(a) * (1 + .12 * math.sin(3 * a)), y + ry * math.sin(a) * (1 + .1 * math.cos(2 * a)))
            for a in [i * math.pi / 6 for i in range(12)]]


def smooth(pts, n=16, closed=True):
    """make_maps.py's Catmull-Rom -> cubic Bezier, sampled: the exact curve the SVG draws."""
    P = [np.array(p, float) for p in pts]; k = len(P); out = []
    for i in range(k if closed else k - 1):
        p0 = P[(i - 1) % k] if closed or i > 0 else P[i]; p1, p2 = P[i], P[(i + 1) % k]
        p3 = P[(i + 2) % k] if closed or i + 2 < k else p2
        c1, c2 = p1 + (p2 - p0) / 6, p2 - (p3 - p1) / 6
        for t in np.linspace(0, 1, n, endpoint=False):
            u = 1 - t; out.append(u ** 3 * p1 + 3 * u * u * t * c1 + 3 * u * t * t * c2 + t ** 3 * p2)
    if not closed: out.append(P[-1])
    return np.array(out)


# ---------------------------------------------------------------- fields
def sstep(a, b, x): t = np.clip((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t)


def seg_dist(X, Y, poly, closed=False):
    """Distance from every (X, Y) to a polyline (world coords)."""
    d2 = np.full(X.shape, np.inf, np.float32); P = np.asarray(poly, np.float32)
    segs = len(P) if closed else len(P) - 1
    for i in range(segs):
        a, b = P[i], P[(i + 1) % len(P)]; ab = b - a; L2 = float(ab @ ab) or 1e-9
        t = np.clip(((X - a[0]) * ab[0] + (Y - a[1]) * ab[1]) / L2, 0, 1)
        d2 = np.minimum(d2, (X - a[0] - t * ab[0]) ** 2 + (Y - a[1] - t * ab[1]) ** 2)
    return np.sqrt(d2)


def seg_dist_val(X, Y, poly, vals):
    """Distance to a polyline + the value linearly interpolated along it at the nearest point (spines with heights)."""
    best = np.full(X.shape, np.inf, np.float32); val = np.zeros(X.shape, np.float32); P = np.asarray(poly, np.float32)
    for i in range(len(P) - 1):
        a, b = P[i], P[i + 1]; ab = b - a; L2 = float(ab @ ab)
        t = np.clip(((X - a[0]) * ab[0] + (Y - a[1]) * ab[1]) / L2, 0, 1)
        d2 = (X - a[0] - t * ab[0]) ** 2 + (Y - a[1] - t * ab[1]) ** 2
        m = d2 < best; best = np.where(m, d2, best); val = np.where(m, vals[i] + t * (vals[i + 1] - vals[i]), val)
    return np.sqrt(best), val


def sdf_poly(X, Y, poly):
    """Signed distance to a closed polygon, + inside (land)."""
    P = np.asarray(poly, np.float32); inside = np.zeros(X.shape, bool)
    for i in range(len(P)):
        (x1, y1), (x2, y2) = P[i], P[(i + 1) % len(P)]
        if y1 == y2: continue
        c = (Y > y1) != (Y > y2)
        xi = x1 + (Y - y1) * (x2 - x1) / (y2 - y1)
        inside ^= c & (X < xi)
    d = seg_dist(X, Y, P, closed=True)
    return np.where(inside, d, -d)


def waves(X, Y, seed, comps):
    """Organic low-frequency relief: a sum of rotated sines (deterministic, no noise library)."""
    r = np.random.RandomState(seed); z = np.zeros(X.shape, np.float32)
    for wl, amp in comps:
        a = r.uniform(0, math.pi); ph = r.uniform(0, 2 * math.pi); k = 2 * math.pi / wl
        z += amp * np.sin(k * (X * math.cos(a) + Y * math.sin(a)) + ph)
    return z


def grid(n=N, size=SIZE):
    c = (np.arange(n, dtype=np.float32) + 0.5) * size / n
    return np.meshgrid(XW + c, YN - c)          # row 0 = north edge


def bilinear(A, n):
    m = A.shape[0]; c = (np.arange(n) + 0.5) * m / n - 0.5
    i0 = np.clip(np.floor(c).astype(int), 0, m - 2); f = np.clip(c - i0, 0, 1).astype(np.float32)
    A = A[:, i0] * (1 - f)[None, :] + A[:, i0 + 1] * f[None, :]
    return A[i0, :] * (1 - f)[:, None] + A[i0 + 1, :] * f[:, None]


PIER_ROOT = None   # set by heightfield(): world (x, y) of Walt's pier root on the town shore

# Knobs. RIDGE_H: Saltback Ridge's crest. HORN: metres the south horn's tip reaches further out (west, a little south),
# which lengthens the way round to Voss; Walt's Cut and everything east stay put. Map B as drawn = 140, 0.
# Defaults = Manan's pick (3 Nov): ridge 220 m, horn +400 m (Voss 4.48 km = 12.4 min at 6 m/s, the gdd's number).
RIDGE_H, HORN = 220.0, 400.0
HORN_W = {9: 0.55, 10: 1.0, 11: 0.8}          # ISLAND indices of the south horn's tip and their share of the stretch


def island_pts():
    return [(x - HORN * HORN_W.get(i, 0), y + 0.3 * HORN * HORN_W.get(i, 0)) for i, (x, y) in enumerate(ISLAND)]


def heightfield(n=N, cut_open=False, sdf_n=1024):
    """Returns dict of n x n float32 fields over world [-2500, 2500]^2 (row 0 north):
    h (metres, + land / - seabed), sdf (+ inland metres to the nearest coast), beach (0 rock .. 1 sand), kind (0 nature,
    1 town, 2 Voss concrete, 3 Walt's Cut, 4 breakwater)."""
    global PIER_ROOT
    # 1) signed distance to the coastlines (exact at sdf_n, bilinear to n: the coast curves are smooth)
    Xs, Ys = grid(sdf_n)
    main = np.maximum(sdf_poly(Xs, Ys, Wl(smooth(island_pts()))), sdf_poly(Xs, Ys, Wl(smooth(TOWN, 12))))
    voss = sdf_poly(Xs, Ys, Wl(smooth(VOSS, 12)))
    isl = {k: sdf_poly(Xs, Ys, Wl(smooth(islet_pts(*v), 8))) for k, v in ISLETS.items()}
    sdf_s = np.maximum.reduce([main, voss] + list(isl.values()))
    up = (lambda A: A) if sdf_n == n else (lambda A: bilinear(A, n))
    sdf, d_main, d_voss = up(sdf_s), up(main), up(voss)
    d_isl = {k: up(v) for k, v in isl.items()}
    X, Y = grid(n)
    M = lambda pts: np.array(Wl(pts), np.float32)

    # 2) beaches vs rock: sandy where the map has beaches (Breaker Point, the bay's inner shores, Mango Cay), else rock
    beach = np.zeros(X.shape, np.float32)
    for (mx, my), r in [((2700, 3740), 520), ((2420, 3230), 300), ((2780, 3060), 170), ((2480, 1900), 330),
                        ((2830, 2180), 160), ((1330, 2640), 110), ((4300, 4300), 330)]:
        wx, wy = W(mx, my); beach = np.maximum(beach, 1 - sstep(r * 0.55, r, np.hypot(X - wx, Y - wy)))
    kind = np.zeros(X.shape, np.uint8)

    # 3) land: hills from spines with heights, clipped by a coastal envelope (cliffs on rock, gentle on sand)
    rk = RIDGE_H / 140.0
    spines = [(RIDGE, [95 * rk, 140 * rk, 138 * rk, 85 * rk], 620),
              ([(2950, 1550), (2600, 1560), (2250, 1640), (1960, 1690)], [95, 62, 40, 24], 260),     # north horn
              ([(3200, 3350), (2900, 3470), (2660, 3440), (2400 - 0.45 * HORN, 3420 + 0.13 * HORN),
                (2100 - HORN, 3370 + 0.3 * HORN)], [85, 42, 9, 26, 15 + 0.012 * HORN], 250)]                # south
    S = np.zeros(X.shape, np.float32)
    for pts, hs, w in spines:
        r, v = seg_dist_val(X, Y, M(pts), hs)
        S = np.maximum(S, v * (1 - sstep(0, w, r)) ** 1.15)
    for k, (h_, w_) in dict(gullrock=(9, 110), lantern=(30, 190), sorrel=(18, 120), mango=(6, 150)).items():
        wx, wy = W(*ISLETS[k][:2]); S = np.maximum(S, h_ * (1 - sstep(0, w_, np.hypot(X - wx, Y - wy))))
    S += waves(X, Y, 3, [(900, 9), (620, 7), (410, 5), (260, 3), (170, 1.6)]) * sstep(0, 120, sdf)
    d = np.maximum(sdf, 0)
    rock_env = 0.9 + 0.07 * d + 26 * sstep(4, 40, d) + 0.45 * rk ** 1.5 * np.maximum(d - 40, 0)
    sand_env = np.minimum(d * 0.05, 2.6) + 0.1 * np.maximum(d - 55, 0)
    env = rock_env + (sand_env - rock_env) * beach
    land = np.maximum(np.minimum(np.maximum(S, 6.0), env), np.minimum(0.05 * d, 2.5))

    # Old Saltmouth: soft terraces from the waterfront (1.6 m seawall) up the hillside; Walt's pier root flat at 1.25 m
    tx, ty = W(2950, 2560); rt = np.hypot((X - tx) * 0.8, Y - ty)
    town = (1 - sstep(170, 330, rt)) * (d_main > -1)
    terr = np.minimum(1.6 + np.maximum(land - 1.6, 0) * 0.55, 22)          # the hillside, eased under the houses
    land = land + (terr - land) * town
    kind[(town > 0.5) & (sdf > 0)] = 1
    # pier root: where world Y = 0 (map y 2550) meets the town blob's west shore
    j = int(round((YN - W(0, 2550)[1]) / (SIZE / n) - 0.5)); xs = X[j]; s_row = sdf[j]
    cand = np.where((s_row[:-1] < 0) & (s_row[1:] >= 0) & (xs[:-1] > W(2700, 0)[0]) & (xs[:-1] < W(3050, 0)[0]))[0]
    i = cand[0]; xr = xs[i] + (xs[i + 1] - xs[i]) * (-s_row[i]) / (s_row[i + 1] - s_row[i])
    PIER_ROOT = (float(xr), float(W(0, 2550)[1]))
    lx, ly = PIER_ROOT[0] + 12.0, PIER_ROOT[1] + 3.0          # Walt's landing yard (M1: flat around the shack)
    yard = 1 - sstep(24, 46, np.hypot(X - lx, Y - ly))
    land = land + (1.25 - land) * yard * (sdf > 0)
    # Voss quay: flat 3.0 m reclaimed concrete
    vq = d_voss > 0
    land = np.where(vq, 3.0, land); kind[vq] = 2

    # 4) seabed: every coast starts with a ~10 m wave-cut shelf (so foam shows everywhere), rock drops fast, sand is a
    #    wide turquoise shallow; the calm bay stays a bowl of ~8-14 m; the open sea falls to ~150 m by the map edge
    s = np.maximum(-sdf, 0)
    bx, by = W(2480, 2560); bay = 1 - sstep(450, 950, np.hypot(X - bx, Y - by))
    L = 38 + (260 - 38) * beach
    shelf = 0.08 * np.minimum(s, 10) + (16 * (1 - 0.35 * bay)) * (1 - np.exp(-np.maximum(s - 10, 0) / L))
    ocean = 135 * sstep(450, 2300, s) * (1 - bay)
    depth = shelf + ocean
    tw = (1 - sstep(80, 160, np.hypot(X - PIER_ROOT[0], Y - PIER_ROOT[1]))) * (sdf < 0)   # the landing's dredged berth
    depth = np.maximum(depth, (2.6 + 0.02 * s) * tw)
    # sand bank of the Bay Shallows (home water) and Gullrock's reef flat (Widowmaker reef break), the north-horn point reef
    def wob(wx, wy, r, seed):          # a lumpy circle, not a compass circle
        a = np.arctan2(Y - wy, X - wx); return np.hypot(X - wx, Y - wy) / (r * (1 + 0.16 * np.sin(3 * a + seed) + 0.09 * np.sin(5 * a + 2 * seed)))
    for k, ((mx, my), r, top) in enumerate([((2500, 2700), 190, 1.6), ((1650, 1330), 150, 2.2)]):
        wx, wy = W(mx, my); q = 1 - sstep(0.35, 1, wob(wx, wy, r, k + 1))
        depth = depth + (np.minimum(depth, top + 4 * (1 - q)) - depth) * q
    sg = np.maximum(-d_isl["gullrock"], 0)
    depth = np.minimum(depth, 0.9 + 0.035 * sg + np.where(sg > 140, 99, 0))
    # the Wreck of the Margaret lies on a 5 m shoal (her bow and mast break the surface: build_island.py)
    wx, wy = W(*PLACES["wreck"]); q = 1 - sstep(0.25, 1, wob(wx, wy, 160, 4))
    depth = depth + (5.0 - depth) * q
    # Voss: dredged basin behind the breakwater (14 m), the breakwater a rubble mound to +3.5 m
    db = seg_dist(X, Y, M(smooth(BREAKWATER, 12, closed=False)))
    vx = W(3990, 2600); behind = (X > W(3960, 0)[0]) & (X < W(4380, 0)[0]) & (np.abs(Y - vx[1]) < 520) & (sdf < 0)
    depth = np.where(behind, np.maximum(depth, 14.0), depth)
    rr = np.hypot(X - (XW + SIZE / 2), Y - (YN - SIZE / 2))                          # a round deep rim toward the sheet
    depth = np.maximum(depth, 150 * sstep(2100, 2600, rr) * sstep(60, 250, s))       # edge: the texture clamps to ocean
    h = np.where(sdf > 0, land, -depth)
    bw = 1 - sstep(10, 34, db)
    h = np.maximum(h, -depth + (3.5 + depth) * bw)
    kind[(db < 22) & (h > -1)] = 4

    # 5) Walt's Cut: a 24 m channel silted to 0.5 m through the horn's low saddle, the low Voss Causeway across its middle
    dc = seg_dist(X, Y, M([(2572, 3195), (2712, 3715)]))
    ch = 1 - sstep(12, 30, dc)
    cut_depth = 6.0 if cut_open else 0.5
    h = np.where(dc < 30, np.minimum(h, np.where(sdf > 0, 1.2 + 0.25 * np.maximum(dc - 12, 0), h)), h)
    h = h + (-cut_depth - h) * ch * (h > -cut_depth)
    kind[(dc < 14)] = 3
    if not cut_open:
        cw = W(2641, 3452); ax = np.array(W(2700, 3680)) - np.array(W(2580, 3230)); ax /= np.linalg.norm(ax)
        along = (X - cw[0]) * ax[0] + (Y - cw[1]) * ax[1]
        cause = (np.abs(along) < 6) & (dc < 34)
        h = np.where(cause, np.maximum(h, 1.7), h)

    return dict(h=h.astype(np.float32), sdf=sdf.astype(np.float32), beach=beach, kind=kind, X=X, Y=Y)


# ---------------------------------------------------------------- PNG writer (no Pillow needed: runs inside Blender too)
def write_png(path, arr):
    """arr: (H, W) uint16 -> 16-bit grey, or (H, W, 3) uint8 -> RGB. Sub filter for compression."""
    if arr.dtype == np.uint16: raw = arr.astype('>u2').view(np.uint8).reshape(arr.shape[0], -1); bpp, ct, bd = 2, 0, 16
    else: raw = arr.reshape(arr.shape[0], -1).astype(np.uint8); bpp, ct, bd = 3, 2, 8
    f = raw.copy(); f[:, bpp:] = (raw[:, bpp:].astype(np.int16) - raw[:, :-bpp]).astype(np.uint8)
    data = zlib.compress(np.hstack([np.ones((raw.shape[0], 1), np.uint8), f]).tobytes(), 9)
    chunk = lambda t, b: struct.pack('>I', len(b)) + t + b + struct.pack('>I', zlib.crc32(t + b) & 0xffffffff)
    with open(path, 'wb') as fh:
        fh.write(b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', arr.shape[1], arr.shape[0], bd, ct, 0, 0, 0))
                 + chunk(b'IDAT', data) + chunk(b'IEND', b''))


BANDS = [(-1e9, '#FFFBF2'), (0.4, '#8EF0E2'), (2, '#46D6C8'), (5, '#30BCC6'), (12, '#2A97C9'), (30, '#2779C2'),
         (70, '#2160B8')]        # depth (m) -> Ponyo sea band: contact foam, shallows #46D6C8 ... deep #2160B8 (systems 4.3)


def hexrgb(h): return tuple(int(h[i:i + 2], 16) for i in (1, 3, 5))


def preview(F):
    h = F["h"]; img = np.zeros(h.shape + (3,), np.uint8); dep = -h
    for lim, c in BANDS: img[dep >= lim] = hexrgb(c)
    land = h > 0
    g = np.clip(h / 140, 0, 1)[..., None]
    lc = (np.array(hexrgb('#8CC46A')) * (1 - g) + np.array(hexrgb('#5E8F4E')) * g)
    lc = np.where((F["beach"] > 0.5)[..., None] & (h < 3)[..., None], hexrgb('#EAD7A8'), lc)
    lc = np.where((F["kind"] == 1)[..., None], hexrgb('#E8C9A0'), lc)
    lc = np.where((F["kind"] == 2)[..., None], hexrgb('#9AA3A8'), lc)
    img[land] = lc[land].astype(np.uint8)
    return img


def write_seabed(F, outdir):
    os.makedirs(outdir, exist_ok=True)
    v = np.round((np.clip(F["h"], HMIN, HMAX) - HMIN) / (HMAX - HMIN) * 65535).astype(np.uint16)
    p = os.path.join(outdir, 'seabed_2048.png'); write_png(p, v)
    write_png(os.path.join(outdir, 'seabed_2048_preview.png'), preview(F))
    meta = dict(file='seabed_2048.png', size_px=N, metres_per_px=SIZE / N,
                world_min=[XW, YN - SIZE], world_max=[XW + SIZE, YN],
                axes="pixel column -> world X (east), row 0 = north edge (world Y = %g); Unity: X east, Z north" % YN,
                encoding="16-bit grey, linear: height_m = value / 65535 * %g + %g; sea level 0; depth = -height" % (HMAX - HMIN, HMIN),
                origin_on_map_b="world (0,0) = make_maps.py map point (%g, %g), the island's bounding-box centre" % (OX, OY),
                walts_cut="silted to 0.5 m with the Voss Causeway (1.7 m) across it; the dredged variant is cut_open=True",
                source="art-src/world/island_shape.py heightfield(); regenerate: python art-src/world/island_shape.py")
    with open(os.path.join(outdir, 'seabed_2048.json'), 'w') as fh: json.dump(meta, fh, indent=1)
    return p


# ---------------------------------------------------------------- measures
def route_lengths(F, start_w, targets, min_depth=1.2, step=4, paths=None):
    """Shortest water path (16-neighbour Dijkstra on a grid of step*2.44 m) from start to each target; a cell is water only
    if every sub-cell is >= min_depth deep (Biscuit draws ~0.3 m; the margin keeps her off the rocks)."""
    h = F["h"]; n = h.shape[0] // step
    deep = (h.reshape(n, step, n, step).max(axis=(1, 3)) <= -min_depth)
    cs = SIZE / n
    def cell(p): return (int((YN - p[1]) / cs), int((p[0] - XW) / cs))
    def nearest_water(c):
        for r in range(0, 40):
            for dj in range(-r, r + 1):
                for di in range(-r, r + 1):
                    j, i = c[0] + dj, c[1] + di
                    if 0 <= j < n and 0 <= i < n and deep[j, i]: return (j, i)
        return c
    nb = [(dj, di, math.hypot(dj, di)) for dj in range(-2, 3) for di in range(-2, 3)
          if (dj, di) != (0, 0) and math.gcd(abs(dj), abs(di)) == 1]
    src = nearest_water(cell(start_w)); dist = np.full((n, n), np.inf); dist[src] = 0
    want = {k: nearest_water(cell(W(*p))) for k, p in targets.items()}; left = set(want.values())
    pq = [(0.0, src)]; dl = deep.tolist(); prev = {}
    while pq and left:
        dcur, (j, i) = heapq.heappop(pq)
        if dcur > dist[j, i]: continue
        left.discard((j, i))
        for dj, di, L in nb:
            jj, ii = j + dj, i + di
            if not (0 <= jj < n and 0 <= ii < n and dl[jj][ii]): continue
            if abs(dj) == 2 or abs(di) == 2:        # a knight move: the two cells it passes must be water too
                if not (dl[j + (dj > 0) - (dj < 0)][i + (di // 2 if abs(di) == 2 else 0)] and
                        dl[j + (dj // 2 if abs(dj) == 2 else 0)][i + (di > 0) - (di < 0)]): continue
            nd = dcur + L * cs
            if nd < dist[jj, ii]: dist[jj, ii] = nd; prev[(jj, ii)] = (j, i); heapq.heappush(pq, (nd, (jj, ii)))
    if paths is not None:                       # world points of each path, start -> target (for drawing)
        for k, c in want.items():
            p = [c]
            while p[-1] in prev: p.append(prev[p[-1]])
            paths[k] = [(XW + (i + 0.5) * cs, YN - (j + 0.5) * cs) for j, i in reversed(p)]
    return {k: float(dist[c]) for k, c in want.items()}


def stats(F):
    h, X, Y = F["h"], F["X"], F["Y"]; px = SIZE / h.shape[0]
    wx0, wx1 = W(1700, 0)[0], W(3850, 0)[0]                # the main island (without Voss's quay and the islets)
    main = (h > 0) & (X > wx0) & (X < wx1) & (np.abs(Y) < 1300) & (F["kind"] != 2)
    xs, ys = X[main], Y[main]
    tips_n, tips_s = W(1900, 1700), W(1900, 3350)
    return dict(ew_km=(xs.max() - xs.min()) / 1000, ns_km=(ys.max() - ys.min()) / 1000,
                land_km2=main.sum() * px * px / 1e6, peak_m=float(h[main].max()),
                bay_mouth_km=math.dist(tips_n, tips_s) / 1000, pier_root=PIER_ROOT,
                bay_depth_km=(PIER_ROOT[0] - tips_n[0]) / 1000)


def report(F, Fo=None):
    st = stats(F); start = (PIER_ROOT[0] - 32, PIER_ROOT[1])     # Walt's pier end (M1's pier is 32 m)
    tg = {k: v[0] for k, v in TARGETS.items()}
    got = route_lengths(F, start, tg)
    rows = [(k, TARGETS[k][1], got[k]) for k in TARGETS]
    if Fo is not None:
        rows.append((CUT_TARGET[0], CUT_TARGET[2], route_lengths(Fo, start, {"v": CUT_TARGET[1]})["v"]))
    return st, rows


if __name__ == '__main__':
    import time
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, 'seabed')
    t = time.time(); F = heightfield(); print('heightfield %.1fs' % (time.time() - t))
    print('wrote', write_seabed(F, out))
    Fo = heightfield(cut_open=True)
    st, rows = report(F, Fo)
    print('STATS', json.dumps(st, default=float))
    for k, gdd, m in rows:
        print('ROUTE %-40s gdd %5.0f m  water path %5.0f m  Biscuit 4.5 m/s %4.1f min  6 m/s %4.1f min  (gdd %4.1f)' % (
            k, gdd, m, m / 4.5 / 60, m / 6 / 60, gdd / 6 / 60))
    print('done %.1fs' % (time.time() - t))
