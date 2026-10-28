# Port of Bevel.Box: checks the mesh is closed (each edge used twice, opposite directions) and the volume is positive (outward winding).
from collections import Counter
def box(sx, sy, sz, r):
    h=(sx/2,sy/2,sz/2); r=min(r, min(h)*0.45)
    V=[]; T=[]
    def sub(a,b): return (a[0]-b[0],a[1]-b[1],a[2]-b[2])
    def cross(a,b): return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])
    def dot(a,b): return sum(x*y for x,y in zip(a,b))
    def poly(*p):
        n=cross(sub(p[1],p[0]),sub(p[2],p[0])); c=tuple(sum(q[i] for q in p)/len(p) for i in range(3))
        flip=dot(n,c)<0; b=len(V); V.extend(p)
        for i in range(1,len(p)-1): T.append((b, b+(i+1 if flip else i), b+(i if flip else i+1)))
    def P(s,ix,iy,iz): return (s[0]*(h[0]-(r if ix else 0)), s[1]*(h[1]-(r if iy else 0)), s[2]*(h[2]-(r if iz else 0)))
    S=(-1,1)
    for s in S:
        poly(P((s,-1,-1),0,1,1),P((s,1,-1),0,1,1),P((s,1,1),0,1,1),P((s,-1,1),0,1,1))
        poly(P((-1,s,-1),1,0,1),P((1,s,-1),1,0,1),P((1,s,1),1,0,1),P((-1,s,1),1,0,1))
        poly(P((-1,-1,s),1,1,0),P((1,-1,s),1,1,0),P((1,1,s),1,1,0),P((-1,1,s),1,1,0))
    for a in S:
        for b in S:
            poly(P((a,b,-1),0,1,1),P((a,b,-1),1,0,1),P((a,b,1),1,0,1),P((a,b,1),0,1,1))
            poly(P((a,-1,b),0,1,1),P((a,-1,b),1,1,0),P((a,1,b),1,1,0),P((a,1,b),0,1,1))
            poly(P((-1,a,b),1,0,1),P((-1,a,b),1,1,0),P((1,a,b),1,1,0),P((1,a,b),1,0,1))
            for c in S: poly(P((a,b,c),0,1,1),P((a,b,c),1,0,1),P((a,b,c),1,1,0))
    key=lambda p: tuple(round(x,6) for x in p)
    E=Counter()
    for t in T:
        for i in range(3):
            E[(key(V[t[i]]),key(V[t[(i+1)%3]]))]+=1
    bad=[e for e,n in E.items() if n!=1 or E.get((e[1],e[0]),0)!=1]
    vol=sum(dot(V[t[0]],cross(V[t[1]],V[t[2]])) for t in T)/6
    # Unity is left-handed with clockwise front faces, so outward faces give a negative right-handed volume.
    return len(V), len(T), len(bad), vol, sx*sy*sz
for size in [(1,1,1),(2.98,0.12,0.3),(6,3.5,0.15)]:
    print(size, box(*size, min(size)*0.12))
