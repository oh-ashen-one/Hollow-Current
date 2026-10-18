#!/usr/bin/env python3
"""Castle exposure model and lantern-reach diagnostic (fc-light, 2026-10-18). Needs numpy + Pillow; no Unity.

    python3 tools/hall_light_model.py reach <dir_off> <dir_on> [--angle 100]
        Per view: % of the frame the lantern brightens (on - off luma > 2) inside the cone (angle from the view axis < angle/2),
        and the near-black % left outside the cone. Before fc-light (Auto torch, 80 deg) the lantern reached 4-37 % of the in-cone
        pixels (fb2 --angle 80: S1 33 %, S2 15 %, skeleton views 4-16 %, chapel 11-37 %): it skipped the hall floor, skeleton, chapel
        floor, pews and altar. With
        TorchForcePixel on, most in-cone surfaces should be reached (expect > 60 %); if not, the pixel-light hypothesis was wrong.

    python3 tools/hall_light_model.py predict <capture_root> [--mist .5] [--alpha .12] [--angle 100]
        <capture_root> holds rooms_off, rooms_on, layout_off, layout_on captured on the wave FB look (hall mist (.12,.15,.21)
        at alpha .15, Auto torch at 80 deg), e.g. docs/haunted-castle/rounds/r13/captures/fb2. Predicts lantern-off and lantern-on
        near-black % (luma < 8, tools/exposure_report.py) for a hall mist brightness (linear scale of the colour), hall mist alpha,
        and a ForcePixel lantern of the given cone angle.

Model, and its limits:
  * Hall mist (CastleAtmosphere ground fog, area 0): 8 unlit alpha-blended planes 0.08-1.1 m over x -6..6, z 14..28. Expected
    coverage per pixel is ray-traced from the capture eye/target (fov 72) to the floor using the shader's alpha ramp, plane-edge
    and camera-fade smoothsteps, and the measured noise mean (.748 on Castle_FogNoise alpha). The coverage is calibrated per view
    (kappa = median observed/predicted on covered pixels; reported as kappa x0.7 .. x1.0 because objects standing in the mist cut
    the ray short). The new frame = old + (new mist - old mist) in linear light, vignette included.
  * Lantern: pixels already brightened by the Auto torch stay as captured; every other near-black pixel inside the new cone becomes
    lit with probability p (p x0.6 in the outer 10 % of the cone), outside the cone nothing changes. p is the unknown: .6 .. .85
    brackets it (on surfaces the torch did reach, 93-100 % ended at luma >= 8).
  * Realtime candles, the bake and reflection probes are NOT modelled; they are taken as captured.
"""
import json, sys
from pathlib import Path
import numpy as np
from PIL import Image

W, H, FOV = 1920, 1080, 72.0
NOISE = .748
MIST_OLD = np.array([.12, .15, .21]); ALPHA_OLD = .15
HALL_PREFIX = ("S1", "art_layout_hall")

def srgb2lin(x):
    x = x / 255.0
    return np.where(x <= .04045, x / 12.92, ((x + .055) / 1.055) ** 2.4)

def lin2srgb(x):
    x = np.clip(x, 0, 1)
    return 255 * np.where(x <= .0031308, x * 12.92, 1.055 * x ** (1 / 2.4) - .055)

def luma(a): return .299 * a[..., 0] + .587 * a[..., 1] + .114 * a[..., 2]
def load(p): return np.asarray(Image.open(p).convert("RGB"), dtype=np.float64)

def rays(eye, target):
    eye = np.array(eye); f = np.array(target) - eye; f /= np.linalg.norm(f)
    r = np.cross([0, 1, 0], f); r /= np.linalg.norm(r); u = np.cross(f, r)
    t = np.tan(np.radians(FOV / 2)); ys, xs = np.mgrid[0:H, 0:W]
    sx = ((xs + .5) / W * 2 - 1) * t * W / H; sy = (1 - (ys + .5) / H * 2) * t
    d = f[None, None, :] + sx[..., None] * r + sy[..., None] * u; n = np.linalg.norm(d, axis=-1)
    return eye, d / n[..., None], np.degrees(np.arccos(1 / n))

def vignette(strength=.30):
    ys, xs = np.mgrid[0:H, 0:W]; u = (xs + .5) / W - .5; v = (ys + .5) / H - .5
    r = np.sqrt(u * u + v * v) / .7071; k = np.clip((r - .35) / .16, 0, 5); k0 = np.floor(np.minimum(k, 4.999)); fr = k - k0
    a = strength * ((k0 / 5) ** 2.2 * (1 - fr) + ((k0 + 1) / 5) ** 2.2 * fr)
    return np.where(r < .35, 0, a)[..., None]

def ss(e0, e1, x):
    x = np.clip((x - e0) / (e1 - e0), 0, 1); return x * x * (3 - 2 * x)

def mist_coverage(eye, d, alpha, layers=8, height=1.1, centre=(0, 0, 21), size=(12, 14)):
    T = np.ones(d.shape[:2]); dy = d[..., 1]
    tfloor = np.where(dy < -1e-4, (0 - eye[1]) / np.minimum(dy, -1e-4), np.inf)
    for i in range(layers):
        tt = i / max(1, layers - 1); y = centre[1] + .08 + (height - .08) * tt; a = alpha * (1.4 + (.5 - 1.4) * tt)
        with np.errstate(divide="ignore", invalid="ignore"): t = (y - eye[1]) / dy
        ok = (t > 0) & (t < tfloor) & np.isfinite(t)
        px = eye[0] + d[..., 0] * t; pz = eye[2] + d[..., 2] * t
        uu = (px - centre[0]) / size[0] + .5; vv = (pz - centre[2]) / size[1] + .5
        e = ss(0, .32, uu) * ss(0, .32, 1 - uu) * ss(0, .32, vv) * ss(0, .32, 1 - vv)
        f = ss(.9, 2.8, np.where(ok, t, 0))
        T *= 1 - np.where(ok & (uu > 0) & (uu < 1) & (vv > 0) & (vv < 1), a * NOISE * e * f, 0)
    return (1 - T)[..., None]

def poses(root):
    out = {}
    for js in ("rooms_off/capture.json", "layout_off/capture_art_layout.json"):
        p = Path(root) / js
        if p.exists():
            for e in json.loads(p.read_text())["entries"]:
                if "eye" in e: out[e["id"]] = ([float(x) for x in e["eye"].split(",")], [float(x) for x in e["target"].split(",")])
    return out

def mist_swap(img, eye, target, mist, alpha, kfac):
    """img (8-bit RGB, captured with the old mist) -> linear frame with the hall mist changed to (mist x linear colour, alpha)."""
    v = vignette(); lin = srgb2lin(img); _, d, _ = rays(eye, target)
    Fo = srgb2lin(MIST_OLD * 255); Fy = luma(Fo[None, None, :])
    c_old = mist_coverage(eye, d, ALPHA_OLD); c_new = mist_coverage(eye, d, alpha)
    warm = (img[..., 0] >= img[..., 2] + 2)[..., None]          # bones, wood, candles: stand inside the mist, so less of it in front
    c_old = np.where(warm, c_old * .5, c_old); c_new = np.where(warm, c_new * .5, c_new)
    m = c_old[..., 0] > .15
    kappa = float(np.median(luma(lin)[m] / np.maximum(c_old[..., 0][m] * Fy * (1 - v[..., 0][m]), 1e-6))) if m.any() else 0
    co = np.minimum(kappa * kfac * c_old, .95); cn = np.minimum(kappa * kfac * c_new, .95)
    S = np.maximum(0, (lin / (1 - v) - co * Fo) / (1 - co))
    return lin + ((cn * mist - co) * Fo + S * (co - cn)) * (1 - v)

def dark(lin): return float((luma(lin2srgb(lin)) < 8).mean() * 100)

def predict(root, mist, alpha, angle):
    P = poses(root)
    print("hall mist x%.2f (linear) alpha %.3f; ForcePixel lantern %g deg; p %.2f..%.2f" % (mist, alpha, angle, .6, .85))
    print("%-36s %22s %26s" % ("view", "off now -> pred", "on now -> pred"))
    for vid, (eye, target) in P.items():
        sub = "rooms_" if vid.startswith("S") else "layout_"
        fo, fn = Path(root) / (sub + "off") / (vid + ".png"), Path(root) / (sub + "on") / (vid + ".png")
        if not fo.exists() or not fn.exists() or vid.startswith(("S4", "S5", "S6", "S7", "S8")): continue
        off, on = load(fo), load(fn); hall = vid.startswith(HALL_PREFIX)
        offs = [mist_swap(off, eye, target, mist, alpha, k) for k in (.7, 1)] if hall else [srgb2lin(off)] * 2
        onl = mist_swap(on, eye, target, mist, alpha, .85) if hall else srgb2lin(on)
        _, _, th = rays(eye, target); recv = (luma(on) - luma(off)) > 2; dk = luma(lin2srgb(onl)) < 8
        ons = []
        for pp in (.85, .6):
            pl = np.where(th < angle / 2 * .9, pp, np.where(th < angle / 2, pp * .6, 0.)); pl = np.where(recv, 0, pl)
            ons.append(float((dk * (1 - pl)).mean() * 100))
        print("%-36s %5.1f -> %4.1f..%4.1f %10.1f -> %4.1f..%4.1f" % (vid, dark(srgb2lin(off)), min(map(dark, offs)), max(map(dark, offs)), dark(srgb2lin(on)), ons[0], ons[1]))

def reach(doff, don, angle):
    P = {}
    for js in list(Path(doff).glob("*.json")):
        for e in json.loads(js.read_text()).get("entries", []):
            if "eye" in e: P[e["id"]] = ([float(x) for x in e["eye"].split(",")], [float(x) for x in e["target"].split(",")])
    print("%-36s %8s %14s %18s" % ("view", "on dark", "in-cone reach", "dark outside cone"))
    for vid, (eye, target) in P.items():
        fo, fn = Path(doff) / (vid + ".png"), Path(don) / (vid + ".png")
        if not fo.exists() or not fn.exists(): continue
        lo, ln = luma(load(fo)), luma(load(fn)); _, _, th = rays(eye, target); cone = th < angle / 2
        print("%-36s %7.1f%% %13.1f%% %17.1f%%" % (vid, (ln < 8).mean() * 100, ((ln - lo) > 2)[cone].mean() * 100, ((ln < 8) & ~cone).mean() * 100))

def arg(name, default):
    return float(sys.argv[sys.argv.index(name) + 1]) if name in sys.argv else default

if __name__ == "__main__":
    a = [x for i, x in enumerate(sys.argv[1:], 1) if not x.startswith("--") and not sys.argv[i - 1].startswith("--")]
    if len(a) >= 3 and a[0] == "reach": reach(a[1], a[2], arg("--angle", 100)); sys.exit(0)
    if len(a) >= 2 and a[0] == "predict": predict(a[1], arg("--mist", .5), arg("--alpha", .12), arg("--angle", 100)); sys.exit(0)
    print(__doc__); sys.exit(2)
