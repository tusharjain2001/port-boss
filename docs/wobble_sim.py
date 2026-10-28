import math
# 1D lean model per axis, mirrors Player.UpdateSway. Units: lean 1.0 = topple.
A, S, D = 0.020, 16.0, 3.2
def instab(n, sprint): return max(0, n-1)*2.9 + (3.5 if sprint else 0)
def run(n, sprint, events, T=3.0, dt=1/120, save=False):
    lean=v=0; vel=0; peak=0; accel=30; t=0
    speed = 7.4 if sprint else 4.6
    speed *= (1-0.07*n)
    target=0
    while t<T:
        target = 0
        for (t0,t1,val) in events:
            if t0<=t<t1: target=val*speed
        old=vel
        if save and lean>0.55: target = vel + 3  # counter-steer forward a bit
        vel += max(-accel*dt, min(accel*dt, target-vel))
        a=(vel-old)/dt
        k=S-instab(n,sprint)
        bob = (0.25*n/5)*math.sin(t*9)*abs(vel)/7.4
        acc = -a*0.1*(n-1)**1.2 + bob*6 - k*lean - D*v
        v+=acc*dt; lean+=v*dt; peak=max(peak,abs(lean)); t+=dt
        if abs(lean)>1: return f"TOPPLE@{t:.2f}"
    return f"{peak:.2f}"
for n in [2,3,4,5]:
    for sp in [False,True]:
        start = run(n,sp,[(0.2,3,1)])
        stop  = run(n,sp,[(0,1.0,1)])
        rev   = run(n,sp,[(0,1.0,1),(1.0,3,-1)])
        print(f"n={n} sprint={sp}: start {start}  stop {stop}  reverse {rev}")
