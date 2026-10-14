#!/usr/bin/env python3
"""Round 12 (hc/art-props) materials: book leathers + pages, worn gilt, root bark, hand flesh, grimy bone, carved niche stone.

    python3 tools/castle_r12_textures.py            # all nine
    python3 tools/castle_r12_textures.py Castle_RootBark ...

Same conventions as tools/castle_textures.py (it supplies the noise toolbox, PNG writers and metadata): 1024 px, albedo /
normal / mask (R metallic, G occlusion, A smoothness), a Standard .mat per material in Resources/Castle/Materials, fixed
seeds, idempotent, numpy + Pillow only. The existing castle textures are not touched (castle_textures.py is not re-run).
Covers (BookLeather*) and pages (BookPages) are NOT tileable: one cover board maps to the whole square (0.22 x 0.30 m of
design stretched to 1024 x 1024) and the plain border band feeds the spine and edges; pages carry a text field plus a
fore-edge band (see the layout notes at BookPages). Everything else tiles.
"""
import sys, math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
sys.path.insert(0, str(Path(__file__).resolve().parent))
import castle_common as cc
import castle_textures as ct
from castle_textures import noise, fbm, smoothstep, blur, norm01, mean_to, scratches, save_set

N = ct.SIZE
SS = 2  # supersample factor for drawn designs

R12 = {k: v[:3] for k, v in cc.MATERIALS.items() if k in (
    "Castle_BookLeatherA", "Castle_BookLeatherB", "Castle_BookLeatherC", "Castle_BookPages", "Castle_GiltWorn", "Castle_RootBark",
    "Castle_RootFlesh", "Castle_BoneGrime", "Castle_NicheStone")}   # declared in castle_common.MATERIALS

# ---------------------------------------------------------------------------------------------------------------------
def colorize(base, lum):
    return np.clip(np.array(base)[None, None, :] * lum[..., None], 0, 1)

def edge_wear(n, rng, width=0.05):
    """1 at the texture border, falling to 0 over `width` (cover edges rub through first); broken up by noise."""
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32) / n
    d = np.minimum(np.minimum(xx, 1 - xx), np.minimum(yy, 1 - yy))
    return np.clip(1 - d / (width * (0.6 + 0.8 * noise(n, 1.2, rng))), 0, 1)

def leather_grain(n, rng):
    """Pebbled leather: dark creases between flat cells (cheap Voronoi from jittered grid) plus soft wrinkles."""
    cells = 70
    jx = (rng.random((cells, cells)) - .5) * .8; jy = (rng.random((cells, cells)) - .5) * .8
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32)
    cx = xx / n * cells; cy = yy / n * cells
    ix = np.floor(cx).astype(int); iy = np.floor(cy).astype(int)
    d1 = np.full((n, n), 9.0, np.float32); d2 = np.full((n, n), 9.0, np.float32)
    for oy in (-1, 0, 1):
        for ox in (-1, 0, 1):
            gx = (ix + ox) % cells; gy = (iy + oy) % cells
            px = ix + ox + .5 + jx[gy, gx]; py = iy + oy + .5 + jy[gy, gx]
            d = np.sqrt((cx - px) ** 2 + (cy - py) ** 2)
            d2 = np.minimum(d2, np.maximum(d, d1)); d1 = np.minimum(d1, d)
    crease = np.clip(1 - (d2 - d1) * 3.2, 0, 1)            # 1 on cell borders
    wrinkle = np.clip(1 - np.abs(noise(n, 1.6, rng) - .5) * 14, 0, 1) * .7
    return crease, wrinkle

def draw_canvas(n):
    big = Image.new("L", (n * SS, n * SS), 0)
    return big, ImageDraw.Draw(big)

def to_arr(big, blur_px=0.0):
    if blur_px: big = big.filter(ImageFilter.GaussianBlur(blur_px * SS))
    return np.asarray(big.resize((N, N), Image.LANCZOS), dtype=np.float32) / 255.0

# cover design space: metres, x 0..0.22 right, y 0..0.30 up; (mapped to the full square)
CW, CH = 0.22, 0.30
def P(x, y, n=N):
    return (x / CW * n * SS, (1 - y / CH) * n * SS)

def line(d, pts, w):
    d.line([P(*p) for p in pts], fill=255, width=max(2, int(w / CW * N * SS)), joint="curve")

def frame(d, inset, w, radius=0.0):
    x0, y0, x1, y1 = inset, inset, CW - inset, CH - inset
    pts = [(x0, y0), (x1, y0), (x1, y1), (x0, y1), (x0, y0)]
    line(d, pts, w)

def circle(d, cx, cy, r, w, start=0, end=360):
    a = [(cx + r * math.cos(math.radians(t)), cy + r * math.sin(math.radians(t))) for t in np.linspace(start, end, 72)]
    line(d, a, w)

def poly(d, pts):
    d.polygon([P(*p) for p in pts], fill=255)

def diamond(d, cx, cy, r):
    poly(d, [(cx + r, cy), (cx, cy + r * 1.35), (cx - r, cy), (cx, cy - r * 1.35)])

def fleuron(d, cx, cy, r, w):
    for k in range(4):
        a = math.radians(45 + 90 * k)
        line(d, [(cx, cy), (cx + r * math.cos(a), cy + r * math.sin(a))], w)
    diamond(d, cx, cy, r * .22)

def runes(d, cx, cy, r, count, size, w, rng):
    for i in range(count):
        a = math.tau * i / count
        px, py = cx + r * math.cos(a), cy + r * math.sin(a)
        ux, uy = math.cos(a + math.pi / 2), math.sin(a + math.pi / 2)   # tangent
        rx, ry = math.cos(a), math.sin(a)
        stem = [(px - rx * size, py - ry * size), (px + rx * size, py + ry * size)]
        line(d, stem, w)
        for _ in range(1 + int(rng.integers(0, 3))):
            t = rng.uniform(-.6, .6); s = rng.choice([-1, 1]); l = rng.uniform(.4, .9) * size
            bx, by = px + rx * size * t, py + ry * size * t
            line(d, [(bx, by), (bx + ux * s * l + rx * l * rng.uniform(-.7, .7), by + uy * s * l + ry * l * rng.uniform(-.7, .7))], w)

def design_A(rng):
    g, gd = draw_canvas(N); b, bd = draw_canvas(N)
    for i, (ins, w) in enumerate(((.022, .0016), (.030, .0008))): frame(gd, ins, w)
    for (cx, cy, sx, sy) in ((.022, .022, 1, 1), (CW - .022, .022, -1, 1), (.022, CH - .022, 1, -1), (CW - .022, CH - .022, -1, -1)):
        circle(gd, cx + sx * .02, cy + sy * .02, .02, .0012, 0, 360); fleuron(gd, cx + sx * .02, cy + sy * .02, .013, .0009)
    # crescent moon + star in a ring on the cover centre
    circle(gd, CW / 2, CH / 2 + .012, .052, .0016); circle(gd, CW / 2, CH / 2 + .012, .046, .0008)
    for k in range(24):
        a = math.tau * k / 24
        line(gd, [(CW / 2 + .056 * math.cos(a), CH / 2 + .012 + .056 * math.sin(a)), (CW / 2 + .066 * math.cos(a), CH / 2 + .012 + .066 * math.sin(a))], .0008)
    a0 = np.linspace(70, 290, 40)
    outer = [(CW / 2 - .004 + .032 * math.cos(math.radians(t)), CH / 2 + .012 + .032 * math.sin(math.radians(t))) for t in a0]
    inner = [(CW / 2 + .008 + .026 * math.cos(math.radians(t)), CH / 2 + .012 + .026 * math.sin(math.radians(t))) for t in a0[::-1]]
    poly(gd, outer + inner)
    for k in range(5):
        a = math.radians(90 + 72 * k)
        line(gd, [(CW / 2 + .025 + .006 * math.cos(a), CH / 2 + .012 + .006 * math.sin(a)), (CW / 2 + .025 + .011 * math.cos(a + math.radians(36)), CH / 2 + .012 + .011 * math.sin(a + math.radians(36)))], .0008)
    # title cartouche lines below
    for y, wd in ((CH * .20, .09), (CH * .165, .06)): line(gd, [(CW / 2 - wd / 2, y), (CW / 2 + wd / 2, y)], .0014)
    # blind-tooled panel lines (deboss only) between the frame and ring
    frame(bd, .040, .0012); frame(bd, .046, .0008)
    for yy in (CH * .30, CH * .70): line(bd, [(.05, yy), (CW - .05, yy)], .0008)
    return to_arr(g, .35), to_arr(b, .35)

def design_B(rng):
    g, gd = draw_canvas(N); b, bd = draw_canvas(N)
    frame(bd, .018, .0030); frame(bd, .026, .0016); frame(bd, .034, .0010)
    for yy in (.075, .105, CH - .105, CH - .075): line(bd, [(.018, yy), (CW - .018, yy)], .0012)
    cx, cy = CW / 2, CH / 2
    circle(bd, cx, cy, .062, .0030); circle(bd, cx, cy, .050, .0016); circle(bd, cx, cy, .040, .0016)
    runes(bd, cx, cy, .056, 16, .0045, .0012, rng)
    # ouroboros-ish eye
    for t in range(-1, 2): line(bd, [(cx - .03, cy + t * .003), (cx + .03, cy + t * .003)], .0009)
    circle(bd, cx, cy, .012, .0016); poly(bd, [(cx - .005, cy), (cx, cy + .007), (cx + .005, cy), (cx, cy - .007)])
    for k in range(8):
        a = math.tau * k / 8
        line(bd, [(cx + .066 * math.cos(a), cy + .066 * math.sin(a)), (cx + .088 * math.cos(a), cy + .088 * math.sin(a))], .0016)
    # a little gilt left in the ring only
    circle(gd, cx, cy, .062, .0010); diamond(gd, cx, cy + .095, .003); diamond(gd, cx, cy - .095, .003)
    return to_arr(g, .35), to_arr(b, .35)

def design_C(rng):
    g, gd = draw_canvas(N); b, bd = draw_canvas(N)
    frame(gd, .016, .0020)
    for (cx, cy) in ((.016, .016), (CW - .016, .016), (.016, CH - .016), (CW - .016, CH - .016)):
        poly(gd, [(cx - .012, cy - .012), (cx + .012, cy - .012), (cx + .012, cy + .012), (cx - .012, cy + .012)]) if False else circle(gd, cx, cy, .007, .0020)
    # title cartouche: rectangle + fake letters
    x0, x1, y0, y1 = .035, CW - .035, CH - .095, CH - .045
    frame_pts = [(x0, y0), (x1, y0), (x1, y1), (x0, y1), (x0, y0)]
    line(gd, frame_pts, .0014)
    for i in range(18):
        x = x0 + .008 + i * (x1 - x0 - .016) / 18
        h = rng.uniform(.010, .018)
        line(gd, [(x, (y0 + y1) / 2 - h / 2), (x, (y0 + y1) / 2 + h / 2)], .0012)
        if rng.random() < .6: line(gd, [(x, (y0 + y1) / 2 + h / 2), (x + .004, (y0 + y1) / 2 + h * .15)], .0010)
    # cross with splayed ends
    cx, cy = CW / 2, CH * .44
    line(gd, [(cx, cy - .075), (cx, cy + .075)], .0050); line(gd, [(cx - .048, cy + .022), (cx + .048, cy + .022)], .0050)
    for (px, py, d) in ((cx, cy + .075, (0, 1)), (cx, cy - .075, (0, -1)), (cx - .048, cy + .022, (-1, 0)), (cx + .048, cy + .022, (1, 0))):
        ux, uy = d[1], -d[0]
        poly(gd, [(px + ux * .008, py + uy * .008), (px - ux * .008, py - uy * .008), (px + d[0] * .010 - ux * .004, py + d[1] * .010 - uy * .004), (px + d[0] * .010 + ux * .004, py + d[1] * .010 + uy * .004)]) if False else circle(gd, px + d[0] * .006, py + d[1] * .006, .0045, .0018)
    for k in range(3): circle(gd, cx, cy + .022, .012 + k * .0055, .0008)
    frame(bd, .028, .0010); frame(bd, .030, .0006)
    return to_arr(g, .35), to_arr(b, .35)

def cover(n, rng, base, designer, gilt_col, worn, grain_scale=1.0):
    crease, wrinkle = leather_grain(n, rng)
    stain = np.clip(noise(n, 2.2, rng) - .56, 0, 1) * 2.4
    mott = fbm(n, rng, (1.4, 2.0), (1, .6))
    gilt, deb = designer(np.random.default_rng(int(rng.integers(1 << 30))))
    wear = np.clip(edge_wear(n, rng, .06) * .9 + smoothstep(.62, .85, noise(n, 1.9, rng)) * .6 - .1, 0, 1)   # scuffed patches and edges
    lum = 0.82 + .24 * (mott - .5) - .26 * crease - .12 * wrinkle - .30 * stain
    lum = lum * (1 - .35 * deb)                                                    # blind tooling sits darker
    col = colorize(base, lum)
    col = col * (1 - .55 * wear[..., None]) + np.array(base)[None, None, :] * 1.35 * .55 * wear[..., None] * (0.6 + 0.4 * crease)[..., None] * 1.0  # rubbed edges go pale
    flakes = smoothstep(.45, .75, noise(n, .8, rng))
    gmask = np.clip(gilt * (1 - worn * (.3 + .7 * flakes) - .5 * wear), 0, 1)
    gc = np.array(gilt_col)[None, None, :] * (0.8 + .4 * noise(n, .6, rng))[..., None]
    col = col * (1 - gmask[..., None]) + gc * gmask[..., None]
    h = norm01(1.0 - .55 * crease * grain_scale - .25 * wrinkle + .3 * mott - .85 * np.maximum(deb, gilt * .8) + .15 * gilt)
    smooth = .24 + .10 * (1 - crease) + .30 * gmask + .15 * wear - .2 * stain
    metal = gmask * .75
    return dict(albedo=np.clip(col, 0, 1), height=h, ns=3.2, smooth=smooth, metal=metal, ao=1 - .45 * crease * .5 - .25 * wear)

def leatherA(n, rng, base): return cover(n, rng, base, design_A, (.78, .58, .24), .45)
def leatherB(n, rng, base): return cover(n, rng, base, design_B, (.62, .46, .20), .75)
def leatherC(n, rng, base): return cover(n, rng, base, design_C, (.74, .60, .30), .35)

# ---------------------------------------------------------------------------------------------------------------------
# pages: text field v in [.14, 1], fore-edge band v in [0, .12]. Lines run along u (across the page, perpendicular to the spine).
def pages(n, rng, base):
    paper = noise(n, 0.9, rng, sx=0.7, sy=2.0) * .5 + noise(n, 0.9, rng, sx=2.0, sy=0.7) * .5
    fox = smoothstep(.62, .85, noise(n, 1.6, rng)) * .35 + np.clip(noise(n, 0.6, rng) - .8, 0, 1) * 4     # foxing blotches and spots
    water = smoothstep(.55, .70, fbm(n, rng, (2.0, 2.6), (1, .6))) * .15
    lum = .90 + .2 * (paper - .5) - .30 * fox - .25 * water
    col = colorize(base, lum)
    # text drawn into a big canvas (ink density)
    ink, d = draw_canvas(n)
    top, bottom = .985, .15
    left, right = .09, .91
    y = top - .05
    line_h = .0265
    while y > bottom + .03:
        x = left
        if rng.random() < .12: y -= line_h * .8; continue                        # paragraph gaps
        indent = rng.uniform(.0, .06) if rng.random() < .25 else 0
        x += indent
        while x < right:
            w = rng.uniform(.018, .075)
            if x + w > right: w = right - x
            if w < .01: break
            pts = []
            steps = max(3, int(w / .008))
            for k in range(steps + 1):
                px = x + w * k / steps
                py = y + (.006 * math.sin(k * 2.1 + rng.uniform(0, 6)) if k % 2 else -.002 + rng.uniform(-.001, .001))
                pts.append((px * n * SS, (1 - py) * n * SS))
            d.line(pts, fill=int(rng.uniform(150, 255)), width=int(2.2 * SS))
            x += w + rng.uniform(.012, .022)
        y -= line_h
    # illuminated drop cap and a marginal diagram
    d.rectangle([left * n * SS, (1 - (top - .05)) * n * SS, (left + .05) * n * SS, (1 - (top - .05 - .05)) * n * SS], outline=255, width=2 * SS)
    cx, cy = .5 * n * SS, (1 - .42) * n * SS
    if True:
        d.ellipse([cx - .1 * n * SS, cy - .1 * n * SS, cx + .1 * n * SS, cy + .1 * n * SS], outline=190, width=2 * SS)
        for k in range(8):
            a = math.tau * k / 8
            d.line([(cx, cy), (cx + .1 * n * SS * math.cos(a), cy + .1 * n * SS * math.sin(a))], fill=150, width=2 * SS)
    inkv = to_arr(ink, .5) * (0.5 + 0.5 * noise(n, 0.7, rng))
    # margins and a gutter-side ink trail: fade text near the very edges
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32) / n
    field = ((yy < 1 - 0.0) & (yy > 0.0)).astype(np.float32)
    inkcol = np.array([.09, .06, .04])[None, None, :]
    col = col * (1 - .85 * inkv[..., None]) + inkcol * .85 * inkv[..., None]
    col = col * (1 - .25 * np.clip(noise(n, 2.4, rng) - .6, 0, 1)[..., None] * 3)
    # some rubricated (red) words
    red = np.clip(noise(n, 0.35, rng) - .86, 0, 1) * 6 * np.clip(inkv * 3, 0, 1)
    col = col * (1 - red[..., None] * .8) + np.array([.42, .05, .05])[None, None, :] * red[..., None] * .8
    # fore-edge band: fine horizontal layer lines, slightly dirtier than the page face
    band = yy > 0.88                                          # image rows from the top: v < .12 (PNG row 0 = v 1)
    layers = (np.sin(yy * n * 2 * np.pi / 5.0 + 6 * noise(n, 1.4, rng, sx=.3, sy=1.0)) * .5 + .5) ** 1.5 * (.55 + .45 * noise(n, .7, rng, sx=.3, sy=3.0))
    bandlum = .62 + .38 * layers - .2 * fox
    bandcol = colorize(np.array(base) * .94, bandlum)
    col = np.where(band[..., None], bandcol, col)
    h = norm01(.6 * paper + .3 * np.where(band, layers, 0) - .5 * inkv + .1 * water)
    return dict(albedo=mean_to(np.clip(col, 0, 1), base) * 1.0, height=h, ns=1.6, smooth=.08 + .05 * (1 - fox), metal=0.0, ao=1 - .3 * np.where(band, 1 - layers, 0) * .6)

# ---------------------------------------------------------------------------------------------------------------------
def gilt_worn(n, rng, base):
    leaf = fbm(n, rng, (1.4, 2.2), (1, .6)); fine = noise(n, .45, rng)
    rub = smoothstep(.60, .78, fbm(n, rng, (.9, 1.3, 1.9), (1, .7, .5)))              # 1 where the gilding is gone
    sc = np.clip(scratches(n, rng, 260, n * .08) + scratches(n, rng, 120, n * .03), 0, 1)
    grime = smoothstep(.55, .85, noise(n, 1.9, rng)) * .6
    gold = np.array(base)[None, None, :] * (.82 + .35 * (leaf - .5) + .12 * (fine - .5))[..., None]
    bole = np.array([.30, .10, .07])[None, None, :] * (.7 + .5 * fine)[..., None]
    k = np.clip(rub - .25 * sc, 0, 1)[..., None]
    col = gold * (1 - k) + bole * k
    col = col * (1 - grime[..., None] * .55) + np.array([.07, .06, .05])[None, None, :] * grime[..., None] * .55
    metal = np.clip(.9 * (1 - k[..., 0]) - .2 * grime, 0, 1)
    h = norm01(.5 * leaf + .3 * fine - .7 * rub * .6 - .5 * sc)
    return dict(albedo=np.clip(col, 0, 1), height=h, ns=2.5, smooth=.62 * (1 - k[..., 0]) + .18 * k[..., 0] - .25 * grime, metal=metal, ao=1 - .35 * grime)

def fibres(n, rng, beta=.85, warp=36.0, warp_cells=3, along="y"):
    """Fine parallel fibres: a 1/f line pattern copied down the image with a slowly wandering phase (tileable both ways)."""
    f = np.fft.rfft(rng.standard_normal(n)); k = np.arange(len(f)); k[0] = 1
    line_ = np.fft.irfft(f / k ** beta, n); line_ = (line_ - line_.min()) / (line_.max() - line_.min() + 1e-9)
    w = noise(n, 2.4, rng)[:, 0:1] if False else None
    phase = np.cumsum(rng.standard_normal(n)); phase -= np.linspace(phase[0], phase[-1], n)      # periodic drift
    g = np.exp(-.5 * (np.arange(-60, 61) / 18.0) ** 2); g /= g.sum()
    phase = np.convolve(np.concatenate([phase[-60:], phase, phase[:60]]), g, mode='same')[60:-60]
    phase = phase / (np.abs(phase).max() + 1e-9) * warp
    xs = (np.arange(n)[None, :] + phase[:, None]).astype(int) % n
    out = line_[xs]
    return out if along == "y" else out.T

def root_bark(n, rng, base):
    fib = fibres(n, rng, .75, 30); fib2 = fibres(n, rng, 1.1, 60); fib3 = fibres(n, rng, .5, 14)
    ridge = np.clip(1 - np.abs(fib2 - .5) * 5, 0, 1)
    crack = np.clip(1 - np.abs(fibres(n, rng, 1.0, 50) - .5) * 38, 0, 1) * smoothstep(.45, .7, noise(n, 1.4, rng, sx=.5, sy=3.0))
    wet = smoothstep(.4, .7, noise(n, 2.2, rng, sx=.4, sy=5.0))
    moss = smoothstep(.62, .82, fbm(n, rng, (1.6, 2.4), (1, .6))) * .45
    lum = .95 + .75 * (fib - .5) + .45 * (fib2 - .5) + .35 * (fib3 - .5) + .30 * (ridge - .3) - .65 * crack - .1 * wet
    col = colorize(base, lum)
    col = col * (1 - moss[..., None]) + np.array([.11, .16, .10])[None, None, :] * moss[..., None] * (.6 + .8 * fib)[..., None]
    pale = np.clip(fib3 - .72, 0, 1) * 5 * (.3 + ridge)                          # pale stringy fibres catching the light
    col = col + pale[..., None] * np.array([.20, .16, .12])[None, None, :]
    h = norm01(.55 * fib + .40 * fib2 + .35 * fib3 + .35 * ridge - .8 * crack)
    return dict(albedo=np.clip(col, 0, 1), height=h, ns=6.5, smooth=.16 + .3 * wet - .15 * crack - .08 * ridge, metal=0.0, ao=1 - .55 * crack - .12 * (1 - ridge))

def root_flesh(n, rng, base):
    v = ct.veins(n, rng, 24, 38); pores = noise(n, .35, rng); mott = fbm(n, rng, (1.4, 2.0), (1, .6))
    crease = np.clip(1 - np.abs(noise(n, 1.7, rng, sx=3.0, sy=.5) - .5) * 26, 0, 1)             # fine transverse wrinkles (knuckles)
    cracks = np.clip(1 - np.abs(noise(n, 1.8, rng) - .5) * 150, 0, 1) * smoothstep(.55, .7, noise(n, 1.2, rng)) * .5
    dirt = smoothstep(.6, .85, noise(n, 1.8, rng)) * .6
    col = colorize(base, .88 + .3 * (mott - .5) + .1 * (pores - .5))
    col = col * (1 - .22 * v[..., None]) + np.array([.34, .38, .50])[None, None, :] * .22 * v[..., None]
    col = col * (1 + .12 * (noise(n, .2, rng) - .5) * 2)[..., None]
    col = col * (1 - .30 * crease[..., None]) * (1 - .6 * cracks[..., None]) + np.array([.22, .08, .07])[None, None, :] * .6 * cracks[..., None]
    col = col * (1 - .45 * dirt[..., None]) + np.array([.14, .11, .08])[None, None, :] * .45 * dirt[..., None]
    h = norm01(.45 * pores + .25 * v + .3 * mott - .45 * crease - .7 * cracks)
    return dict(albedo=np.clip(col, 0, 1), height=h, ns=3.2, smooth=.34 + .2 * (1 - pores) - .25 * dirt + .3 * cracks, metal=0.0, ao=1 - .4 * cracks - .15 * crease)

def bone_grime(n, rng, base):
    f = fbm(n, rng); grain = noise(n, 1.4, rng, sx=.25, sy=7.0); fine = noise(n, .3, rng)
    pit = np.clip(noise(n, .8, rng) - .76, 0, 1) * 4
    crack = np.clip(1 - np.abs(noise(n, 1.9, rng, sx=.6, sy=2.5) - .5) * 170, 0, 1) * smoothstep(.45, .65, noise(n, 1.2, rng))
    soil = smoothstep(.5, .8, fbm(n, rng, (1.6, 2.4, 3.2), (1, .6, .4)))
    dust = smoothstep(.62, .85, noise(n, 1.3, rng)) * .5
    stain = smoothstep(.55, .78, noise(n, 2.1, rng, sx=.5, sy=3.0))               # long-bone green-black staining
    crev = np.clip(pit + crack, 0, 1)
    lum = .85 + .35 * (f - .5) + .25 * (grain - .5) + .08 * (fine - .5) - .35 * pit - .5 * crack
    col = colorize(base, lum)
    col = col * (1 - .6 * (soil * (.3 + crev))[..., None]) + np.array([.14, .11, .08])[None, None, :] * (.6 * (soil * (.3 + crev)))[..., None]
    col = col * (1 - .45 * stain[..., None]) + np.array([.12, .15, .11])[None, None, :] * .45 * stain[..., None]
    col = col + dust[..., None] * np.array([.14, .13, .11])[None, None, :]
    h = norm01(.6 * f + .35 * grain + .1 * fine - .5 * pit - .6 * crack - .3 * soil)
    return dict(albedo=np.clip(col, 0, 1), height=h, ns=4.2, smooth=.22 + .1 * (1 - soil) - .12 * dust - .1 * pit, metal=0.0, ao=np.clip(1 - .5 * crev - .3 * soil, 0, 1))

def niche_stone(n, rng, base):
    drag = noise(n, 1.4, rng, sx=.25, sy=7.0)                                      # chisel drag
    cell = fbm(n, rng, (1.2, 1.9), (1, .5)); pit = np.clip(noise(n, .8, rng) - .78, 0, 1) * 4
    chip = smoothstep(.7, .8, noise(n, 1.1, rng))
    crack = np.clip(1 - np.abs(noise(n, 1.9, rng) - .5) * 160, 0, 1) * smoothstep(.5, .7, noise(n, 1.3, rng))
    damp = smoothstep(.45, .75, noise(n, 2.2, rng, sx=.4, sy=6.0))
    soot = smoothstep(.55, .85, fbm(n, rng, (1.8, 2.6), (1, .5))) * .5
    lum = .78 + .35 * (cell - .5) + .2 * (drag - .5) - .4 * pit - .5 * crack - .25 * damp - .3 * soot
    col = colorize(base, lum)
    col = col * (1 + .15 * chip[..., None])
    h = norm01(.5 * cell + .45 * drag - .6 * pit - .8 * crack - .3 * chip)
    return dict(albedo=mean_to(np.clip(col, 0, 1), base), height=h, ns=4.0, smooth=.20 + .35 * damp - .15 * pit - .1 * soot, metal=0.0, ao=1 - .5 * crack - .3 * np.clip(pit, 0, 1))

GENS = {
    "Castle_BookLeatherA": (leatherA, dict(filter_mode=2, aniso=8)),
    "Castle_BookLeatherB": (leatherB, dict(filter_mode=2, aniso=8)),
    "Castle_BookLeatherC": (leatherC, dict(filter_mode=2, aniso=8)),
    "Castle_BookPages": (pages, dict(filter_mode=2, aniso=8)),
    "Castle_GiltWorn": (gilt_worn, {}),
    "Castle_RootBark": (root_bark, dict(filter_mode=2, aniso=8)),
    "Castle_RootFlesh": (root_flesh, dict(filter_mode=2, aniso=8)),
    "Castle_BoneGrime": (bone_grime, {}),
    "Castle_NicheStone": (niche_stone, {}),
}

def main():
    wanted = [a for a in sys.argv[1:] if a.startswith("Castle_")] or list(GENS)
    for name in wanted:
        fn, kw = GENS[name]
        idx = list(GENS).index(name)
        rng = np.random.default_rng(7000 + idx)
        base, smooth, metal_v = R12[name]
        r = fn(N, rng, base)
        g = save_set(name, r, N, **kw)
        cc.write_standard_mat(cc.MATS / (name + ".mat"), name, base, smooth, metal_v, g["albedo"], g["normal"], mask_guid=g["mask"], bump_scale=1.0)
        print("texture", name, N)
    print("CASTLE_R12_TEXTURES_PASS")

if __name__ == "__main__":
    main()
