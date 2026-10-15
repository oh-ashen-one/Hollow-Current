#!/usr/bin/env python3
"""Dark basalt for the exterior rock outcrop (wave L, hc/exterior-l). Seamless 512x512 PBR set, deterministic, numpy + PIL:

    python3 tools/castle_exterior_basalt.py

Writes Resources/Castle/Textures/Castle_Basalt_{albedo,normal,mask}.png (+ meta). Used by CastleArt.Surface("Castle_Basalt", World, tint):
the same triplanar shader as the stone, at 0.3 tiles per metre. Columnar jointing (periodic Voronoi cracks), faint horizontal strata,
mottled blue-black grain, a few pale weathered scuffs. Mask: R metallic 0, G occlusion (cracks dark), A smoothness (damp patches glossier).
"""
import sys
from pathlib import Path
import numpy as np
from PIL import Image
sys.path.insert(0, str(Path(__file__).resolve().parent))
import castle_common as cc

N = 512
rng = np.random.default_rng(20261016)

def periodic_noise(octaves, seed, base=4):
    r = np.random.default_rng(seed); out = np.zeros((N, N), np.float32); amp = 1.0; tot = 0
    fy = np.fft.fftfreq(N)[:, None]; fx = np.fft.fftfreq(N)[None, :]
    rad = np.sqrt(fx * fx + fy * fy) + 1e-6
    for o in range(octaves):
        white = r.normal(size=(N, N)).astype(np.float32)
        f0 = base * (2 ** o) / N
        filt = np.exp(-(rad / f0) ** 2) * (rad / f0)       # band-pass around f0
        n = np.real(np.fft.ifft2(np.fft.fft2(white) * filt)).astype(np.float32)
        n = (n - n.mean()) / (n.std() + 1e-6)
        out += amp * n; tot += amp; amp *= .55
    out /= tot
    return out

def voronoi_edges(cells, seed, warp, jitter=.48):
    r = np.random.default_rng(seed)
    pts = (np.stack(np.meshgrid(np.arange(cells), np.arange(cells), indexing="ij"), -1) + .5 + (r.random((cells, cells, 2)) - .5) * 2 * jitter) / cells
    ys, xs = np.mgrid[0:N, 0:N]; P = np.stack([ys, xs], -1).astype(np.float32) / N
    P = P + np.stack([warp[0], warp[1]], -1) * .045                 # wobble the joints so they are not a cobble grid
    d1 = np.full((N, N), 9.0, np.float32); d2 = d1.copy()
    # brute force over all points with wrap offsets (81 points x 9 images)
    flat = pts.reshape(-1, 2)
    for oy in (-1, 0, 1):
        for ox in (-1, 0, 1):
            for p in flat:
                q = p + np.array([oy, ox])
                d = np.sqrt(((P - q) ** 2).sum(-1))
                m1 = d < d1
                d2 = np.where(m1, d1, np.minimum(d2, d)); d1 = np.where(m1, d, d1)
    return d2 - d1, d1

def main():
    big = periodic_noise(5, 1, 3); mid = periodic_noise(4, 2, 8); fine = periodic_noise(3, 3, 40)
    edge, d1 = voronoi_edges(6, 7, (periodic_noise(3, 21, 3), periodic_noise(3, 22, 3)))
    crack = .8 * np.clip(1 - edge * 30.0, 0, 1) ** 1.8                  # thin dark joints between the columns
    cell_tone = periodic_noise(1, 11, 9)                           # per-column tone drift
    strata = .5 + .5 * np.sin(np.linspace(0, 2 * np.pi * 5, N)[:, None] + big * 2.5)
    h = .55 * mid + .25 * fine + .35 * big - .9 * crack + .12 * strata
    base = np.array([.155, .165, .205], np.float32)
    tone = 1 + .38 * big + .22 * mid + .12 * fine + .16 * cell_tone
    tone = tone * (1 - .45 * crack) * (1 + .08 * (strata - .5))
    scuff = np.clip((fine - 1.7) * 1.2, 0, 1) * (1 - crack)         # a few pale weathered scuffs
    alb = base[None, None, :] * tone[..., None] + scuff[..., None] * np.array([.10, .10, .11], np.float32)
    alb = np.clip(alb, 0, 1)
    gy, gx = np.gradient(h * 3.0)
    nz = np.ones_like(h); nl = np.sqrt(gx * gx + gy * gy + nz * nz)
    nrm = np.stack([-gx / nl, gy / nl, nz / nl], -1) * .5 + .5
    occ = np.clip(1 - .75 * crack - .12 * np.clip(-mid, 0, 1), 0, 1)
    smooth = np.clip(.22 + .10 * (big > .6) + .20 * (crack * 0) + .03 * fine, 0, 1)
    mask = np.stack([np.zeros_like(h), occ, np.zeros_like(h), smooth], -1)
    outd = cc.TEX
    for nm, arr, mode, kind in (("albedo", alb, "RGB", "color"), ("normal", nrm, "RGB", "normal"), ("mask", mask, "RGBA", "mask")):
        p = outd / ("Castle_Basalt_%s.png" % nm)
        Image.fromarray((np.clip(arr, 0, 1) * 255 + .5).astype(np.uint8), mode).save(p, optimize=True)
        cc.write_texture_meta(p, kind)
        print("wrote", p.name, p.stat().st_size // 1024, "KB")
    print("CASTLE_BASALT_PASS")

if __name__ == "__main__":
    main()
