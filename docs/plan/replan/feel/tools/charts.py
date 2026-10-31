# Proof charts for the feel audit: rig proportions vs Mixamo X Bot, and game speeds vs the clips' own speeds.
import matplotlib; matplotlib.use('Agg')
import matplotlib.pyplot as plt
OUT = r'C:\Users\asus\Desktop\Port Boss\plan\replan\feel'
SURF, INK, INK2, GRID = '#fcfcfb', '#1f1f1e', '#5b5a55', '#e4e3df'
OURS, XBOT = '#2a78d6', '#eb6834'
plt.rcParams.update({'font.family': 'Arial', 'font.size': 12, 'axes.edgecolor': GRID, 'text.color': INK,
                     'axes.labelcolor': INK2, 'xtick.color': INK2, 'ytick.color': INK})

# 1. Proportions, % of skeleton height (head-top joint). Ours: art-src/worker/build_worker.py bone positions
# (head top 1.80). X Bot: parsed from mixamo-incoming/X Bot.fbx (tools/fbxbones.py, head top 182.0 cm).
H1, H2 = 1.80, 181.97
rows = [  # label, ours (m), X Bot (cm)
    ('Shoulder joints apart', 0.50, 30.33),
    ('Arm (shoulder to wrist)', 0.51, 56.17),
    ('Hand (wrist to fingertip)', 0.183, 20.0),
    ('Leg (hip to ankle)', 0.781, 88.90),
    ('Hip height', 0.88, 97.52),
    ('Shoulder height', 1.38, 144.06),
    ('Head + neck (neck base to top)', 0.37, 31.66),
]
fig, ax = plt.subplots(figsize=(12, 6.4), dpi=150); fig.patch.set_facecolor(SURF); ax.set_facecolor(SURF)
y = list(range(len(rows)))[::-1]; h = 0.36
for i, (lab, o, x) in enumerate(rows):
    po, px = 100 * o / H1, 100 * x / H2
    ax.barh(y[i] + h / 2 + 0.01, po, h, color=OURS, edgecolor=SURF, linewidth=2)
    ax.barh(y[i] - h / 2 - 0.01, px, h, color=XBOT, edgecolor=SURF, linewidth=2)
    ax.text(po + 0.8, y[i] + h / 2, f'{po:.0f}%', va='center', color=INK, fontsize=11)
    ax.text(px + 0.8, y[i] - h / 2, f'{px:.0f}%', va='center', color=INK2, fontsize=11)
    r = po / px
    ax.text(101, y[i], f'{r:.2f}x', va='center', ha='left', fontsize=12, fontweight='bold', color=INK if abs(r - 1) > 0.1 else INK2)
ax.set_yticks(y); ax.set_yticklabels([r[0] for r in rows])
ax.set_xlim(0, 100); ax.set_xlabel('% of skeleton height (head top)')
ax.xaxis.grid(True, color=GRID); ax.set_axisbelow(True)
for s in ('top', 'right', 'left'): ax.spines[s].set_visible(False)
ax.text(101, len(rows) - 0.35, 'ours / X Bot', fontsize=11, color=INK2)
from matplotlib.patches import Patch
ax.legend(handles=[Patch(color=OURS, label='Our dock worker (build_worker.py)'), Patch(color=XBOT, label='Mixamo X Bot (the clips were made on this body)')],
          loc='lower right', frameon=False)
ax.set_title('Our worker vs the body the Mixamo clips were made for', loc='left', fontsize=15, fontweight='bold')
fig.text(0.01, 0.01, 'Shoulders 1.7x wider apart, arms and legs ~0.9x as long, hips 0.91x as high:\nthe clips swing arms through a chest that is not there on X Bot, and short legs must cycle faster to cover the same ground.', fontsize=10, color=INK2)
fig.subplots_adjust(left=0.25, right=0.9, bottom=0.17)
fig.savefig(OUT + r'\02-proportions-vs-mixamo.png', facecolor=SURF); plt.close(fig)

# 2. Speeds: what the game moves at vs the speed each clip was made for (measured on our rig, [PB] gait log lines).
fig, ax = plt.subplots(figsize=(12, 4.6), dpi=150); fig.patch.set_facecolor(SURF); ax.set_facecolor(SURF)
clips = [('Standard Walk', 1.24), ('Standard Run', 3.47), ('Sprint', 4.48)]
game = [('walk (stick 25%)', 1.15), ('swim', 2.2), ('carry 5 crates', 2.99), ('RUN (keyboard default)', 4.6), ('SPRINT', 7.4)]
ax.hlines([1, 0], 0, 8, color=GRID, linewidth=1)
for i, (n, v) in enumerate(clips):
    ax.plot(v, 1, 'o', ms=12, color=XBOT, markeredgecolor=SURF, markeredgewidth=2)
    ax.annotate(f'{n}\n{v:.2f} m/s', (v, 1), xytext=(0, 14), textcoords='offset points', ha='center', fontsize=11, color=INK)
for i, (n, v) in enumerate(game):
    ax.plot(v, 0, 'o', ms=12, color=OURS, markeredgecolor=SURF, markeredgewidth=2)
    ax.annotate(f'{n}\n{v:.2f} m/s', (v, 0), xytext=(0, -34), textcoords='offset points', ha='center', fontsize=11, color=INK)
ax.annotate('', xy=(4.48, 0.88), xytext=(4.6, 0.12), arrowprops=dict(arrowstyle='->', color=INK2, lw=1.4))
ax.annotate('', xy=(4.55, 0.88), xytext=(7.4, 0.12), arrowprops=dict(arrowstyle='->', color=INK2, lw=1.4))
ax.text(5.35, 1.3, 'Run AND sprint both play the Sprint clip:\nrun at 1.03x, sprint at 1.65x speed. Standard Run\nis only seen for ~0.1 s while speeding up.', fontsize=10.5, color=INK, va='center')
ax.set_yticks([1, 0]); ax.set_yticklabels(['clip made for', 'game moves at'])
ax.set_xlim(0, 8); ax.set_ylim(-0.75, 1.6); ax.set_xlabel('m/s')
for s in ('top', 'right', 'left'): ax.spines[s].set_visible(False)
ax.tick_params(axis='y', length=0)
ax.set_title('The game runs faster than any clip it has', loc='left', fontsize=15, fontweight='bold')
fig.subplots_adjust(left=0.13, right=0.98, bottom=0.17, top=0.88)
fig.savefig(OUT + r'\01b-speeds-vs-clips.png', facecolor=SURF); plt.close(fig)
print('ok')

# 3. Start / stop speed curves: today's Player.cs (MoveTowards at 30 m/s^2 on the ground, any load) vs the feel spec.
import numpy as np
def curve(v0, v1, acc, T=0.8, dt=1/120):
    t = np.arange(0, T, dt); v = []; x = v0
    for _ in t: v.append(x); x = x + np.clip(v1 - x, -acc * dt, acc * dt)
    return t, np.array(v)
def ease(v0, v1, t90, T=0.8, dt=1/120):  # spec: critically damped ease, 90% of the change in t90 seconds
    t = np.arange(0, T, dt); k = 2.3 / t90; return t, v1 + (v0 - v1) * np.exp(-k * t)
fig, axs = plt.subplots(1, 2, figsize=(12, 4.6), dpi=150, sharey=True); fig.patch.set_facecolor(SURF)
for ax, (title, start) in zip(axs, [('Start (stick pushed)', True), ('Stop (stick released)', False)]):
    ax.set_facecolor(SURF)
    for lab, v, acc, t90, col in [('no crates', (4.6, 4.2), 30, (0.22, 0.18), OURS), ('5 crates', (2.99, 3.0), 30, (0.45, 0.55), XBOT)]:
        a, b = (0, v[0]) if start else (v[0], 0)
        t, y = curve(a, b, acc); ax.plot(t, y, color=col, lw=2, label=f'today, {lab}')
        a, b = (0, v[1]) if start else (v[1], 0)
        t, y = ease(a, b, t90[0] if start else t90[1]); ax.plot(t, y, color=col, lw=2, ls=(0, (4, 3)), label=f'spec, {lab}')
    ax.set_title(title, loc='left', fontsize=13, fontweight='bold'); ax.set_xlabel('seconds'); ax.set_xlim(0, 0.8)
    ax.yaxis.grid(True, color=GRID); ax.set_axisbelow(True)
    for s in ('top', 'right', 'left'): ax.spines[s].set_visible(False)
axs[0].set_ylabel('speed, m/s')
axs[0].text(0.3, 3.35, 'today: full speed in 0.15 s with no crates,\n0.10 s with 5 (the same 30 m/s² push)', fontsize=10, color=INK)
axs[1].legend(frameon=False, loc='upper right')
fig.suptitle('Today the worker has no weight: 5 crates start and stop as hard as none', x=0.01, ha='left', fontsize=14, fontweight='bold')
fig.subplots_adjust(left=0.07, right=0.98, bottom=0.14, top=0.83, wspace=0.08)
fig.savefig(OUT + r'\07-start-stop-curves.png', facecolor=SURF); plt.close(fig)
print('ok3')
