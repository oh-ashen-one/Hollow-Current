#!/usr/bin/env python3
"""System-python half of the exterior proofs: composites the Blender Workbench layers (tools/castle_exterior_proof.py) over the
real SkyCastle equirect by camera direction and writes docs/haunted-castle/rounds/r12/art/exteriorL_proof_<view>.png. Four layers per view:
back (ridge ring), scene (lit stone), mask (windows magenta) and glow (window texture); the windows get the engine's maths (sRGB texel to linear,
times CastleWindow _Intensity 1.5, clamp, back to sRGB).

    python3 tools/castle_exterior_composite.py [DIR]      (default docs/haunted-castle/rounds/r12/art)
"""
import json, math, sys
from pathlib import Path
import numpy as np, cv2
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
out = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "docs/haunted-castle/rounds/r12/art"
sky = np.asarray(Image.open(ROOT / "docs/haunted-castle/art/sky_preview.jpg").convert("RGB")).astype(np.float32) / 255.0
def load(p): return np.asarray(Image.open(p).convert("RGBA")).astype(np.float32) / 255.0
def lin(c): return np.where(c <= .04045, c / 12.92, ((c + .055) / 1.055) ** 2.4)
def srgb(c): return np.where(c <= .0031308, c * 12.92, 1.055 * np.maximum(c, 1e-9) ** (1 / 2.4) - .055)
def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t)

# ---- numpy twin of Resources/Castle/Shaders/CastleWater.shader (same constants; _Time.y fixed at 3 s) ----
WAVES = [((.80, .60), 3.1, 1.1, .030), ((-.50, .87), 4.7, 1.6, .022), ((.20, -.98), 7.3, 2.1, .012), ((-.90, -.40), 11.0, 2.7, .007), ((.60, -.80), 2.1, .7, .040)]
RIPPLE = .55; DEEP = np.array([.016, .0018, .004]); CASTLE = np.array([.012, .016, .030]); REFLECT = 2.2
M_PALE = np.array([-.3207, .8192, .4755]); M_RED = np.array([.4973, .6691, .5522])
def water_shade(eye, d, fwd, T=3.0):
    """d: (n,3) unit view rays of the water pixels. Returns display RGB (n,3)."""
    t = (-.55 - eye[1]) / d[:, 1]
    P = eye[None, :] + d * t[:, None]
    g = np.zeros((len(d), 2))
    for (dx, dz), k, w, a in WAVES:
        c = a * k * np.cos(k * (dx * P[:, 0] + dz * P[:, 2]) + w * T)
        g[:, 0] += c * dx; g[:, 1] += c * dz
    g *= RIPPLE
    N = np.stack([-g[:, 0], np.ones(len(d)), -g[:, 1]], -1); N /= np.linalg.norm(N, axis=1, keepdims=True)
    V = -d
    R = 2 * (N * V).sum(1, keepdims=True) * N - V; R[:, 1] = np.abs(R[:, 1])        # reflect(-V, N) = 2 (N.V) N - V
    ndv = np.clip((N * V).sum(1), 0, 1); F = .03 + .97 * (1 - ndv) ** 5
    el = R[:, 1]
    hor = np.array([.0033, .0063, .0203]); zen = np.array([.00001, .00003, .00033])
    c = hor[None] * (1 - smooth(0, .7, el)[:, None]) + zen[None] * smooth(0, .7, el)[:, None]
    a1 = np.arccos(np.clip(R @ M_PALE, -1, 1)); a2 = np.arccos(np.clip(R @ M_RED, -1, 1))
    c += np.array([.50, .57, .70])[None] * smooth(.215, .185, a1)[:, None] * .9 + np.array([.05, .085, .26])[None] * (np.exp(-a1 / .16) * .9)[:, None]
    c += np.array([.42, .010, .004])[None] * smooth(.085, .065, a2)[:, None] * .9 + np.array([.20, .008, .008])[None] * (np.exp(-a2 / .07) * .8)[:, None]
    az = np.arctan2(R[:, 0], R[:, 2])
    center = np.radians(31) + np.radians(9) * np.sin(az * 2.3 + .7) + np.radians(2.5) * np.sin(az * 7.0)
    dz_ = (el - center) / np.radians(8)
    win = smooth(np.radians(-18), np.radians(10), az) * (1 - smooth(np.radians(62), np.radians(100), az))
    c += np.array([.006, .86, .23])[None] * (np.exp(-dz_ * dz_ * 1.2) * win * (.5 + .5 * np.sin(az * 5 + 1.3)) * .16)[:, None]
    def hit(mn, mx):
        inv = 1.0 / (R + 1e-5); t0 = (np.array(mn) - P) * inv; t1 = (np.array(mx) - P) * inv
        tn = np.minimum(t0, t1).max(1); tf = np.maximum(t0, t1).min(1)
        return tf > np.maximum(tn, 0)
    hb = hit((-14.45, -3.2, -.45), (14.45, 7.2, 28.45)) | hit((-6, -3.2, 14), (6, 9.2, 28.45)) | hit((-.5, -3.2, 17.6), (5.1, 32, 23.2))
    c[hb] = CASTLE
    col = DEEP[None] * (1 - F * .6)[:, None] + c * np.array([1.0, .86, .82])[None] * (F * REFLECT)[:, None]
    depth = t * (d @ fwd)
    f = np.exp(-(.05 * depth) ** 2)[:, None]
    fog = lin(np.array([.30, .37, .52]))
    col = fog[None] * (1 - f) + col * f
    return srgb(np.clip(col, 0, 1))
for v in json.loads((out / "_raw/views.json").read_text()):
    W, H = v["size"]; f = np.array(v["dir"]); fov = v["fov"]
    r = np.cross([0, 1, 0], f); r /= np.linalg.norm(r); u = np.cross(f, r)
    ys, xs = np.mgrid[0:H, 0:W].astype(np.float32)
    th = math.tan(math.radians(fov) / 2); asp = W / H
    nx = ((xs + .5) / W * 2 - 1) * asp * th; ny = (1 - (ys + .5) / H * 2) * th
    d = f[None, None, :] + nx[..., None] * r[None, None, :] + ny[..., None] * u[None, None, :]
    d /= np.linalg.norm(d, axis=-1, keepdims=True)
    az = np.arctan2(d[..., 0], d[..., 2]); el = np.arcsin(np.clip(d[..., 1], -1, 1))
    su = ((az + math.pi) / (2 * math.pi) * (sky.shape[1] - 1)).astype(np.float32)
    sv = ((math.pi / 2 - el) / math.pi * (sky.shape[0] - 1)).astype(np.float32)
    img = cv2.remap(sky, su, sv, cv2.INTER_LINEAR, borderMode=cv2.BORDER_WRAP)
    for tag in ("back", "scene"):
        L = load(out / "_raw" / ("%s_%s.png" % (v["name"], tag))); a = L[..., 3:4]
        img = L[..., :3] * a + img * (1 - a)
    mask = load(out / "_raw" / ("%s_mask.png" % v["name"]))[..., :3]
    wm = np.clip(mask[..., 1] - .5 * (mask[..., 0] + mask[..., 2]), 0, 1)
    if (wm > .01).any():
        sel = wm > .01
        dsel = d[sel]
        wcol = water_shade(np.array(v["eye"]), dsel, f)
        img[sel] = img[sel] * (1 - wm[sel][:, None]) + wcol * wm[sel][:, None]
    m = np.clip(np.minimum(mask[..., 0], mask[..., 2]) - mask[..., 1] * .5, 0, 1)[..., None]
    tex = load(out / "_raw" / ("%s_glow.png" % v["name"]))[..., :3]
    lin_t = np.where(tex <= .04045, tex / 12.92, ((tex + .055) / 1.055) ** 2.4) * 1.5
    lin_t = np.clip(lin_t, 0, 1)
    glow = np.where(lin_t <= .0031308, lin_t * 12.92, 1.055 * lin_t ** (1 / 2.4) - .055)
    img = img * (1 - m) + glow * m
    im = Image.fromarray((np.clip(img, 0, 1) * 255 + .5).astype(np.uint8))
    if W <= 1920: im = im.resize((1280, int(round(1280 * H / W))), Image.LANCZOS)    # the S4 frame stays at capture size (2416x1292)
    im.save(out / ("exteriorL_proof_%s.png" % v["name"]), optimize=True)
    print("wrote", "exteriorL_proof_%s.png" % v["name"], "mean", round(float(np.asarray(im.convert("L")).mean()), 1))
