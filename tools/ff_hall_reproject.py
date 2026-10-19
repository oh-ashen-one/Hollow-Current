#!/usr/bin/env python3
"""Wave FF (hc/ff-art) offline S1 estimate: re-projects a real S1 capture onto a new S1 camera / layout through the BVH proxies of
tools/ff_hall_raycast.py, so the new framing's exposure, table legibility and test masks can be judged before a Unity build.

    python3 tools/ff_hall_reproject.py OLD_CLASS.npz OLD_FRAME.png NEW_CLASS.npz OUT.png [--emit lancet_S=40,58,80 --emit lancet_N=...]
        [--sheen SHEEN.npy] [--json out.json]

For every pixel of the new view the proxy gives a surface point P. If the old capture saw the same point (projected into the old camera,
proxy depth there within 4 % + 5 cm), its colour is copied; otherwise the pixel takes its class's median colour in the old frame, or for a
class the old frame never showed, the --emit colour (new emissive glass; display sRGB) or black. Additive light from the new floor/table
sheen can be supplied as an .npy of display-space RGB added before clamping. Prints the near-black share (Rec.601 luma < 8) of the estimate
next to the old frame's. It is an estimate: view-dependent light (the sheen, flames, specular) is copied from the old angle.
"""
import json, math, sys
from pathlib import Path
import numpy as np
from PIL import Image

def cam(eye, fwd, W=1920, H=1080, fov=72.0):
    f = np.asarray(fwd, float); f /= np.linalg.norm(f)
    r = np.cross([0, 1, 0], f); r /= np.linalg.norm(r); u = np.cross(f, r)
    return np.asarray(eye, float), f, r, u, math.tan(math.radians(fov) / 2), W / H

def project(P, c, W=1920, H=1080):
    e, f, r, u, th, asp = c
    v = P - e; z = v @ f
    x = ((v @ r) / np.maximum(z, 1e-6) / (asp * th) * .5 + .5) * W
    y = (.5 - (v @ u) / np.maximum(z, 1e-6) / th * .5) * H
    return x, y, z

def main():
    a = [x for x in sys.argv[1:] if not x.startswith("--")]
    oldz, oldf, newz, out = np.load(a[0]), a[1], np.load(a[2]), Path(a[3])
    emit = {}
    force = {}
    for i, x in enumerate(sys.argv):
        if x == "--emit":
            k, v = sys.argv[i + 1].split("="); emit[k] = np.array([float(t) for t in v.split(",")])
        if x == "--force":                                   # classes that always take this colour (new unlit glass / sky card)
            k, v = sys.argv[i + 1].split("="); force[k] = np.array([float(t) for t in v.split(",")])
    old = np.asarray(Image.open(oldf).convert("RGB")).astype(np.float64)
    H, W = old.shape[:2]
    ocid, onames = oldz["cid"], list(oldz["names"]); ncid, nnames = newz["cid"], list(newz["names"])
    assert ocid.shape == (H, W) and ncid.shape == (H, W), "run tools/ff_hall_raycast.py at --scale 1.0"
    oc = cam(oldz["eye"], oldz["fwd"]); od = oldz["depth"]
    P = newz["hit"].reshape(-1, 3).astype(np.float64); valid = (ncid.reshape(-1) >= 0)
    x, y, z = project(P, oc)
    xi, yi = np.round(x - .5).astype(int), np.round(y - .5).astype(int)
    inb = valid & (xi >= 0) & (xi < W) & (yi >= 0) & (yi < H) & (z > .05)
    dist = np.linalg.norm(P - oc[0][None], axis=1)
    seen = np.zeros(len(P), bool)
    seen[inb] = np.abs(od[yi[inb], xi[inb]] - dist[inb]) < dist[inb] * .04 + .05
    est = np.zeros((len(P), 3))
    est[seen] = old[yi[seen], xi[seen]]
    # class medians of the old frame
    med = {}
    for c, n in enumerate(onames):
        m = ocid == c
        if m.sum() > 50: med[n] = np.median(old[m], axis=0)
    nc = ncid.reshape(-1)
    fill = valid & ~seen
    for c, n in enumerate(nnames):
        m = fill & (nc == c)
        if not m.any(): continue
        key = next((k for k in emit if n.startswith(k)), None)
        if key: est[m] = emit[key]
        elif n in med: est[m] = med[n]
        else: est[m] = 0
    # --mist B: re-mist the hall ground fog (CastleAtmosphere: 8 alpha-blended layers 0.08-1.1 m over x +-6, z 14-28, colour (.12,.15,.21)
    # linear x HallMistBrightness, per-layer alpha .16 x lerp(1.4,.5,t) x noise (mean .5) x uv edge falloff x camera fade .9-2.8 m). The r14
    # frame was captured at B0 = .375. For every pixel whose surface point lies under the top layer inside the area, the layers above it are
    # replayed: linear c_new = c_old + A * (m(B) - m(B0)), floored at c_old * B / B0 (a point lit only by the mist scales with B).
    if "--mist" in sys.argv:
        B = float(sys.argv[sys.argv.index("--mist") + 1]); B0 = float(sys.argv[sys.argv.index("--mist0") + 1]) if "--mist0" in sys.argv else .375
        def lin(c): c = c / 255.0; return np.where(c <= .04045, c / 12.92, ((c + .055) / 1.055) ** 2.4)
        def srgb(c): c = np.clip(c, 0, None); return np.where(c <= .0031308, c * 12.92, 1.055 * np.maximum(c, 1e-9) ** (1 / 2.4) - .055) * 255
        base = lin(np.array([.12, .15, .21]) * 255)
        e0 = np.asarray(newz["eye"], float)
        dcam = np.linalg.norm(P - e0[None], axis=1)
        def sm(a_, b_, x_): t = np.clip((x_ - a_) / (b_ - a_), 0, 1); return t * t * (3 - 2 * t)
        ux, uz = (P[:, 0] + 6) / 12, (P[:, 2] - 14) / 14
        edge = sm(0, .32, ux) * sm(0, .32, 1 - ux) * sm(0, .32, uz) * sm(0, .32, 1 - uz)
        fade = sm(.9, 2.8, dcam)
        T = np.ones(len(P))
        for i in range(8):
            t = i / 7; hgt = .08 + (1.1 - .08) * t
            a_i = .16 * (1.4 + (.5 - 1.4) * t) * .5
            under = valid & (P[:, 1] < hgt) & (ux > 0) & (ux < 1) & (uz > 0) & (uz < 1)
            T = np.where(under, T * (1 - a_i * edge * fade), T)
        A = 1 - T
        cl = lin(est)
        new = np.maximum(cl + A[:, None] * base[None] * (B - B0), cl * min(1.0, B / B0))
        new = np.where((A > 0)[:, None], new, cl)
        est = srgb(new)
    for c, n in enumerate(nnames):
        key = next((k for k in force if n.startswith(k)), None)
        if key: est[valid & (nc == c)] = force[key]
    # --moons: the two WindowMoon discs (fixed directions from the S1 eye through the north lancets, radius .5 m on the plane z 28.85),
    # painted where the pixel sees a sky panel (the night-sky card behind the openings)
    if "--moons" in sys.argv:
        e1 = np.array([0, 2.95, 14.55]); eye = np.asarray(newz["eye"], float)
        D = P - eye[None]; D /= np.linalg.norm(D, axis=1, keepdims=True)
        tz = (28.85 - eye[2]) / np.maximum(D[:, 2], 1e-6); Q = eye[None] + D * tz[:, None]
        skyc = np.zeros(len(P), bool)
        for c, n in enumerate(nnames):
            if n.startswith("sky_"): skyc |= nc == c
        for sx, col in ((-2.6, (226, 228, 236)), (2.6, (205, 52, 44))):
            d = np.array([sx, 5.6, 28.0]) - e1; d /= np.linalg.norm(d)
            ctr = eye + d * ((28.85 - eye[2]) / d[2])
            disc = skyc & (np.linalg.norm(Q - ctr[None], axis=1) < .5)
            est[disc] = col
    # pixels that hit nothing: old frame's "none" median (black)
    if "--sheen" in sys.argv:
        est += np.load(sys.argv[sys.argv.index("--sheen") + 1]).reshape(-1, 3)
    if "--sheen-lin" in sys.argv:                          # additive light in linear RGB (tools/ff_sheen_twin.py)
        def lin_(c): c = np.clip(c, 0, 255) / 255.0; return np.where(c <= .04045, c / 12.92, ((c + .055) / 1.055) ** 2.4)
        def srgb_(c): c = np.clip(c, 0, None); return np.where(c <= .0031308, c * 12.92, 1.055 * np.maximum(c, 1e-9) ** (1 / 2.4) - .055) * 255
        est = srgb_(np.clip(lin_(est) + np.load(sys.argv[sys.argv.index("--sheen-lin") + 1]).reshape(-1, 3), 0, None))
    img = np.clip(est, 0, 255).reshape(H, W, 3).astype(np.uint8)
    Image.fromarray(img).save(out)
    Y = img.astype(float) @ [.299, .587, .114]; Yo = old @ [.299, .587, .114]
    res = dict(near_black_estimate=round(float((Y < 8).mean() * 100), 2), near_black_old=round(float((Yo < 8).mean() * 100), 2),
               reprojected_share=round(float(seen.sum() / max(valid.sum(), 1) * 100), 1))
    print("HC_FF_REPROJECT", json.dumps(res))
    if "--json" in sys.argv: Path(sys.argv[sys.argv.index("--json") + 1]).write_text(json.dumps(res, indent=1))

if __name__ == "__main__":
    main()
