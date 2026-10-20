#!/usr/bin/env python3
"""Castle card art, owner-approved variant A "smoked glass" (2026-10-20): translucent black glass, antique-gold filigree outline.

usage: python3 tools/ui_smoked_glass.py [--extract path/to/variant_A_fullres.png]

--extract keys the gold ornaments out of the approved Higgsfield mockup (GPT Image 2, job in docs/haunted-castle/ASSET_LOG.md) into
docs/haunted-castle/art/ui_smoked_glass/{corner,crest}.png (RGBA: alpha from the gold's warmth and brightness against the cold sky).
Without it, the committed ornaments are used. Writes (same names and sizes as the Blood Ledger set, so every layout and nine-slice cut
line in Core/DocLayout.cs keeps working):
  Resources/UI/BloodLedger/Panel.png 1448x1086, PanelCard.png 1448x1426 (ornaments inside the ColL/ColR/RowT/RowB corner cells),
  Dialogue.png and Warning.png 2172x724 (Warning: crimson inner line), Resources/UI/Documents/DocGlass.png 2048x1152 (castle reader).
"""
import sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'docs/haunted-castle/art/ui_smoked_glass'
UI = ROOT / 'Assets/HollowCurrent/Resources/UI'
GOLD = np.array([186, 150, 92], float)

def key_gold(rgb):
    a = rgb.astype(float); r, g, b = a[..., 0], a[..., 1], a[..., 2]
    lum = .3 * r + .59 * g + .11 * b; warm = r - b
    alpha = (np.clip((warm - 6) / 30, 0, 1) * np.clip((lum - 28) / 60, 0, 1)) ** .6
    return np.dstack([np.clip(a * 1.15, 0, 255), alpha * 255]).clip(0, 255).astype(np.uint8)

def extract(src):
    im = Image.open(src).convert('RGB'); k = im.width / 1400
    def crop(x0, y0, x1, y1): return np.array(im.crop((int(x0 * k), int(y0 * k), int(x1 * k), int(y1 * k))))
    ART.mkdir(parents=True, exist_ok=True)
    corner = key_gold(crop(300, 85, 430, 215))
    # the corner crop also holds the start of both border runs: keep the ornament, drop the straight lines past 70 %
    h, w = corner.shape[:2]; corner[int(h * .72):, int(w * .22):, 3] = 0; corner[int(h * .22):, int(w * .72):, 3] = 0
    Image.fromarray(corner).save(ART / 'corner.png')
    Image.fromarray(key_gold(crop(590, 60, 810, 140))).save(ART / 'crest.png')
    print('extracted ornaments ->', ART)

def glass(w, h, seed, alpha_mid=.56, alpha_edge=.70):
    rng = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:h, 0:w]; dx = (xx - w / 2) / (w / 2); dy = (yy - h / 2) / (h / 2)
    edge = np.clip(np.maximum(np.abs(dx), np.abs(dy)) ** 6, 0, 1)
    smoke = np.array(Image.fromarray((rng.random((h // 16 + 1, w // 16 + 1)) * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC)
                     .filter(ImageFilter.GaussianBlur(24)), float) / 255
    a = alpha_mid + (alpha_edge - alpha_mid) * edge + (smoke - .5) * .08
    rgb = np.stack([np.full((h, w), 9.), np.full((h, w), 10.), np.full((h, w), 15.)], -1) + (smoke[..., None] - .5) * 10
    return np.dstack([rgb, a * 255]).clip(0, 255)

def line(img, x0, y0, x1, y1, color, alpha, width=2):
    sub = img[y0:y1, x0:x1]; a = alpha * 255
    sub[..., :3] = sub[..., :3] * (1 - alpha) + np.array(color) * alpha; sub[..., 3] = np.maximum(sub[..., 3], a)

def frame(img, m, color=GOLD, inner=None):
    h, w = img.shape[:2]
    for (o, t, al) in ((m, 3, .90), (m + 9, 1, .55)):
        line(img, o, o, w - o, o + t, color, al); line(img, o, h - o - t, w - o, h - o, color, al)
        line(img, o, o, o + t, h - o, color, al); line(img, w - o - t, o, w - o, h - o, color, al)
    if inner is not None:
        o = m + 18; line(img, o, o, w - o, o + 1, inner, .45); line(img, o, h - o - 1, w - o, h - o, inner, .45)
        line(img, o, o, o + 1, h - o, inner, .45); line(img, w - o - 1, o, w - o, h - o, inner, .45)

def paste(img, orn, x, y):
    oh, ow = orn.shape[:2]; h, w = img.shape[:2]
    x0, y0 = max(0, x), max(0, y); x1, y1 = min(w, x + ow), min(h, y + oh)
    if x1 <= x0 or y1 <= y0: return
    o = orn[y0 - y:y1 - y, x0 - x:x1 - x].astype(float); a = o[..., 3:4] / 255
    sub = img[y0:y1, x0:x1]; sub[..., :3] = sub[..., :3] * (1 - a) + o[..., :3] * a; sub[..., 3:4] = np.maximum(sub[..., 3:4], a * 255)

def corners(img, size, m):
    c = np.array(Image.open(ART / 'corner.png').resize((size, size), Image.LANCZOS)).astype(float)
    h, w = img.shape[:2]; d = m - size // 9
    paste(img, c, d, d); paste(img, c[:, ::-1], w - d - size, d); paste(img, c[::-1], d, h - d - size); paste(img, c[::-1, ::-1], w - d - size, h - d - size)

def crest(img, width, y, flip=False):
    c = Image.open(ART / 'crest.png'); hgt = int(c.height * width / c.width)
    c = np.array(c.resize((width, hgt), Image.LANCZOS)).astype(float)
    if flip: c = c[::-1]
    paste(img, c, img.shape[1] // 2 - width // 2, y - hgt // 2)

def save(img, path):
    Image.fromarray(img.clip(0, 255).astype(np.uint8), 'RGBA').save(path); print('wrote', path.relative_to(ROOT), img.shape[1], 'x', img.shape[0])

def main():
    if '--extract' in sys.argv: extract(sys.argv[sys.argv.index('--extract') + 1])
    bl = UI / 'BloodLedger'
    p = glass(1448, 1086, 1); frame(p, 40); corners(p, 290, 40); crest(p, 460, 46); save(p, bl / 'Panel.png')
    # PanelCard: ornaments must stay inside the fixed corner cells (ColL 300, ColR 960, RowT 390, RowB 900); no crest (the middle cells stretch)
    p = glass(1448, 1426, 2); frame(p, 40); corners(p, 290, 40); save(p, bl / 'PanelCard.png')
    p = glass(2172, 724, 3); frame(p, 34); corners(p, 230, 34); crest(p, 420, 40); save(p, bl / 'Dialogue.png')
    p = glass(2172, 724, 4, .60, .74); frame(p, 34, inner=np.array([150, 24, 22.])); corners(p, 230, 34); crest(p, 420, 40); save(p, bl / 'Warning.png')
    p = glass(2048, 1152, 5, .62, .74); frame(p, 44); corners(p, 320, 44); crest(p, 520, 50); crest(p, 300, 1152 - 50, True); save(p, UI / 'Documents/DocGlass.png')

if __name__ == '__main__': main()
