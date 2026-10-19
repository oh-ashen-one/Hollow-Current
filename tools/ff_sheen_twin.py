#!/usr/bin/env python3
"""Wave FF (hc/ff-art): numpy twin of Resources/Castle/Shaders/CastleFloorSheen.shader (FF version) on the S1 proxy, for the floor-mirror test.

    python3 tools/ff_sheen_twin.py NEW_CLASS.npz OUT_LINEAR.npy [--strength 1.0] [--table-strength 1.0] [--delta]

--delta subtracts wave FD's sheen (no streak, lancet sources at x -+3.7) from the floor, for pasting onto a capture that already has it.

NEW_CLASS.npz is tools/ff_hall_raycast.py's output (class id and surface point per pixel). The hall floor quad (x +-5.55, z 14.5-27.4) and the
table-top quad (CastleWorld.FloorSheen, y .78, _WetMin .9) are shaded with the same sources as CastleWorld.FloorSheen (_S0.._S7), the puddle
noise Castle_FogNoise.png (alpha) at _Tile .21, Schlick fresnel and the hall's exp2 fog (density .03) fading to black. Writes the additive
linear RGB per pixel (H x W x 3) for tools/ff_hall_reproject.py --sheen-lin.
"""
import sys, math
from pathlib import Path
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCES = [(-3.4, 1.65, 20.1, .55), (3.4, 1.65, 20.1, .55), (-3.4, 1.65, 16.9, .5), (3.4, 1.65, 16.9, .5), (0, 3.9, 22.9, .7),
           (0, 7.1, 27.4, -.55), (-2.6, 5.4, 27.4, -.35), (2.6, 5.4, 27.4, -.35)]           # CastleWorld.FloorSheen (wave FF)
SOURCES_OLD = SOURCES[:5] + [(0, 7.1, 27.4, -.55), (-3.7, 5.8, 27.4, -.35), (3.7, 5.8, 27.4, -.35)]   # wave FD
WARM = np.array([1.0, .52, .20]); COLD = np.array([.22, .32, .62])
P_ = dict(_Tile=.21, _Strength=1.0, _Rough=.045, _Streak=1.0, _StreakLen=.9, _StreakWidth=.035, _WinHalf=.6)

def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t)

def noise_a(uv, tex):
    h, w = tex.shape
    x = (uv[:, 0] % 1.0) * w - .5; y = (uv[:, 1] % 1.0) * h - .5
    x0 = np.floor(x).astype(int); y0 = np.floor(y).astype(int); fx = x - x0; fy = y - y0
    def at(xx, yy): return tex[yy % h, xx % w]
    return (at(x0, y0) * (1 - fx) * (1 - fy) + at(x0 + 1, y0) * fx * (1 - fy) + at(x0, y0 + 1) * (1 - fx) * fy + at(x0 + 1, y0 + 1) * fx * fy)

def sheen(P, eye, tex, p, wet_min, sources=None):
    sources = sources or SOURCES
    V = eye[None] - P; V /= np.linalg.norm(V, axis=1, keepdims=True)
    uv = P[:, [0, 2]] * p["_Tile"]
    a = noise_a(uv, tex); ax = noise_a(uv + [.01, 0], tex); az = noise_a(uv + [0, .01], tex)
    wet = smooth(.38, .62, a) * (1 - wet_min) + wet_min
    N = np.stack([(a - ax) * .9, np.ones(len(P)), (a - az) * .9], -1); N /= np.linalg.norm(N, axis=1, keepdims=True)
    R = 2 * (N * V).sum(1, keepdims=True) * N - V
    F = .04 + .96 * (1 - np.clip((N * V).sum(1), 0, 1)) ** 5
    col = np.zeros((len(P), 3))
    for (sx, sy, sz, w) in sources:
        d = np.array([sx, sy, sz])[None] - P; dist = np.linalg.norm(d, axis=1); d /= dist[:, None]
        k = p["_Rough"] ** 2 + (.12 / dist) ** 2
        lobe = np.exp(((R * d).sum(1) - 1) / k)
        if w < 0:
            daz = np.arctan2(d[:, 0], d[:, 2]) - np.arctan2(R[:, 0], R[:, 2])
            daz = daz - 2 * math.pi * np.floor((daz + math.pi) / (2 * math.pi))
            de = np.arcsin(np.clip(d[:, 1], -1, 1)) - np.arcsin(np.clip(R[:, 1], -1, 1))
            wa = p["_StreakWidth"] + p["_WinHalf"] / dist
            lobe = np.maximum(lobe, p["_Streak"] * np.exp(-daz ** 2 / wa ** 2 - de ** 2 / p["_StreakLen"] ** 2))
        c = WARM if w > 0 else COLD
        col += c[None] * (abs(w) * lobe / (1 + dist * .12))[:, None]
    col *= (wet * F * p["_Strength"])[:, None]
    depth = np.linalg.norm(P - eye[None], axis=1)
    return col * np.exp(-(.03 * depth) ** 2)[:, None]

def main():
    a = [x for x in sys.argv[1:] if not x.startswith("--")]
    z = np.load(a[0]); out = Path(a[1])
    p = dict(P_)
    if "--strength" in sys.argv: p["_Strength"] = float(sys.argv[sys.argv.index("--strength") + 1])
    ts = float(sys.argv[sys.argv.index("--table-strength") + 1]) if "--table-strength" in sys.argv else 1.0
    tex = np.asarray(Image.open(ROOT / "Assets/HollowCurrent/Resources/Castle/Textures/Castle_FogNoise.png").convert("RGBA"), np.float64)[..., 3] / 255
    tex = tex[::-1]                                       # Unity's v = 0 is the bottom row
    cid, names, hit, eye = z["cid"], list(z["names"]), z["hit"].astype(np.float64), z["eye"].astype(np.float64)
    H, W = cid.shape; add = np.zeros((H * W, 3))
    flat = cid.reshape(-1); P = hit.reshape(-1, 3)
    fl = flat == names.index("floor")
    fl &= (np.abs(P[:, 0]) <= 5.55) & (P[:, 2] >= 14.5) & (P[:, 2] <= 27.4)
    add[fl] = sheen(P[fl], eye, tex, p, .15)
    if "--delta" in sys.argv:                             # the captured frame already holds wave FD's sheen: add only the change
        add[fl] -= sheen(P[fl], eye, tex, dict(p, _Streak=0.0, _Strength=1.0), .15, SOURCES_OLD)
    tb = (flat == names.index("table")) & (np.abs(P[:, 1] - .775) < .03) & (np.abs(P[:, 0]) <= .65) & (np.abs(P[:, 2] - 23.7) <= 1.65)
    pt = dict(p, _Strength=p["_Strength"] * ts)
    add[tb] = sheen(P[tb], eye, tex, pt, .9)
    np.save(out, add.reshape(H, W, 3))
    print("HC_FF_SHEEN floor px %d table-top px %d max linear %.3f" % (fl.sum(), tb.sum(), add.max()))

if __name__ == "__main__":
    main()
