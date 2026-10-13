#!/usr/bin/env python3
"""Castle UI art (Haunted Castle, piece 0): Blood Ledger cards and the castle title backdrop.

Deterministic, no network, no paid generation. Derived from the project's own
Mourning Note cards (Resources/UI/MourningNote, CC BY 4.0) and the castle sky
(Resources/Castle/Sky/Sky_Front.png from tools/castle_sky.py). Re-run after
either source changes:

    python3 tools/ui_blood_ledger.py

Writes Resources/UI/BloodLedger/{Panel,Dialogue,Warning}.png and
Resources/UI/CastleBackdrop.png (+ .meta files on first run).
"""
import math
import random
import shutil
import uuid
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "Assets/HollowCurrent/Resources/UI"
SRC = UI / "MourningNote"
OUT = UI / "BloodLedger"
SKY = ROOT / "Assets/HollowCurrent/Resources/Castle/Sky/Sky_Front.png"


def tint(im, rgb):
    r, g, b, a = im.split()
    r = r.point(lambda v: int(v * rgb[0]))
    g = g.point(lambda v: int(v * rgb[1]))
    b = b.point(lambda v: int(v * rgb[2]))
    return Image.merge("RGBA", (r, g, b, a))


def stains(im, seed, count, colour, strength):
    """Soft blood stains, only where the card is opaque."""
    rnd = random.Random(seed)
    w, h = im.size
    layer = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(layer)
    for _ in range(count):
        cx = rnd.choice([rnd.uniform(.05, .3), rnd.uniform(.7, .95)]) * w
        cy = rnd.uniform(.1, .9) * h
        rad = rnd.uniform(.03, .09) * min(w, h)
        for k in range(14):
            ox, oy = rnd.gauss(0, rad * .5), rnd.gauss(0, rad * .5)
            rr = rad * rnd.uniform(.3, .8)
            d.ellipse([cx + ox - rr, cy + oy - rr, cx + ox + rr, cy + oy + rr], fill=int(255 * strength))
    layer = layer.filter(ImageFilter.GaussianBlur(min(w, h) * .012))
    alpha = ImageChops.multiply(layer, im.split()[3])
    blood = Image.new("RGBA", (w, h), colour + (255,))
    base = im.copy()
    base.paste(blood, (0, 0), alpha)
    return base


def seal(im, cx, cy, radius):
    """Red wax seal with a raised ring and a crude bat sigil."""
    d = ImageDraw.Draw(im)
    for i in range(radius, 0, -1):
        t = i / radius
        c = (int(96 + 60 * (1 - t)), int(10 + 8 * (1 - t)), int(16 + 6 * (1 - t)), 255)
        d.ellipse([cx - i, cy - i, cx + i, cy + i], fill=c)
    d.ellipse([cx - radius * .72, cy - radius * .72, cx + radius * .72, cy + radius * .72], outline=(60, 4, 10, 255), width=max(2, radius // 14))
    s = radius * .45
    wing = [(cx, cy - s * .1), (cx - s, cy - s * .55), (cx - s * .7, cy + s * .05), (cx - s * .45, cy - s * .1), (cx - s * .2, cy + s * .25),
            (cx, cy + s * .05), (cx + s * .2, cy + s * .25), (cx + s * .45, cy - s * .1), (cx + s * .7, cy + s * .05), (cx + s, cy - s * .55)]
    d.polygon(wing, fill=(58, 3, 9, 255))


def write_meta(png, template):
    meta = Path(str(png) + ".meta")
    if meta.exists():
        return
    text = Path(str(template) + ".meta").read_text().splitlines(True)
    text = [f"guid: {uuid.uuid4().hex}\n" if line.startswith("guid:") else line for line in text]
    meta.write_text("".join(text))


def cards():
    OUT.mkdir(parents=True, exist_ok=True)
    folder_meta = Path(str(OUT) + ".meta")
    if not folder_meta.exists():
        shutil.copy(Path(str(SRC) + ".meta"), folder_meta)
        folder_meta.write_text("".join(f"guid: {uuid.uuid4().hex}\n" if l.startswith("guid:") else l for l in folder_meta.read_text().splitlines(True)))
    # Aged vellum: warmer and darker than the Mourning Note paper, ink stays legible.
    panel = tint(Image.open(SRC / "Panel.png").convert("RGBA"), (.86, .72, .62))
    panel = stains(panel, 7, 5, (92, 10, 16), .55)
    w, h = panel.size
    seal(panel, int(w * .86), int(h * .80), int(min(w, h) * .075))
    ImageDraw.Draw(panel).line([(w * .1, h * .16), (w * .9, h * .16)], fill=(110, 18, 24, 200), width=3)
    panel.save(OUT / "Panel.png", optimize=True)
    dialogue = tint(Image.open(SRC / "Dialogue.png").convert("RGBA"), (.86, .72, .62))
    dialogue = stains(dialogue, 11, 3, (92, 10, 16), .45)
    dialogue.save(OUT / "Dialogue.png", optimize=True)
    warning = tint(Image.open(SRC / "Warning.png").convert("RGBA"), (1.0, .55, .55))
    warning = stains(warning, 13, 3, (70, 4, 10), .5)
    warning.save(OUT / "Warning.png", optimize=True)
    for name in ("Panel", "Dialogue", "Warning"):
        write_meta(OUT / f"{name}.png", SRC / f"{name}.png")


def backdrop():
    """1920x1080 title key art: the two-moon sky over a black castle on a red moat."""
    sky = Image.open(SKY).convert("RGB")
    sw, sh = sky.size
    sky = sky.crop((0, 0, sw, int(sw * 9 / 16))).resize((1920, 1080), Image.LANCZOS)
    img = sky.copy()
    d = ImageDraw.Draw(img)
    horizon = 760
    black = (6, 5, 9)
    # Curtain wall with battlements.
    d.rectangle([0, horizon, 1920, 1080], fill=black)
    for x in range(0, 1920, 46):
        d.rectangle([x, horizon - 26, x + 26, horizon], fill=black)
    rnd = random.Random(3)
    towers = [(220, 120, 430), (520, 90, 360), (960, 150, 560), (1390, 95, 380), (1700, 120, 450)]
    for cx, half, height in towers:
        top = horizon - height
        d.rectangle([cx - half // 2, top, cx + half // 2, horizon], fill=black)
        d.polygon([(cx - half // 2 - 14, top), (cx + half // 2 + 14, top), (cx, top - half * 1.6)], fill=black)
        for _ in range(3):
            wx = cx + rnd.randint(-half // 4, half // 4)
            wy = rnd.randint(top + 40, horizon - 60)
            d.rectangle([wx - 5, wy - 12, wx + 5, wy + 12], fill=(255, 168, 70))
    # Gate arch, warm light inside.
    d.rectangle([905, horizon - 120, 1015, horizon + 40], fill=(28, 14, 8))
    d.pieslice([905, horizon - 175, 1015, horizon - 65], 180, 360, fill=(28, 14, 8))
    # Red moat with the moons' reflection, then the near bank.
    moat_top = 930
    moat = sky.crop((0, 0, 1920, 1080 - moat_top)).transpose(Image.FLIP_TOP_BOTTOM).filter(ImageFilter.GaussianBlur(6))
    red = Image.new("RGB", moat.size, (120, 8, 14))
    moat = Image.blend(moat, red, .62)
    img.paste(moat, (0, moat_top))
    d = ImageDraw.Draw(img)
    for y in range(moat_top, 1080, 9):
        d.line([(0, y), (1920, y)], fill=(70, 4, 8), width=1)
    d.rectangle([0, 1050, 1920, 1080], fill=black)
    img.save(UI / "CastleBackdrop.png", optimize=True)
    write_meta(UI / "CastleBackdrop.png", UI / "MenuBackdrop.png")


if __name__ == "__main__":
    cards()
    backdrop()
    print("HC_UI_BLOOD_LEDGER wrote", OUT, UI / "CastleBackdrop.png")
