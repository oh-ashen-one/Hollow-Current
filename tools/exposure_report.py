#!/usr/bin/env python3
"""Exposure report for a captures folder (Haunted Castle look loop).

    python3 tools/exposure_report.py docs/haunted-castle/rounds/r6/captures [--json out.json] [--prev rounds/r5/captures] [--lantern off|on]

For every PNG prints: mean luma, % pixels with luma < 8, % warm pixels (R > G+20, G > B, luma > 40: the critic's formula), p1 luma,
and the band verdict against the 12-30% target (aim 20%). Luma = 0.299 R + 0.587 G + 0.114 B on 8-bit sRGB values, 2560x1440 or
whatever size the PNG is. UI captures (S7/S8) are listed but not judged. With --prev it also shows the previous round's value and
the change, which is the oscillation check: adjust fills by HALF the error toward 20% each round, never a full swing.
Needs numpy + Pillow only; no Unity.

Wave FB (owner: "too well-lit"; the castle is dark and lantern-led): --lantern off|on judges the INTERIOR world views (S1, S2, S3 and the
art_* views) against the new bands instead of the old 12-30 %: lantern off (captured with `-hc-capture-lantern off`, the default) must be
35-55 % near-black (aim 45), lantern on (`-hc-capture-lantern on`) 12-25 % (aim 18). "Near-black" is unchanged: luma < 8 of 255 on the PNG.
Other views (S4 exterior, S5/S6, hunt frames, UI) are listed but not judged under --lantern, because they have their own lighting.
"""
import sys, json
from pathlib import Path
import numpy as np
from PIL import Image

LOW, HIGH, TARGET = 12.0, 30.0, 20.0
UI = ("S7", "S8")
LANTERN_BANDS = {"off": (35.0, 55.0, 45.0), "on": (12.0, 25.0, 18.0)}
LANTERN_VIEWS = ("S1", "S2", "S3", "art_")

def measure(path):
    a = np.asarray(Image.open(path).convert("RGB"), dtype=np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    luma = 0.299 * r + 0.587 * g + 0.114 * b
    warm = (r > g + 20) & (g > b) & (luma > 40)
    return dict(mean=float(luma.mean()), dark=float((luma < 8).mean() * 100), warm=float(warm.mean() * 100), p1=float(np.percentile(luma, 1)))

def verdict(name, dark, lantern=None):
    if name.startswith(UI): return "ui"
    if lantern:
        if not name.startswith(LANTERN_VIEWS): return "n/a"
        low, high, _ = LANTERN_BANDS[lantern]
    else:
        low, high = LOW, HIGH
    if dark < low: return "TOO BRIGHT"
    if dark > high: return "TOO DARK"
    return "ok"

def run(folder):
    out = {}
    for p in sorted(Path(folder).glob("*.png")):
        out[p.stem] = measure(p)
    return out

def half_step(dark, lantern=None):
    """Suggested change in dark% for the next round: half the distance to the target (20 %, or the lantern band's aim; zero if inside the band)."""
    low, high, target = LANTERN_BANDS[lantern] if lantern else (LOW, HIGH, TARGET)
    if low <= dark <= high: return 0.0
    return (target - dark) / 2.0

def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    if not args:
        print(__doc__); return 2
    folder = args[0]
    prev = None
    if "--prev" in sys.argv: prev = run(sys.argv[sys.argv.index("--prev") + 1])
    res = run(folder)
    lantern = sys.argv[sys.argv.index("--lantern") + 1] if "--lantern" in sys.argv else None
    if lantern not in (None, "off", "on"):
        print("--lantern must be off or on"); return 2
    if lantern: print("lantern %s: judged views %s must be %.0f-%.0f %% near-black (aim %.0f)" % ((lantern, ", ".join(LANTERN_VIEWS)) + LANTERN_BANDS[lantern]))
    print("%-22s %7s %9s %7s %6s  %-10s %s" % ("capture", "mean", "dark<8 %", "warm %", "p1", "verdict", "next-round step (dark %)" + ("   prev" if prev else "")))
    bad = 0
    for k, v in res.items():
        vd = verdict(k, v["dark"], lantern)
        step = "" if vd in ("ok", "ui", "n/a") else "%+.1f -> aim %.1f" % (half_step(v["dark"], lantern), v["dark"] + half_step(v["dark"], lantern))
        pv = ""
        if prev and k in prev: pv = "   %.1f (%+.1f)" % (prev[k]["dark"], v["dark"] - prev[k]["dark"])
        print("%-22s %7.1f %9.1f %7.1f %6.1f  %-10s %s%s" % (k, v["mean"], v["dark"], v["warm"], v["p1"], vd, step, pv))
        if vd in ("TOO BRIGHT", "TOO DARK"): bad += 1
    judged = [k for k in res if not k.startswith(UI) and verdict(k, res[k]["dark"], lantern) != "n/a"]
    print("in band: %d / %d judged" % (sum(1 for k in judged if verdict(k, res[k]["dark"], lantern) == "ok"), len(judged)))
    if "--json" in sys.argv:
        Path(sys.argv[sys.argv.index("--json") + 1]).write_text(json.dumps(res, indent=1))
    return 0

if __name__ == "__main__":
    sys.exit(main())
