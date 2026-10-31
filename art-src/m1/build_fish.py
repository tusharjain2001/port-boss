# Port Boss fish (art direction "Ponyo Coast"): 15 species modelled from code in Blender 5.1, no skeleton.
#   blender -b -P build_fish.py [-- sprat mullet ...]      (no ids = all species)
# Output: Assets/Game/Art/Fish/PB_Fish_<id>.fbx  +  art-src/m1/out/{fish.blend, fish-side.png, fish-34.png}
# Conventions the game relies on: one mesh "Fish", one material "PB_Toon", colour only from the face-corner byte
# colour "Col" (sRGB; alpha 0 = emissive, only the anglerfish lure), length nose->tail tip exactly 1 m, head at -Y
# (Unity +Z), back +Z, origin at the body's centre of mass. Body = lofted rings along Y (smooth), fins = thick strips (flat).
import bpy, bmesh, math, os, sys
from mathutils import Vector, Matrix

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, '..', '..'))
OUT = os.path.join(HERE, 'out')
FBX_DIR = os.path.join(REPO, 'Assets', 'Game', 'Art', 'Fish')
ARGV = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
os.makedirs(OUT, exist_ok=True); os.makedirs(FBX_DIR, exist_ok=True)

FIN_ROOT, FIN_EDGE = 0.026, 0.017       # fin thickness (m at 1 m length): >= 1.5 cm everywhere
INK, PAPER, IRON = '#3F2C27', '#FFFBF2', '#5E5A66'


def lerp(a, b, u): return a + (b - a) * u
def clamp(x, a=0.0, b=1.0): return max(a, min(b, x))
def frac(x): return x - math.floor(x)
def hash2(i, j, seed): return frac(math.sin(i * 127.1 + j * 311.7 + seed * 74.7) * 43758.5453)


def vnoise(x, y, seed=0):
    i, j = math.floor(x), math.floor(y); fx, fy = x - i, y - j
    fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    a, b, c, d = hash2(i, j, seed), hash2(i + 1, j, seed), hash2(i, j + 1, seed), hash2(i + 1, j + 1, seed)
    return lerp(lerp(a, b, fx), lerp(c, d, fx), fy)


def curve(keys, t):
    """Monotone cubic (Fritsch-Carlson) through [(t, v)], flat outside: smooth profiles without overshoot."""
    if t <= keys[0][0]: return keys[0][1]
    if t >= keys[-1][0]: return keys[-1][1]
    xs, ys = [k[0] for k in keys], [k[1] for k in keys]
    n = len(keys); d = [(ys[i + 1] - ys[i]) / (xs[i + 1] - xs[i]) for i in range(n - 1)]
    m = [d[0]] + [0 if d[i - 1] * d[i] <= 0 else (d[i - 1] + d[i]) / 2 for i in range(1, n - 1)] + [d[-1]]
    for i in range(n - 1):
        if d[i] == 0: m[i] = m[i + 1] = 0
        else:
            a, b = m[i] / d[i], m[i + 1] / d[i]
            if a * a + b * b > 9: s = 3 / math.sqrt(a * a + b * b); m[i] = s * a * d[i]; m[i + 1] = s * b * d[i]
    i = max(k for k in range(n - 1) if xs[k] <= t); h = xs[i + 1] - xs[i]; u = (t - xs[i]) / h
    h00, h10, h01, h11 = 2 * u ** 3 - 3 * u ** 2 + 1, u ** 3 - 2 * u ** 2 + u, -2 * u ** 3 + 3 * u ** 2, u ** 3 - u ** 2
    return h00 * ys[i] + h10 * h * m[i] + h01 * ys[i + 1] + h11 * h * m[i + 1]


# ---------------------------------------------------------------- mesh accumulator ------------------------------------------
class Mesh:
    def __init__(self, thick=1.0):
        self.v, self.f, self.parts = [], [], []     # f: (idx, rgba, smooth); parts: (v0, v1, name)
        self.thick = thick; self.body_faces = (0, 0); self.patterns = []

    def begin(self, name): self._p = (len(self.v), name)
    def end(self): self.parts.append((self._p[0], len(self.v), self._p[1]))
    def vert(self, p): self.v.append(Vector(p)); return len(self.v) - 1
    def face(self, idx, col, smooth=True, alpha=1.0):
        c = col if isinstance(col, tuple) else tuple(int(col[i:i + 2], 16) / 255 for i in (1, 3, 5))
        self.f.append((list(idx), c + (alpha,), smooth))

    def mirror(self, v0, f0, name):
        """Copy verts v0.. and faces f0.. with x negated (the other side)."""
        v1, f1 = len(self.v), len(self.f); off = v1 - v0
        for i in range(v0, v1): p = self.v[i]; self.v.append(Vector((-p.x, p.y, p.z)))
        for k in range(f0, f1): idx, c, s = self.f[k]; self.f.append(([i + off for i in idx], c, s))
        self.parts.append((v1, len(self.v), name + '_m'))


def frame(n):
    n = Vector(n).normalized(); a = Vector((0, 0, 1)) if abs(n.z) < 0.9 else Vector((1, 0, 0))
    u = n.cross(a).normalized(); return n, u, n.cross(u)


def dome(M, c, n, r, h, segs, col, rows=2, alpha=1.0, name='dome'):
    """A closed spheroid cap: flat base at c, apex at c + n*h."""
    M.begin(name); n, u, w = frame(n); c = Vector(c)
    rings = []
    for k in range(rows):
        a = k / rows * math.pi / 2; rr, hh = r * math.cos(a), h * math.sin(a)
        rings.append([M.vert(c + n * hh + (u * math.cos(2 * math.pi * j / segs) + w * math.sin(2 * math.pi * j / segs)) * rr) for j in range(segs)])
    top, base = M.vert(c + n * h), M.vert(c)
    for k in range(rows - 1):
        for j in range(segs): M.face([rings[k][j], rings[k][(j + 1) % segs], rings[k + 1][(j + 1) % segs], rings[k + 1][j]], col, True, alpha)
    for j in range(segs):
        M.face([rings[-1][j], rings[-1][(j + 1) % segs], top], col, True, alpha)
        M.face([rings[0][(j + 1) % segs], rings[0][j], base], col, True, alpha)
    M.end()


def ball(M, c, r, segs, rows, col, alpha=1.0, squash=(1, 1, 1)):
    M.begin('ball'); c = Vector(c); rings = []
    for k in range(1, rows):
        a = math.pi * k / rows - math.pi / 2
        rings.append([M.vert(c + Vector((math.cos(a) * math.cos(2 * math.pi * j / segs) * squash[0], math.cos(a) * math.sin(2 * math.pi * j / segs) * squash[1], math.sin(a) * squash[2])) * r) for j in range(segs)])
    bot, top = M.vert(c - Vector((0, 0, r * squash[2]))), M.vert(c + Vector((0, 0, r * squash[2])))
    for k in range(len(rings) - 1):
        for j in range(segs): M.face([rings[k][j], rings[k][(j + 1) % segs], rings[k + 1][(j + 1) % segs], rings[k + 1][j]], col, True, alpha)
    for j in range(segs):
        M.face([rings[0][(j + 1) % segs], rings[0][j], bot], col, True, alpha)
        M.face([rings[-1][j], rings[-1][(j + 1) % segs], top], col, True, alpha)
    M.end()


def tube(M, pts, radii, sides, col, up=(0, 0, 1), flat=1.0, smooth=True, name='tube', alpha=1.0):
    """Closed swept tube; flat squashes the section along the 'up' side vector."""
    M.begin(name); pts = [Vector(p) for p in pts]; up = Vector(up); rings = []
    for i, p in enumerate(pts):
        T = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
        Nn = up - T * up.dot(T)
        if Nn.length < 1e-6: Nn = T.orthogonal()
        Nn.normalize(); B = T.cross(Nn)
        rings.append([M.vert(p + (B * math.cos(2 * math.pi * j / sides) + Nn * math.sin(2 * math.pi * j / sides) * flat) * radii[i]) for j in range(sides)])
    for i in range(len(rings) - 1):
        for j in range(sides): M.face([rings[i][j], rings[i][(j + 1) % sides], rings[i + 1][(j + 1) % sides], rings[i + 1][j]], col, smooth, alpha)
    a, b = M.vert(pts[0]), M.vert(pts[-1])
    for j in range(sides):
        M.face([rings[0][(j + 1) % sides], rings[0][j], a], col, smooth, alpha)
        M.face([rings[-1][j], rings[-1][(j + 1) % sides], b], col, smooth, alpha)
    M.end()


def cone(M, base, tip, r, sides, col):
    M.begin('cone'); base, tip = Vector(base), Vector(tip); n, u, w = frame(tip - base)
    ring = [M.vert(base + (u * math.cos(2 * math.pi * j / sides) + w * math.sin(2 * math.pi * j / sides)) * r) for j in range(sides)]
    a = M.vert(tip)
    for j in range(sides): M.face([ring[j], ring[(j + 1) % sides], a], col, False)
    M.face(list(reversed(ring)), col, False); M.end()


def fin(M, R, O, cols, N=None, bands=2, name='fin', shape=None):
    """A chunky fin: root polyline R (sunk into the body) to outer polyline O, thickness tapering root->edge.
    cols = (root colour, edge colour). Closed prism, flat shaded."""
    M.begin(name); K = len(R); R = [Vector(p) for p in R]; O = [Vector(p) for p in O]
    ss = [b / bands for b in range(bands + 1)] if shape is None else shape
    tr, te = FIN_ROOT * M.thick, FIN_EDGE * M.thick
    ns = []
    for i in range(K):
        if N is not None: n = Vector(N)
        else:
            tg = R[min(i + 1, K - 1)] - R[max(i - 1, 0)]
            n = tg.cross(((O[i] - R[i]) + (O[min(i + 1, K - 1)] - R[min(i + 1, K - 1)])))
            n = n.normalized() if n.length > 1e-9 else Vector((1, 0, 0))
            if ns and n.dot(ns[-1]) < 0: n = -n
        ns.append(n.normalized())
    F, B = [], []
    for r, s in enumerate(ss):
        th = lerp(tr, te, s) / 2
        F.append([M.vert(lerp(R[i], O[i], s) + ns[i] * th) for i in range(K)])
        B.append([M.vert(lerp(R[i], O[i], s) - ns[i] * th) for i in range(K)])
    last = len(ss) - 1
    for r in range(last):
        c = cols[min(r * len(cols) // last, len(cols) - 1)]
        for i in range(K - 1):
            M.face([F[r][i], F[r][i + 1], F[r + 1][i + 1], F[r + 1][i]], c, False)
            M.face([B[r][i], B[r + 1][i], B[r + 1][i + 1], B[r][i + 1]], c, False)
        M.face([F[r][0], F[r + 1][0], B[r + 1][0], B[r][0]], c, False)
        M.face([F[r][K - 1], B[r][K - 1], B[r + 1][K - 1], F[r + 1][K - 1]], c, False)
    for i in range(K - 1):
        M.face([F[last][i], B[last][i], B[last][i + 1], F[last][i + 1]], cols[-1], False)
        M.face([F[0][i], F[0][i + 1], B[0][i + 1], B[0][i]], cols[0], False)
    M.end()


# ---------------------------------------------------------------- the body ------------------------------------------------
class Body:
    """Lofted body along +Y from y0 (nose) to y1. Profiles are keys over t in [0, 1]: top/bot half-heights, half-width,
    centre-line z. Cross-section = superellipse (exponent p). nose/tail = rounding lengths (in t) of the end caps."""
    def __init__(self, y0, y1, top, bot, wid, zc=((0, 0), (1, 0)), p=2.0, nose=0.08, tail=0.05, rings=22, seg=16,
                 lump=None, nose_pow=0.5):
        self.__dict__.update(y0=y0, y1=y1, top=top, bot=bot, wid=wid, zck=zc, p=p, nose=nose, tailr=tail, rings=rings, seg=seg,
                             lump=lump, nose_pow=nose_pow)

    def y(self, t): return lerp(self.y0, self.y1, t)
    def cap(self, t):
        a = clamp(t / self.nose); b = clamp((1 - t) / self.tailr)
        return (1 - (1 - a) ** 2) ** self.nose_pow * math.sqrt(1 - (1 - b) ** 2)
    def prof(self, t):
        c = self.cap(t)
        return curve(self.top, t) * c, curve(self.bot, t) * c, curve(self.wid, t) * c, curve(self.zck, t)

    def point(self, t, th):
        hT, hB, w, zc = self.prof(t); e = 2 / self.p
        cs, sn = math.cos(th), math.sin(th)
        x = w * math.copysign(abs(cs) ** e, cs); z = (hT if sn > 0 else hB) * math.copysign(abs(sn) ** e, sn)
        if self.lump: k = self.lump(t, th); x *= k; z *= k
        return Vector((x, self.y(t), zc + z))

    def normal(self, t, th):
        d = 1e-3
        n = (self.point(t + d, th) - self.point(t - d, th)).cross(self.point(t, th + d) - self.point(t, th - d))
        n = n.normalized()
        return n if n.dot(self.point(t, th) - Vector((0, self.y(t), self.prof(t)[3]))) >= 0 else -n

    def theta_at(self, t, z, side=1):
        """Angle on the +X (side=1) or -X side at which the section of ring t reaches height z."""
        hT, hB, w, zc = self.prof(t); h = hT if z > zc else hB
        q = clamp(abs(z - zc) / max(h, 1e-9)) ** (self.p / 2); a = math.copysign(math.asin(q), z - zc)
        return a if side > 0 else math.pi - a

    def sil(self, t, side):
        hT, hB, w, zc = self.prof(t); return Vector((0, self.y(t), zc + (hT if side > 0 else -hB)))

    def ts(self):
        n = self.rings
        return [lerp(u, (1 - math.cos(math.pi * u)) / 2, 0.45) for u in ((k + 0.5) / n for k in range(n))]

    def build(self, M, colfn):
        M.begin('body'); f0 = len(M.f); S = self.seg
        ths = [-math.pi / 2 + 2 * math.pi * j / S for j in range(S)]
        tt = self.ts(); rings = [[M.vert(self.point(t, th)) for th in ths] for t in tt]
        a = M.vert((0, self.y0, curve(self.zck, 0))); b = M.vert((0, self.y1, curve(self.zck, 1)))
        def col(t, th):
            c = self.point(t, th); return colfn(t, math.sin(th), c)
        for k in range(len(tt) - 1):
            tm = (tt[k] + tt[k + 1]) / 2
            for j in range(S):
                th = ths[j] + math.pi / S
                M.face([rings[k][j], rings[k][(j + 1) % S], rings[k + 1][(j + 1) % S], rings[k + 1][j]], col(tm, th), True)
        for j in range(S):
            th = ths[j] + math.pi / S
            M.face([rings[0][(j + 1) % S], rings[0][j], a], col(tt[0] / 2, th), True)
            M.face([rings[-1][j], rings[-1][(j + 1) % S], b], col((tt[-1] + 1) / 2, th), True)
        M.end(); M.body_faces = (f0, len(M.f)); M.body = self

    def zs(self, t, s):
        hT, hB, w, zc = self.prof(t); return zc + s * (hT if s > 0 else hB)

    def pattern(self, M, f, col):
        """Recolour the body where f(P) > 0 (P on the +X side, model units); the mesh is cut along f = 0 so the
        boundary is a clean line, not a staircase of faces. col = hex or fn(base hex) -> hex."""
        M.patterns.append((lambda P: f(Vector((abs(P.x), P.y, P.z))), col))

    def spots(self, M, spots, col):
        C = [(self.surf(t, self.zs(t, s), 1), r) for t, s, r in spots]
        self.pattern(M, lambda P: max(r - (P - c).length for c, r in C), col)

    # ---- attachments ----
    def sil_fin(self, M, samples, h, lean, cols, sink=0.018, bands=2, name='mfin', shape=None):
        """Median fin along the silhouette. samples: [(t, side)] clockwise (top: t rising, bottom: t falling);
        h(i, u) = height, lean(u) = backward tilt (rad)."""
        P = [self.sil(t, s) for t, s in samples]; K = len(P); R, O = [], []
        for i in range(K):
            tg = P[min(i + 1, K - 1)] - P[max(i - 1, 0)]; n = Vector((0, -tg.z, tg.y)).normalized()
            u = i / (K - 1); a = lean(u) * (-1 if n.z > 0 else 1)
            d = Vector((0, n.y * math.cos(a) - n.z * math.sin(a), n.y * math.sin(a) + n.z * math.cos(a)))
            r = P[i] - n * sink; R.append(r); O.append(r + d * (h(i, u) + sink))
        fin(M, R, O, cols, N=(1, 0, 0), bands=bands, name=name, shape=shape)

    def top_fin(self, M, ta, tb, K, h, lean, cols, **kw):
        self.sil_fin(M, [(lerp(ta, tb, i / (K - 1)), 1) for i in range(K)], h, lean, cols, **kw)

    def bot_fin(self, M, ta, tb, K, h, lean, cols, **kw):
        self.sil_fin(M, [(lerp(tb, ta, i / (K - 1)), -1) for i in range(K)], lambda i, u: h(K - 1 - i, 1 - u), lambda u: lean(1 - u), cols, **kw)

    def tail(self, M, kind, L, span, cols, K=9, fork=0.45, t_in=0.03, root=0.85, lo=None, lift=0.0, scallop=0.0, bands=2):
        """Caudal fin from a vertical root inside the peduncle to the trailing outline (top lobe tip -> bottom lobe tip)."""
        t = 1 - t_in; hT, hB, w, zc = self.prof(t); y = self.y(t); top, bot = zc + hT * root, zc - hB * root
        lo = span if lo is None else lo; R, O = [], []
        for i in range(K):
            v = 1 - 2 * i / (K - 1); sp = span if v > 0 else lo
            if kind == 'fork': yo, zo = L * (fork + (1 - fork) * abs(v) ** 1.25), v * sp
            elif kind == 'crescent': yo, zo = L * (fork + (1 - fork) * abs(v) ** 0.75), math.copysign(abs(v) ** 0.8, v) * sp
            elif kind == 'round': yo, zo = L * (1 - 0.32 * v * v), v * sp
            elif kind == 'trunc': yo, zo = L * (0.86 + 0.14 * abs(v) ** 2), v * sp
            elif kind == 'shark':
                Lv = L if v > 0 else L * fork
                yo, zo = Lv * (0.25 + 0.75 * abs(v) ** 1.6), v * sp
            if scallop and 0 < i < K - 1: yo *= 1 - scallop * (i % 2)
            R.append(Vector((0, y, lerp(top, bot, i / (K - 1))))); O.append(Vector((0, y + yo, zc + zo + lift * abs(v))))
        fin(M, R, O, cols, N=(1, 0, 0), bands=bands, name='tail')

    def eye(self, M, t, s, r, fwd=0.25, up=0.1, look=0.2, iris=None, pupil=0.62):
        v0, f0 = len(M.v), len(M.f)
        th = self.theta_at(t, self.prof(t)[3] + s * (self.prof(t)[0] if s > 0 else self.prof(t)[1]))
        P = self.point(t, th); n = (self.normal(t, th) + Vector((0, -fwd, up))).normalized()
        c = P - n * r * 0.3
        dome(M, c, n, r, r * 0.62, 9, PAPER, name='sclera')
        n2 = (n + Vector((0, -look, 0.05))).normalized(); c2 = c + n * r * 0.42 + (n2 - n) * r * 0.5
        if iris:
            dome(M, c2, n2, r * pupil, r * 0.34, 9, iris, name='iris'); c2 = c2 + n2 * r * 0.1; pr = r * pupil * 0.62
        else: pr = r * pupil
        dome(M, c2, n2, pr, r * 0.3, 7, INK, name='pupil')
        _, uu, ww = frame(n2); upv = Vector((0, 0, 1)) - n2 * n2.z; upv.normalize()
        fwv = Vector((0, -1, 0)) - n2 * (-n2.y); fwv.normalize()
        c3 = c2 + n2 * r * 0.25 + upv * pr * 0.42 + fwv * pr * 0.3
        dome(M, c3, n2, r * 0.17, r * 0.12, 5, PAPER, rows=1, name='glint')
        M.mirror(v0, f0, 'eye')

    def surf(self, t, z, side=1, out=0.0):
        th = self.theta_at(t, z, side); return self.point(t, th) + self.normal(t, th) * out

    def mouth_curve(self, t_c, z_m, droop=0.0, n=9, out=0.004, powr=0.8):
        """A curve on the snout at height z_m wrapping from one corner (t_c) round the front to the other."""
        zc = self.prof(0)[3]; lo, hi = 0.0, 0.5
        for _ in range(30):     # first t where the section reaches z_m
            m = (lo + hi) / 2; hT, hB, w, zc = self.prof(m)
            if (hT if z_m > zc else hB) * 0.995 < abs(z_m - zc): lo = m
            else: hi = m
        t0 = hi; pts = []
        for k in range(n):
            ph = lerp(-1, 1, k / (n - 1)); t = lerp(t0, t_c, abs(ph) ** powr); z = z_m - droop * ph * ph
            pts.append(self.surf(t, z, 1 if ph >= 0 else -1, out))
        return pts

    def mouth(self, M, t_c, z_m, droop=0.0, r=0.009, col=INK, n=7, out=0.003, sides=3):
        tube(M, self.mouth_curve(t_c, z_m, droop, n, out), [r] * n, sides, col, name='mouth')

    def paired(self, M, t, s, rootdir, rootlen, udir, L, A, K, cols, skew=0.3, pw=0.8, sink=0.012, bands=2, side_out=1.0, name='pfin'):
        """Paired fin (pectoral/pelvic) on the +X side, mirrored. Root along rootdir (leading end first)."""
        v0, f0 = len(M.v), len(M.f)
        zc = self.prof(t)[3]; z = zc + s * (self.prof(t)[0] if s > 0 else self.prof(t)[1])
        anchor = self.surf(t, z, 1, -sink); rootdir, udir = Vector(rootdir).normalized(), Vector(udir).normalized()
        R, O = [], []
        for i in range(K):
            ph = math.pi * i / (K - 1); c = math.cos(ph)
            R.append(anchor + rootdir * (rootlen / 2 * c))
            b = L * (0.22 + 0.78 * math.sin(ph) ** pw) * (1 + skew * c)
            O.append(anchor + rootdir * (A * c) + udir * b)
        fin(M, R, O, cols, bands=bands, name=name)
        M.mirror(v0, f0, name)


# ---------------------------------------------------------------- colour helpers ------------------------------------------
def bands(s, table):
    for thr, c in table:
        if s >= thr: return c
    return table[-1][1]


# ---------------------------------------------------------------- species ---------------------------------------------------
def sprat(M):
    b = Body(0, 0.8, top=[(0, .05), (.15, .075), (.4, .09), (.75, .05), (1, .03)], bot=[(0, .05), (.15, .07), (.45, .085), (.75, .045), (1, .028)],
             wid=[(0, .04), (.3, .052), (.7, .033), (1, .02)], zc=[(0, -.005), (1, 0)], nose=0.11, rings=20, seg=14)
    b.build(M, lambda t, s, P: bands(s, [(.55, '#3E8E9C'), (.38, '#5FB3B3'), (.12, '#A9C4D6'), (-.65, '#DDE3EA'), (-2, PAPER)]))
    fc = ('#B9CAD8', '#9FB0C8')
    b.tail(M, 'fork', 0.24, 0.13, ('#A9C4D6', '#7F9BB8'), fork=0.38)
    b.top_fin(M, .44, .58, 5, lambda i, u: 0.085 * (1 - 0.75 * u), lambda u: 0.5, fc)
    b.bot_fin(M, .66, .8, 4, lambda i, u: 0.045 * (1 - 0.6 * u), lambda u: 0.6, fc, bands=1)
    b.paired(M, .24, -.35, (0, 0, 1), .03, (.35, 1, -.25), .1, .03, 5, fc, bands=1)
    b.eye(M, .12, .2, .055)
    b.mouth(M, .1, -0.014, -0.012, r=0.007)


def mullet(M):
    b = Body(0, 0.79, top=[(0, .05), (.12, .07), (.4, .1), (.75, .06), (1, .035)], bot=[(0, .045), (.15, .07), (.4, .09), (.75, .055), (1, .033)],
             wid=[(0, .065), (.15, .085), (.4, .088), (.75, .045), (1, .022)], zc=[(0, -.01), (1, 0)], p=2.3, nose=0.1, rings=20, seg=14)
    def col(t, s, P):
        c = bands(s, [(.55, '#6F7558'), (.4, '#8C9478'), (-.6, '#C9D3E2'), (-2, '#EEF1F5')])
        if c == '#C9D3E2' and t > .12 and (frac(s * 3.2 + .1) < .3): c = '#A9B4C6'
        return c
    b.build(M, col)
    fc = ('#8C9478', '#6F7558')
    b.tail(M, 'fork', 0.24, 0.14, fc, fork=0.5)
    b.top_fin(M, .36, .46, 5, lambda i, u: (0.085 if i % 2 == 0 else 0.055) * (1 - 0.5 * u), lambda u: 0.6, fc)
    b.top_fin(M, .64, .75, 4, lambda i, u: 0.065 * (1 - 0.6 * u), lambda u: 0.55, fc, bands=1)
    b.bot_fin(M, .63, .76, 4, lambda i, u: 0.055 * (1 - 0.6 * u), lambda u: 0.55, ('#B9C3D2', '#8C9478'), bands=1)
    b.paired(M, .25, .05, (0, 0, 1), .03, (.4, 1, .05), .11, .03, 5, fc, bands=1)
    b.paired(M, .42, -.85, (0, -1, 0), .03, (.3, .8, -1), .06, .02, 4, ('#C9D3E2', '#A9B4C6'), bands=1, name='pelvic')
    b.eye(M, .11, .2, .046)
    b.mouth(M, .07, -0.022, -0.004, r=0.008)


def mackerel(M):
    b = Body(0, 0.8, top=[(0, .04), (.12, .062), (.4, .085), (.78, .04), (1, .022)], bot=[(0, .04), (.15, .062), (.4, .08), (.78, .036), (1, .02)],
             wid=[(0, .04), (.3, .068), (.75, .035), (1, .018)], zc=[(0, -.005), (1, 0)], nose=0.13, nose_pow=0.6, rings=18, seg=12)
    b.build(M, lambda t, s, P: bands(s, [(.3, '#2C8FB0'), (.05, '#46B8C8'), (-.45, '#C9D3E2'), (-2, PAPER)]))
    def stripes(P):
        t = clamp(P.y / 0.8); zl = b.zs(t, 0.32)
        w = math.sin(2 * math.pi * (P.y * 8.5 + 0.2 * math.sin(P.z * 60 + P.y * 9))) - 0.15
        return min(w, (P.z - zl) * 60, (P.y - 0.13) * 60, (0.74 - P.y) * 60)
    b.pattern(M, stripes, '#1E3A5C')
    fc = ('#5E8FB0', '#2C6C90')
    b.tail(M, 'fork', 0.25, 0.15, ('#4A84A8', '#1E3A5C'), fork=0.3, K=7)
    b.top_fin(M, .3, .44, 5, lambda i, u: (0.085 if i % 2 == 0 else 0.06) * (1 - 0.55 * u), lambda u: 0.55, fc, bands=1)
    b.top_fin(M, .58, .66, 3, lambda i, u: 0.05 * (1 - 0.5 * u), lambda u: 0.6, fc, bands=1)
    b.bot_fin(M, .6, .67, 3, lambda i, u: 0.045 * (1 - 0.5 * u), lambda u: 0.6, ('#C9D3E2', '#9FB0C8'), bands=1)
    for k in range(3):     # finlets
        t = .74 + k * .07
        for side in (1, -1):
            r = b.sil(t, side) - Vector((0, 0, side * 0.012)); r2 = b.sil(t + .035, side) - Vector((0, 0, side * 0.008))
            fin(M, [r, r2], [r + Vector((0, 0.03, side * 0.035)), r2 + Vector((0, 0.008, side * 0.012))], ('#F7C548',), N=(1, 0, 0), bands=1, name='finlet')
    b.paired(M, .25, .02, (0, 0, 1), .025, (.4, 1, .1), .1, .025, 5, fc, bands=1)
    b.eye(M, .12, .2, .046)
    b.mouth(M, .09, -0.012, -0.004, r=0.007)


def garfish(M):
    b = Body(0.17, 0.92, top=[(0, .026), (.1, .036), (.5, .042), (.85, .034), (1, .02)], bot=[(0, .024), (.1, .034), (.5, .04), (.85, .032), (1, .019)],
             wid=[(0, .026), (.5, .034), (1, .015)], zc=[(0, 0), (1, 0)], nose=0.05, rings=24, seg=12)
    b.build(M, lambda t, s, P: bands(s, [(.45, '#2C8C80'), (.2, '#46D6C8'), (-.5, '#C9D3E2'), (-2, '#EEF1F5')]))
    # beak: lower jaw a bit longer than the upper, bony green
    tube(M, [(0, 0.19, -0.006), (0, 0.1, -0.008), (0, 0.0, -0.009)], [0.019, 0.01, 0.0035], 6, '#3F7F70', name='lowjaw')
    tube(M, [(0, 0.19, 0.009), (0, 0.1, 0.007), (0, 0.035, 0.005)], [0.017, 0.009, 0.0035], 6, '#2C8C80', name='upjaw')
    fc = ('#46B8A8', '#2C6C70')
    b.tail(M, 'fork', 0.1, 0.07, fc, fork=0.45, K=7)
    b.top_fin(M, .78, .9, 4, lambda i, u: 0.045 * (1 - 0.4 * u), lambda u: 0.6, fc, bands=1)
    b.bot_fin(M, .74, .88, 4, lambda i, u: 0.042 * (1 - 0.4 * u), lambda u: 0.6, ('#C9D3E2', '#9FB0C8'), bands=1)
    b.paired(M, .09, -.2, (0, 0, 1), .015, (.3, 1, -.2), .05, .015, 4, fc, bands=1)
    b.eye(M, .04, .15, .032, fwd=0.1)


def bream(M):
    b = Body(0, 0.78, top=[(0, .05), (.15, .14), (.42, .2), (.75, .1), (1, .045)], bot=[(0, .05), (.2, .13), (.45, .17), (.75, .08), (1, .04)],
             wid=[(0, .04), (.35, .075), (.75, .045), (1, .022)], zc=[(0, -.02), (.4, 0), (1, 0)], nose=0.12, rings=20, seg=14)
    def col(t, s, P):
        c = bands(s, [(.5, '#5E5A66'), (.1, '#7F8BA3'), (-.5, '#9FB0C8'), (-2, '#C9D3E2')])
        if .2 < t < .85 and s > -.4 and frac(t * 6.5) < .28: c = {'#5E5A66': '#4A4752', '#7F8BA3': '#6A7590', '#9FB0C8': '#8795B0'}.get(c, c)
        return c
    b.build(M, col)
    fc = ('#6A6876', '#3F3A48')
    b.tail(M, 'fork', 0.24, 0.17, fc, fork=0.5)
    b.top_fin(M, .3, .72, 13, lambda i, u: (0.11 if i % 2 == 0 else 0.07) * (1 - 0.3 * u) if u < .75 else 0.08, lambda u: 0.45, fc)
    b.bot_fin(M, .62, .8, 5, lambda i, u: (0.07 if i % 2 == 0 else 0.05) * (1 - 0.2 * u), lambda u: 0.5, fc, bands=1)
    b.paired(M, .3, -.1, (0, 0, 1), .035, (.35, 1, -.1), .14, .03, 5, fc, bands=1)
    b.paired(M, .36, -.9, (0, -1, 0), .03, (.3, .8, -1), .07, .02, 4, fc, bands=1, name='pelvic')
    b.eye(M, .14, .3, .05)
    b.mouth(M, .08, -0.03, -0.006, r=0.008)


def conger(M):
    b = Body(0, 0.97, top=[(0, .05), (.07, .07), (.18, .068), (.6, .048), (.85, .034), (1, .018)], bot=[(0, .045), (.07, .062), (.2, .062), (.6, .042), (.85, .03), (1, .016)],
             wid=[(0, .05), (.08, .066), (.25, .058), (.6, .036), (.85, .02), (1, .01)], zc=[(0, 0), (1, 0)], nose=0.06, tail=0.03, rings=30, seg=10)
    b.build(M, lambda t, s, P: bands(s, [(.3, '#6E625A'), (-.25, '#9C9088'), (-2, '#EDE4D6')]))
    # continuous fin fringe: dorsal from behind the head, round the tail, anal back to mid-body
    samples = [(lerp(.26, .985, i / 10), 1) for i in range(11)] + [(1.0, 1)] + [(lerp(.985, .48, i / 7), -1) for i in range(1, 8)]
    b.sil_fin(M, samples, lambda i, u: 0.03 + 0.02 * math.sin(math.pi * u), lambda u: 0.0, ('#C4B4A2', '#7A665A'), sink=0.012, shape=[0, .7, 1])
    b.paired(M, .13, -.05, (0, 0, 1), .025, (.45, 1, 0), .06, .025, 4, ('#B4A89C', '#7A6E64'), bands=1)
    b.eye(M, .06, .35, .04, fwd=0.15)
    zm = -0.01
    tube(M, b.mouth_curve(.075, zm + 0.01, -0.003, 7, 0.007), [0.011] * 7, 4, '#D2BFAF', name='lip_up')
    tube(M, b.mouth_curve(.07, zm - 0.011, 0.004, 7, 0.007), [0.012] * 7, 4, '#D2BFAF', name='lip_lo')
    tube(M, b.mouth_curve(.072, zm, 0.0, 7, 0.011), [0.005] * 7, 3, INK, name='mouth')


def snapper(M):
    b = Body(0, 0.78, top=[(0, .04), (.12, .1), (.4, .165), (.75, .085), (1, .042)], bot=[(0, .045), (.2, .12), (.45, .14), (.75, .07), (1, .038)],
             wid=[(0, .04), (.35, .075), (.75, .04), (1, .02)], zc=[(0, -.03), (.35, 0), (1, 0)], nose=0.12, nose_pow=0.6, rings=20, seg=14)
    b.build(M, lambda t, s, P: bands(s, [(.45, '#D9483B'), (-.25, '#E2553F'), (-.7, '#F28A70'), (-2, '#F8B9A4')]))
    fc = ('#E2553F', '#F6A27A')
    b.tail(M, 'trunc', 0.24, 0.16, ('#D9483B', '#F28A70'), fork=0.5)
    b.top_fin(M, .28, .64, 11, lambda i, u: (0.11 if i % 2 == 0 else 0.065) * (1 - 0.25 * u), lambda u: 0.4, fc)
    b.top_fin(M, .64, .8, 5, lambda i, u: 0.075 * (1 - 0.3 * u), lambda u: 0.55, fc, bands=1)
    b.bot_fin(M, .62, .78, 5, lambda i, u: 0.07 * (1 - 0.2 * u), lambda u: 0.55, fc, bands=1)
    b.paired(M, .3, -.15, (0, 0, 1), .035, (.3, 1, -.15), .15, .03, 5, ('#F28A70', '#F6A27A'), bands=1)
    b.paired(M, .36, -.9, (0, -1, 0), .03, (.3, .8, -1), .08, .02, 4, fc, bands=1, name='pelvic')
    b.eye(M, .14, .3, .05)
    b.mouth(M, .08, -0.045, -0.006, r=0.008)


def mahi(M):
    b = Body(0, 0.8, top=[(0, .1), (.06, .15), (.15, .16), (.5, .1), (.85, .045), (1, .03)], bot=[(0, .06), (.2, .1), (.45, .1), (.8, .045), (1, .028)],
             wid=[(0, .05), (.25, .065), (.7, .035), (1, .018)], zc=[(0, -.04), (.15, 0), (1, 0)], nose=0.05, nose_pow=0.35, rings=18, seg=14)
    b.build(M, lambda t, s, P: bands(s, [(.5, '#2FA88E'), (.32, '#7DBE6A'), (-.6, '#F7C548'), (-2, '#FBE3A0')]))
    sp = [(lerp(.2, .78, (k + hash2(k, 1, 4) * .6) / 8), lerp(-.35, .12, hash2(k, 2, 4)), lerp(.026, .032, hash2(k, 3, 4))) for k in range(8)]
    b.spots(M, sp, lambda c: '#3A86E0' if c == '#F7C548' else None)
    b.tail(M, 'fork', 0.22, 0.16, ('#F7C548', '#E8A93A'), fork=0.25, t_in=0.04)
    b.top_fin(M, .05, .94, 10, lambda i, u: 0.075 * (1 - 0.55 * u) * (1 + 0.15 * (i % 2 == 0)), lambda u: 0.35, ('#2FA88E', '#2160B8'))
    b.bot_fin(M, .5, .94, 7, lambda i, u: 0.055 * (1 - 0.5 * u), lambda u: 0.4, ('#F7C548', '#E8A93A'), bands=1)
    b.paired(M, .27, -.25, (0, 0, 1), .03, (.35, 1, -.15), .12, .03, 5, ('#F7C548', '#E8A93A'), bands=1)
    b.eye(M, .09, .02, .046)
    b.mouth(M, .06, -0.035, -0.005, r=0.008)


def grouper_body(M, gold=False):
    rings, seg = (28, 20) if gold else (16, 12)
    b = Body(0, 0.79, top=[(0, .07), (.12, .13), (.38, .17), (.75, .1), (1, .055)], bot=[(0, .065), (.2, .15), (.45, .155), (.75, .085), (1, .05)],
             wid=[(0, .075), (.25, .125), (.5, .115), (.8, .06), (1, .03)], zc=[(0, -.02), (.3, 0), (1, 0)], p=2.15, nose=0.1, rings=rings, seg=seg)
    if gold:
        back, side, belly, blot, finc = '#D29A3E', '#E8B04A', '#F4D58A', '#A2583A', ('#E0A645', '#A2583A')
    else:
        back, side, belly, blot, finc = '#5F563A', '#7B6B45', '#C9BC8E', '#C8BC8E', ('#7B6B45', '#4F4630')
    b.build(M, lambda t, s, P: bands(s, [(.5, back), (-.55, side), (-2, belly)]))
    if gold:
        f = lambda P: min(vnoise(P.y * 10, P.z * 10 + 0.3, 7) - 0.62, (P.y - 0.09) * 30, (0.74 - P.y) * 30, (P.z + 0.07) * 30)
    else:
        f = lambda P: min(vnoise(P.y * 9, P.z * 9 + 0.5, 3) - 0.6, (P.y - 0.08) * 30, (0.75 - P.y) * 30, (P.z + 0.08) * 30)
    b.pattern(M, f, lambda c: blot if c != belly else None)
    k = 1.15 if gold else 1.0
    b.tail(M, 'round', 0.22 * k, 0.15 * k, finc, K=11 if gold else 7, t_in=0.035)
    b.top_fin(M, .3, .62, 13 if gold else 7, lambda i, u: (0.085 if i % 2 == 0 else 0.065) * k, lambda u: 0.35, finc)
    b.top_fin(M, .62, .84, 7 if gold else 5, lambda i, u: 0.09 * k * math.sin(math.pi * (0.3 + 0.7 * u)) ** 0.5, lambda u: 0.45, finc, bands=2 if gold else 1)
    b.bot_fin(M, .62, .82, 6 if gold else 5, lambda i, u: 0.075 * k * math.sin(math.pi * (0.25 + 0.75 * u)) ** 0.5, lambda u: 0.45, finc, bands=2 if gold else 1)
    b.paired(M, .32, -.2, (0, 0, 1), .05, (.4, 1, -.1), .15 * k, .055 * k, 7 if gold else 5, finc, skew=0.0, pw=0.6, bands=2 if gold else 1)
    b.paired(M, .36, -.9, (0, -1, 0), .04, (.3, .7, -1), .09 * k, .03, 4 if gold else 3, finc, bands=1, name='pelvic')
    b.eye(M, .15, .38, .046, iris=('#F7C548' if gold else None))
    lip = '#F6C27A' if gold else '#B39A6A'
    zm = -0.025; n = 9 if gold else 6
    tube(M, b.mouth_curve(.13, zm + 0.016, -0.006, n, 0.012), [0.017] * n, 6 if gold else 4, lip, name='lip_up')
    tube(M, b.mouth_curve(.12, zm - 0.018, 0.008, n, 0.014), [0.019] * n, 6 if gold else 4, lip, name='lip_lo')
    tube(M, b.mouth_curve(.125, zm, 0.0, n, 0.02), [0.006] * n, 3, INK, name='mouth')
    return b


def grouper(M): grouper_body(M)


def oldbrass(M):
    b = grouper_body(M, gold=True)
    # the old hook: its point through the lower lip at the corner, the bend under the lip, the shank and eye sticking out
    p = b.surf(.1, -0.05, 1, 0.004)
    pts = [p + Vector(d) for d in ((0.004, 0.0, 0.012), (0.012, 0.004, -0.012), (0.02, 0.0, -0.035), (0.026, -0.018, -0.05), (0.03, -0.036, -0.044), (0.034, -0.05, -0.065), (0.036, -0.058, -0.095))]
    tube(M, pts, [0.004, 0.006, 0.007, 0.007, 0.007, 0.007, 0.007], 5, IRON, up=(1, 0, 0), name='hook')
    cone(M, p + Vector((0.004, 0.0, 0.006)), p + Vector((0.0, 0.004, 0.024)), 0.006, 4, IRON)
    ball(M, p + Vector((0.037, -0.06, -0.108)), 0.014, 6, 4, '#4A4752')
    tube(M, [p + Vector((0.038, -0.061, -0.118)), p + Vector((0.04, -0.06, -0.15)), p + Vector((0.042, -0.05, -0.18))], [0.0035] * 3, 4, '#E8E0CC', name='line')


def sunfish(M):
    b = Body(0, 0.86, top=[(0, .1), (.1, .25), (.35, .34), (.65, .32), (.88, .25), (1, .2)], bot=[(0, .1), (.1, .24), (.35, .32), (.65, .3), (.88, .24), (1, .19)],
             wid=[(0, .06), (.3, .09), (.7, .075), (1, .04)], zc=[(0, -.01), (1, 0)], nose=0.22, tail=0.06, nose_pow=0.55, rings=20, seg=16)
    b.build(M, lambda t, s, P: bands(s, [(.55, '#8E9BB8'), (-.45, '#C9D3E2'), (-.7, '#E6EBF2'), (-2, PAPER)]))
    fc = ('#8E9BB8', '#6F7C9C')
    b.top_fin(M, .62, .82, 5, lambda i, u: 0.36 * (0.55 + 0.45 * math.sin(math.pi * u * 0.8)), lambda u: 0.45 + 0.25 * u, fc, sink=0.04)
    b.bot_fin(M, .62, .82, 5, lambda i, u: 0.33 * (0.55 + 0.45 * math.sin(math.pi * u * 0.8)), lambda u: 0.45 + 0.25 * u, fc, sink=0.04)
    # clavus: a frilly rounded rim round the back of the disc
    samples = [(lerp(.86, .995, i / 4), 1) for i in range(5)] + [(1.0, 1)] + [(lerp(.995, .86, i / 4), -1) for i in range(5)]
    b.sil_fin(M, samples, lambda i, u: 0.14 if i % 2 == 0 else 0.1, lambda u: 0.0, ('#A9B6CE', '#8E9BB8'), sink=0.03)
    b.paired(M, .3, .05, (0, 0, 1), .04, (.3, 1, .1), .08, .03, 4, fc, bands=1)
    b.eye(M, .12, .25, .05)
    b.mouth(M, .04, -0.01, -0.006, r=0.009)


def blueshark(M):
    b = Body(0, 0.74, top=[(0, .025), (.12, .05), (.4, .075), (.75, .045), (1, .022)], bot=[(0, .02), (.15, .05), (.4, .068), (.75, .038), (1, .02)],
             wid=[(0, .03), (.15, .06), (.4, .072), (.75, .035), (1, .016)], zc=[(0, .005), (.2, 0), (1, .012)], nose=0.14, nose_pow=0.55, rings=20, seg=14)
    b.build(M, lambda t, s, P: bands(s, [(.35, '#2160B8'), (-.05, '#3A86E0'), (-.3, '#A9C8EE'), (-2, PAPER)]))
    fc = ('#2160B8', '#1A4C94')
    b.tail(M, 'shark', 0.28, 0.17, fc, fork=0.42, lo=0.1, lift=0.05, K=7)
    b.top_fin(M, .38, .52, 5, lambda i, u: 0.12 * (1 - 0.85 * u) + 0.012, lambda u: 0.75, fc)
    b.top_fin(M, .8, .86, 3, lambda i, u: 0.035 * (1 - 0.6 * u), lambda u: 0.7, fc, bands=1)
    b.bot_fin(M, .79, .85, 3, lambda i, u: 0.03 * (1 - 0.6 * u), lambda u: 0.7, ('#A9C8EE', '#3A86E0'), bands=1)
    b.paired(M, .28, -.5, (0, -1, 0), .07, (1, .9, -.45), .3, .035, 6, fc, skew=0.35, pw=1.1, bands=1)
    b.paired(M, .62, -.85, (0, -1, 0), .04, (.4, .8, -.6), .06, .02, 3, fc, bands=1, name='pelvic')
    for k in range(5):     # gill slits
        t = .2 + k * .022; v0, f0 = len(M.v), len(M.f)
        tube(M, [b.surf(t, -0.035, 1, 0.002), b.surf(t + .004, 0.02, 1, 0.002)], [0.0045, 0.0045], 3, '#1A3A6E', name='gill')
        M.mirror(v0, f0, 'gill')
    b.eye(M, .1, .2, .036, fwd=0.2)
    b.mouth(M, .14, -0.032, -0.012, r=0.007)


def swordfish(M):
    b = Body(0.3, 0.87, top=[(0, .05), (.12, .085), (.35, .1), (.75, .05), (1, .025)], bot=[(0, .04), (.15, .08), (.4, .09), (.75, .042), (1, .022)],
             wid=[(0, .05), (.35, .08), (.75, .04), (1, .02)], zc=[(0, 0), (1, 0)], nose=0.1, rings=20, seg=14)
    b.build(M, lambda t, s, P: bands(s, [(.4, '#3B3570'), (.15, '#5B5A96'), (-.5, '#9FB0C8'), (-2, '#D5DCE8')]))
    tube(M, [(0, 0.33, 0.005), (0, 0.2, 0.005), (0, 0.08, 0.004), (0, 0.0, 0.003)], [0.03, 0.02, 0.012, 0.003], 6, IRON, up=(0, 0, 1), flat=0.42, name='sword')
    fc = ('#3B3570', '#2A2552')
    b.tail(M, 'crescent', 0.13, 0.2, fc, fork=0.15, t_in=0.02, root=0.9)
    b.top_fin(M, .12, .3, 6, lambda i, u: 0.2 * (1 - u) ** 0.8 + 0.015, lambda u: 0.55 + 0.4 * u, fc, sink=0.02)
    b.top_fin(M, .9, .94, 3, lambda i, u: 0.03 * (1 - 0.5 * u), lambda u: 0.6, fc, bands=1)
    b.bot_fin(M, .62, .7, 4, lambda i, u: 0.06 * (1 - 0.8 * u), lambda u: 0.7, fc, bands=1)
    b.paired(M, .2, -.5, (0, -1, 0), .03, (.6, 1, -.4), .14, .02, 5, fc, skew=0.2, pw=1.1, bands=1)
    b.eye(M, .08, .15, .044, fwd=0.15)
    b.mouth(M, .1, -0.02, -0.004, r=0.008)


def anglerfish(M):
    lump = lambda t, th: 1 + 0.07 * (vnoise(t * 9, th * 1.6, 11) - 0.5) * clamp(t * 6)
    b = Body(0, 0.86, top=[(0, .16), (.1, .24), (.35, .29), (.7, .15), (1, .05)], bot=[(0, .17), (.15, .26), (.35, .27), (.7, .12), (1, .045)],
             wid=[(0, .19), (.15, .25), (.4, .24), (.75, .1), (1, .03)], zc=[(0, -.02), (1, .02)], nose=0.11, nose_pow=0.45, rings=16, seg=14, lump=lump)
    zm = -0.05
    def col(t, s, P):
        if t < .22 and abs(P.z - zm) < 0.045: return '#1E1424'
        return bands(s, [(.2, '#4A3A5A'), (-.5, '#584768'), (-2, '#6E5C80')])
    b.build(M, col)
    lip = '#86709A'
    up = b.mouth_curve(.2, zm + 0.055, -0.02, 9, 0.008); lo = b.mouth_curve(.21, zm - 0.06, 0.025, 9, 0.02)
    tube(M, up, [0.02] * 9, 5, lip, name='lip_up'); tube(M, lo, [0.026] * 9, 5, lip, name='lip_lo')
    for i in range(1, 8):      # teeth, bigger at the front
        sz = 0.048 if i in (2, 4, 6) else 0.034
        p = up[i]; cone(M, p - Vector((0, 0, 0.012)), p - Vector((0, 0.01, 0.012 + sz)), 0.012, 4, PAPER)
        sz = 0.034 if i in (2, 4, 6) else 0.044
        p = lo[i]; cone(M, p + Vector((0, 0, 0.018)), p + Vector((0, 0.006, 0.018 + sz)), 0.012, 4, PAPER)
    # illicium: the rod curls forward over the head, the lure hangs above the snout (inside the nose->tail length)
    base = b.sil(.16, 1) - Vector((0, 0, 0.01))
    rod = [base + Vector(d) for d in ((0, 0, 0), (0, -0.02, 0.07), (0, -0.07, 0.13), (0, -0.13, 0.14), (0, -0.16, 0.1))]
    tube(M, rod, [0.013, 0.012, 0.01, 0.009, 0.008], 5, '#4A3A5A', up=(1, 0, 0), name='rod')
    ball(M, rod[-1] - Vector((0, 0.0, 0.03)), 0.036, 8, 5, '#FFE89A', alpha=0.0)
    fc = ('#4A3A5A', '#3A2C48')
    b.tail(M, 'round', 0.14, 0.09, fc, K=7)
    b.top_fin(M, .72, .86, 4, lambda i, u: 0.06, lambda u: 0.5, fc, bands=1)
    b.bot_fin(M, .72, .84, 4, lambda i, u: 0.05, lambda u: 0.5, fc, bands=1)
    b.paired(M, .5, -.3, (0, 0, 1), .04, (.5, 1, -.1), .08, .04, 5, fc, skew=0.0, bands=1)
    b.eye(M, .17, .42, .026, fwd=0.1, up=0.3, pupil=0.66)


def oarfish(M):
    b = Body(0, 0.99, top=[(0, .045), (.06, .058), (.3, .058), (.8, .04), (1, .012)], bot=[(0, .04), (.06, .052), (.3, .052), (.8, .034), (1, .01)],
             wid=[(0, .022), (.1, .022), (.6, .018), (1, .008)], zc=[(0, 0), (1, 0)], p=2.0, nose=0.03, tail=0.02, rings=32, seg=8)
    def col(t, s, P):      # faint blue dots: single faces (round iso-spots would be smaller than these faces)
        c = bands(s, [(.5, '#9FB0C8'), (-.5, '#C9D3E2'), (-2, '#E6EBF2')])
        if .06 < t < .95 and -.6 < s < .7 and hash2(round(t * 33), round(s * 3), 9) < .2: c = '#86A3DC'
        return c
    b.build(M, col)
    b.top_fin(M, .045, 1.0, 20, lambda i, u: 0.04 * (1 - 0.4 * u), lambda u: 0.25, ('#D9483B', '#F07A45'), sink=0.012, bands=1)
    # crest: the red plume on the head
    base = [b.sil(lerp(.012, .045, i / 6), 1) - Vector((0, 0, 0.008)) for i in range(7)]
    O = [base[i] + Vector((0, 0.04 + 0.035 * i, 0.09 + (0.07 if i % 2 == 0 else 0.0) * (1 - i / 9))) for i in range(7)]
    fin(M, base, O, ('#D9483B', '#E2553F'), N=(1, 0, 0), bands=1, name='crest')
    # the 'oars': long thin pelvic rays with red tips
    v0, f0 = len(M.v), len(M.f)
    p = b.surf(.07, -0.04, 1, -0.002)
    tube(M, [p, p + Vector((0.01, 0.04, -0.05)), p + Vector((0.02, 0.1, -0.08))], [0.005, 0.005, 0.005], 4, '#E2553F', name='oar')
    ball(M, p + Vector((0.02, 0.1, -0.08)), 0.011, 6, 4, '#D9483B')
    M.mirror(v0, f0, 'oar')
    b.paired(M, .05, 0.0, (0, 0, 1), .012, (.4, 1, 0), .03, .012, 4, ('#C9D3E2', '#9FB0C8'), bands=1)
    b.eye(M, .022, .15, .028, fwd=0.1)
    b.mouth(M, .022, -0.014, -0.006, r=0.005, n=7)


SPECIES = [('sprat', 'Harbour Sprat', sprat), ('mullet', 'Grey Mullet', mullet), ('mackerel', 'Mackerel', mackerel),
           ('garfish', 'Garfish', garfish), ('bream', 'Black Bream', bream), ('conger', 'Conger Eel', conger),
           ('snapper', 'Red Snapper', snapper), ('mahi', 'Mahi-mahi', mahi), ('grouper', 'Dusky Grouper', grouper),
           ('sunfish', 'Ocean Sunfish', sunfish), ('blueshark', 'Blue Shark', blueshark), ('swordfish', 'Swordfish', swordfish),
           ('anglerfish', 'Deep Anglerfish', anglerfish), ('oarfish', 'Giant Oarfish', oarfish), ('oldbrass', 'Old Brass', oldbrass)]


# ---------------------------------------------------------------- mesh object --------------------------------------------
def signed_volume(vs, faces):
    v = 0.0
    for f in faces:
        a = vs[f[0]]
        for k in range(1, len(f) - 1): v += a.dot(vs[f[k]].cross(vs[f[k + 1]])) / 6
    return v


def iso_cut(bm, faces, f):
    """Split the (triangle) faces along the zero line of f, so a colour region gets a straight-edged boundary."""
    val = {}
    for fc in faces:
        for v in fc.verts:
            if v not in val: x = f(v.co); val[v] = x if abs(x) > 1e-7 else 1e-7
    edges = {e for fc in faces for e in fc.edges if (val[e.verts[0]] > 0) != (val[e.verts[1]] > 0)}
    cut = set()
    for e in edges:
        a, b = e.verts; u = val[a] / (val[a] - val[b]); co = a.co.lerp(b.co, u)
        if u < 0.04 or u > 0.96: continue        # a sliver: keep the staircase there
        _, nv = bmesh.utils.edge_split(e, a, u); nv.co = co; cut.add(nv)
    for fc in {fc for v in cut for fc in v.link_faces}:
        vs = [v for v in fc.verts if v in cut]
        if len(vs) == 2 and not any(set(e.verts) == set(vs) for e in fc.edges): bmesh.utils.face_split(fc, vs[0], vs[1])


def make_fish(sid, fn):
    M = Mesh(); fn(M)
    ys = [p.y for p in M.v]; s = 1.0 / (max(ys) - min(ys))
    M = Mesh(thick=1.0 / s); fn(M)          # rebuild with fins thickened so they are >= 1.5 cm after scaling to 1 m
    ys = [p.y for p in M.v]; s = 1.0 / (max(ys) - min(ys))
    # centre of mass of the body (solid), x = 0
    f0, f1 = M.body_faces; vol, cen = 0.0, Vector()
    for idx, _, _ in M.f[f0:f1]:
        a = M.v[idx[0]]
        for k in range(1, len(idx) - 1):
            b, c = M.v[idx[k]], M.v[idx[k + 1]]; dv = a.dot(b.cross(c)) / 6; vol += dv; cen += (a + b + c) * dv / 4
    cen /= vol; cen.x = 0
    vs = [(p - cen) * s for p in M.v]
    me = bpy.data.meshes.new('Fish_' + sid)
    me.from_pydata([tuple(p) for p in M.v], [], [f[0] for f in M.f])
    bm = bmesh.new(); bm.from_mesh(me)
    cid = bm.faces.layers.int.new('cid'); isb = bm.faces.layers.int.new('body'); pal = []
    bm.faces.ensure_lookup_table()
    for i, (idx, c, sm) in enumerate(M.f):
        if c not in pal: pal.append(c)
        bm.faces[i][cid] = pal.index(c); bm.faces[i].smooth = sm; bm.faces[i][isb] = int(f0 <= i < f1)
    bmesh.ops.triangulate(bm, faces=bm.faces, quad_method='BEAUTY', ngon_method='BEAUTY')
    for f, rec in M.patterns:
        iso_cut(bm, [fc for fc in bm.faces if fc[isb]], f)
        bmesh.ops.triangulate(bm, faces=[fc for fc in bm.faces if len(fc.verts) > 3], quad_method='BEAUTY', ngon_method='BEAUTY')
        for fc in bm.faces:
            if fc[isb] and f(fc.calc_center_median()) > 0:
                base = pal[fc[cid]]; hexs = '#%02X%02X%02X' % tuple(round(x * 255) for x in base[:3])
                c = rec(hexs) if callable(rec) else rec
                if c is None: continue
                c = tuple(int(c[i:i + 2], 16) / 255 for i in (1, 3, 5)) + (base[3],)
                if c not in pal: pal.append(c)
                fc[cid] = pal.index(c)
    for v in bm.verts: v.co = (v.co - cen) * s
    bm.verts.index_update()
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    # every part must be a closed shell with outward normals
    part_of = {}
    for pi, (a, b, name) in enumerate(M.parts):
        for i in range(a, b): part_of[i] = pi
    groups = {}
    vs = [v.co.copy() for v in bm.verts]
    for f in bm.faces: groups.setdefault(part_of[min(v.index for v in f.verts)] if not f[isb] else -1, []).append([v.index for v in f.verts])
    gf = {}
    for f in bm.faces: gf.setdefault(part_of[min(v.index for v in f.verts)] if not f[isb] else -1, []).append(f)
    for pi, fs in groups.items():      # recalc's heuristic can pick the wrong side of a thin closed shell: the signed volume decides
        if signed_volume(vs, fs) < 0: bmesh.ops.reverse_faces(bm, faces=gf[pi]); groups[pi] = [list(reversed(f)) for f in fs]
    bad = [(M.parts[pi][2] if pi >= 0 else 'body') for pi, fs in groups.items() if signed_volume(vs, fs) <= 0]
    cost = {}
    for pi, fs in groups.items(): k = M.parts[pi][2] if pi >= 0 else 'body'; cost[k] = cost.get(k, 0) + len(fs)
    print('PARTS', sid, sorted(cost.items(), key=lambda kv: -kv[1]))
    nonman = [e for e in bm.edges if not e.is_manifold]
    if bad or nonman: print('WARN', sid, 'inverted parts:', bad, 'non-manifold edges:', len(nonman))
    bm.to_mesh(me); bm.free(); vs = None
    col = me.color_attributes.new('Col', 'BYTE_COLOR', 'CORNER')
    layer = me.attributes['cid']
    for poly in me.polygons:
        c = pal[layer.data[poly.index].value]
        for li in poly.loop_indices: col.data[li].color_srgb = c
    me.attributes.remove(me.attributes['cid']); me.attributes.remove(me.attributes['body'])
    ob = bpy.data.objects.new('Fish', me); bpy.context.scene.collection.objects.link(ob)
    ob.data.materials.append(material())
    tris = len(me.polygons)
    thin = min(FIN_EDGE * M.thick * s, FIN_ROOT * M.thick * s)
    bb = [Vector(v) for v in ob.bound_box]
    print('FISH %-10s tris %5d  length %.4f  fin edge %.3f m  scale %.3f  bbox z %.3f..%.3f' % (sid, tris, max(v.y for v in bb) - min(v.y for v in bb), thin, s,
          min(v.z for v in bb), max(v.z for v in bb)))
    return ob, tris


_MAT = None
def material():
    """PB_Toon: the game swaps in its toon shader; here a cel preview (2-step ramp x vertex colour, alpha 0 glows)."""
    global _MAT
    if _MAT: return _MAT
    m = bpy.data.materials.new('PB_Toon'); m.use_backface_culling = True
    m.use_nodes = True; nt = m.node_tree; nt.nodes.clear(); N = nt.nodes.new; L = nt.links.new
    out = N('ShaderNodeOutputMaterial'); attr = N('ShaderNodeVertexColor'); attr.layer_name = 'Col'
    dif = N('ShaderNodeBsdfDiffuse'); s2r = N('ShaderNodeShaderToRGB'); ramp = N('ShaderNodeValToRGB')
    ramp.color_ramp.interpolation = 'CONSTANT'
    ramp.color_ramp.elements[0].color = (0.62, 0.66, 0.92, 1); ramp.color_ramp.elements[1].position = 0.18; ramp.color_ramp.elements[1].color = (1, 1, 1, 1)
    mul = N('ShaderNodeMix'); mul.data_type = 'RGBA'; mul.blend_type = 'MULTIPLY'; mul.inputs['Factor'].default_value = 1
    glow = N('ShaderNodeMath'); glow.operation = 'SUBTRACT'; glow.inputs[0].default_value = 1
    g2 = N('ShaderNodeMath'); g2.operation = 'MULTIPLY_ADD'; g2.inputs[1].default_value = 2.5; g2.inputs[2].default_value = 1.0
    em = N('ShaderNodeEmission')
    L(dif.outputs[0], s2r.inputs[0]); L(s2r.outputs['Color'], ramp.inputs[0])
    L(attr.outputs['Color'], mul.inputs['A']); L(ramp.outputs['Color'], mul.inputs['B'])
    L(attr.outputs['Alpha'], glow.inputs[1]); L(glow.outputs[0], g2.inputs[0])
    L(mul.outputs['Result'], em.inputs['Color']); L(g2.outputs[0], em.inputs['Strength']); L(em.outputs[0], out.inputs[0])
    _MAT = m; return m


def export(ob, sid):
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    path = os.path.join(FBX_DIR, 'PB_Fish_%s.fbx' % sid)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y',
                             apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, apply_unit_scale=True,
                             mesh_smooth_type='FACE', use_mesh_modifiers=True, add_leaf_bones=False, colors_type='SRGB')
    return path


# ---------------------------------------------------------------- contact sheets ------------------------------------------
def sheet(fish, path, view, cellw=1.36):
    sc = bpy.context.scene
    d = Vector(view).normalized(); r = d.cross(Vector((0, 0, 1))).normalized(); u = r.cross(d)
    cols, rows = 5, 3; cellh = cellw * cols / rows * 1000 / 1600
    cam = sc.camera; cam.data.type = 'ORTHO'; cam.data.ortho_scale = cellw * cols
    cam.matrix_world = Matrix((r, u, -d)).transposed().to_4x4() @ Matrix.Identity(4); cam.location = -d * 20
    rot = Matrix((r, u, -d)).transposed().to_4x4()
    for i, (ob, label) in enumerate(fish):
        cx = (i % cols - (cols - 1) / 2) * cellw; cy = ((rows - 1) / 2 - i // cols) * cellh
        ob.matrix_world = Matrix.Identity(4)
        bb = [Vector(v) for v in ob.bound_box]; mid = sum(bb, Vector()) / 8
        ob.location = r * cx + u * (cy + 0.04) - (r * mid.dot(r) + u * mid.dot(u))
        ob.hide_render = False
        tx = bpy.data.objects.get('lbl_%d' % i)
        if not tx:
            cu = bpy.data.curves.new('lbl_%d' % i, 'FONT'); cu.body = label; cu.align_x = 'CENTER'; cu.size = 0.1
            tx = bpy.data.objects.new('lbl_%d' % i, cu); sc.collection.objects.link(tx); cu.materials.append(ink_mat())
        tx.matrix_world = rot; tx.location = r * cx + u * (cy - cellh / 2 + 0.1) - d * 3
    sc.render.filepath = path; bpy.ops.render.render(write_still=True)


_INK = None
def ink_mat():
    global _INK
    if not _INK:
        _INK = bpy.data.materials.new('ink'); _INK.use_nodes = True; nt = _INK.node_tree; nt.nodes.clear()
        e = nt.nodes.new('ShaderNodeEmission'); e.inputs[0].default_value = (0.06, 0.03, 0.025, 1)
        nt.links.new(e.outputs[0], nt.nodes.new('ShaderNodeOutputMaterial').inputs[0])
    return _INK


def setup_render():
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE'; sc.render.resolution_x, sc.render.resolution_y = 1600, 1000
    sc.view_settings.view_transform = 'Standard'; sc.render.film_transparent = False
    wd = bpy.data.worlds.new('w'); sc.world = wd; wd.use_nodes = True
    bg = wd.node_tree.nodes['Background']; bg.inputs[0].default_value = (0.97, 0.94, 0.86, 1); bg.inputs[1].default_value = 1.0
    sun = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); sun.data.energy = 3; sun.data.angle = 0.3
    sun.rotation_euler = (math.radians(40), math.radians(-20), math.radians(-60)); sc.collection.objects.link(sun)
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.clip_end = 100
    try:
        sc.render.use_freestyle = True; sc.render.line_thickness_mode = 'ABSOLUTE'; sc.render.line_thickness = 1.0
        fs = sc.view_layers[0].freestyle_settings; lset = fs.linesets[0] if len(fs.linesets) else fs.linesets.new('ink')
        lset.linestyle = lset.linestyle or bpy.data.linestyles.new('ink'); lset.linestyle.color = (0.25, 0.17, 0.15)
        lset.select_crease = False     # like the game's hull outline: silhouettes and overlaps only
    except Exception as e: print('no freestyle', e)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    setup_render()
    want = set(ARGV) or {s[0] for s in SPECIES}
    fish, report = [], []
    for sid, label, fn in SPECIES:
        if sid not in want: continue
        ob, tris = make_fish(sid, fn)
        report.append('%s %d %s' % (sid, tris, export(ob, sid)))
        ob.name = 'Fish_' + sid; fish.append((ob, label))
    open(os.path.join(OUT, 'tris.txt'), 'w').write('\n'.join(report) + '\n')
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, 'fish.blend'))
    sheet(fish, os.path.join(OUT, 'fish-side.png'), (1, 0, 0))
    sheet(fish, os.path.join(OUT, 'fish-34.png'), (0.85, 0.6, -0.35))


main()
