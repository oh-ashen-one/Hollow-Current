#!/usr/bin/env python3
"""Procedural tileable PBR textures, Unity .mat files and the castle surface shaders for the Haunted Castle.

    python3 tools/castle_textures.py

Writes, per material, Assets/HollowCurrent/Resources/Castle/Textures/<Mat>_albedo.png, _normal.png and _mask.png
(1024 px, periodic so they tile seamlessly; mask = Unity metallic map: R metallic, G occlusion, A smoothness = 1 - roughness),
Materials/<Mat>.mat, Shaders/*.shader (+ .meta) and Textures/Portrait.png. Deterministic (fixed seeds), idempotent,
no downloads, numpy + Pillow only.

Tile sizes (what the world-space CastleTriplanar shader assumes, metres per texture repeat):
  Stone 2.0 (8 courses of 25 cm), FlagStone 2.0 (1 m slabs), BlackStone 1.5, Oak 1.0 (8 planks), Brass/Silver/Iron 0.5,
  Oxblood 1.0, Bone 1.0, Paper 0.5, Canvas 0.5, Blood 2.0.
"""
import sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
sys.path.insert(0, str(Path(__file__).resolve().parent))
import castle_common as cc

SIZE = 1024
SHADERS = cc.CASTLE / "Shaders"

# ---------------------------------------------------------------------------------------------------------------------
# noise toolbox (everything periodic)
# ---------------------------------------------------------------------------------------------------------------------
def norm01(a):
    return (a - a.min()) / (a.max() - a.min() + 1e-9)

def noise(n, beta, rng, sx=1.0, sy=1.0):
    """Periodic 1/f^beta noise in [0,1]. sx/sy > 1 attenuate that axis' frequencies (features stretch along it)."""
    w = rng.standard_normal((n, n))
    f = np.fft.fft2(w)
    fy = np.fft.fftfreq(n)[:, None] * sy
    fx = np.fft.fftfreq(n)[None, :] * sx
    r = np.sqrt(fx * fx + fy * fy); r[0, 0] = 1
    f *= 1.0 / r ** beta; f[0, 0] = 0
    return norm01(np.real(np.fft.ifft2(f)))

def fbm(n, rng, octs=(1.2, 1.8, 2.4), wts=(1, .5, .25)):
    return norm01(sum(w * noise(n, b, rng) for b, w in zip(octs, wts)))

def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a + 1e-9), 0, 1)
    return t * t * (3 - 2 * t)

def blur(a, k=1):
    for _ in range(k):
        a = (4 * a + np.roll(a, 1, 0) + np.roll(a, -1, 0) + np.roll(a, 1, 1) + np.roll(a, -1, 1)) / 8
    return a

def to_normal(h, strength):
    gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5
    gy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 0.5
    nx, ny, nz = -gx * strength, gy * strength, np.ones_like(h)   # +Y up (OpenGL / Unity)
    l = np.sqrt(nx * nx + ny * ny + nz * nz)
    return np.stack([nx / l, ny / l, nz / l], -1) * 0.5 + 0.5

def scratches(n, rng, count, length, angle=None, wide=1):
    """Wrapped random line scratches in [0,1] (periodic)."""
    img = Image.new("L", (n, n), 0)
    d = ImageDraw.Draw(img)
    for _ in range(count):
        x, y = rng.random() * n, rng.random() * n
        a = rng.random() * np.pi if angle is None else angle + rng.normal(0, .08)
        l = length * (0.3 + rng.random())
        dx, dy = np.cos(a) * l, np.sin(a) * l
        v = int(80 + rng.random() * 175)
        for ox in (-n, 0, n):
            for oy in (-n, 0, n):
                d.line([(x + ox, y + oy), (x + dx + ox, y + dy + oy)], fill=v, width=wide)
    return np.asarray(img, dtype=np.float32) / 255.0

def pack_mask(smooth, metal=0.0, ao=1.0):
    smooth = np.clip(smooth, 0, 1); n = smooth.shape
    metal = np.broadcast_to(np.asarray(metal, dtype=np.float32), n); ao = np.broadcast_to(np.asarray(ao, dtype=np.float32), n)
    out = np.stack([metal, np.clip(ao, 0, 1), np.zeros(n, np.float32), smooth], -1)
    return (out * 255 + .5).astype(np.uint8)

def mean_to(albedo, base):
    """Rescale albedo so its mean colour equals `base` (keeps the declared material colour honest)."""
    m = albedo.reshape(-1, 3).mean(0)
    return np.clip(albedo * (np.array(base) / np.maximum(m, 1e-4))[None, None, :], 0, 1)

# ---------------------------------------------------------------------------------------------------------------------
# materials. Each returns dict(albedo, height, ns, smooth, metal, ao)
# ---------------------------------------------------------------------------------------------------------------------
def ashlar_layout(n, rng, rows, cols_choice, offset_rows=True):
    """Courses of uneven blocks. Returns distance-to-block-edge in px (min of dx, dy), per-block tone and hue."""
    rh = n // rows
    dxy = np.zeros((n, n)); tone = np.zeros((n, n)); hue = np.zeros((n, n))
    for r in range(rows):
        cols = int(rng.choice(cols_choice))
        widths = rng.dirichlet(np.ones(cols) * 5)
        edges = np.concatenate([[0], np.round(np.cumsum(widths) * n)]).astype(int); edges[-1] = n
        off = int(rng.integers(0, n)) if offset_rows else 0
        ys = slice(r * rh, (r + 1) * rh)
        dyl = np.minimum(np.arange(rh), rh - 1 - np.arange(rh))
        for i in range(cols):
            w = edges[i + 1] - edges[i]
            if w < 8:
                continue
            xs = (np.arange(edges[i], edges[i + 1]) + off) % n
            dxl = np.minimum(np.arange(w), w - 1 - np.arange(w))
            dxy[ys, xs] = np.minimum(dxl[None, :], dyl[:, None])
            tone[ys, xs] = rng.random(); hue[ys, xs] = rng.random()
    return dxy, tone, hue

def stone(n, rng, base, rows=8, mortar_px=5, bevel=22, polish=0.0, wet=0.0, cols=(2, 3, 3, 4), chip_t=0.66, crack_k=120):
    d, tone, hue = ashlar_layout(n, rng, rows, list(cols))
    grain = fbm(n, rng, (1.0, 1.6, 2.2), (1, .7, .5)); fine = noise(n, 0.3, rng); pits = noise(n, 0.6, rng)
    chipn = fbm(n, rng, (1.3, 1.9, 2.6), (1, .6, .4))
    mortar = (d < mortar_px).astype(np.float32); mortar = blur(mortar, 1)
    edge_w = bevel * (0.5 + chipn)                                   # uneven bevel width = chipped, worn edges
    bev = smoothstep(mortar_px, mortar_px + edge_w, d)                 # 0 at the joint -> 1 on the block face
    chip = np.clip((chipn - chip_t) * 5.0, 0, 1) * (1 - smoothstep(mortar_px, mortar_px + 46, d))
    cracks = np.clip(1 - np.abs(noise(n, 1.7, rng) - 0.5) * crack_k, 0, 1) * smoothstep(8, 40, d)
    streak = noise(n, 2.4, rng, sx=5.0, sy=0.6)                       # vertical damp streaks
    damp = np.clip((streak - 0.52) * 3.2, 0, 1) * np.linspace(1.0, 0.35, n)[:, None]
    height = 0.34 * bev + 0.34 * grain + 0.10 * fine + 0.08 * tone - 0.30 * chip - 0.22 * cracks - 0.55 * mortar
    height = norm01(height)
    lum = 0.80 + 0.30 * (grain - .5) + 0.10 * (fine - .5) + 0.20 * (tone - .5) + 0.10 * bev + 0.20 * chip - 0.45 * mortar \
        - 0.30 * cracks - 0.18 * damp - 0.10 * np.clip((pits - .72) * 4, 0, 1)
    col = np.array(base)[None, None, :] * (1 + (hue[..., None] - .5) * np.array([0.10, 0.04, -0.06])[None, None, :])
    moss = np.clip(noise(n, 1.8, rng) - 0.72, 0, 1)[..., None] * np.array([-0.03, 0.06, -0.03]) * (0.4 + damp[..., None])
    albedo = mean_to(np.clip(col * lum[..., None] + moss, 0, 1), base)
    smooth = 0.16 + 0.10 * (1 - grain) + 0.22 * damp * (0.5 + wet) + polish * (0.45 + 0.2 * (1 - fine)) - 0.10 * chip - 0.08 * mortar
    ao = 1 - 0.65 * mortar - 0.30 * (1 - bev) - 0.25 * cracks
    return dict(albedo=albedo, height=height, ns=9.0, smooth=smooth, metal=0.0, ao=ao)

def flagstone(n, rng, base):
    """1 m slabs in running bond, hairline cracks, wet pooling: low-frequency smoothness variation."""
    r = stone(n, rng, base, rows=2, mortar_px=4, bevel=26, wet=1.0)
    wetmask = smoothstep(0.34, 0.62, fbm(n, rng, (1.6, 2.2), (1, .6)))   # puddled patches
    r["smooth"] = 0.42 + 0.50 * wetmask * (0.5 + 0.5 * np.clip(r["ao"], 0, 1))   # wet look: 0.42 dry slab up to ~0.92 in puddles
    r["smooth"] = np.clip(r["smooth"], 0.30, 0.94)
    r["albedo"] = np.clip(r["albedo"] * (1 - 0.22 * wetmask[..., None]), 0, 1)
    r["ns"] = 7.0
    return r

def blackstone(n, rng, base):
    veins = np.clip(1 - np.abs(noise(n, 1.9, rng) - 0.5) * 22, 0, 1) ** 2
    veins2 = np.clip(1 - np.abs(noise(n, 1.5, rng) - 0.5) * 30, 0, 1) ** 2
    grain = fbm(n, rng); fine = noise(n, 0.3, rng)
    d, tone, hue = ashlar_layout(n, rng, 3, [1, 2])
    mortar = blur((d < 4).astype(np.float32), 1)
    lum = 0.85 + 0.35 * (grain - .5) + 0.10 * (fine - .5) + 0.55 * veins + 0.30 * veins2 - 0.5 * mortar + 0.15 * (tone - .5)
    col = np.array(base)[None, None, :] * np.array([1.0, 1.0, 1.15])
    albedo = mean_to(np.clip(col * lum[..., None], 0, 1), base)
    height = norm01(0.5 * grain + 0.1 * fine - 0.8 * mortar - 0.2 * veins)
    smooth = 0.62 + 0.18 * veins - 0.25 * mortar - 0.1 * (1 - grain)
    return dict(albedo=albedo, height=height, ns=4.0, smooth=smooth, metal=0.0, ao=1 - 0.6 * mortar)

def planks(n, rng, base, count=8, gap_px=4):
    """8 planks across the tile, long grain with rings, knots, nail heads, dark gaps."""
    w = n // count
    plank_tone = np.zeros((n, n)); gap = np.zeros((n, n)); shift = np.zeros((n, n))
    for i in range(count):
        xs = np.arange(i * w, (i + 1) * w)
        plank_tone[:, xs] = rng.random()
        shift[:, xs] = rng.random() * 40
        gap[:, np.arange(i * w, i * w + gap_px) % n] = 1
    grain_n = noise(n, 2.0, rng, sx=0.35, sy=7.0)                  # stretched along y
    warp = noise(n, 2.2, rng, sx=1, sy=5.0)
    x = np.arange(n)[None, :].astype(np.float32)
    rings = np.sin((x + shift + 70 * warp) * 2 * np.pi / 14.0) * 0.5 + 0.5
    fiber = noise(n, 0.8, rng, sx=0.25, sy=9.0)
    knots = np.zeros((n, n), np.float32)
    yy, xx = np.mgrid[0:n, 0:n]
    for _ in range(6):
        cx, cy, r = rng.random() * n, rng.random() * n, 14 + rng.random() * 12
        dxk = np.minimum(np.abs(xx - cx), n - np.abs(xx - cx)); dyk = np.minimum(np.abs(yy - cy), n - np.abs(yy - cy))
        knots = np.maximum(knots, np.clip(1 - np.sqrt((dxk / 1.0) ** 2 + (dyk / 2.0) ** 2) / r, 0, 1))
    gapb = blur(gap, 1)
    height = norm01(0.35 * rings + 0.30 * grain_n + 0.15 * fiber - 0.9 * gapb - 0.4 * knots)
    lum = 0.70 + 0.30 * (rings - .5) + 0.30 * (grain_n - .5) + 0.30 * (plank_tone - .5) + 0.15 * (fiber - .5) - 0.6 * gapb - 0.35 * knots
    albedo = mean_to(np.clip(np.array(base)[None, None, :] * lum[..., None], 0, 1), base)
    smooth = 0.25 + 0.20 * (1 - fiber) - 0.18 * gapb + 0.10 * (1 - grain_n)
    return dict(albedo=albedo, height=height, ns=5.0, smooth=smooth, metal=0.0, ao=1 - 0.8 * gapb - 0.25 * knots)

def cloth(n, rng, base):
    """Plain weave, 8 px thread pitch, with fuzz and stains."""
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32)
    p = 8.0
    warp = np.sin(xx * 2 * np.pi / p) * 0.5 + 0.5
    weft = np.sin(yy * 2 * np.pi / p) * 0.5 + 0.5
    over = (((xx // (p / 2)).astype(int) + (yy // (p / 2)).astype(int)) % 2) == 0
    weave = np.where(over, warp, weft)
    yarn = noise(n, 1.0, rng, sx=0.6, sy=0.6); fuzz = noise(n, 0.25, rng)
    stain = np.clip(noise(n, 2.3, rng) - 0.58, 0, 1) * 2.2
    height = norm01(0.7 * weave + 0.15 * yarn + 0.15 * fuzz)
    lum = 0.72 + 0.30 * (weave - .5) + 0.20 * (yarn - .5) + 0.08 * (fuzz - .5) - 0.38 * stain
    albedo = mean_to(np.clip(np.array(base)[None, None, :] * lum[..., None], 0, 1), base)
    return dict(albedo=albedo, height=height, ns=1.8, smooth=0.06 + 0.06 * (1 - weave) + 0.1 * stain, metal=0.0, ao=0.85 + 0.15 * weave)

def metal(n, rng, base, patina=0.0, brushed=True, metal_v=0.85, scratch_count=380):
    """Brushed metal with random scratches (bright, rough) and tarnish blotches (dark, rougher, less metallic)."""
    f = noise(n, 1.8, rng)
    br = noise(n, 1.0, rng, sx=0.2, sy=5.0) if brushed else noise(n, 1.0, rng)
    sc = scratches(n, rng, scratch_count, n * 0.10, None if not brushed else 0.0)
    sc = np.clip(blur(sc, 0) + 0.5 * scratches(n, rng, scratch_count // 2, n * 0.04), 0, 1)
    tarn = smoothstep(0.35, 0.85, fbm(n, rng, (1.5, 2.2), (1, .6))) * (0.25 + 0.5 * patina)
    pat = np.clip(noise(n, 2.2, rng) - 0.62, 0, 1) * 2.0 * patina
    lum = 0.72 + 0.30 * (f - .5) + 0.25 * (br - .5) + 0.30 * sc - 0.40 * tarn
    albedo = np.array(base)[None, None, :] * lum[..., None]
    albedo = albedo * (1 - pat[..., None]) + pat[..., None] * np.array([0.12, 0.30, 0.24])
    albedo = mean_to(np.clip(albedo, 0, 1), base)
    height = norm01(0.4 * f + 0.4 * br - 0.8 * sc)
    smooth = 0.55 + 0.15 * (1 - br) - 0.30 * sc - 0.35 * tarn
    metallic = np.clip(metal_v - 0.45 * tarn - 0.5 * pat, 0, 1)
    return dict(albedo=albedo, height=height, ns=2.5, smooth=smooth, metal=metallic, ao=1 - 0.3 * tarn)

def bone(n, rng, base):
    f = fbm(n, rng); pit = np.clip(noise(n, 0.8, rng) - 0.80, 0, 1) * 4
    crack = np.clip(1 - np.abs(noise(n, 1.8, rng) - 0.5) * 40, 0, 1)
    fine = noise(n, 0.3, rng)
    lum = 0.80 + 0.35 * (f - .5) + 0.06 * (fine - .5) - 0.40 * pit - 0.35 * crack
    albedo = mean_to(np.clip(np.array(base)[None, None, :] * lum[..., None], 0, 1), base)
    return dict(albedo=albedo, height=norm01(0.7 * f - 0.4 * pit - 0.5 * crack + 0.1 * fine), ns=3.5,
                smooth=0.30 + 0.12 * (1 - f) - 0.15 * pit, metal=0.0, ao=1 - 0.5 * crack - 0.4 * np.clip(pit, 0, 1))

def paper(n, rng, base):
    fibre = noise(n, 0.9, rng, sx=0.7, sy=2.0) * 0.5 + noise(n, 0.9, rng, sx=2.0, sy=0.7) * 0.5
    stain = np.clip(noise(n, 2.3, rng) - 0.5, 0, 1) * 2
    fold = np.clip(1 - np.abs(noise(n, 2.6, rng) - 0.5) * 30, 0, 1) * 0.6
    lum = 0.88 + 0.22 * (fibre - .5) - 0.35 * stain - 0.15 * fold
    albedo = mean_to(np.clip(np.array(base)[None, None, :] * lum[..., None], 0, 1), base)
    return dict(albedo=albedo, height=norm01(0.7 * fibre - 0.3 * fold), ns=2.5, smooth=0.07 + 0.04 * stain, metal=0.0, ao=1 - 0.3 * fold)

def canvas_normal(n, rng):
    """Canvas weave for the portrait (normal + mask only; the painting itself is Portrait.png)."""
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32)
    p = 4.0
    weave = np.sin(xx * 2 * np.pi / p) * np.sin(yy * 2 * np.pi / p) * 0.5 + 0.5
    h = norm01(0.8 * weave + 0.2 * noise(n, 0.6, rng))
    return dict(albedo=None, height=h, ns=2.0, smooth=0.12 + 0.08 * weave, metal=0.0, ao=0.8 + 0.2 * weave)

def blood(n, rng, base):
    """Dark red liquid. The height is a periodic sum of integer-wavenumber waves, so it tiles and scrolls cleanly."""
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32)
    h = np.zeros((n, n), np.float32)
    for _ in range(22):
        kx, ky = int(rng.integers(-7, 8)), int(rng.integers(-7, 8))
        if kx == 0 and ky == 0:
            continue
        amp = 1.0 / np.hypot(kx, ky)
        h += amp * np.sin((kx * xx + ky * yy) * 2 * np.pi / n + rng.random() * 6.283)
    h = norm01(h) * 0.8 + 0.2 * noise(n, 1.4, rng)
    depth = fbm(n, rng, (1.6, 2.4), (1, .5))
    lum = 0.75 + 0.55 * depth
    albedo = np.clip(np.array(base)[None, None, :] * lum[..., None], 0, 1)
    return dict(albedo=albedo, height=h, ns=5.5, smooth=0.98 - 0.03 * depth, metal=0.0, ao=1.0)

# ---------------------------------------------------------------------------------------------------------------------
# sun window: stained-glass sun painted over with black; blue moonlight leaks round the edges and through flaked paint.
# ---------------------------------------------------------------------------------------------------------------------
SW_W, SW_H = 512, 870          # 1.0 m x 1.7 m
def sun_window(rng):
    n_w, n_h = SW_W, SW_H
    yy, xx = np.mgrid[0:n_h, 0:n_w].astype(np.float32)
    x = (xx + .5) / n_w * 1.0 - 0.5                 # metres, centred
    y = (n_h - yy - .5) / n_h * 1.7                  # metres from the sill, up
    W, ys = 1.0, 1.7 - 0.8660254                      # equilateral pointed arch, spring line ys
    inside = np.where(y <= ys, np.abs(x) <= W / 2,
                      np.where(x <= 0, np.hypot(x - W / 2, y - ys) <= W, np.hypot(x + W / 2, y - ys) <= W))
    # distance to the outline in metres (approximate, good enough for a rim band)
    side = W / 2 - np.abs(x)
    arc = np.where(x <= 0, W - np.hypot(x - W / 2, y - ys), W - np.hypot(x + W / 2, y - ys))
    dist = np.where(y <= ys, np.minimum(side, y), np.minimum(arc, np.where(y <= ys + .1, side, 9)))
    dist = np.minimum(dist, y)                         # sill edge
    px = dist * n_w                                    # px (512 px per metre)
    nz = lambda b, sx=1, sy=1: np.asarray(Image.fromarray((noise(1024, b, rng, sx, sy) * 255).astype(np.uint8)).resize((n_w, n_h), Image.BILINEAR), np.float32) / 255
    wobble = nz(1.6); flake = nz(1.2); drips = nz(2.0, 6.0, 0.4)
    gap = 5 + 16 * wobble                              # paint stops this many px from the frame (uneven brush edge)
    paint_edge = smoothstep(gap - 2, gap + 3, px)
    flakes = smoothstep(0.79, 0.84, flake) * smoothstep(10, 40, px)
    paint = np.clip(paint_edge - flakes, 0, 1)
    # the buried sun: disc + 16 rays in the lead cames, visible only through hairline cracks in the paint
    cx, cy = 0.0, ys - 0.18
    r = np.hypot(x - cx, y - cy); ang = np.arctan2(y - cy, x - cx)
    ring = np.exp(-((r - 0.21) / 0.008) ** 2)
    perp = np.abs(((ang * 16 / (2 * np.pi)) % 1.0) - .5) * (2 * np.pi / 16) * r      # metres from the nearest ray line
    rays = np.exp(-(perp / 0.006) ** 2) * smoothstep(.26, .30, r) * (1 - smoothstep(.62, .70, r))
    crack = np.clip(1 - np.abs(nz(1.8) - 0.5) * 45, 0, 1)
    lead = np.clip(ring + rays, 0, 1) * (0.22 + 0.78 * crack) * smoothstep(8, 30, px)
    glass = np.array([0.05, 0.09, 0.17])
    paint_col = np.array([0.014, 0.014, 0.018])[None, None, :] * (0.7 + 0.8 * nz(0.9)[..., None])
    albedo = glass[None, None, :] * (1 - paint[..., None]) + paint_col * paint[..., None]
    albedo = np.where(inside[..., None], albedo, 0.0)
    # emission: leak band round the edge fading inward, flaked windows, lead lines through cracks
    leak = (1 - smoothstep(0, gap + 14, px)) * (1 - paint_edge * 0.0)
    em_amt = np.clip(leak * (0.45 + 0.55 * wobble) + flakes * 0.55 + lead * 0.75, 0, 1) * inside
    em_col = np.array([0.30, 0.52, 1.0])[None, None, :]
    emission = em_col * em_amt[..., None] * (0.85 + 0.3 * flake[..., None])
    emission = np.where(inside[..., None], emission, 0.0)
    height = norm01(0.6 * paint * (0.5 + 0.5 * drips) + 0.3 * nz(0.7) - 0.5 * flakes - 0.3 * (1 - paint_edge))
    smooth = 0.30 + 0.55 * (1 - paint) + 0.1 * paint * drips
    return albedo, emission, height, smooth


def stone_weathered(n, rng, base):
    """Weathered grey-blue ashlar: larger blocks, worn rounded edges, heavy chipping and cracks, rain streaks."""
    r = stone(n, rng, base, rows=6, mortar_px=7, bevel=34, cols=(2, 2, 3), chip_t=0.58, crack_k=70)
    streak = noise(n, 2.6, rng, sx=0.5, sy=6.0)
    rain = np.clip((streak - 0.55) * 3.0, 0, 1)
    r["albedo"] = np.clip(r["albedo"] * (1 - 0.25 * rain[..., None]) , 0, 1)
    r["smooth"] = r["smooth"] + 0.18 * rain
    return r

def stone_dark(n, rng, base):
    """Dark rough-dressed stone: tight coursing, deep joints, pitted faces, almost no polish."""
    r = stone(n, rng, base, rows=10, mortar_px=6, bevel=14, cols=(3, 4, 4, 5), chip_t=0.70, crack_k=90)
    pit = np.clip(noise(n, 0.5, rng) - 0.68, 0, 1) * 3.0
    r["albedo"] = np.clip(r["albedo"] * (1 - 0.5 * pit[..., None]), 0, 1)
    r["height"] = norm01(r["height"] - 0.25 * pit)
    r["ns"] = 11.0
    r["smooth"] = np.clip(r["smooth"] * 0.9 - 0.08 * pit, 0.05, 0.6)
    return r

def stone_mossy(n, rng, base):
    """Weathered stone with moss and algae ONLY where moisture collects: joints, chipped edges, cracks and the lower part of
    each course, gated by sparse noise. The block faces stay stone (round 5 read the old version as flat green panels)."""
    r = stone(n, rng, base, rows=8, mortar_px=6, bevel=26, wet=0.6, chip_t=0.64)
    crev = np.clip(1.0 - r["ao"], 0, 1)                                  # joints, bevels, cracks (0 on block faces)
    gate = smoothstep(0.38, 0.62, fbm(n, rng, (1.5, 2.2, 3.0), (1, .7, .4)))   # sparse patches along the crevices
    yy = (np.arange(n)[:, None] % (n // 8)) / (n // 8)                   # 0 at the top of a course, 1 at its bottom edge
    low = smoothstep(0.78, 1.0, yy) * smoothstep(0.45, 0.70, fbm(n, rng, (1.4, 2.0), (1, .6)))   # a little growth at the foot of some courses
    moss = np.clip(crev * 1.5 * (0.25 + 0.75 * gate) + 0.5 * low * gate, 0, 1)
    moss = blur(moss, 2)
    tex = 0.65 + 0.7 * noise(n, 0.5, rng)
    mcol = np.array([0.17, 0.25, 0.10])[None, None, :] * tex[..., None]
    dcol = np.array([0.24, 0.20, 0.12])[None, None, :]
    col = r["albedo"] * (1 - 0.8 * moss[..., None]) + mcol * 0.85 * moss[..., None]
    streak = noise(n, 2.5, rng, sx=0.5, sy=6.0)
    damp = np.clip((streak - 0.55) * 3.0, 0, 1) * 0.5
    col = col * (1 - 0.18 * damp[..., None]) + dcol * 0.10 * damp[..., None]
    r["albedo"] = np.clip(col, 0, 1)
    r["height"] = norm01(r["height"] + 0.10 * moss)
    r["smooth"] = np.clip(r["smooth"] * (1 - 0.5 * moss) + 0.45 * moss + 0.2 * damp, 0.08, 0.85)
    r["ao"] = np.clip(r["ao"] - 0.1 * moss, 0, 1)
    return r

def oak_plank(n, rng, base):
    """Four wide boards per metre with deep gaps, long grain, knots, weathering and a row of nails at each board edge."""
    r = planks(n, rng, base, count=4, gap_px=9)
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32)
    nails = np.zeros((n, n), np.float32)
    for pi in range(4):
        for ny_ in range(4):
            for off in (-.5, .5):
                cx = pi * (n // 4) + (n // 8) + off * (n // 4 - 70) ; cy = ny_ * (n // 4) + n // 8 + 20 * ((pi + ny_) % 2)
                nails = np.maximum(nails, np.clip(1 - np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2) / 8.0, 0, 1))
    wear = smoothstep(0.5, 0.75, fbm(n, rng, (1.6, 2.4), (1, .6)))
    r["albedo"] = np.clip(r["albedo"] * (1 - 0.18 * wear[..., None]) * (1 - 0.5 * nails[..., None]) + nails[..., None] * np.array([.10, .10, .11]) * 0.5, 0, 1)
    r["height"] = norm01(r["height"] + 0.35 * nails - 0.12 * wear)
    r["metal"] = np.clip(nails * 0.8, 0, 1); r["smooth"] = np.clip(r["smooth"] + 0.25 * nails - 0.1 * wear, 0.1, 0.9)
    return r

# ---------------------------------------------------------------------------------------------------------------------
# gore set for the flayed-demon Count (round 6): pallid skin, wet striated muscle, vein-webbed wing membrane, blood overlay
# ---------------------------------------------------------------------------------------------------------------------
def veins(n, rng, k1=26, k2=40):
    a = np.clip(1 - np.abs(noise(n, 1.9, rng) - 0.5) * k1, 0, 1) ** 1.5
    b = np.clip(1 - np.abs(noise(n, 1.5, rng) - 0.5) * k2, 0, 1) ** 1.5
    return np.clip(a + 0.7 * b, 0, 1)

def gore_skin(n, rng, base):
    v = veins(n, rng); pores = noise(n, 0.35, rng); mott = fbm(n, rng, (1.4, 2.0), (1, .6))
    crack = np.clip(1 - np.abs(noise(n, 1.8, rng) - 0.5) * 70, 0, 1) * smoothstep(0.4, 0.6, noise(n, 1.2, rng))
    col = np.array(base)[None, None, :] * (0.85 + 0.3 * (mott - .5))[..., None] * (0.95 + 0.1 * pores)[..., None]
    col = col * (1 - 0.35 * v[..., None]) + np.array([0.36, 0.38, 0.52])[None, None, :] * 0.35 * v[..., None]       # blue-grey veins
    col = col * (1 - crack[..., None]) + np.array([0.30, 0.04, 0.04])[None, None, :] * crack[..., None]          # split skin, red beneath
    h = norm01(0.4 * pores + 0.25 * v + 0.2 * mott - 0.6 * crack)
    return dict(albedo=mean_to(np.clip(col, 0, 1), base), height=h, ns=3.0, smooth=0.38 + 0.2 * smoothstep(0.5, 0.8, mott) - 0.1 * pores + 0.4 * crack, metal=0.0, ao=1 - 0.5 * crack)

def gore_flesh(n, rng, base):
    fib = noise(n, 1.7, rng, sx=0.25, sy=9.0); fib2 = noise(n, 1.0, rng, sx=0.3, sy=6.0)
    fascia = np.clip(1 - np.abs(noise(n, 2.0, rng, sx=0.4, sy=5.0) - 0.5) * 18, 0, 1)
    dark = smoothstep(0.45, 0.7, fbm(n, rng, (1.6, 2.4), (1, .6)))
    lum = 0.75 + 0.55 * (fib - .5) + 0.25 * (fib2 - .5) - 0.35 * dark
    col = np.array(base)[None, None, :] * lum[..., None]
    col = col * (1 - 0.7 * fascia[..., None]) + np.array([0.70, 0.45, 0.40])[None, None, :] * 0.7 * fascia[..., None]
    h = norm01(0.6 * fib + 0.3 * fib2 + 0.3 * fascia)
    return dict(albedo=mean_to(np.clip(col, 0, 1), base), height=h, ns=5.0, smooth=0.86 + 0.1 * (fib2 - .5) - 0.2 * fascia, metal=0.0, ao=1 - 0.3 * dark)

def gore_membrane(n, rng, base):
    v = veins(n, rng, 20, 34); fine = noise(n, 1.1, rng); blot = fbm(n, rng, (1.5, 2.2), (1, .6))
    col = np.array(base)[None, None, :] * (0.8 + 0.4 * (fine - .5))[..., None]
    col = col * (1 - 0.5 * blot[..., None] * np.array([0.0, 0.6, 0.6])[None, None, :]) * (1 - 0.3 * v[..., None]) + np.array([0.45, 0.10, 0.10])[None, None, :] * 0.4 * v[..., None]
    return dict(albedo=mean_to(np.clip(col, 0, 1), base), height=norm01(0.5 * v + 0.3 * fine), ns=3.5, smooth=0.32 + 0.2 * v, metal=0.0, ao=1 - 0.15 * v)

def moonstone(n, rng, base):
    """Milky moonstone: pale blue-white body, drifting blue adularescent sheen, faint inclusions, faceted micro-relief."""
    sheen = smoothstep(0.45, 0.75, fbm(n, rng, (1.4, 2.0, 2.8), (1, .7, .4)))
    milk = fbm(n, rng, (1.2, 1.8), (1, .5)); fine = noise(n, 0.6, rng)
    inc = np.clip(noise(n, 0.7, rng) - 0.78, 0, 1) * 3.0
    col = np.array(base)[None, None, :] * (0.88 + 0.2 * (milk - .5))[..., None]
    col = col * (1 - 0.35 * sheen[..., None]) + np.array([0.45, 0.62, 1.0])[None, None, :] * 0.35 * sheen[..., None]
    col = col * (1 - 0.5 * inc[..., None]) + np.array([0.55, 0.52, 0.48])[None, None, :] * 0.2 * inc[..., None]
    h = norm01(0.5 * milk + 0.3 * fine + 0.3 * sheen)
    return dict(albedo=mean_to(np.clip(col, 0, 1), base), height=h, ns=4.5, smooth=0.55 + 0.3 * sheen - 0.15 * fine, metal=0.0, ao=1 - 0.2 * inc)

def gore_wet(n, rng):
    """Blood overlay: RGB dark red, alpha = coverage with vertical drip runs. Tileable."""
    streak = noise(n, 2.3, rng, sx=0.35, sy=9.0)
    blot = fbm(n, rng, (1.5, 2.2), (1, .6))
    cover = np.clip((streak * 0.65 + blot * 0.35 - 0.50) * 3.2, 0, 1)
    tone = 0.7 + 0.6 * noise(n, 1.0, rng)
    out = np.zeros((n, n, 4), np.uint8)
    out[..., 0] = (np.clip(0.30 * tone, 0, 1) * 255).astype(np.uint8)
    out[..., 1] = (np.clip(0.012 * tone, 0, 1) * 255).astype(np.uint8)
    out[..., 2] = (np.clip(0.02 * tone, 0, 1) * 255).astype(np.uint8)
    out[..., 3] = (np.clip(cover, 0, 1) * 255 + .5).astype(np.uint8)
    p = cc.TEX / "Castle_GoreWet.png"
    save_png(p, out)
    return cc.write_texture_meta(p, "emission")   # repeat wrap, mips, sRGB; alpha is data

# ---------------------------------------------------------------------------------------------------------------------
# decals: RGBA, alpha = coverage. Cracks, chips and stains sit on top of any wall; soot gradients darken wall bases and
# surround candelabras. Clamp-wrapped, not tiled.
# ---------------------------------------------------------------------------------------------------------------------
def draw_crack(d, rng, n, x, y, ang, length, width, depth=0, branch_p=0.18):
    pts = [(x, y)]
    step = max(6.0, n / 60)
    for i in range(int(length / step)):
        ang += rng.normal(0, 0.28)
        x += np.cos(ang) * step; y += np.sin(ang) * step
        pts.append((x, y))
        if depth < 3 and rng.random() < branch_p:
            draw_crack(d, rng, n, x, y, ang + rng.choice([-1, 1]) * rng.uniform(.5, 1.1), length * rng.uniform(.25, .5) - i * step * .2, max(1, width - 1), depth + 1, branch_p * .6)
    d.line(pts, fill=255, width=int(width), joint="curve")

def decal_cracks(n, rng, count):
    img = Image.new("L", (n, n), 0); d = ImageDraw.Draw(img)
    for _ in range(count):
        edge = rng.integers(0, 4)
        x, y = (rng.uniform(.2, .8) * n, rng.uniform(.0, .15) * n) if edge == 0 else (rng.uniform(.2, .8) * n, rng.uniform(.85, 1) * n) if edge == 1 else (rng.uniform(.0, .15) * n, rng.uniform(.2, .8) * n)
        ang = rng.uniform(0.3, 2.8) if edge == 0 else rng.uniform(-2.8, -0.3) if edge == 1 else rng.uniform(-1.0, 1.0)
        draw_crack(d, rng, n, x, y, ang, n * rng.uniform(.5, .9), rng.integers(3, 6))
    a = np.asarray(img, np.float32) / 255.0
    core = blur(a, 1)
    halo = blur(a, 6)
    alpha = np.clip(core * 1.3 + halo * 0.55, 0, 1)
    alpha *= np.clip(1 - np.maximum(np.abs(np.linspace(-1, 1, n))[None, :], np.abs(np.linspace(-1, 1, n))[:, None]) ** 6, 0, 1)   # fade to nothing at the edge
    out = np.zeros((n, n, 4), np.uint8)
    out[..., :3] = (np.array([0.035, 0.037, 0.045]) * 255).astype(np.uint8)
    out[..., 3] = (alpha * 255 + .5).astype(np.uint8)
    return out

def decal_chips(n, rng):
    """Spalled patches: jagged polygons with a bright exposed core and a dark fractured rim."""
    mask = Image.new("L", (n, n), 0); core = Image.new("L", (n, n), 0)
    dm, dc = ImageDraw.Draw(mask), ImageDraw.Draw(core)
    for _ in range(int(rng.integers(8, 13))):
        cx, cy = rng.uniform(.18, .82) * n, rng.uniform(.18, .82) * n
        r = rng.uniform(.035, .10) * n; k = int(rng.integers(7, 12))
        a0 = rng.uniform(0, 6.28)
        pts = [(cx + np.cos(a0 + i * 6.283 / k) * r * rng.uniform(.55, 1.15) * (1.35 if i % 3 == 0 else 1.0),
                cy + np.sin(a0 + i * 6.283 / k) * r * rng.uniform(.55, 1.15) * .8) for i in range(k)]
        dm.polygon(pts, fill=255)
        dc.polygon([(cx + (x - cx) * .55, cy + (y - cy) * .55) for x, y in pts], fill=255)
    m = np.asarray(mask, np.float32) / 255.0
    alpha = blur(m, 1)
    # Round 7: a chip, not a hole. Mid-tone stone inside (close to the wall's own value), a lighter bevelled rim on the upper-left
    # edge catching the light, a thin dark undercut on the lower-right, and a soft fade so the patch blends into the wall.
    # light from the upper left: the upper-left inner wall of the recess is in shadow, the lower-right inner wall catches light
    sh = 8
    ul = np.clip(m - np.roll(np.roll(m, sh, 0), sh, 1), 0, 1)          # sliver inside the chip along its upper-left edge
    lr = np.clip(m - np.roll(np.roll(m, -sh, 0), -sh, 1), 0, 1)        # sliver along its lower-right edge
    ul, lr = blur(ul, 2), blur(lr, 2)
    grain = 0.8 + 0.4 * noise(n, 0.7, rng)
    fresh = 0.85 + 0.3 * fbm(n, rng, (1.4, 2.0), (1, .6))
    face = np.array([0.44, 0.47, 0.54])[None, None, :] * (grain * fresh)[..., None]            # freshly broken stone: a little lighter than the weathered face
    col = face * (1 - 0.65 * ul[..., None]) + np.array([0.05, 0.05, 0.07])[None, None, :] * 0.65 * ul[..., None]
    col = col * (1 - lr[..., None]) + np.array([0.66, 0.68, 0.74])[None, None, :] * lr[..., None]
    out = np.zeros((n, n, 4), np.uint8)
    out[..., :3] = (np.clip(col, 0, 1) * 255).astype(np.uint8)
    out[..., 3] = (np.clip(blur(m, 1) * 0.93, 0, 1) * 255 + .5).astype(np.uint8)
    return out

def decal_stain(n, rng):
    """Water stain streaks running down from a top edge."""
    streak = noise(n, 2.2, rng, sx=0.4, sy=8.0)
    s2 = noise(n, 1.6, rng, sx=0.5, sy=5.0)
    y = np.linspace(0, 1, n)[:, None]
    x = np.linspace(-1, 1, n)[None, :]
    env = np.clip(1 - y * 0.9, 0, 1) ** 1.2 * np.clip(1 - np.abs(x) ** 3, 0, 1)
    a = np.clip((streak * .6 + s2 * .4 - 0.45) * 2.6, 0, 1) * env
    out = np.zeros((n, n, 4), np.uint8)
    out[..., :3] = (np.array([0.03, 0.05, 0.05]) * 255).astype(np.uint8)
    out[..., 3] = (np.clip(a * 0.75, 0, 1) * 255 + .5).astype(np.uint8)
    return out

def soot_base(w, h, rng):
    """Dirt gradient: opaque-ish at the bottom edge, soft noisy fade to 0 by the top; stretch to the wall width."""
    nz = noise(max(w, h), 1.8, rng)[:h, :w]
    nz2 = noise(max(w, h), 0.8, rng)[:h, :w]
    y = (np.linspace(1, 0, h)[:, None])                     # 1 at the bottom row
    a = np.clip(y ** 1.7 * (0.75 + 0.5 * (nz - .5)) + 0.18 * (nz2 - .5) * y, 0, 1) * 0.85
    out = np.zeros((h, w, 4), np.uint8)
    out[..., :3] = (np.array([0.012, 0.011, 0.012]) * 255).astype(np.uint8)
    out[..., 3] = (a * 255 + .5).astype(np.uint8)
    return out

def soot_radial(n, rng):
    """Soot bloom for around a candelabrum or sconce: dense in the centre, drifting upward, ragged edge."""
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32)
    cx = n / 2; cy = n * .55
    dx = (xx - cx) / (n * .42); dy = (yy - cy) / (n * .42)
    dy = np.where(dy < 0, dy * .7, dy * 1.15)               # extends further up than down
    r = np.sqrt(dx * dx + dy * dy)
    nz = noise(n, 1.7, rng)
    a = np.clip((1 - r + (nz - .5) * .6), 0, 1) ** 1.4 * 0.8
    out = np.zeros((n, n, 4), np.uint8)
    out[..., :3] = (np.array([0.012, 0.011, 0.012]) * 255).astype(np.uint8)
    out[..., 3] = (a * 255 + .5).astype(np.uint8)
    return out

DECALS = {}

def write_decals():
    specs = {
        "Castle_DecalCracksA": decal_cracks(1024, np.random.default_rng(2001), 4),
        "Castle_DecalCracksB": decal_cracks(1024, np.random.default_rng(2002), 6),
        "Castle_DecalChips": decal_chips(1024, np.random.default_rng(2003)),
        "Castle_DecalStain": decal_stain(1024, np.random.default_rng(2004)),
        "Castle_SootBase": soot_base(1024, 256, np.random.default_rng(2005)),
        "Castle_SootRadial": soot_radial(512, np.random.default_rng(2006)),
    }
    for name, arr in specs.items():
        p = cc.TEX / (name + ".png")
        save_png(p, arr)
        cc.write_texture_meta(p, "decal")
        print("decal", name, arr.shape[1], "x", arr.shape[0])

DECAL_SHADER = r'''// Generated by tools/castle_textures.py. Do not edit by hand.
// Alpha-blended lit decal for the built-in pipeline: draw on a 1x1 quad a couple of centimetres off the wall.
Shader "HollowCurrent/CastleDecal"
{
    Properties
    {
        _MainTex ("Decal (RGBA)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Smooth ("Smoothness", Range(0,1)) = 0.12
    }
    SubShader
    {
        Tags { "Queue"="Transparent-100" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Offset -1, -1
        ZWrite Off
        CGPROGRAM
        #pragma surface surf Standard alpha:blend
        #pragma target 3.0
        sampler2D _MainTex; fixed4 _Color; half _Smooth;
        float4 _CastleFillSky, _CastleFillGround;
        struct Input { float2 uv_MainTex; float3 worldNormal; };
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb; o.Alpha = c.a; o.Smoothness = _Smooth; o.Metallic = 0;
            o.Emission = c.rgb * lerp(_CastleFillGround.rgb, _CastleFillSky.rgb, IN.worldNormal.y * 0.5 + 0.5);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
'''

EMISSIVE_SHADER = r'''// Generated by tools/castle_textures.py. Do not edit by hand.
// Plain unlit HDR colour. Used by Castle_Flame so the flame meshes on standing candelabras glow without relying on the
// Standard shader's _EMISSION keyword (which was missing from the re-serialised .mat and rendered the flames black).
Shader "HollowCurrent/CastleEmissive"
{
    Properties { _Color ("Colour (HDR)", Color) = (1.15,0.42,0.07,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct v2f { float4 pos : SV_POSITION; };
            v2f vert (float4 v : POSITION) { v2f o; o.pos = UnityObjectToClipPos(v); return o; }
            fixed4 frag (v2f i) : SV_Target { return _Color; }
            ENDCG
        }
    }
}
'''

GORE_SHADER = r'''// Generated by tools/castle_textures.py. Do not edit by hand.
// Skin/flesh shader for the flayed-demon Count: UV-mapped albedo (the generated character texture), tileable blood overlay with
// a wet specular, pale skin tint and a cheap translucency glow at grazing angles. Falls back via CastleArt.Gore().
Shader "HollowCurrent/CastleGore"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal strength", Range(0,2)) = 1
        _MaskTex ("Mask (R metal, G occlusion, A smoothness)", 2D) = "black" {}
        _Smooth ("Base smoothness", Range(0,1)) = 0.4
        _WetTex ("Blood overlay (RGBA)", 2D) = "black" {}
        _WetTile ("Blood tiles per UV", Float) = 3
        _Wet ("Blood amount", Range(0,1)) = 0.6
        _WetSmooth ("Wet smoothness", Range(0,1)) = 0.92
        _Trans ("Translucency colour", Color) = (0.55,0.12,0.10,1)
        _TransAmt ("Translucency amount", Range(0,1)) = 0.35
        _TransPow ("Translucency rim power", Range(0.5,8)) = 2.5
        _EmissionColor ("Emission", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex, _BumpMap, _MaskTex, _WetTex;
        fixed4 _Color, _Trans, _EmissionColor;
        half _BumpScale, _Smooth, _WetTile, _Wet, _WetSmooth, _TransAmt, _TransPow;
        float4 _CastleFillSky, _CastleFillGround;
        struct Input { float2 uv_MainTex; float3 viewDir; float3 worldNormal; INTERNAL_DATA };
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            half3 n = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            n.xy *= _BumpScale;
            fixed4 wet = tex2D(_WetTex, IN.uv_MainTex * _WetTile);
            half w = saturate(wet.a * _Wet);
            fixed4 mk = tex2D(_MaskTex, IN.uv_MainTex);
            half3 albedo = lerp(c.rgb, wet.rgb, w * 0.9);
            o.Normal = normalize(n);
            o.Albedo = albedo;
            o.Metallic = mk.r * (1 - w);
            o.Smoothness = lerp(max(mk.a, _Smooth), _WetSmooth, w);
            o.Occlusion = lerp(1, mk.g, 0.85);
            half rim = pow(1 - saturate(dot(normalize(IN.viewDir), o.Normal)), _TransPow);
            half hemi = IN.worldNormal.y * 0.5 + 0.5;
            o.Emission = _EmissionColor.rgb + _Trans.rgb * rim * _TransAmt * (1 - w) + albedo * lerp(_CastleFillGround.rgb, _CastleFillSky.rgb, hemi);
        }
        ENDCG
    }
    FallBack "Standard"
}
'''

def write_custom_shader(name, source):
    p = SHADERS / (name + ".shader")
    p.write_text(source)
    Path(str(p) + ".meta").write_text(
        "fileFormatVersion: 2\nguid: %s\nShaderImporter:\n  externalObjects: {}\n  defaultTextures: []\n  nonModifiableTextures: []\n  preprocessorOverride: 0\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
        % cc.guid(cc.rel_to_project(p)))
    return cc.guid(cc.rel_to_project(p))

def write_shader_mat(name, shader_name, colors=None, texs=None, floats=None):
    """Material using one of our custom shaders. texs = {prop: guid}; colors = {prop: (r,g,b,a)}; floats = {prop: v}."""
    g = cc.guid(cc.rel_to_project(SHADERS / (shader_name + ".shader")))
    tex = "".join(cc._tex_env(k, v) for k, v in (texs or {}).items())
    cols = "".join("    - %s: {r: %s, g: %s, b: %s, a: %s}\n" % (k, *v) for k, v in (colors or {}).items())
    fl = "".join("    - %s: %s\n" % (k, v) for k, v in (floats or {}).items())
    path = cc.MATS / (name + ".mat")
    path.write_text(f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
  m_Shader: {{fileID: 4800000, guid: {g}, type: 3}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords: []
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap: {{}}
  disabledShaderPasses: []
  m_LockedProperties: 
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
{tex}    m_Ints: []
    m_Floats:
{fl}    m_Colors:
{cols}  m_BuildTextureStacks: []
  m_AllowLocking: 1
""")
    Path(str(path) + ".meta").write_text(
        "fileFormatVersion: 2\nguid: %s\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
        % cc.guid(cc.rel_to_project(path)))

def write_flame_mat():
    """Castle_Flame: HDR orange through the unlit CastleEmissive shader (guid derived from its path)."""
    g = cc.guid(cc.rel_to_project(SHADERS / "CastleEmissive.shader"))
    path = cc.MATS / "Castle_Flame.mat"
    path.write_text(f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: Castle_Flame
  m_Shader: {{fileID: 4800000, guid: {g}, type: 3}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords: []
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap: {{}}
  disabledShaderPasses: []
  m_LockedProperties: 
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs: []
    m_Ints: []
    m_Floats: []
    m_Colors:
    - _Color: {{r: 1.15, g: 0.42, b: 0.07, a: 1}}
  m_BuildTextureStacks: []
  m_AllowLocking: 1
""")

# ---------------------------------------------------------------------------------------------------------------------
def save_png(path, arr, mode=None):
    Image.fromarray(arr, mode).save(path, optimize=True)

def save_set(name, r, size, kind_albedo="color", filter_mode=1, aniso=None):
    out = {}
    if r["albedo"] is not None:
        a = cc.TEX / (name + "_albedo.png")
        save_png(a, (np.clip(r["albedo"], 0, 1) * 255 + .5).astype(np.uint8))
        out["albedo"] = cc.write_texture_meta(a, kind_albedo, filter_mode, aniso)
    b = cc.TEX / (name + "_normal.png")
    save_png(b, (to_normal(r["height"], r["ns"]) * 255 + .5).astype(np.uint8))
    out["normal"] = cc.write_texture_meta(b, "normal", filter_mode, aniso)
    m = cc.TEX / (name + "_mask.png")
    save_png(m, pack_mask(r["smooth"], r["metal"], r["ao"]))
    out["mask"] = cc.write_texture_meta(m, "mask", filter_mode, aniso)
    return out

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
    v = 1 - 0.55 * (((x - w / 2) / (w / 2)) ** 2 + ((y - h / 2) / (h / 2)) ** 2) ** 1.5
    img *= np.clip(v, .2, 1)[..., None]
    p = cc.TEX / "Portrait.png"
    Image.fromarray((np.clip(img, 0, 1) * 255 + .5).astype(np.uint8), "RGB").save(p, optimize=True)
    return cc.write_texture_meta(p, "color")

# ---------------------------------------------------------------------------------------------------------------------
# shaders. One template, three variants. Unity built-in pipeline surface shaders (Standard lighting).
#   CastleTriplanar : world-space triplanar projection (tiles per metre), for CastleWorld's scaled cube primitives
#   CastleLiquid    : triplanar + two scrolling normal layers (blood moat / crypt channel)
#   CastleLit       : ordinary UV mapping, for the FBX props; both add a global hemispheric "ambient fill"
# CastleArt.Surface() checks Shader.isSupported and falls back to the plain Standard .mat files if a variant fails to compile.
# ---------------------------------------------------------------------------------------------------------------------
SHADER_TEMPLATE = r'''// Generated by tools/castle_textures.py. Do not edit by hand.
Shader "HollowCurrent/__NAME__"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _MaskTex ("Mask (R metallic, G occlusion, A smoothness)", 2D) = "white" {}
        _BumpScale ("Normal strength", Range(0,3)) = 1
        _Tile ("Tiles per metre", Float) = 0.5
        _Sharp ("Projection blend", Range(1,16)) = 6
        _EmissionColor ("Emission", Color) = (0,0,0,1)
        _FillAmount ("Ambient fill", Range(0,2)) = 1
        _Flow ("Flow: scroll xy, layer-2 scale z", Vector) = (0.020,0.012,1.7,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        CGPROGRAM
__DEFINES__
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex, _BumpMap, _MaskTex;
        fixed4 _Color; half4 _EmissionColor; half _BumpScale, _Tile, _Sharp, _FillAmount;
        float4 _Flow;
        // set globally by CastleAtmosphere (linear values)
        float4 _CastleFillSky, _CastleFillGround;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
            INTERNAL_DATA
        };

#ifdef CASTLE_TRIPLANAR
        half3 SampleNormal(float2 uv)
        {
            half3 n = UnpackNormal(tex2D(_BumpMap, uv));
#ifdef CASTLE_FLOW
            half3 m = UnpackNormal(tex2D(_BumpMap, uv * _Flow.z + float2(-_Flow.y, _Flow.x) * _Time.y * 1.3 + float2(0.37, 0.61)));
            n = normalize(half3(n.xy + m.xy, n.z * m.z));
#endif
            return n;
        }
#endif

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 wn = normalize(WorldNormalVector(IN, float3(0,0,1)));
#ifdef CASTLE_TRIPLANAR
            float3 w = pow(abs(wn), _Sharp); w /= (w.x + w.y + w.z + 1e-5);
            float2 off = float2(0,0);
#ifdef CASTLE_FLOW
            off = _Flow.xy * _Time.y;
#endif
            float2 uvX = IN.worldPos.zy * _Tile + off;
            float2 uvY = IN.worldPos.xz * _Tile + off;
            float2 uvZ = IN.worldPos.xy * _Tile + off;
            fixed4 c = tex2D(_MainTex, uvX) * w.x + tex2D(_MainTex, uvY) * w.y + tex2D(_MainTex, uvZ) * w.z;
            fixed4 mk = tex2D(_MaskTex, uvX) * w.x + tex2D(_MaskTex, uvY) * w.y + tex2D(_MaskTex, uvZ) * w.z;
            half3 tx = SampleNormal(uvX), ty = SampleNormal(uvY), tz = SampleNormal(uvZ);
            tx.xy *= _BumpScale; ty.xy *= _BumpScale; tz.xy *= _BumpScale;
            // whiteout blend of the three tangent normals in world space
            tx = half3(tx.xy + wn.zy, abs(tx.z) * wn.x);
            ty = half3(ty.xy + wn.xz, abs(ty.z) * wn.y);
            tz = half3(tz.xy + wn.xy, abs(tz.z) * wn.z);
            float3 nW = normalize(tx.zyx * w.x + ty.xzy * w.y + tz.xyz * w.z);
            float3 T = WorldNormalVector(IN, float3(1,0,0));
            float3 B = WorldNormalVector(IN, float3(0,1,0));
            o.Normal = normalize(float3(dot(nW, T), dot(nW, B), dot(nW, wn)));
#else
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 mk = tex2D(_MaskTex, IN.uv_MainTex);
            half3 tn = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            tn.xy *= _BumpScale;
            o.Normal = normalize(tn);
#endif
            fixed3 albedo = c.rgb * _Color.rgb;
            o.Albedo = albedo;
            o.Metallic = mk.r;
            o.Smoothness = mk.a;
            o.Occlusion = lerp(1, mk.g, 0.85);
            float hemi = wn.y * 0.5 + 0.5;
            o.Emission = _EmissionColor.rgb + albedo * lerp(_CastleFillGround.rgb, _CastleFillSky.rgb, hemi) * _FillAmount * o.Occlusion;
        }
        ENDCG
    }
    FallBack "Standard"
}
'''
SHADER_VARIANTS = {
    "CastleTriplanar": "        #define CASTLE_TRIPLANAR 1",
    "CastleLiquid": "        #define CASTLE_TRIPLANAR 1\n        #define CASTLE_FLOW 1",
    "CastleLit": "        // plain UV mapping",
}

def write_shaders():
    SHADERS.mkdir(parents=True, exist_ok=True)
    cc.write_folder_meta(SHADERS)
    for name, defines in SHADER_VARIANTS.items():
        p = SHADERS / (name + ".shader")
        p.write_text(SHADER_TEMPLATE.replace("__NAME__", name).replace("__DEFINES__", defines))
        Path(str(p) + ".meta").write_text(
            "fileFormatVersion: 2\nguid: %s\nShaderImporter:\n  externalObjects: {}\n  defaultTextures: []\n  nonModifiableTextures: []\n  preprocessorOverride: 0\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
            % cc.guid(cc.rel_to_project(p)))
        print("shader", name)

FOG_SHADER = r'''// Generated by tools/castle_textures.py. Do not edit by hand.
// Unlit alpha-blended ground-fog layer for the built-in pipeline: world-space scrolling noise, soft plane edges,
// fades out near the camera so stacked layers never pop. Several of these at 0-1.2 m make the ground fog.
Shader "HollowCurrent/CastleFogLayer"
{
    Properties
    {
        _MainTex ("Noise (A)", 2D) = "white" {}
        _Color ("Fog colour", Color) = (0.30,0.36,0.50,1)
        _Alpha ("Alpha", Range(0,1)) = 0.2
        _Tile ("Noise tiles per metre", Float) = 0.22
        _Scroll ("Scroll xy (tiles per s)", Vector) = (0.010,0.006,0,0)
        _Fade ("Camera fade start, end (m)", Vector) = (0.8,2.6,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 _Color; half _Alpha, _Tile; float4 _Scroll, _Fade;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 wpos : TEXCOORD1; };
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float2 w = i.wpos.xz * _Tile + _Scroll.xy * _Time.y;
                float a = tex2D(_MainTex, w).a * 0.6 + tex2D(_MainTex, w * 2.3 + float2(0.31, 0.77) - _Scroll.xy * _Time.y * 1.7).a * 0.4;
                a = saturate((a - 0.22) * 2.4);
                float2 e = smoothstep(0.0, 0.32, i.uv) * smoothstep(0.0, 0.32, 1.0 - i.uv);
                float d = distance(_WorldSpaceCameraPos, i.wpos);
                float f = smoothstep(_Fade.x, _Fade.y, d);
                fixed4 c = _Color;
                c.a = a * e.x * e.y * f * _Alpha;
                return c;
            }
            ENDCG
        }
    }
}
'''

def fog_noise(n, rng):
    """Soft tileable cloud in the alpha channel (RGB white) for the ground-fog layers."""
    a = norm01(0.6 * noise(n, 2.2, rng) + 0.4 * noise(n, 1.4, rng))
    out = np.zeros((n, n, 4), np.uint8); out[..., :3] = 255; out[..., 3] = (a * 255 + .5).astype(np.uint8)
    p = cc.TEX / "Castle_FogNoise.png"
    save_png(p, out)
    return cc.write_texture_meta(p, "emission")   # sRGB off is not needed for alpha; repeat wrap, mips

# ---------------------------------------------------------------------------------------------------------------------
def main():
    for d in (cc.CASTLE, cc.TEX, cc.MATS, cc.SKY):
        d.mkdir(parents=True, exist_ok=True)
    for d in (cc.CASTLE, cc.TEX, cc.MATS, cc.SKY):
        cc.write_folder_meta(d)
    n = SIZE
    gens = {
        "Castle_Stone": lambda r, b: stone(n, r, b),
        "Castle_BlackStone": lambda r, b: blackstone(n, r, b),
        "Castle_FlagStone": lambda r, b: flagstone(n, r, b),
        "Castle_Oak": lambda r, b: planks(n, r, b),
        "Castle_Brass": lambda r, b: metal(n, r, b, patina=0.45, metal_v=0.88),
        "Castle_Oxblood": lambda r, b: cloth(n, r, b),
        "Castle_Silver": lambda r, b: metal(n, r, b, patina=0.12, metal_v=0.92),
        "Castle_Iron": lambda r, b: metal(n, r, b, patina=0.05, brushed=False, metal_v=0.85, scratch_count=300),
        "Castle_Bone": lambda r, b: bone(n, r, b),
        "Castle_Paper": lambda r, b: paper(n, r, b),
        "Castle_Blood": lambda r, b: blood(n, r, b),
        "Castle_StoneWeathered": lambda r, b: stone_weathered(n, r, b),
        "Castle_StoneDark": lambda r, b: stone_dark(n, r, b),
        "Castle_StoneMossy": lambda r, b: stone_mossy(n, r, b),
        "Castle_OakPlank": lambda r, b: oak_plank(n, r, b),
        "Castle_GoreSkin": lambda r, b: gore_skin(n, r, b),
        "Castle_GoreFlesh": lambda r, b: gore_flesh(n, r, b),
        "Castle_GoreMembrane": lambda r, b: gore_membrane(n, r, b),
        "Castle_Moonstone": lambda r, b: moonstone(n, r, b),
    }
    for i, (name, fn) in enumerate(gens.items()):
        rng = np.random.default_rng(1000 + i)
        base, smooth, metal_v, _ = cc.MATERIALS[name]
        r = fn(rng, base)
        # cloth: trilinear + 8x aniso so the 8 px weave cannot crawl or stripe at grazing angles (S5 banners, round 7)
        g = save_set(name, r, n, filter_mode=2 if name == "Castle_Oxblood" else 1, aniso=8 if name == "Castle_Oxblood" else None)
        if name == "Castle_Moonstone":
            write_shader_mat(name, "CastleLit", colors={"_Color": (1, 1, 1, 1), "_EmissionColor": (0.10, 0.17, 0.38, 1)},
                             texs={"_MainTex": g["albedo"], "_BumpMap": g["normal"], "_MaskTex": g["mask"]}, floats={"_BumpScale": 1, "_FillAmount": 1})
            print("texture", name, n)
            continue
        em = None
        bump = 1.0
        if name == "Castle_Blood":
            em = (0.30, 0.012, 0.020)
        cc.write_standard_mat(cc.MATS / (name + ".mat"), name, base, smooth, metal_v, g["albedo"], g["normal"],
                              emission=em, mask_guid=g["mask"], bump_scale=bump)
        print("texture", name, n)
    # portrait + canvas weave
    pg = portrait(np.random.default_rng(77))
    r = canvas_normal(n, np.random.default_rng(78))
    g = save_set("Castle_Canvas", r, n)
    cc.write_standard_mat(cc.MATS / "Castle_Canvas.mat", "Castle_Canvas", (1, 1, 1), 0.12, 0.0, pg, g["normal"],
                          mask_guid=g["mask"], bump_scale=0.6)
    print("texture Castle_Canvas", n)
    # sun window (own aspect ratio), emissive blue leak
    rng = np.random.default_rng(79)
    albedo, emission, height, smooth = sun_window(rng)
    a = cc.TEX / "Castle_SunWindow_albedo.png"; e = cc.TEX / "Castle_SunWindow_emission.png"
    nm = cc.TEX / "Castle_SunWindow_normal.png"; mk = cc.TEX / "Castle_SunWindow_mask.png"
    save_png(a, (np.clip(albedo, 0, 1) * 255 + .5).astype(np.uint8)); ga = cc.write_texture_meta(a, "color")
    save_png(e, (np.clip(emission, 0, 1) * 255 + .5).astype(np.uint8)); ge = cc.write_texture_meta(e, "emission")
    save_png(nm, (to_normal(height, 3.0) * 255 + .5).astype(np.uint8)); gn = cc.write_texture_meta(nm, "normal")
    save_png(mk, pack_mask(smooth, 0.0, 1.0)); gm = cc.write_texture_meta(mk, "mask")
    cc.write_standard_mat(cc.MATS / "Castle_SunWindow.mat", "Castle_SunWindow", (1, 1, 1), 0.55, 0.0, ga, gn,
                          emission=(1.7, 1.7, 1.7), mask_guid=gm, emission_guid=ge)
    print("texture Castle_SunWindow", albedo.shape)
    # flat-colour materials
    gore_wet(512, np.random.default_rng(3001))
    write_custom_shader("CastleDecal", DECAL_SHADER)
    write_custom_shader("CastleGore", GORE_SHADER)
    write_custom_shader("CastleEmissive", EMISSIVE_SHADER)
    write_flame_mat()
    write_shader_mat("Castle_RuneGlow", "CastleEmissive", colors={"_Color": (0.25, 0.60, 1.6, 1)})
    write_decals()
    fog_noise(512, np.random.default_rng(80))
    (SHADERS / "CastleFogLayer.shader").write_text(FOG_SHADER)
    Path(str(SHADERS / "CastleFogLayer.shader") + ".meta").write_text(
        "fileFormatVersion: 2\nguid: %s\nShaderImporter:\n  externalObjects: {}\n  defaultTextures: []\n  nonModifiableTextures: []\n  preprocessorOverride: 0\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
        % cc.guid(cc.rel_to_project(SHADERS / "CastleFogLayer.shader")))
    write_shaders()
    print("CASTLE_TEXTURES_PASS")

if __name__ == "__main__":
    main()
