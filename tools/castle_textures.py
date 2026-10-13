#!/usr/bin/env python3
"""Procedural tileable PBR textures + Unity .mat files for the Haunted Castle.

    python3 tools/castle_textures.py

Writes Assets/HollowCurrent/Resources/Castle/Textures/<Mat>_albedo.png / _normal.png (+ .meta),
Materials/<Mat>.mat (+ .meta) and Textures/Portrait.png. Deterministic (fixed seeds), idempotent,
no downloads. All tiles are periodic (FFT noise / wrapped rolls) so they tile seamlessly.
"""
import sys
from pathlib import Path
import numpy as np
from PIL import Image
sys.path.insert(0, str(Path(__file__).resolve().parent))
import castle_common as cc

def noise(n, beta, rng):
    """Periodic 1/f^beta noise in [0,1]."""
    w = rng.standard_normal((n, n))
    f = np.fft.fft2(w)
    fy = np.fft.fftfreq(n)[:, None]; fx = np.fft.fftfreq(n)[None, :]
    r = np.sqrt(fx * fx + fy * fy); r[0, 0] = 1
    f *= 1.0 / r ** beta; f[0, 0] = 0
    a = np.real(np.fft.ifft2(f))
    a = (a - a.min()) / (a.max() - a.min() + 1e-9)
    return a

def fbm(n, rng, octs=(1.2, 1.8, 2.4), wts=(1, .5, .25)):
    s = sum(w * noise(n, b, rng) for b, w in zip(octs, wts))
    return (s - s.min()) / (s.max() - s.min() + 1e-9)

def to_normal(h, strength):
    gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5
    gy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 0.5
    nx, ny, nz = -gx * strength, gy * strength, np.ones_like(h)   # +Y up (OpenGL / Unity)
    l = np.sqrt(nx * nx + ny * ny + nz * nz)
    return np.stack([nx / l, ny / l, nz / l], -1) * 0.5 + 0.5

def tint(h, c):  # h in [0,1] -> rgb around colour c
    c = np.array(c)[None, None, :]
    return c * (0.55 + 0.9 * h[..., None])

def ashlar(n, rng, rows=4):
    """Block coursing: returns height (grooves), per-block tone, mortar mask. Tile = 2 m x 2 m."""
    h = np.ones((n, n)); tone = np.zeros((n, n)); mortar = np.zeros((n, n))
    rh = n // rows
    for r in range(rows):
        cols = rng.choice([2, 2, 3]) if r % 2 == 0 else rng.choice([2, 3])
        # split row into `cols` blocks of uneven widths, wrap-around consistent
        cuts = np.sort(rng.random(cols)); widths = (cuts / cuts.sum())
        edges = np.concatenate([[0], np.cumsum(widths)]) * n
        off = int(rng.integers(0, n))
        for i in range(cols):
            x0, x1 = int(edges[i]) + off, int(edges[i + 1]) + off
            t = rng.random()
            xs = np.arange(x0, x1) % n
            tone[r * rh:(r + 1) * rh, xs] = t
            g = 3
            mortar[r * rh:(r + 1) * rh, np.arange(x0, x0 + g) % n] = 1
            mortar[r * rh:r * rh + g, xs] = 1
    return tone, mortar

def stone(n, rng, base, dark=False):
    tone, mortar = ashlar(n, rng, rows=4)
    from numpy import clip
    grain = fbm(n, rng, (1.0, 1.6, 2.2), (1, .7, .5)); fine = noise(n, 0.3, rng)
    streak = noise(n, 2.2, rng); streak = np.roll(streak, 0, 0)
    stain = np.clip((streak - 0.5) * 2, 0, 1) ** 1.5 * np.linspace(1, .3, n)[:, None]
    # blur mortar to soften grooves
    m = mortar.copy()
    for _ in range(2):
        m = (m + np.roll(m, 1, 0) + np.roll(m, -1, 0) + np.roll(m, 1, 1) + np.roll(m, -1, 1)) / 5
    height = 0.55 * grain + 0.2 * fine + 0.25 * tone * 0.5 - 0.6 * m
    height = (height - height.min()) / (height.max() - height.min())
    lum = 0.55 + 0.35 * grain + 0.1 * fine + 0.18 * (tone - 0.5) - 0.38 * m - 0.22 * stain
    cool = np.array(base)[None, None, :] * (1.0 + 0.15 * (tone - 0.5))[..., None]
    moss = np.clip(noise(n, 1.8, rng) - 0.72, 0, 1)[..., None] * np.array([-0.02, 0.05, -0.02])
    albedo = np.clip(cool * lum[..., None] * 1.5 + moss, 0, 1)
    return albedo, height, 7.0

def planks(n, rng, base):
    count = 8; w = n // count
    tone = np.zeros((n, n)); gap = np.zeros((n, n)); h0 = np.zeros((n, n))
    grain = noise(n, 1.6, rng)
    for i in range(count):
        x = np.arange(i * w, (i + 1) * w)
        tone[:, x] = rng.random() * 0.5 + 0.25
        gap[:, i * w:i * w + 2] = 1
        # long grain: stretch noise vertically
    stretch = np.tile(noise(n, 2.6, rng)[:1, :], (n, 1))
    g = np.sin((np.arange(n)[None, :] * 0.35) + 9 * noise(n, 2.0, rng)) * 0.5 + 0.5
    height = 0.5 * g + 0.25 * grain + 0.2 * tone - 0.8 * gap
    height = (height - height.min()) / (height.max() - height.min())
    lum = 0.5 + 0.55 * g * 0.5 + 0.25 * tone + 0.15 * grain - 0.5 * gap
    albedo = np.clip(np.array(base)[None, None, :] * lum[..., None] * 1.6, 0, 1)
    return albedo, height, 4.0

def cloth(n, rng, base):
    y, x = np.mgrid[0:n, 0:n]
    weave = (np.sin(x * np.pi / 2 * 1.0) * np.sin(y * np.pi / 2 * 1.0)) * 0.5 + 0.5
    weave = np.sin(x * 2 * np.pi / 8) * np.sin(y * 2 * np.pi / 8) * 0.5 + 0.5
    warp = noise(n, 2.0, rng); stain = np.clip(noise(n, 2.2, rng) - 0.6, 0, 1) * 2
    height = 0.7 * weave + 0.3 * warp
    lum = 0.7 + 0.25 * weave + 0.25 * warp - 0.35 * stain
    albedo = np.clip(np.array(base)[None, None, :] * lum[..., None] * 1.4 - stain[..., None] * np.array([0, 0.01, 0.01]), 0, 1)
    return albedo, height, 3.0

def metal(n, rng, base, patina=0.0, brushed=True):
    f = noise(n, 1.8, rng)
    br = noise(n, 1.0, rng) if not brushed else np.tile(noise(n, 1.0, rng)[:, :1], (1, n)) * 0.5 + 0.5 * noise(n, 1.0, rng)
    br = (br - br.min()) / (br.max() - br.min())
    scratch = noise(n, 0.4, rng)
    pat = np.clip(noise(n, 2.2, rng) - 0.55, 0, 1) * 2.5 * patina
    lum = 0.7 + 0.35 * f + 0.15 * br - 0.1 * scratch
    albedo = np.array(base)[None, None, :] * lum[..., None] * 1.15
    albedo = albedo * (1 - pat[..., None]) + pat[..., None] * np.array([0.12, 0.30, 0.24])
    return np.clip(albedo, 0, 1), 0.5 * f + 0.5 * br, 2.0

def bone(n, rng, base):
    f = fbm(n, rng); pit = np.clip(noise(n, 0.8, rng) - 0.82, 0, 1) * 4
    lum = 0.75 + 0.35 * f - 0.4 * pit
    return np.clip(np.array(base)[None, None, :] * lum[..., None] * 1.3, 0, 1), 0.7 * f - 0.3 * pit, 3.0

def save(name, albedo, height, nstrength, size):
    img = Image.fromarray((np.clip(albedo, 0, 1) * 255 + .5).astype(np.uint8), "RGB")
    nm = Image.fromarray((to_normal(height, nstrength) * 255 + .5).astype(np.uint8), "RGB")
    a = cc.TEX / (name + "_albedo.png"); b = cc.TEX / (name + "_normal.png")
    img.save(a, optimize=True); nm.save(b, optimize=True)
    return cc.write_texture_meta(a, "color"), cc.write_texture_meta(b, "normal")

def portrait(rng, w=512, h=704):
    """Dark oil portrait of a tall pale figure in an oxblood coat with two pale eyes (piece 3 may overlay)."""
    y, x = np.mgrid[0:h, 0:w].astype(float)
    bg = 0.04 + 0.12 * np.exp(-(((x - w * .5) / (w * .6)) ** 2 + ((y - h * .38) / (h * .5)) ** 2))
    img = np.stack([bg * 0.9, bg * 0.95, bg * 1.1], -1)
    coat = ((np.abs(x - w * .5) < (w * .13 + (y - h * .45) * .45)) & (y > h * .48)).astype(float)
    img = img * (1 - coat[..., None]) + coat[..., None] * np.array([0.20, 0.025, 0.03])
    face = (((x - w * .5) / (w * .105)) ** 2 + ((y - h * .33) / (h * .13)) ** 2 < 1).astype(float)
    img = img * (1 - face[..., None]) + face[..., None] * np.array([0.45, 0.46, 0.47])
    hair = (((x - w * .5) / (w * .115)) ** 2 + ((y - h * .285) / (h * .1)) ** 2 < 1) & (y < h * .30)
    img[hair] = 0.02
    for ex in (-1, 1):
        eye = (((x - w * (.5 + .045 * ex)) / 9) ** 2 + ((y - h * .335) / 5) ** 2 < 1)
        img[eye] = [0.85, 0.82, 0.7]
    img += (noise(max(w, h), 1.5, rng)[:h, :w, None] - .5) * 0.04
    # craquelure vignette
    v = 1 - 0.55 * (((x - w / 2) / (w / 2)) ** 2 + ((y - h / 2) / (h / 2)) ** 2) ** 1.5
    img *= np.clip(v, .2, 1)[..., None]
    p = cc.TEX / "Portrait.png"
    Image.fromarray((np.clip(img, 0, 1) * 255 + .5).astype(np.uint8), "RGB").save(p, optimize=True)
    return cc.write_texture_meta(p, "color")

def main():
    for d in (cc.CASTLE, cc.TEX, cc.MATS, cc.SKY):
        d.mkdir(parents=True, exist_ok=True)
    for d in (cc.CASTLE, cc.TEX, cc.MATS):
        cc.write_folder_meta(d)
    cc.write_folder_meta(cc.SKY)
    gens = {
        "Castle_Stone": (1024, lambda n, r, b: stone(n, r, b)),
        "Castle_BlackStone": (512, lambda n, r, b: (lambda a: (a[0], a[1], 5.0))(stone(n, r, b, True))),
        "Castle_Oak": (512, planks),
        "Castle_Brass": (512, lambda n, r, b: metal(n, r, b, patina=0.45)),
        "Castle_Oxblood": (512, cloth),
        "Castle_Silver": (512, lambda n, r, b: metal(n, r, b, patina=0.12)),
        "Castle_Iron": (512, lambda n, r, b: metal(n, r, b, patina=0.0, brushed=False)),
        "Castle_Bone": (512, bone),
    }
    for i, (name, (size, fn)) in enumerate(gens.items()):
        rng = np.random.default_rng(1000 + i)
        base, smooth, metal_v, _ = cc.MATERIALS[name]
        albedo, height, ns = fn(size, rng, base)
        if name == "Castle_Stone":  # normalise mean luminance to the declared base colour
            albedo = np.clip(albedo * (np.array(base) / albedo.reshape(-1, 3).mean(0))[None, None, :], 0, 1)
        ag, ng = save(name, albedo, height, ns, size)
        cc.write_standard_mat(cc.MATS / (name + ".mat"), name, base, smooth, metal_v, ag, ng)
        print("texture", name, size)
    for name, (base, smooth, metal_v, textured) in cc.MATERIALS.items():
        if textured:
            continue
        em = (2.4, 1.2, 0.35) if name == "Castle_Flame" else None
        cc.write_standard_mat(cc.MATS / (name + ".mat"), name, base, smooth, metal_v, emission=em)
    pg = portrait(np.random.default_rng(77))
    cc.write_standard_mat(cc.MATS / "Castle_Canvas.mat", "Castle_Canvas", (1, 1, 1), 0.05, 0.0, pg, None)
    print("CASTLE_TEXTURES_PASS")

if __name__ == "__main__":
    main()
