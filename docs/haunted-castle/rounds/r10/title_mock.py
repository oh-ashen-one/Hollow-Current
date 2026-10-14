#!/usr/bin/env python3
"""Pillow mock of the new title screen (design space 1600x900 rendered at 2560x1440, scale 1.6).
Mirrors Interface.Title() / TitleScreen.cs coordinates, fonts and sizes; fog is the same fbm value noise.
usage: title_mock.py [castle|bellwether] [out.png]"""
import sys, numpy as np
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[4]
UI = ROOT / "Assets/HollowCurrent/Resources/UI"
CINZEL = str(UI / "Fonts/Cinzel-Medium.ttf")
GEORGIA = "/System/Library/Fonts/Supplemental/Georgia.ttf"
GEORGIA_I = "/System/Library/Fonts/Supplemental/Georgia Italic.ttf"
S = 1.6
W, H = 2560, 1440
INK = (232, 227, 209); MUTED = (163, 168, 161); COPPER = (161, 130, 89); LINE = (107, 92, 74)
sel = sys.argv[1] if len(sys.argv) > 1 else "castle"
out = sys.argv[2] if len(sys.argv) > 2 else str(Path(__file__).with_name("title_mock.png"))

def font(path, size): return ImageFont.truetype(path, round(size * S))

def cover(img, w, h, zoom=1.0, pivot=(.62, .5)):
    c = max(w / img.width, h / img.height)
    iw, ih = img.width * c, img.height * c
    x0, y0 = (w - iw) / 2, (h - ih) / 2
    px, py = w * pivot[0], h * pivot[1]
    x = px + (x0 - px) * zoom; y = py + (y0 - py) * zoom
    r = img.resize((round(iw * zoom), round(ih * zoom)), Image.LANCZOS)
    canvas = Image.new("RGB", (w, h)); canvas.paste(r, (round(x), round(y))); return canvas

def smooth(x): x = np.clip(x, 0, 1); return x * x * (3 - 2 * x)

def hash_(x, y, seed):
    h = (x.astype(np.int64) * 374761393 + y.astype(np.int64) * 668265263 + seed * 144665) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF; h ^= h >> 16
    return (h & 0xFFFF) / 65535.0

def lattice(x, y, period, seed):
    x0 = np.floor(x).astype(np.int64); y0 = np.floor(y).astype(np.int64)
    fx = smooth(x - x0); fy = smooth(y - y0)
    w = lambda i: i % period
    a = hash_(w(x0), w(y0), seed); b = hash_(w(x0 + 1), w(y0), seed)
    c = hash_(w(x0), w(y0 + 1), seed); d = hash_(w(x0 + 1), w(y0 + 1), seed)
    return (a + (b - a) * fx) * (1 - fy) + (c + (d - c) * fx) * fy

def fog_texture(size=256):
    u = (np.arange(size) / size)[None, :].repeat(size, 0); v = (np.arange(size) / size)[:, None].repeat(size, 1)
    n = np.zeros((size, size)); amp = .5; tot = 0; period = 3
    for o in range(5):
        n += amp * lattice(u * period, v * period, period, o * 17 + 3); tot += amp; amp *= .5; period *= 2
    return smooth((n / tot - .30) / .5)

art_castle = Image.open(UI / "CastleBackdrop.png").convert("RGB")
art_bell = Image.open(UI / "MenuBackdrop.png").convert("RGB")
art = art_castle if sel == "castle" else art_bell
img = cover(art, W, H, 1.0).convert("RGBA")

# fog over the lower half, ramped in by strips (tile 512 design px)
dens = fog_texture(); tile = round(512 * S)
big = np.array(Image.fromarray((dens * 255).astype(np.uint8)).resize((tile, tile), Image.BILINEAR)) / 255.0
reps = np.tile(big, (H // tile + 2, W // tile + 2))[:H, :W]
top = H * .42; yy = np.arange(H)[:, None]
ramp = smooth(((yy - top) / (H - top)) / .6) * (yy >= top)
alpha = (reps * ramp * .12)
fogc = np.array([190, 200, 212], float)
arr = np.array(img).astype(float)
arr[..., :3] = arr[..., :3] * (1 - alpha[..., None]) + fogc * alpha[..., None]
# vignette
gx, gy = np.meshgrid((np.arange(W) + .5) / W * 2 - 1, (np.arange(H) + .5) / H * 2 - 1)
d = np.sqrt(gx ** 2 + gy ** 2); va = .66 * smooth((d - .52) / .88)
arr[..., :3] *= (1 - va[..., None])
# left gradient across 45 %
xs = np.arange(W) / (W * .45); la = np.where(xs < 1, .78 * (1 - xs), 0)[None, :]
arr[..., :3] *= (1 - la[..., None])
img = Image.fromarray(arr.astype(np.uint8)).convert("RGBA")

ov = Image.new("RGBA", (W, H), (0, 0, 0, 0)); dr = ImageDraw.Draw(ov)
def X(v): return round(v * S)

def tracked(text, x, y, size, tracking, color, path=CINZEL, d=dr):
    f = font(path, size); cx = x * S
    for ch in text:
        d.text((cx, y * S), ch, font=f, fill=color, anchor="la"); cx += f.getlength(ch) + tracking * S
    return (cx - x * S) / S - tracking
def measure(text, size, tracking, path=CINZEL):
    f = font(path, size); return sum(f.getlength(c) for c in text) / S + tracking * (len(text) - 1)
def linebox(size, path=CINZEL):
    a, dsc = font(path, size).getmetrics(); return (a + dsc) / S
def rect(x, y, w, h, fill=None, outline=None, width=1, d=dr):
    d.rectangle([X(x), X(y), X(x + w) - 1, X(y + h) - 1], fill=fill, outline=outline, width=width)

a = lambda c, al: (*c, round(255 * al))
# brand
tracked("HOLLOW CURRENT", 80, 44, 12, 3, a(INK, .6), GEORGIA)
tracked("FIELD INVESTIGATION OFFICE", 80, 62, 12, 3, a(MUTED, .6), GEORGIA)
# type block
tracked("HOLLOW", 80, 78, 96, 10, a(INK, 1))
tw = tracked("CURRENT", 80, 168, 96, 10, a(INK, 1))
rect(80, 280, tw, 1, fill=a(COPPER, 1))
book = {"castle": ("Haunted Castle", "A castle half out of the world. Two moons over a red moat.\nA bell no one rings keeps the bargain."),
        "bellwether": ("The Bellwether House", "An empty family home. A tune beneath the floor.\nSomeone is still keeping the house warm.")}[sel]
tracked(book[0], 80, 296, 34, 2.5, a(INK, 1))
for i, line in enumerate(book[1].split("\n")):
    dr.text((80 * S, (350 + i * 23.5) * S), line, font=font(GEORGIA_I, 20), fill=a(INK, .7), anchor="la")

# BEGIN
r = (80, 668, 680, 60)
rect(*r, fill=a(COPPER, .18)); rect(*r, outline=a(COPPER, 1), width=1)
tracked("BEGIN INVESTIGATION", r[0] + 26, r[1] + r[3] / 2 - linebox(20) / 2, 20, 2.6, (255, 255, 255, 255))
cap = (r[0] + r[2] - 66, r[1] + (r[3] - 24) / 2, 48, 24)
rect(*cap, outline=a(LINE, .9), width=1)
f = font(GEORGIA, 12); dr.text((X(cap[0] + 24), X(cap[1] + 12)), "ENTER", font=f, fill=a(INK, 1), anchor="mm")

# cards
crops = {"castle": (.36, .12, .64, .64), "bellwether": (.30, .17, .64, .64)}
names = [("bellwether", "The Bellwether House", art_bell), ("castle", "Haunted Castle", art_castle)]
# catalog order: Bellwether, Castle
for i, (key, nm, im) in enumerate(names):
    cx, cy, cw, ch = 80 + i * 350, 440, 330, 185
    chosen = key == sel
    cr = crops[key]
    box = (round(cr[0] * im.width), round(cr[1] * im.height), round((cr[0] + cr[2]) * im.width), round((cr[1] + cr[3]) * im.height))
    tile_ = im.crop(box).resize((X(cw), X(ch)), Image.LANCZOS).convert("RGBA")
    b = 1.0 if chosen else .6
    t = np.array(tile_).astype(float); t[..., :3] *= b
    # bottom shade
    h_ = t.shape[0]; yv = np.arange(h_) / h_
    sh = np.where(yv >= .34, .88 * smooth((yv - .34) / .66), 0)[:, None]
    t[..., :3] *= (1 - sh[..., None])
    ov.paste(Image.fromarray(t.astype(np.uint8)), (X(cx), X(cy)))
    size = 18
    while size > 13 and measure(nm, size, 1.6) > cw - 32: size -= 1
    tracked(nm, cx + 16, cy + ch - 16 - linebox(size), size, 1.6, a(INK, 1))
    if chosen: rect(cx, cy, cw, ch, outline=a(COPPER, 1), width=2 * round(S) // 1 if False else round(2 * S))
    else: rect(cx, cy, cw, ch, outline=a(LINE, .45), width=1)

# text actions
for text, x in (("FIELD SETTINGS", 80), ("EXIT TO DESKTOP", 310)):
    tracked(text, x, 766 - linebox(16) / 2, 16, 2.2, a(INK, .78))

# footer
rect(80, 843, 1440, 1, fill=a(LINE, .65))
dr.text((80 * S, 863 * S), "1 / 2 choose map   ·   WASD move   ·   Mouse look   ·   E interact   ·   Headphones suggested", font=font(GEORGIA, 15), fill=a(MUTED, 1), anchor="la")
dr.text((1260 * S, 863 * S), "HAUNTED CASTLE" if sel == "castle" else "THE BELLWETHER HOUSE", font=font(GEORGIA, 13), fill=a(COPPER, 1), anchor="la")

img = Image.alpha_composite(img, ov).convert("RGB")
img.save(out); print(out, img.size)
