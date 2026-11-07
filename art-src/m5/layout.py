# Office set-spot layouts + the checker (pure Python: build_props.py imports it; `python layout.py` runs the check alone).
# Coordinates: office local space (origin = floor centre of the office tier, floor top z = 0, front/door = -Y).
# yaw (deg): the item's front (-Y in its own file) turned by yaw about Z: 0 faces -Y, 90 faces +X, 180 faces +Y, -90 faces -X.
import math, json, os

# real bounds (x0, x1, y0, y1, z0, z1) of each prop in its own space, measured from the built meshes by build_props.py
# (out/props.json); these defaults are used until it has run once.
def _c(w, d, h, z0=0.0): return (-w / 2, w / 2, -d / 2, d / 2, z0, z0 + h)
def _w(w, d, h): return (-w / 2, w / 2, -d, 0.0, -h / 2, h / 2)      # wall pieces: back face on the wall (y = 0)
FOOT = dict(cot_fixed=_c(0.80, 1.95, 0.5), bunk=_c(2.0, 1.05, 1.75), double=_c(1.45, 2.05, 1.0), chair=_c(0.62, 0.6, 0.9),
            desk=_c(1.3, 0.65, 0.8), shelves=_c(1.0, 0.35, 1.8), wardrobe=(-0.5, 0.5, -0.55, 0.3, 0, 1.95), rug=_c(1.6, 1.0, 0.02),
            plant=_c(0.45, 0.45, 0.9), lamp=_c(0.3, 0.3, 0.5), floorlamp=_c(0.45, 0.45, 1.6), stove=_c(0.6, 0.6, 2.3),
            maptable=_c(1.4, 0.9, 0.9), locker=_c(0.47, 0.45, 1.82), crate=_c(0.40, 0.31, 0.42), chest=_c(0.8, 0.45, 0.62),
            painting=_w(0.72, 0.05, 0.56), painting2=_w(0.52, 0.05, 0.72), trophy=_w(0.9, 0.13, 0.42))
_pj = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'out', 'props.json')
if os.path.exists(_pj):
    for k, v in json.load(open(_pj)).items():
        if k in FOOT: FOOT[k] = tuple(v)
SPOT2PROP = dict(spot_cot='cot_fixed', spot_bunk='bunk', spot_double='double', spot_chair='chair', spot_desk='desk',
                 spot_shelves='shelves', spot_wardrobe='wardrobe', spot_rug='rug', spot_plant='plant', spot_lamp='lamp',
                 spot_floorlamp='floorlamp', spot_stove='stove', spot_maptable='maptable', spot_locker='locker',
                 fix_crate='crate', fix_chest='chest', spot_painting='painting', spot_painting2='painting2', spot_trophy='trophy')
WALL = {'spot_painting', 'spot_painting2', 'spot_trophy'}
ONTOP = {'spot_lamp'}             # stands on a fixed piece (the bedside crate), not on the floor
WALKABLE = {'spot_rug'}           # flat: you walk over it

# Each tier: inner floor box, the door gap on the front wall, windows [(wall, a0, a1, z0, z1)], and the spots
# {name: (x, y, z, yaw)}. Walls: 'F' y = y0, 'B' y = y1, 'L' x = x0, 'R' x = x1.
LAYOUT = {
    # (Tushar 7 Nov, take 2: "the room is too small, bro": every tier grows. office0 4 x 3.5 -> 6 x 5, office1 6 x 4.5 ->
    # 7.6 x 5.6, office2 8 x 6 -> 9.2 x 6.8; office1/2's spots grown from their old plans, the wall pieces kept on their walls)
    'office0': dict(inner=(-2.94, 2.94, -2.44, 2.44), door=(0.75, 1.95), eave=2.5,
                    windows=[('F', -2.2, -1.2, 0.95, 1.65), ('B', 0.4, 1.2, 1.0, 1.7), ('R', -0.5, 0.3, 0.95, 1.65)],
                    spots=dict(
                        spot_cot=(-2.54, -1.35, 0, 0), fix_crate=(-2.72, -0.1, 0, 0), spot_lamp=(-2.72, -0.1, 0.42, 0),
                        fix_chest=(-1.5, -2.215, 0, 0),
                        spot_wardrobe=(-2.64, 1.15, 0, 90), spot_floorlamp=(-1.6, 2.2, 0, 0),
                        spot_desk=(-0.55, 2.115, 0, 0), spot_chair=(-0.55, 1.38, 0, 180),
                        spot_stove=(2.2, 2.1, 0, 0), spot_shelves=(2.765, 1.0, 0, -90),
                        spot_locker=(2.45, -2.215, 0, 180), spot_plant=(0.25, -2.215, 0, 180),
                        spot_rug=(0.4, -0.3, 0, 0),
                        spot_painting=(-0.55, 2.44, 1.45, 0), spot_painting2=(-2.94, -1.35, 1.35, 90),
                        spot_trophy=(2.94, -1.3, 1.6, -90))),
    # office1/office2 coordinates here are BUILDING-centred; build_props.py adds OFFSET[tier]: office1 (-0.6, +0.3), office2 (-1.2, +0.6)
    'office1': dict(inner=(-3.74, 3.74, -2.74, 2.74), door=(0.86, 2.06), eave=2.7,
                    windows=[('F', -2.04, -1.02, 0.95, 1.75), ('F', -0.64, 0.0, 0.95, 1.75), ('B', 2.29, 2.99, 1.0, 1.7), ('R', 0.94, 1.94, 0.95, 1.75)],
                    spots=dict(
                        spot_double=(-3.015, 1.695, 0, 0), fix_crate=(-1.514, 2.54, 0, 0), spot_lamp=(-1.514, 2.54, 0.42, 0),
                        spot_bunk=(-3.215, -1.715, 0, 90), spot_cot=(-1.145, -2.34, 0, 90), spot_stove=(-0.509, 2.42, 0, 0),
                        spot_desk=(1.845, 2.395, 0, 0), spot_chair=(1.845, 1.276, 0, 180), spot_floorlamp=(3.515, 2.495, 0, 0),
                        spot_shelves=(3.565, 0.063, 0, -90), spot_wardrobe=(3.44, -1.314, 0, -90), spot_maptable=(-0.509, 0.0, 0, 0),
                        spot_rug=(1.552, -0.751, 0, 0), spot_locker=(0.401, -2.515, 0, 180), spot_plant=(2.538, -2.515, 0, 180),
                        spot_painting=(-2.353, 2.74, 1.6, 0), spot_painting2=(1.272, 2.74, 1.5, 0), spot_trophy=(1.463, -2.74, 2.3, 180))),
    'office2': dict(inner=(-4.54, 4.54, -3.34, 3.34), door=(-3.245, -2.045), eave=2.8,
                    windows=[('F', -0.92, 0.46, 0.95, 1.85), ('L', -0.57, 0.91, 0.95, 1.85), ('B', -2.19, -1.27, 1.0, 1.8)],
                    spots=dict(
                        spot_double=(-3.795, 2.295, 0, 0), fix_crate=(-2.535, 3.14, 0, 0), spot_lamp=(-2.535, 3.14, 0.42, 0),
                        spot_cot=(-1.037, 2.345, 0, 0), spot_stove=(0.0, 3.02, 0, 0), spot_desk=(1.844, 2.995, 0, 0),
                        spot_chair=(1.844, 2.011, 0, 180), spot_floorlamp=(2.996, 3.09, 0, 0), spot_shelves=(4.365, 1.818, 0, -90),
                        spot_wardrobe=(4.24, 0.0, 0, -90), spot_bunk=(4.015, -2.32, 0, -90), spot_maptable=(0.461, -0.454, 0, 0),
                        spot_rug=(-2.189, -0.682, 0, 0), spot_locker=(-1.383, -3.115, 0, 180), spot_plant=(-0.691, -3.115, 0, 180),
                        spot_painting=(-4.54, -2.045, 1.5, 90), spot_painting2=(1.844, 3.34, 1.55, 0), spot_trophy=(-2.65, -3.34, 2.45, 180))),
}


def rot_bb(bb, yaw):
    x0, x1, y0, y1 = bb[:4]; q = int(round(yaw / 90.0)) % 4
    return [(x0, x1, y0, y1), (-y1, -y0, x0, x1), (-x1, -x0, -y1, -y0), (y0, y1, -x1, -x0)][q]


def rect(name, s):
    """Floor AABB of a spot's prop (its real bounds turned by the yaw): (x0, x1, y0, y1)."""
    x, y, z, yaw = s; r = rot_bb(FOOT[SPOT2PROP[name]], yaw)
    return (x + r[0], x + r[1], y + r[2], y + r[3])


def height(name): return FOOT[SPOT2PROP[name]][5]


def front_dir(yaw): return (round(math.sin(math.radians(yaw))), round(-math.cos(math.radians(yaw))))


def check(tier, L=None, walk=0.8, verbose=True):
    L = L or LAYOUT[tier]; x0, x1, y0, y1 = L['inner']; S = L['spots']; bad = []
    floor = {k: rect(k, v) for k, v in S.items() if k in SPOT2PROP and k not in WALL and k not in ONTOP}
    # 1) inside the room
    for k, r in floor.items():
        if r[0] < x0 - 1e-3 or r[1] > x1 + 1e-3 or r[2] < y0 - 1e-3 or r[3] > y1 + 1e-3: bad.append('%s outside the room %s' % (k, fmt(r)))
    # 2) no overlaps (the rug may lie under nothing either)
    ks = sorted(floor)
    for i, a in enumerate(ks):
        for b in ks[i + 1:]:
            A, B = floor[a], floor[b]
            if min(A[1], B[1]) - max(A[0], B[0]) > 0.005 and min(A[3], B[3]) - max(A[2], B[2]) > 0.005: bad.append('overlap %s / %s' % (a, b))
    # 3) the door zone: the gap and 0.9 m in front of it inside stays clear
    d0, d1 = L['door']; dz = (d0, d1, y0, y0 + 0.9)
    for k, r in floor.items():
        if k in WALKABLE: continue
        if min(r[1], dz[1]) - max(r[0], dz[0]) > 0.005 and min(r[3], dz[3]) - max(r[2], dz[2]) > 0.005: bad.append('%s blocks the door' % k)
    # 4) wall items: on a wall, not over windows / door / each other / a tall item below them
    wl = []
    for k in [k for k in S if k in WALL]:
        x, y, z, yaw = S[k]; bb = FOOT[SPOT2PROP[k]]; fx, fy = front_dir(yaw)
        wall = 'F' if abs(y - y0) < 0.02 and fy == 1 else 'B' if abs(y - y1) < 0.02 and fy == -1 else 'L' if abs(x - x0) < 0.02 and fx == 1 else 'R' if abs(x - x1) < 0.02 and fx == -1 else None
        if not wall: bad.append('%s not on a wall facing in' % k); continue
        r = rect(k, S[k]); a0, a1 = (r[0], r[1]) if wall in 'FB' else (r[2], r[3])
        iv = (wall, a0, a1, z + bb[4], z + bb[5]); wl.append((k, iv))
        if iv[4] > L['eave'] + 0.01 and wall in 'FB': bad.append('%s above the eave' % k)
        for wn in L['windows'] + ([('F', d0, d1, 0, 2.05)]):
            if wn[0] == wall and min(iv[2], wn[2]) - max(iv[1], wn[1]) > 0 and min(iv[4], wn[4]) - max(iv[3], wn[3]) > 0: bad.append('%s over a window/door' % k)
        for k2, r in floor.items():
            h2 = height(k2)
            near = {'F': r[2] < y0 + 0.1, 'B': r[3] > y1 - 0.1, 'L': r[0] < x0 + 0.1, 'R': r[1] > x1 - 0.1}[wall]
            a0, a1 = (r[0], r[1]) if wall in 'FB' else (r[2], r[3])
            if near and min(a1, iv[2]) - max(a0, iv[1]) > 0 and h2 > iv[3]: bad.append('%s hangs behind %s' % (k, k2))
    for i, (a, A) in enumerate(wl):
        for b, B in wl[i + 1:]:
            if A[0] == B[0] and min(A[2], B[2]) - max(A[1], B[1]) > 0 and min(A[4], B[4]) - max(A[3], B[3]) > 0: bad.append('overlap %s / %s' % (a, b))
    # tall floor items in front of windows
    for k, r in floor.items():
        h2 = height(k)
        for wn in L['windows']:
            near = {'F': r[2] < y0 + 0.15, 'B': r[3] > y1 - 0.15, 'L': r[0] < x0 + 0.15, 'R': r[1] > x1 - 0.15}[wn[0]]
            a0, a1 = (r[0], r[1]) if wn[0] in 'FB' else (r[2], r[3])
            if near and min(a1, wn[2]) - max(a0, wn[1]) > 0.02 and h2 > wn[3] + 0.05: bad.append('%s in front of a window' % k)
    # 5) walkway: a 0.8 m wide body (disc r 0.4) can get from the door to within reach of every item
    g = 0.05; nx = int(round((x1 - x0) / g)); ny = int(round((y1 - y0) / g)); rr = walk / 2
    blocked = [(r[0], r[1], r[2], r[3]) for k, r in floor.items() if k not in WALKABLE]
    def free(cx, cy):
        if cx - x0 < rr or x1 - cx < rr or cy - y0 < rr - 0.0 and not (d0 + rr <= cx <= d1 - rr) or y1 - cy < rr: return False
        for a in blocked:
            dx = max(a[0] - cx, 0, cx - a[1]); dy = max(a[2] - cy, 0, cy - a[3])
            if dx * dx + dy * dy < rr * rr: return False
        return True
    cells = {}
    for i in range(nx):
        for j in range(ny):
            cx, cy = x0 + (i + 0.5) * g, y0 + (j + 0.5) * g
            if free(cx, cy): cells[(i, j)] = (cx, cy)
    # start: the middle of the door gap, just inside (walkway may begin in the door itself)
    si = int(((d0 + d1) / 2 - x0) / g); start = None
    for j in range(ny):
        if (si, j) in cells: start = (si, j); break
    seen = set()
    if start:
        st = [start]; seen.add(start)
        while st:
            i, j = st.pop()
            for n in ((i + 1, j), (i - 1, j), (i, j + 1), (i, j - 1)):
                if n in cells and n not in seen: seen.add(n); st.append(n)
    else: bad.append('no walkable cell at the door')
    reach = {}
    for k in S:
        if k not in SPOT2PROP or k.startswith('fix_'): continue   # fixed pieces need no walkway
        if k in WALL or k in ONTOP:
            x, y, z, yaw = S[k]; tgt = (x, x, y, y); lim = rr + 1.0
        else: tgt = floor[k]; lim = rr + 0.2
        best = min((math.hypot(max(tgt[0] - c[0], 0, c[0] - tgt[1]), max(tgt[2] - c[1], 0, c[1] - tgt[3])) for c in (cells[s] for s in seen)), default=9)
        reach[k] = best - rr
        if best > lim: bad.append('%s: no %.1f m walkway to it (nearest free spot %.2f m away)' % (k, walk, best - rr))
    if verbose:
        print('%s: %d spots, %s' % (tier, len(S), 'OK' if not bad else '%d problems' % len(bad)))
        for b in bad: print('   ', b)
    return bad, reach


def fmt(r): return '(%.2f..%.2f, %.2f..%.2f)' % r


def snap(tier):
    """Slide floor items that poke through a wall back inside (the real meshes are a little bigger than the plan);
    the lamp rides along with the bedside crate."""
    L = LAYOUT[tier]; x0, x1, y0, y1 = L['inner']; S = L['spots']
    for k in list(S):
        if k not in SPOT2PROP or k in WALL or k in ONTOP: continue
        r = rect(k, S[k]); dx = max(0.0, x0 - r[0]) - max(0.0, r[1] - x1); dy = max(0.0, y0 - r[2]) - max(0.0, r[3] - y1)
        if dx or dy:
            x, y, z, yaw = S[k]; S[k] = (round(x + dx, 3), round(y + dy, 3), z, yaw)
            if k == 'fix_crate': lx, ly, lz, lyaw = S['spot_lamp']; S['spot_lamp'] = (round(lx + dx, 3), round(ly + dy, 3), lz, lyaw)


for _t in LAYOUT: snap(_t)


if __name__ == '__main__':
    for t in LAYOUT:
        if LAYOUT[t]['spots']: check(t)
