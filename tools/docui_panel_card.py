#!/usr/bin/env python3
"""Builds Resources/UI/BloodLedger/PanelCard.png from the shipped Panel.png (piece fd-journal).

The castle journal / end card draw the torn-paper card as a nine-slice (Core/DocLayout.cs DocPanel). The middle row of Panel.png
(y 390..560) holds only plain paper and the torn left/right edges, but a tall card stretches it ~3x vertically, which smears the
paper fibres into streaks. This tool extends that row to three tiles (normal, mirrored, normal), so the seams with the rows above
and below stay exact and the stretch drops to about 1x. Output is 1448 x 1426; all constants in DocPanel assume it.
No new artwork: it is a rearrangement of Panel.png (same licence/provenance, see ASSET_LOG.md).
Run:  python3 tools/docui_panel_card.py
"""
from pathlib import Path
from PIL import Image, ImageOps

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Assets/HollowCurrent/Resources/UI/BloodLedger/Panel.png"
DST = ROOT / "Assets/HollowCurrent/Resources/UI/BloodLedger/PanelCard.png"
ROW_T, ROW_B = 390, 560


def main():
    im = Image.open(SRC).convert("RGBA")
    w, h = im.size
    assert (w, h) == (1448, 1086), im.size
    top = im.crop((0, 0, w, ROW_T))
    mid = im.crop((0, ROW_T, w, ROW_B))
    bot = im.crop((0, ROW_B, w, h))
    flip = ImageOps.flip(mid)
    out = Image.new("RGBA", (w, h + 2 * (ROW_B - ROW_T)), (0, 0, 0, 0))
    y = 0
    for part in (top, mid, flip, mid, bot):
        out.paste(part, (0, y))
        y += part.height
    out.save(DST, optimize=True)
    print("wrote", DST, out.size)


if __name__ == "__main__":
    main()
