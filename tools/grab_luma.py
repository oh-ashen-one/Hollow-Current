#!/usr/bin/env python3
"""ff-count: the r14 critic's hide-grab readability test, run on a playtest's grab frames.

For every grab frame listed in <run>/grabcam.txt (the driver writes one line per HC_COUNT_GRABCAM sample) whose path is a hide grab
(path=nearmiss or path=hideopen; --all for every path) at t=0.16, it measures:
  - the frame's mean luma (Rec. 709 weights on 8-bit sRGB values, the critic's measure);
  - the share of pixels with luma > 60 inside the Count's screen box (countRect=, logged at 2560 x 1440 scale like skull_px, scaled here
    to the frame's size). Frames from builds without countRect= fall back to the central 60 % box and say so.
Pass (r14 critic, top change 2): mean >= 18 and >= 5 % of the box over 60.

Usage: python3 tools/grab_luma.py <run dir> [<run dir> ...] [--all] [--t 0.16]
Exit 1 if any measured frame fails, 2 if no frame was measured. Reads PNGs only; never launches anything.
"""
import re
import sys
from pathlib import Path

try:
    import numpy as np
    from PIL import Image
except ImportError:  # pragma: no cover
    sys.exit("needs numpy and Pillow")

MEAN_MIN, BRIGHT, SHARE_MIN = 18.0, 60.0, 0.05


def measure(png, rect):
    a = np.asarray(Image.open(png).convert("RGB")).astype(np.float32)
    luma = 0.2126 * a[..., 0] + 0.7152 * a[..., 1] + 0.0722 * a[..., 2]
    h, w = luma.shape
    if rect:
        x0, y0, x1, y1 = rect
        sx, sy = w / 2560.0, h / 1440.0
        box = luma[max(0, int(y0 * sy)):min(h, int(y1 * sy) + 1), max(0, int(x0 * sx)):min(w, int(x1 * sx) + 1)]
        kind = "countRect"
    else:
        box = luma[int(h * .2):int(h * .8), int(w * .2):int(w * .8)]
        kind = "central60 (no countRect in this build)"
    share = float((box > BRIGHT).mean()) if box.size else 0.0
    return float(luma.mean()), share, kind


def main(argv):
    every = "--all" in argv
    want_t = 0.16
    if "--t" in argv:
        want_t = float(argv[argv.index("--t") + 1])
    dirs = [Path(a) for a in argv if not a.startswith("--") and a != str(want_t)]
    measured = failed = 0
    for d in dirs:
        listing = d / "grabcam.txt"
        if not listing.is_file():
            print(f"{d}: no grabcam.txt")
            continue
        for line in listing.read_text(errors="replace").splitlines():
            if "\t" not in line:
                continue
            name, fields = line.split("\t", 1)
            path = re.search(r"\bpath=(\S+)", fields)
            t = re.search(r"\bt=([0-9.]+)", fields)
            if not path or not t or abs(float(t.group(1)) - want_t) > 0.005:
                continue
            if not every and path.group(1) not in ("nearmiss", "hideopen"):
                continue
            png = d / name
            if not png.is_file():
                print(f"{png}: missing frame")
                continue
            m = re.search(r"\bcountRect=\(([-0-9.]+),([-0-9.]+),([-0-9.]+),([-0-9.]+)\)", fields)
            rect = tuple(float(v) for v in m.groups()) if m else None
            mean, share, kind = measure(png, rect)
            ok = mean >= MEAN_MIN and share >= SHARE_MIN
            measured += 1
            failed += 0 if ok else 1
            print(f"{'PASS' if ok else 'FAIL'} {png} path={path.group(1)} t={t.group(1)} mean_luma={mean:.1f} (>= {MEAN_MIN:.0f}) "
                  f"bright_in_box={share * 100:.1f}% (>= {SHARE_MIN * 100:.0f}%, {kind})")
    if measured == 0:
        print("no hide-grab frame at t=%.2f found" % want_t)
        return 2
    print(f"grab_luma: {measured - failed}/{measured} frames pass")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
