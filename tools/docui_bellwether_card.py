#!/usr/bin/env python3
"""Builds Resources/UI/BellwetherCard.jpg, the Bellwether map-card thumbnail on the title screen (piece fd-journal, r13 critic section 4:
"the Bellwether card thumbnail is almost black"; measured mean luma 5.8/255 against 21.5 for the castle card).

Derived only from the shipped Resources/UI/MenuBackdrop.png (the existing Bellwether key art; no new artwork): a 16:9 crop centred on
the house, a gamma lift (0.60) and a small gain so the porch light, door and roofline read at the card's brightness steps, then
downsampled to 660 x 371 (the card is 330 x 185 design units). Deterministic.
Run:  python3 tools/docui_bellwether_card.py
"""
from pathlib import Path
from PIL import Image, ImageFilter, ImageStat

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Assets/HollowCurrent/Resources/UI/MenuBackdrop.png"
DST = ROOT / "Assets/HollowCurrent/Resources/UI/BellwetherCard.jpg"
GAMMA, GAIN = 0.60, 0.95


def main():
    im = Image.open(SRC).convert("RGB")
    w, h = im.size                      # 1672 x 941
    cw = 1100
    ch = round(cw * 9 / 16)
    cx = 1060                           # house centre, px
    x0 = max(0, min(w - cw, cx - cw // 2))
    y0 = 150
    im = im.crop((x0, y0, x0 + cw, y0 + ch)).resize((660, 371), Image.LANCZOS)
    lut = [min(255, round(255 * GAIN * (v / 255.0) ** GAMMA)) for v in range(256)]
    im = im.point(lut * 3).filter(ImageFilter.GaussianBlur(.6))
    im.save(DST, quality=90, optimize=True)
    print("wrote", DST, im.size, "mean luma %.1f" % ImageStat.Stat(im.convert("L")).mean[0])


if __name__ == "__main__":
    main()
