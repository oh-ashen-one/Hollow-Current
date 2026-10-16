#!/usr/bin/env python3
"""Procedural paper sheets for the document / note UI (piece fb-docui, owner feedback item 10).

Writes three full-bleed 2048x1152 JPEG sheets to Assets/HollowCurrent/Resources/UI/Documents/:
  DocLedger.jpg  castle ledger / inscription: aged vellum, foxing, tide line, blood, charred edge, ink blots
  DocLetter.jpg  castle letter / tally: lighter vellum, letter folds, small broken wax seal, same wear
  DocField.jpg   Bellwether field report: modern off-white notebook sheet, punch holes, coffee ring, soft vignette

Everything is numpy + Pillow with fixed seeds (no network, no third-party imagery), so the files are original
project work (CC BY 4.0) and reproducible: `python3 tools/docui_paper.py`.

Layout contract with Runtime/DocumentUi.cs: the sheet maps to the 1600x900 design space (scale 1.28). Text lives
inside TEXT_ZONE (design units); stains, foxing, folds and the seal stay outside it or are held to a faint level
inside it, so body text keeps >= 4.5:1 contrast. `python3 tools/docui_paper.py --check` prints the measured
worst-case contrast of the ink colours used by DocumentUi against each sheet's text zone.
"""
import sys
from pathlib import Path
import numpy as np
from PIL import Image
from scipy.ndimage import gaussian_filter

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/HollowCurrent/Resources/UI/Documents"
W, H = 2048, 1152
K = W / 1600.0  # design unit -> texture px
# design-space rectangles (x0, y0, x1, y1) that carry text for each sheet
TEXT_ZONE = {"DocLedger": (330, 96, 1270, 806), "DocLetter": (330, 96, 1270, 806), "DocField": (140, 90, 1470, 806)}
# the quiet close line (design units, x0 y0 x1 y1), drawn at 92 % ink
CLOSE_ZONE = (640, 800, 960, 834)
# colours DocumentUi paints text with (sRGB 0..1), for the --check report
INK = {"DocLedger": {"body": (.17, .11, .075), "title": (.36, .05, .045), "kicker": (.31, .10, .07)},
       "DocLetter": {"body": (.17, .11, .075), "title": (.36, .05, .045), "kicker": (.31, .10, .07)},
       "DocField": {"body": (.10, .11, .16), "title": (.50, .08, .08), "kicker": (.22, .24, .30)}}


def rng(seed):
    return np.random.default_rng(seed)


def fbm(seed, shape=(H, W), cells=(6, 12, 24, 48, 96), gains=(1, .6, .4, .25, .15)):
    """Smooth value noise in about [-1, 1]: bicubic-upsampled random grids, summed over octaves."""
    r = rng(seed)
    h, w = shape
    total = np.zeros(shape, np.float32)
    norm = 0.0
    for c, g in zip(cells, gains):
        gh, gw = max(2, int(round(h / (w / c)))) + 1, c + 1
        grid = r.standard_normal((gh, gw)).astype(np.float32)
        img = Image.fromarray(grid, mode="F").resize((w, h), Image.BICUBIC)
        total += g * np.asarray(img)
        norm += g
    total /= norm
    return total / (np.abs(total).max() + 1e-6)


def streaks(seed, axis, amount):
    """Paper fibre: noise stretched along one axis."""
    r = rng(seed)
    if axis == 0:  # horizontal fibres
        g = r.standard_normal((H, W // 28)).astype(np.float32)
    else:
        g = r.standard_normal((H // 28, W)).astype(np.float32)
    img = Image.fromarray(g, mode="F").resize((W, H), Image.BICUBIC)
    a = np.asarray(img)
    return a / (np.abs(a).max() + 1e-6) * amount


def smooth(x, a, b):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def grid():
    y, x = np.mgrid[0:H, 0:W].astype(np.float32)
    return x, y


def zone_mask(name, feather=70):
    """1 outside the text zone, 0 inside, soft edge. Used to keep wear out of the text."""
    x0, y0, x1, y1 = [v * K for v in TEXT_ZONE[name]]
    x, y = grid()
    dx = np.maximum(np.maximum(x0 - x, x - x1), 0)
    dy = np.maximum(np.maximum(y0 - y, y - y1), 0)
    d = np.sqrt(dx * dx + dy * dy)
    return smooth(d, 0, feather)


def blob(cx, cy, rx, ry, seed, rough=.35, shape=None):
    """Irregular blob mask in [0, 1] (hard-ish edge) centred at cx, cy."""
    x, y = grid()
    n = fbm(seed, cells=(4, 8, 16, 32), gains=(1, .8, .6, .5))
    d = np.sqrt(((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2)
    return smooth(1 - d + n * rough, 0.0, 0.10)


def over(img, rgb, alpha):
    a = alpha[..., None]
    return img * (1 - a) + np.array(rgb, np.float32) * a


def charred_edges(img, seed, band=1.0):
    x, y = grid()
    d = np.minimum(np.minimum(x, W - 1 - x), np.minimum(y, H - 1 - y))
    wob = fbm(seed, cells=(5, 11, 23, 47), gains=(1, .8, .6, .5)) * 46 * band + fbm(seed + 1, cells=(90, 180), gains=(1, .7)) * 10
    # keep the burn off the quiet close line at the bottom centre
    keep = smooth(1 - np.abs(x - W / 2) / 420.0, 0, .4) * smooth(1 - (H - 1 - y) / 140.0, 0, .5)
    de = d + wob + np.where(y > H / 2, 110 * keep, 0)
    # wide toning toward the edge, then a scorched band, then char
    tone = np.clip(1 - de / 300.0, 0, 1) ** 1.8
    img = img * (1 - .45 * tone[..., None]) + np.array([.30, .17, .07], np.float32) * (.45 * tone[..., None]) * .6
    scorch = smooth(70 - de, 0, 55)
    img = over(img, (.17, .085, .035), scorch * .9)
    char = smooth(30 - de, 0, 22)
    img = over(img, (.025, .017, .012), char)
    # a few bitten-out corners
    for cx, cy, r, s in ((0, 0, 130, 3), (W, H, 115, 5), (W, 0, 80, 7), (0, H, 95, 9)):
        m = blob(cx, cy, r, r * .8, seed + s, rough=.5)
        img = over(img, (.02, .014, .01), m)
    return img


def foxing(img, name, seed, count=340):
    r = rng(seed)
    mask = zone_mask(name)
    x, y = grid()
    layer = np.zeros((H, W), np.float32)
    colour = np.zeros((H, W, 3), np.float32)
    cl = []
    for _ in range(14):
        cl.append((r.uniform(0, W), r.uniform(0, H), r.uniform(120, 340)))
    for i in range(count):
        if r.random() < .7:
            cx, cy, s = cl[r.integers(len(cl))]
            px, py = r.normal(cx, s), r.normal(cy, s)
        else:
            px, py = r.uniform(0, W), r.uniform(0, H)
        if not (0 <= px < W and 0 <= py < H):
            continue
        rad = r.uniform(2.5, 10) * (1 if r.random() < .85 else 1.8)
        x0, x1, y0, y1 = int(max(px - rad * 3, 0)), int(min(px + rad * 3, W)), int(max(py - rad * 3, 0)), int(min(py + rad * 3, H))
        if x1 <= x0 or y1 <= y0:
            continue
        sx, sy = x[y0:y1, x0:x1], y[y0:y1, x0:x1]
        d = np.sqrt((sx - px) ** 2 + (sy - py) ** 2) / rad
        a = np.exp(-d ** 2 * 1.6) * r.uniform(.25, .6)
        layer[y0:y1, x0:x1] = np.maximum(layer[y0:y1, x0:x1], a)
    layer *= mask * .92 + (1 - mask) * 0 + .08 * (1 - mask)  # keep ~8% inside the text zone
    return over(img, (.47, .27, .10), layer)


def tide_ring(img, cx, cy, rad, seed, name, strength=.55):
    x, y = grid()
    n = fbm(seed, cells=(5, 11, 24), gains=(1, .7, .5)) * .16
    d = np.sqrt((x - cx) ** 2 + (y - cy) ** 2) / rad + n
    gaps = .55 + .45 * np.clip(fbm(seed + 2, cells=(10, 22, 44), gains=(1, .8, .5)) * 2.2 + .6, 0, 1)
    wid = .02 + .03 * np.clip(fbm(seed + 3, cells=(8, 16), gains=(1, .6)) * 1.5 + .5, 0, 1)
    ring = np.exp(-((d - 1) / wid) ** 2) * strength * gaps
    fill = smooth(1 - d, 0, .3) * .08
    m = zone_mask(name)
    a = (ring + fill) * (m * .9 + .1)
    return over(img, (.45, .28, .12), a)


def stain_field(cx, cy, rx, ry, seed, rough):
    """Signed 'depth' field of an irregular stain: > 0 inside, 0 at the edge."""
    x, y = grid()
    n = fbm(seed, cells=(3, 6, 12, 24, 48, 96), gains=(1, .9, .7, .5, .35, .2))
    d = np.sqrt(((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2)
    return (1 - d) + n * rough


def blood(img, cx, cy, rx, ry, seed, name, strength=.92):
    """Dried blood: thick dark core, lighter wet margin, a darker rim where the edge dried last, drips, cracks."""
    m = zone_mask(name, 40)
    f = stain_field(cx, cy, rx, ry, seed, .75)
    mottle = fbm(seed + 1, cells=(20, 40, 80), gains=(1, .7, .5))
    # wicked, watery margin: soft, mottled, low alpha (never a flat disc)
    halo_f = stain_field(cx, cy, rx * 1.6, ry * 1.6, seed + 2, .9)
    halo = smooth(halo_f, -.05, .55) * (.55 + .45 * mottle) * .26
    img = over(img, (.58, .27, .16), halo * m)
    thick = np.clip(f / .55, 0, 1)
    body = smooth(f, 0, .045)
    rim = np.exp(-((f - .03) / .035) ** 2)
    col = np.array([.52, .075, .06], np.float32) * (1 - thick[..., None]) + np.array([.20, .018, .022], np.float32) * thick[..., None]
    col = col * (1 + .12 * mottle[..., None])
    img = over(img, np.clip(col, 0, 1), body * strength * m)
    img = over(img, (.13, .012, .015), rim * body * .55 * m)
    # shrinkage cracks in the thick part
    cr = fbm(seed + 3, cells=(40, 80, 160), gains=(1, .7, .5))
    lines = (np.abs(cr) < .012).astype(np.float32) * smooth(f, .25, .5) * .38 * m
    img = over(img, (.07, .006, .008), lines)
    return img


def ink_blot(img, cx, cy, rad, seed, name):
    m = zone_mask(name, 40)
    f = stain_field(cx, cy, rad, rad * .9, seed, 1.0)
    img = over(img, (.07, .05, .052), smooth(f, 0, .12) * .82 * m)
    img = over(img, (.30, .20, .12), np.exp(-((f + .12) / .12) ** 2) * .16 * m)  # faint brown bleed round the blot
    r = rng(seed)
    for _ in range(10):
        a = r.uniform(0, 6.283)
        dist = r.uniform(rad * 1.2, rad * 2.8)
        g = stain_field(cx + np.cos(a) * dist, cy + np.sin(a) * dist, r.uniform(2, 5), r.uniform(2, 5), seed + int(dist * 7), .5)
        img = over(img, (.07, .05, .05), smooth(g, 0, .3) * .8 * m)
    return img


def crease(img, x0, y0, x1, y1, strength=.05, width=3.0):
    xs, ys = grid()
    dx, dy = x1 - x0, y1 - y0
    L = np.hypot(dx, dy)
    d = ((xs - x0) * dy - (ys - y0) * dx) / L  # signed distance
    dark = np.exp(-(d / width) ** 2) * strength * 1.6
    shade = np.where(d > 0, 1, -1) * np.exp(-(np.abs(d) / (width * 7))) * strength * .55
    light = np.exp(-((d - width * 1.6) / (width * .9)) ** 2) * strength * .9
    return np.clip(img * (1 - dark[..., None] - shade[..., None] * .5) + light[..., None], 0, 1)


def wax_seal(img, cx, cy, rad, seed):
    x, y = grid()
    n = fbm(seed, cells=(7, 15, 33), gains=(1, .8, .6)) * .28
    d = np.sqrt((x - cx) ** 2 + (y - cy) ** 2) / rad + n
    body = smooth(1.02 - d, 0, .05)
    # one chipped wedge: the seal was broken when the letter was opened
    ang = np.arctan2(y - cy, x - cx)
    wedge = (np.abs(ang - 2.2) < .32) & (d > .25)
    body = body * (~wedge).astype(np.float32)
    rim = smooth(1.02 - d, 0, .05) * (1 - smooth(.88 - d, 0, .1))
    base = np.array([.40, .045, .05], np.float32)
    lit = np.clip(1 - (((x - cx + rad * .35) ** 2 + (y - cy + rad * .4) ** 2) ** .5) / (rad * 1.3), 0, 1)
    shadow = blob(cx + rad * .12, cy + rad * .16, rad * 1.12, rad * 1.05, seed + 3, rough=.1) * .5
    img = img * (1 - shadow[..., None] * .55)
    img = over(img, base, body)
    img = over(img, (.55, .12, .10), body * lit * .5)
    img = over(img, (.25, .02, .03), rim * .8)
    inner = np.exp(-((d - .55) / .03) ** 2) * body * .6  # pressed ring
    return over(img, (.22, .02, .03), inner)


def finish(img, seed, name, grain=.012):
    r = rng(seed)
    img = img + r.standard_normal((H, W, 1)).astype(np.float32) * grain
    img = np.clip(img, 0, 1)
    return Image.fromarray((img * 255 + .5).astype(np.uint8), "RGB")


def vellum(name, seed, base, letter):
    mott = fbm(seed, cells=(3, 6, 12, 24, 48), gains=(1, .8, .6, .4, .25))
    img = np.ones((H, W, 3), np.float32) * np.array(base, np.float32)
    img *= (1 + .075 * mott[..., None] * np.array([1, 1.1, 1.5], np.float32) * .9)
    fib = streaks(seed + 1, 0, .018) + streaks(seed + 2, 1, .012) + fbm(seed + 3, cells=(160, 320), gains=(1, .7)) * .018
    img *= (1 + fib[..., None])
    # blood-red and tobacco toning clouds, mostly toward the edges
    cloud = fbm(seed + 4, cells=(3, 5, 9), gains=(1, .7, .5))
    x, y = grid()
    edge = np.clip(1 - np.minimum(np.minimum(x, W - x), np.minimum(y, H - y)) / 420.0, 0, 1)
    tone = np.clip(cloud * .5 + .35, 0, 1) * (.25 + .75 * edge) * (zone_mask(name, 140) * .85 + .15)
    img = over(img, (.62, .42, .20), tone * .22)
    img = foxing(img, name, seed + 5)
    if letter:
        img = tide_ring(img, 330, 960, 150, seed + 6, name, .5)
        img = blood(img, 1815, 215, 130, 90, seed + 7, name)
        img = ink_blot(img, 240, 330, 24, seed + 8, name)
        img = crease(img, 0, H * .333, W, H * .333 + 6, .055)
        img = crease(img, 0, H * .666, W, H * .666 - 5, .055)
        img = crease(img, W * .5, 0, W * .5 + 4, H, .035)
        img = wax_seal(img, 1735, 985, 62, seed + 9)
    else:
        img = tide_ring(img, 1680, 340, 175, seed + 6, name, .5)
        img = blood(img, 1835, 985, 150, 110, seed + 7, name)
        img = blood(img, 190, 160, 110, 70, seed + 10, name, .8)
        img = ink_blot(img, 300, 880, 20, seed + 8, name)
        img = ink_blot(img, 1790, 560, 14, seed + 11, name)
    img = charred_edges(img, seed + 20)
    return finish(img, seed + 30, name)


def field(name, seed):
    mott = fbm(seed, cells=(3, 7, 15, 30), gains=(1, .6, .4, .25))
    img = np.ones((H, W, 3), np.float32) * np.array([.935, .915, .865], np.float32)
    img *= (1 + .028 * mott[..., None])
    img *= (1 + (streaks(seed + 1, 0, .006) + streaks(seed + 2, 1, .006) + fbm(seed + 3, cells=(200, 400), gains=(1, .7)) * .008)[..., None])
    x, y = grid()
    edge = np.minimum(np.minimum(x, W - x), np.minimum(y, H - y))
    vig = np.clip(1 - edge / 260.0, 0, 1) ** 2.2
    img = img * (1 - .20 * vig[..., None]) * np.array([1, .985, .96], np.float32)
    left = np.clip(1 - x / 150.0, 0, 1) ** 2 * .05
    img *= (1 - left[..., None])
    # coffee ring (outside the text zone) and a thumb smudge
    img = tide_ring(img, 1755, 985, 95, seed + 4, name, .75)
    img = over(img, (.50, .36, .20), blob(1755, 985, 85, 85, seed + 5, rough=.18) * .06 * zone_mask(name, 40))
    img = over(img, (.55, .50, .44), blob(240, 1010, 120, 70, seed + 6, rough=.4) * .12 * zone_mask(name, 40))
    # three punch holes with a dark desk showing through and a soft ring
    for cy in (260, 576, 892):
        cx = 62
        d = np.sqrt((x - cx) ** 2 + (y - cy) ** 2)
        img = over(img, (.055, .055, .06), smooth(17 - d, -1, 1))
        img = over(img, (.75, .73, .68), np.exp(-((d - 21) / 2.0) ** 2) * .35)
    # a pale blue-black ink dot trail near the top margin
    img = ink_blot(img, 1840, 150, 9, seed + 7, name)
    return finish(img, seed + 30, name, .008)


def luminance(rgb):
    def lin(c):
        c = np.asarray(c, np.float64)
        return np.where(c <= .04045, c / 12.92, ((c + .055) / 1.055) ** 2.4)
    l = lin(rgb)
    return .2126 * l[..., 0] + .7152 * l[..., 1] + .0722 * l[..., 2]


def contrast_report(name, im):
    arr = np.asarray(im, np.float32) / 255
    x0, y0, x1, y1 = [int(v * K) for v in TEXT_ZONE[name]]
    zone = arr[y0:y1, x0:x1]
    lp = luminance(zone)
    # worst-case paper under text = the darkest 0.5% of the zone (stains included)
    p = np.percentile(lp, .5)
    lines = []
    for key, rgb in INK[name].items():
        li = float(luminance(np.array(rgb)))
        lines.append("%s %s: worst %.2f:1  median %.2f:1" % (name, key, (p + .05) / (li + .05), (np.median(lp) + .05) / (li + .05)))
    # close line: 92 % ink over the darkest 0.5 % of the close rectangle
    x0, y0, x1, y1 = [int(v * K) for v in CLOSE_ZONE]
    cz = arr[y0:y1, x0:x1]
    pc = np.percentile(luminance(cz), .5)
    body = np.array(INK[name]["body"])
    paper_c = np.percentile(cz.reshape(-1, 3), .5, axis=0)
    eff = float(luminance(.92 * body + .08 * paper_c))
    lines.append("%s close line (92%% ink): worst %.2f:1" % (name, (pc + .05) / (eff + .05)))
    return lines


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    sheets = {"DocLedger": vellum("DocLedger", 1015, (.845, .715, .515), False),
              "DocLetter": vellum("DocLetter", 2026, (.875, .765, .575), True),
              "DocField": field("DocField", 3037)}
    for name, im in sheets.items():
        if "--check" not in sys.argv:
            im.save(OUT / (name + ".jpg"), quality=90, optimize=True, subsampling=0)
        for line in contrast_report(name, im):
            print(line)


if __name__ == "__main__":
    main()
