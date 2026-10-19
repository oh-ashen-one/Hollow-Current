#!/usr/bin/env python3
"""Wave FD (hc/fd-art) offline exterior proof, system-python half: shades the ray casts written by tools/castle_fd_raycast.py.

    python3 tools/castle_fd_shade.py RAYCAST_DIR [OUT_DIR]

Per view: SkyCastle equirect (docs/haunted-castle/art/sky_preview.jpg) where nothing is hit; flat palette albedo lit by the castle moonlight
direction plus ambient and the two far-bank brazier point lights (Unity's built-in point falloff); the exterior exponential-squared fog
(CastleAtmosphere: colour (.20,.24,.34), density .034); the moat and lake through a numpy twin of Resources/Castle/Shaders/CastleWater.shader
(same constants, _Time.y = 3 s). It proves composition, the water colour, the glade and the lamp glints, not final lighting (no lightmaps,
no fog layers, flat palette). Writes <view>.png and metrics.json with the r13 critic's moat tests:
  moat_RG          mean R / mean G (display 0-255) over the water pixels below the horizon (pass >= 1.6)
  glade_px2        bright (luma >= 150) water pixels, scaled to the capture size (pass >= 400)
  far_bank_lit_px  far-bank pixels (z < -16) that receive >= 8/255 from a brazier light, scaled to the capture size
"""
import json, math, sys
from pathlib import Path
import numpy as np, cv2
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
RC = Path(sys.argv[1]); OUT = Path(sys.argv[2]) if len(sys.argv) > 2 else RC
OUT.mkdir(parents=True, exist_ok=True)
sky = np.asarray(Image.open(ROOT / "docs/haunted-castle/art/sky_preview.jpg").convert("RGB")).astype(np.float32) / 255.0
names = json.loads((RC / "materials.json").read_text())
def lin(c): c = np.asarray(c, np.float64); return np.where(c <= .04045, c / 12.92, ((c + .055) / 1.055) ** 2.4)
def srgb(c): return np.where(c <= .0031308, c * 12.92, 1.055 * np.maximum(c, 1e-9) ** (1 / 2.4) - .055)
def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t)

PAL = {  # display albedo
    "Castle_Stone": (.40, .42, .50), "Castle_StoneWeathered": (.38, .41, .50), "Castle_StoneDark": (.24, .25, .31), "Castle_StoneMossy": (.27, .31, .30),
    "Castle_BlackStone": (.13, .14, .20), "Castle_Iron": (.17, .17, .19), "Castle_Oxblood": (.52, .17, .21), "Castle_Basalt": (.17, .19, .25),
    "Proof_Wall": (.36, .38, .46), "Proof_Roof": (.22, .23, .29), "Proof_Earth": (.20, .23, .21), "Proof_Wood": (.42, .32, .24), "Proof_Plank": (.40, .30, .22),
    "Proof_Iron": (.15, .15, .17), "Proof_Tree": (.11, .10, .10), "Proof_Paving": (.30, .30, .33), "Castle_ExtBackdrop": (.10, .13, .21),
}
EMIT = {"Proof_Flame": (1.0, .62, .25), "Castle_ExtWindow": (.85, .58, .23)}
MOON_L = np.array([.369, .766, -.527]); MOON_L /= np.linalg.norm(MOON_L)
AMB, MOONI = .30, .75
BRAZIERS = [np.array([-3.0, 1.65, -23.0]), np.array([3.0, 1.65, -23.0])]
BCOL, BI, BR = lin((1.0, .56, .26)), 3.0, 11.0
FOG = lin((.20, .24, .34)); DENS = .034

# ---- numpy twin of CastleWater.shader (wave FD) ----
WAVES = [((.80, .60), 3.1, 1.1, .030), ((-.50, .87), 4.7, 1.6, .022), ((.20, -.98), 7.3, 2.1, .012), ((-.90, -.40), 11.0, 2.7, .007), ((.60, -.80), 2.1, .7, .040)]
CHOP = [((.31, .95), 19.0, 3.3, .010), ((-.83, .56), 23.0, 3.9, .008), ((.97, -.24), 29.0, 4.4, .006), ((-.45, -.89), 37.0, 5.1, .005)]
RIPPLE = .55; DEEP = np.array([.050, .0034, .0040]); CASTLE = np.array([.010, .012, .022]); ROCK = np.array([.0035, .0040, .0065]); REFLECT = 2.2
BLOOD = np.array([5.6, .45, .40]); MOONKEEP = .35; RIDGE = np.array([.0040, .0060, .0140])
GLADE, GLADE_ELEV, GLADE_ROUGH, GLADE_COL = 1.0, 13.0, .20, np.array([.62, .52, .50])
LAMP_COL = np.array([1.0, .36, .08]); LAMPS = [(-3.0, 1.55, -23.0, 1.8), (3.0, 1.55, -23.0, 1.8), (-2.3, 2.05, -7.4, .3), (2.3, 2.05, -7.4, .3)]
FOGKEEP = .45
M_PALE = np.array([-.3207, .8192, .4755]); M_RED = np.array([.4973, .6691, .5522])

def grad(P, waves, T):
    g = np.zeros((len(P), 2))
    for (dx, dz), k, w, a in waves:
        c = a * k * np.cos(k * (dx * P[:, 0] + dz * P[:, 2]) + w * T)
        g[:, 0] += c * dx; g[:, 1] += c * dz
    return g
def norm(v): return v / np.linalg.norm(v, axis=-1, keepdims=True)
def hitbox(P, R, mn, mx):
    inv = 1.0 / (R + 1e-5); t0 = (np.array(mn) - P) * inv; t1 = (np.array(mx) - P) * inv
    tn = np.minimum(t0, t1).max(1); tf = np.maximum(t0, t1).min(1)
    return tf > np.maximum(tn, 0)

def water(P, eye, fwd, T=3.0):
    V = norm(eye[None] - P)
    g = grad(P, WAVES, T) * RIPPLE
    N = norm(np.stack([-g[:, 0], np.ones(len(P)), -g[:, 1]], -1))
    gc = g + grad(P, CHOP, T)
    Nr = norm(np.stack([-gc[:, 0], np.ones(len(P)), -gc[:, 1]], -1))
    R = 2 * (N * V).sum(1, keepdims=True) * N - V; R[:, 1] = np.abs(R[:, 1])
    ndv = np.clip((N * V).sum(1), 0, 1); F = .03 + .97 * (1 - ndv) ** 5
    el = R[:, 1]
    hor = np.array([.0033, .0063, .0203]); zen = np.array([.00001, .00003, .00033])
    c = hor[None] * (1 - smooth(0, .7, el)[:, None]) + zen[None] * smooth(0, .7, el)[:, None]
    a1 = np.arccos(np.clip(R @ M_PALE, -1, 1)); a2 = np.arccos(np.clip(R @ M_RED, -1, 1))
    d1 = smooth(.215, .185, a1); d2 = smooth(.085, .065, a2)
    c += np.array([.50, .57, .70])[None] * d1[:, None] * .9 + np.array([.05, .085, .26])[None] * (np.exp(-a1 / .16) * .9)[:, None]
    c += np.array([.42, .010, .004])[None] * d2[:, None] * .9 + np.array([.20, .008, .008])[None] * (np.exp(-a2 / .07) * .8)[:, None]
    moon = np.clip(d1 + d2, 0, 1)
    az = np.arctan2(R[:, 0], R[:, 2])
    center = np.radians(31) + np.radians(9) * np.sin(az * 2.3 + .7) + np.radians(2.5) * np.sin(az * 7.0)
    dz_ = (el - center) / np.radians(8)
    win = smooth(np.radians(-18), np.radians(10), az) * (1 - smooth(np.radians(62), np.radians(100), az))
    c += np.array([.006, .86, .23])[None] * (np.exp(-dz_ * dz_ * 1.2) * win * (.5 + .5 * np.sin(az * 5 + 1.3)) * .16)[:, None]
    skyline = .10 + .06 * np.sin(az * 3.1 + .4) + .035 * np.sin(az * 7.3 + 1.1) + .02 * np.sin(az * 17.0)
    under = 1 - smooth(skyline - .012, skyline + .012, el)
    c = c * (1 - under[:, None]) + RIDGE[None] * under[:, None]
    hb = hitbox(P, R, (-14.45, -3.2, -.45), (14.45, 7.2, 28.45)) | hitbox(P, R, (-6, -3.2, 14), (6, 9.2, 28.45)) | hitbox(P, R, (-.5, -3.2, 17.6), (5.1, 32, 23.2))
    c[hb] = CASTLE
    rk = ~hb & hitbox(P, R, (-21.5, -3.0, -6.6), (21.5, 3.6, 35.0)); c[rk] = ROCK
    cw = hitbox(P, R, (-2.3, -1.6, -16.0), (2.3, 1.0, -6.3)); c[cw] = CASTLE * .6
    lum = c @ np.array([.2126, .7152, .0722])
    refl = lum[:, None] * BLOOD[None] + c * (moon * MOONKEEP * 3.0)[:, None]
    col = DEEP[None] * (1 - F * .6)[:, None] + refl * (F * REFLECT)[:, None]
    # glade
    f2 = np.array([fwd[0], fwd[2]]); f2 /= np.linalg.norm(f2)
    m2 = np.array([M_PALE[0], M_PALE[2]]); m2 /= np.linalg.norm(m2)
    side = m2 @ f2
    if side < 0:
        r2 = np.array([f2[1], -f2[0]]); m2 = f2 + .38 * (r2 if m2 @ r2 >= 0 else -r2); m2 /= np.linalg.norm(m2)
    ce, se = math.cos(math.radians(GLADE_ELEV)), math.sin(math.radians(GLADE_ELEV))
    Lg = np.array([m2[0] * ce, se, m2[1] * ce])
    Hg = norm(V + Lg[None]); hc = np.maximum(Hg[:, 1], 1e-3); ht2 = (1 - hc * hc) / (hc * hc)
    lobe = np.exp(-ht2 / GLADE_ROUGH ** 2)
    glint = np.clip((Nr * Hg).sum(1), 0, 1) ** 160
    cell = np.floor(P[:, [0, 2]] * 8.0)
    spark = (np.modf(np.abs(np.sin(cell @ np.array([12.9898, 78.233]) + np.floor(T * 3.0) * 1.7) * 43758.5453))[0] >= .55).astype(float)
    col += GLADE_COL[None] * GLADE * (lobe * .8 + glint * lobe * 2.2 * (.35 + spark) + glint * .2 * spark * np.clip(lobe * 8, 0, 1))[:, None]
    lamps = np.zeros(len(P))
    for (lx, ly, lz, li) in LAMPS:
        dd = np.array([lx, ly, lz])[None] - P; dist = np.linalg.norm(dd, axis=1); dd /= dist[:, None]
        H = norm(V + dd); cc = np.maximum(H[:, 1], 1e-3); t2 = (1 - cc * cc) / (cc * cc)
        soft = np.exp(-t2 / .11 ** 2); sharp = np.clip((Nr * H).sum(1), 0, 1) ** 400 * np.exp(-t2 / .30 ** 2)
        lamps += li * (soft * .55 + sharp * 3.0) * np.clip(30 / (dist + 6), 0, 1)
    col += LAMP_COL[None] * lamps[:, None]
    return col

metrics = {}
for f in sorted(RC.glob("*.npz")):
    z = np.load(f); name = f.stem
    hit, dist, nrm, mid, pos, d = z["hit"], z["dist"], z["nrm"], z["mid"], z["pos"], z["dir"]
    eye, fwd = z["eye"], z["fwd"]; W, H = z["size"]; h, w = hit.shape
    az = np.arctan2(d[..., 0], d[..., 2]); el = np.arcsin(np.clip(d[..., 1], -1, 1))
    su = ((az + math.pi) / (2 * math.pi) * (sky.shape[1] - 1)).astype(np.float32)
    sv = ((math.pi / 2 - el) / math.pi * (sky.shape[0] - 1)).astype(np.float32)
    img = lin(cv2.remap(sky, su, sv, cv2.INTER_LINEAR, borderMode=cv2.BORDER_WRAP))
    out = img.copy()
    matname = np.array(names + ["none"])[np.where(mid >= 0, mid, len(names))]
    depth = dist * (d @ fwd)
    fogf = np.exp(-(DENS * depth) ** 2)
    is_w = hit & (matname == "Proof_Water")
    brz = np.zeros((h, w))
    # lit surfaces
    solid = hit & ~is_w
    alb = np.zeros((h, w, 3)); emi = np.zeros((h, w, 3)); is_e = np.zeros((h, w), bool)
    for n_ in set(matname[solid].tolist()):
        m = solid & (matname == n_)
        if n_ in EMIT: emi[m] = lin(EMIT[n_]) * 1.4; is_e |= m
        else: alb[m] = lin(PAL.get(n_, (.2, .2, .2)))
    ndl = np.clip((nrm * MOON_L).sum(-1), 0, 1)
    shade = alb * (AMB + MOONI * ndl)[..., None]
    for B in BRAZIERS:
        L = B[None, None] - pos; dl = np.linalg.norm(L, axis=-1); L /= np.maximum(dl, 1e-4)[..., None]
        att = 1 / (1 + 25 * (dl / BR) ** 2) * np.clip(1 - (dl / BR) ** 4, 0, 1)
        k = BI * att * np.clip((nrm * L).sum(-1), 0, 1)
        brz += k
        shade += alb * BCOL[None, None] * k[..., None]
    col = np.where(is_e[..., None], emi, shade)
    col = col * fogf[..., None] + FOG[None, None] * (1 - fogf[..., None])
    out[solid] = col[solid]
    if is_w.any():
        wc = water(pos[is_w].astype(np.float64), eye, fwd)
        fw = fogf[is_w][:, None]
        fogged = wc * fw + FOG[None] * (1 - fw)
        out[is_w] = wc * (1 - FOGKEEP) + fogged * FOGKEEP
    disp = np.clip(srgb(np.clip(out, 0, None)), 0, 1)
    u8 = (disp * 255 + .5).astype(np.uint8)
    Image.fromarray(u8).save(OUT / (name + ".png"))
    sc = (W / w) * (H / h)
    below = is_w & (el < 0)
    m = {}
    if below.any():
        R, G = u8[below][:, 0].astype(float), u8[below][:, 1].astype(float)
        luma = .2126 * u8[..., 0] + .7152 * u8[..., 1] + .0722 * u8[..., 2]
        m.update(moat_px=int(below.sum() * sc), moat_RG=round(R.mean() / max(G.mean(), 1e-3), 2), moat_mean=[round(float(u8[below][:, i].mean()), 1) for i in range(3)],
                 glade_px2=int(((luma >= 150) & below).sum() * sc))
    if below.any():                                            # the water mask (below the horizon) for tools/fd_art_moat_test.py
        Image.fromarray((below * 255).astype(np.uint8)).save(OUT / (name + "_water_mask.png"))
    farbank = solid & (pos[..., 2] < -16)
    m["far_bank_px"] = int(farbank.sum() * sc); m["far_bank_lit_px"] = int((farbank & (brz * 255 * .2 >= 8)).sum() * sc)
    metrics[name] = m
    print("HC_FD_SHADE", name, m)
(OUT / "metrics.json").write_text(json.dumps(metrics, indent=1) + "\n")
