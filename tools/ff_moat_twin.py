#!/usr/bin/env python3
"""Wave FF (hc/ff-art) offline moat proof: a numpy twin of Resources/Castle/Shaders/CastleWater.shader (FF version, same constants) and a
preview that pastes the twin's water into a real capture.

    python3 tools/ff_moat_twin.py RAYCAST_DIR CAPTURE_DIR OUT_DIR [--time 3.0] [--set _Glade=1.2,_GladeWidth=.05]

RAYCAST_DIR comes from `Blender -b --factory-startup -P tools/castle_fd_raycast.py -- RAYCAST_DIR --ff --scale 1.0` (one ray per pixel at the
capture eyes, 1920x1080). CAPTURE_DIR holds earlier captures of the same views (any build: only their non-water pixels are used), as
cap_rooms/S4_exterior_sky.png and art_exterior/art_exterior_<view>.png, or flat. For every view the script replaces the pixels whose first hit
is the proxy's water with the twin's shading (castle exterior fog: exp2, density .034, colour (.20,.24,.34), _FogKeep .30; no fog-layer quads)
and writes OUT_DIR/<view>.png plus OUT_DIR/<view>_twin_water.png (water only). tools/check_art_views.py then runs the critic's tests on OUT_DIR.
This proves the shader's maths on the real camera and geometry, not Unity's exact output (no fog-layer quads, no MSAA, no tonemapping).
"""
import json, math, sys
from pathlib import Path
import numpy as np
from PIL import Image

PROPS = dict(
    _Deep=(0.050, 0.0034, 0.0040), _Reflect=2.2, _Ripple=0.30, _CastleTint=(0.010, 0.012, 0.022), _RockTint=(0.0035, 0.0040, 0.0065),
    _BoxSoft=1.5, _BloodTint=(5.6, 0.45, 0.40), _MoonKeep=0.35, _Ridge=(0.0040, 0.0060, 0.0140), _SkySoft=0.06,
    _Glade=1.8, _GladeElev=13.0, _GladeRough=0.26, _GladeColor=(0.62, 0.55, 0.55), _RedGlade=1.0, _RedGladeColor=(0.30, 0.0045, 0.004),
    _GladeBody=0.25, _GladeWidth=0.075, _RedGladeWidth=0.07, _GladeWobble=0.6, _HorizonGlow=0.03, _LampColor=(1.0, 0.36, 0.08), _FogKeep=0.30,
)
LAMPS = [(-3.0, 1.55, -23.0, 1.8), (3.0, 1.55, -23.0, 1.8), (-2.3, 2.05, -7.4, .6), (2.3, 2.05, -7.4, .6)]   # CastleExterior.Lamps
WAVES = [((.80, .60), 3.1, 1.1, .030), ((-.50, .87), 4.7, 1.6, .022), ((.20, -.98), 7.3, 2.1, .012), ((-.90, -.40), 11.0, 2.7, .007), ((.60, -.80), 2.1, .7, .040)]
M_PALE = np.array([-.3207, .8192, .4755]); M_RED = np.array([.4973, .6691, .5522])
FOG = None; DENS = .034

def lin(c):
    c = np.asarray(c, np.float64); return np.where(c <= .04045, c / 12.92, ((c + .055) / 1.055) ** 2.4)
def srgb(c):
    c = np.clip(c, 0, None); return np.where(c <= .0031308, c * 12.92, 1.055 * np.maximum(c, 1e-9) ** (1 / 2.4) - .055)
def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t)
def norm(v): return v / np.linalg.norm(v, axis=-1, keepdims=True)

def grad(P, T):
    g = np.zeros((len(P), 2))
    for (dx, dz), k, w, a in WAVES:
        c = a * k * np.cos(k * (dx * P[:, 0] + dz * P[:, 2]) + w * T)
        g[:, 0] += c * dx; g[:, 1] += c * dz
    return g

def boxw(P, R, mn, mx, soft, enter_only):
    inv = 1.0 / (R + 1e-5); t0 = (np.array(mn) - P) * inv; t1 = (np.array(mx) - P) * inv
    tn = np.minimum(t0, t1).max(1); tf = np.maximum(t0, t1).min(1)
    w = np.clip((tf - np.maximum(tn, 0)) / soft, 0, 1)
    if enter_only: w = np.where(tn <= 0, 0.0, w)
    return w

def sky_color(R):
    el = R[:, 1]
    hor = np.array([.0033, .0063, .0203]); zen = np.array([.00001, .00003, .00033])
    s = smooth(0, .7, el)[:, None]
    c = hor[None] * (1 - s) + zen[None] * s
    a1 = np.arccos(np.clip(R @ M_PALE, -1, 1)); a2 = np.arccos(np.clip(R @ M_RED, -1, 1))
    d1 = smooth(.215, .185, a1); d2 = smooth(.085, .065, a2)
    c = c + np.array([.50, .57, .70])[None] * d1[:, None] * .9 + np.array([.05, .085, .26])[None] * (np.exp(-a1 / .16) * .9)[:, None]
    c = c + np.array([.42, .010, .004])[None] * d2[:, None] * .9 + np.array([.20, .008, .008])[None] * (np.exp(-a2 / .07) * .8)[:, None]
    moon = np.clip(d1 + d2, 0, 1)
    az = np.arctan2(R[:, 0], R[:, 2])
    center = np.radians(31) + np.radians(9) * np.sin(az * 2.3 + .7) + np.radians(2.5) * np.sin(az * 7.0)
    dz = (el - center) / np.radians(8)
    win = smooth(np.radians(-18), np.radians(10), az) * (1 - smooth(np.radians(62), np.radians(100), az))
    c = c + np.array([.006, .86, .23])[None] * (np.exp(-dz * dz * 1.2) * win * (.5 + .5 * np.sin(az * 5 + 1.3)) * .16)[:, None]
    return c, moon, az

def moon_glade(M, D, V, g, up, fwd, p, width):
    f2 = fwd[[0, 2]] / np.linalg.norm(fwd[[0, 2]]); m2 = M[[0, 2]] / np.linalg.norm(M[[0, 2]])
    if m2 @ f2 > 0 and M @ fwd > 0:
        n = np.cross(up, M); n /= np.linalg.norm(n)
        s = D @ n + (g @ n[[0, 2]]) * p["_GladeWobble"]
        graze = .55 + .45 * smooth(-.35, -.05, D[:, 1])
        return np.exp(-s * s / width ** 2) * graze
    r2 = np.array([f2[1], -f2[0]])
    m2 = f2 + .38 * (r2 if m2 @ r2 >= 0 else -r2); m2 /= np.linalg.norm(m2)
    ce, se = math.cos(math.radians(p["_GladeElev"])), math.sin(math.radians(p["_GladeElev"]))
    Hg = norm(V + np.array([m2[0] * ce, se, m2[1] * ce])[None])
    hc = np.maximum(Hg[:, 1], 1e-3); ht2 = (1 - hc * hc) / (hc * hc)
    return np.exp(-ht2 / p["_GladeRough"] ** 2) * p["_GladeBody"]

def water(P, eye, fwd, up, T=3.0, p=PROPS):
    """Linear RGB of CastleWater (FF) at castle-local points P seen from eye; fog applied as CastleAtmosphere's exterior profile."""
    V = norm(eye[None] - P)
    g = grad(P, T) * p["_Ripple"]
    N = norm(np.stack([-g[:, 0], np.ones(len(P)), -g[:, 1]], -1))
    R = 2 * (N * V).sum(1, keepdims=True) * N - V; R[:, 1] = np.abs(R[:, 1])
    ndv = np.clip((N * V).sum(1), 0, 1); F = .03 + .97 * (1 - ndv) ** 5
    sky, moon, az = sky_color(R)
    skyline = .10 + .06 * np.sin(az * 3.1 + .4) + .035 * np.sin(az * 7.3 + 1.1) + .02 * np.sin(az * 17.0)
    under = 1 - smooth(skyline - p["_SkySoft"], skyline + p["_SkySoft"], R[:, 1])
    glow = p["_HorizonGlow"] * (1 - smooth(skyline, skyline + .28, R[:, 1])) * (1 - under)   # the night glow just above the ridge line
    sky = sky * (1 - under[:, None]) + np.array(p["_Ridge"])[None] * under[:, None] + glow[:, None] * np.array([.30, .32, .45])[None]
    soft = p["_BoxSoft"]
    wc = np.maximum(np.maximum(boxw(P, R, (-14.45, -3.2, -.45), (14.45, 7.2, 28.45), soft, False),
                               boxw(P, R, (-6, -3.2, 14), (6, 9.2, 28.45), soft, False)),
                    boxw(P, R, (-.5, -3.2, 17.6), (5.1, 32, 23.2), soft, False))
    wr = boxw(P, R, (-21.5, -3.0, -6.6), (21.5, 3.6, 35.0), soft, True) * (1 - wc)
    sky = sky * (1 - wc[:, None]) + np.array(p["_CastleTint"])[None] * wc[:, None]
    sky = sky * (1 - wr[:, None]) + np.array(p["_RockTint"])[None] * wr[:, None]
    ww = boxw(P, R, (-2.3, -1.6, -16.0), (2.3, 1.0, -6.3), soft, False)
    sky = sky * (1 - ww[:, None]) + (np.array(p["_CastleTint"]) * .6)[None] * ww[:, None]
    moon = moon * np.clip(1 - wc - wr - ww, 0, 1)
    lum = sky @ np.array([.2126, .7152, .0722])
    refl = lum[:, None] * np.array(p["_BloodTint"])[None] + sky * (moon * p["_MoonKeep"] * 3.0)[:, None]
    col = np.array(p["_Deep"])[None] * (1 - F * .6)[:, None] + refl * (F * p["_Reflect"])[:, None]
    D = -V
    col += np.array(p["_GladeColor"])[None] * (p["_Glade"] * moon_glade(M_PALE, D, V, g, up, fwd, p, p["_GladeWidth"]))[:, None]
    col += np.array(p["_RedGladeColor"])[None] * (p["_RedGlade"] * moon_glade(M_RED, D, V, g, up, fwd, p, p["_RedGladeWidth"]))[:, None]
    lamps = np.zeros(len(P))
    for (lx, ly, lz, li) in LAMPS:
        dd = np.array([lx, ly, lz])[None] - P; dist = np.linalg.norm(dd, axis=1); dd /= dist[:, None]
        H = norm(V + dd); cc = np.maximum(H[:, 1], 1e-3); t2 = (1 - cc * cc) / (cc * cc)
        lamps += li * np.exp(-t2 / .11 ** 2) * .55 * np.clip(30 / (dist + 6), 0, 1)
    col += np.array(p["_LampColor"])[None] * lamps[:, None]
    depth = np.linalg.norm(P - eye[None], axis=1)        # Unity's exp2 fog uses the clip-space depth; the view-ray length is within 10 % here
    fogf = np.exp(-(DENS * depth) ** 2)[:, None]
    fogged = col * fogf + lin((.20, .24, .34))[None] * (1 - fogf)
    return col * (1 - p["_FogKeep"]) + fogged * p["_FogKeep"]

def find_capture(cap, view):
    for c in (cap / "cap_rooms" / (view + ".png"), cap / "art_exterior" / (view + ".png"), cap / (view + ".png")):
        if c.exists(): return c
    return None

def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    rc, cap, out = Path(args[0]), Path(args[1]), Path(args[2]); out.mkdir(parents=True, exist_ok=True)
    T = float(sys.argv[sys.argv.index("--time") + 1]) if "--time" in sys.argv else 3.0
    p = dict(PROPS)
    if "--set" in sys.argv:
        for kv in sys.argv[sys.argv.index("--set") + 1].split(","):
            k, v = kv.split("="); vs = [float(x) for x in v.split(":")]
            p[k] = vs[0] if len(vs) == 1 else tuple(vs[:3])
    names = json.loads((rc / "materials.json").read_text()); wi = names.index("Proof_Water")
    for f in sorted(rc.glob("*.npz")):
        z = np.load(f); view = f.stem
        hit, mid, pos = z["hit"], z["mid"], z["pos"]; eye, fwd = z["eye"].astype(float), z["fwd"].astype(float)
        r = np.cross([0, 1, 0], fwd); r /= np.linalg.norm(r); up = np.cross(fwd, r)
        is_w = hit & (mid == wi)
        src = find_capture(cap, view)
        base = np.asarray(Image.open(src).convert("RGB")).copy() if src else np.zeros(hit.shape + (3,), np.uint8)
        wc = water(pos[is_w].astype(np.float64), eye, fwd, up, T, p)
        px = (np.clip(srgb(wc), 0, 1) * 255 + .5).astype(np.uint8)
        img = base.copy(); img[is_w] = px
        only = np.zeros_like(base); only[is_w] = px
        Image.fromarray(img).save(out / (view + ".png")); Image.fromarray(only).save(out / (view + "_twin_water.png"))
        print("HC_FF_TWIN", view, "water_px", int(is_w.sum()), "base", src.name if src else None, flush=True)

if __name__ == "__main__":
    main()
