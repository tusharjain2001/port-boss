# Minimal binary-FBX reader: prints each Model's local translation (Lcl Translation) and parent, then joint-chain lengths.
# Usage: python -I fbxbones.py file.fbx
import sys, struct, zlib
data = open(sys.argv[1], 'rb').read()
ver = struct.unpack_from('<I', data, 23)[0]
big = ver >= 7500
def read_prop(o):
    t = chr(data[o]); o += 1
    if t in 'YCIFDL':
        fmt = {'Y': '<h', 'C': '<?', 'I': '<i', 'F': '<f', 'D': '<d', 'L': '<q'}[t]
        return struct.unpack_from(fmt, data, o)[0], o + struct.calcsize(fmt)
    if t in 'fdlib':
        n, enc, clen = struct.unpack_from('<III', data, o); o += 12
        raw = data[o:o + clen]; o += clen
        if enc: raw = zlib.decompress(raw)
        fmt = {'f': 'f', 'd': 'd', 'l': 'q', 'i': 'i', 'b': '?'}[t]
        return list(struct.unpack('<%d%s' % (n, fmt), raw)), o
    if t in 'SR':
        n = struct.unpack_from('<I', data, o)[0]; o += 4
        return data[o:o + n], o + n
    raise ValueError(t)
def read_node(o):
    if big: end, nprops, plen = struct.unpack_from('<QQQ', data, o); o += 24
    else: end, nprops, plen = struct.unpack_from('<III', data, o); o += 12
    nl = data[o]; o += 1
    name = data[o:o + nl].decode(); o += nl
    if end == 0: return None, o
    props = []
    p = o
    for _ in range(nprops):
        v, p = read_prop(p); props.append(v)
    o += plen
    kids = []
    while o < end:
        k, o2 = read_node(o)
        if k is None: o = o2; break
        kids.append(k); o = o2
    return (name, props, kids), end
o = 27; top = []
while o < len(data) - 200:
    n, o = read_node(o)
    if n is None: break
    top.append(n)
models = {}; conns = []
for n in top:
    if n[0] == 'Objects':
        for ob in n[2]:
            if ob[0] == 'Model':
                uid = ob[1][0]; nm = ob[1][1].split(b'\x00')[0].decode().split(':')[-1]
                t = (0, 0, 0); r = (0, 0, 0); pre = (0, 0, 0)
                for k in ob[2]:
                    if k[0] == 'Properties70':
                        for p in k[2]:
                            if p[1][0] == b'Lcl Translation': t = tuple(p[1][4:7])
                            if p[1][0] == b'Lcl Rotation': r = tuple(p[1][4:7])
                            if p[1][0] == b'PreRotation': pre = tuple(p[1][4:7])
                models[uid] = [nm, t, None, r, pre]
    if n[0] == 'Connections':
        for c in n[2]:
            if c[1][0] == b'OO': conns.append((c[1][1], c[1][2]))
for a, b in conns:
    if a in models and b in models: models[a][2] = b
byname = {v[0]: v for v in models.values()}
import math
def L(name): t = byname[name][1]; return math.sqrt(sum(x * x for x in t))
import numpy as np
def rot(e):
    x, y, z = [math.radians(v) for v in e]
    Rx = np.array([[1,0,0],[0,math.cos(x),-math.sin(x)],[0,math.sin(x),math.cos(x)]])
    Ry = np.array([[math.cos(y),0,math.sin(y)],[0,1,0],[-math.sin(y),0,math.cos(y)]])
    Rz = np.array([[math.cos(z),-math.sin(z),0],[math.sin(z),math.cos(z),0],[0,0,1]])
    return Rz @ Ry @ Rx
def M(v):
    m = np.eye(4); m[:3,:3] = rot(v[4]) @ rot(v[3]); m[:3,3] = v[1]; return m
def world(name):
    v = byname[name]; m = np.eye(4)
    while v:
        m = M(v) @ m; v = models.get(v[2])
    return list(m[:3,3])
ks = [k for k in byname if k.startswith('mixamorig')]
pre = 'mixamorig'
def B(s): return pre + s if pre + s in byname else s
get = lambda s: byname.get(B(s))
need = ['Hips', 'LeftUpLeg', 'LeftLeg', 'LeftFoot', 'LeftToeBase', 'LeftArm', 'LeftForeArm', 'LeftHand', 'LeftHandMiddle1', 'LeftHandMiddle4', 'Head', 'HeadTop_End', 'Neck', 'LeftShoulder', 'RightArm']
W = {s: world(B(s)) for s in need if get(s)}
for s in need:
    if s in W: print('%-16s world %s  local-len %.3f' % (s, ['%.3f' % v for v in W[s]], L(B(s))))
