#!/usr/bin/env python3
"""Prepare and verify the Haunted Castle wall-art images (docs/haunted-castle/CODEX_HANDOFF.md).

Pure Pillow, no network, no paid generation, no Unity. Single source of truth for sizes, crops, destinations and eye bands.

    python3 tools/wallart_prepare.py list
    python3 tools/wallart_prepare.py make <id> <generated.png> [--shift PX]   # crop + resize + write the destination + .meta
    python3 tools/wallart_prepare.py check [id ...] [--overlay DIR]           # verify what is on disk (all ids when none given)

`make` takes the raw imagegen output (any size, same aspect as the "gen" size below), crops it to the canvas aspect
(horizontal window centred, moved by --shift source pixels; positive = window moves right), resizes with Lanczos to the
final size, writes PNG (sRGB, no alpha unless the id is `claw`) and the Unity .meta (deterministic guid from the path, so
re-running never changes it). `check` re-reads the files, tests size / mode and, for the six hall portraits, that the
eyes land at canvas UV (0.40 / 0.60, 0.64) as SurrealAssets.PortraitEyeU / PortraitEyeV expect.
"""
import argparse
import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageStat

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))
import castle_common as cc  # noqa: E402  (pure python: guid + Unity .meta writers)

RES = ROOT / "Assets/HollowCurrent/Resources"
PORTRAITS = RES / "Castle/Portraits"

# Canvas aspect (width / height), measured from Portrait.fbx and PortraitDoorLeaf.fbx: 0.66 x 1.06 m and 0.63 x 1.24 m.
# crop = the window cut from the generated image (source pixels); final = the file Unity loads.
SPECS = {}
for _id, _name in (("margrave", "the margrave"), ("margravine", "the margravine"), ("steward", "the steward"),
                   ("astronomer", "the astronomer"), ("painter", "the painter"), ("cellarer", "the cellarer")):
    SPECS[_id] = dict(dest=PORTRAITS / f"Portrait_{_id}.png", gen=(1024, 1536), crop=(956, 1536), final=(1024, 2048),
                      meta="emission", eyes=True, note=_name)
SPECS["child"] = dict(dest=PORTRAITS / "Portrait_child.png", gen=(1024, 1536), crop=(780, 1536), final=(1024, 2048),
                      meta="emission", eyes=False, note="the child (door leaf canvas 0.63 x 1.24 m)")
for _id, _file in (("ledger_steward", "LedgerSteward"), ("ledger_astronomer", "LedgerAstronomer"), ("ledger_painter", "LedgerPainter")):
    SPECS[_id] = dict(dest=RES / f"Castle/{_file}.png", gen=(1536, 1024), crop=(1536, 1024), final=(1536, 1024),
                      meta="sky", eyes=False, note="Blood Ledger page shown on the note screen")
SPECS["claw"] = dict(dest=RES / "Castle/ClawScores.png", gen=(1024, 1024), crop=(1024, 910), final=(1024, 1024),
                     meta="decal", eyes=False, alpha=True, note="latent claw-scores quad 0.45 x 0.40 m, additive UVTrace shader")
SPECS["photo2"] = dict(dest=RES / "Surfaces/FamilyPhoto2.png", gen=(1536, 1024), crop=(1365, 1024), final=(1448, 1086),
                       meta="emission", eyes=False, optional=True, note="OPTIONAL second Bellwether snapshot (needs the World.cs change)")

EYE_U = (0.40, 0.60)   # 0.5 -/+ SurrealAssets.PortraitEyeU
EYE_V = 0.64           # SurrealAssets.PortraitEyeV, measured from the BOTTOM of the canvas (0 = bottom, 1 = top)


def eye_boxes(size):
    w, h = size
    cy = (1 - EYE_V) * h
    bw, bh = .07 * w, .035 * h
    return [(u * w - bw, cy - bh, u * w + bw, cy + bh) for u in EYE_U]


def luma(im, box=None):
    g = im.convert("L")
    if box:
        g = g.crop(tuple(int(round(v)) for v in box))
    return g


def check_one(spec_id, overlay=None):
    s = SPECS[spec_id]
    path = s["dest"]
    problems = []
    if not path.exists():
        return None, ["missing file " + str(path.relative_to(ROOT))]
    im = Image.open(path)
    want_mode = "RGBA" if s.get("alpha") else "RGB"
    if im.size != s["final"]:
        problems.append(f"size {im.size} != {s['final']}")
    if im.mode != want_mode:
        problems.append(f"mode {im.mode} != {want_mode}")
    if not Path(str(path) + ".meta").exists():
        problems.append("no .meta (run `make`, or let Unity create it)")
    info = f"{im.size[0]}x{im.size[1]} {im.mode}"
    if s["eyes"]:
        rgb = im.convert("RGB")
        w, h = rgb.size
        left, right = (luma(rgb, b) for b in eye_boxes(rgb.size))
        flank_rows = (int((1 - EYE_V - .035) * h), int((1 - EYE_V + .035) * h))
        flank = [luma(rgb, (0, flank_rows[0], .15 * w, flank_rows[1])), luma(rgb, (.85 * w, flank_rows[0], w, flank_rows[1]))]
        e = (ImageStat.Stat(left).mean[0] + ImageStat.Stat(right).mean[0]) / 2
        f = (ImageStat.Stat(flank[0]).mean[0] + ImageStat.Stat(flank[1]).mean[0]) / 2
        sd = min(ImageStat.Stat(left).stddev[0], ImageStat.Stat(right).stddev[0])
        # face centre: brightest 25% of columns in the eye band
        band = luma(rgb, (0, flank_rows[0], w, flank_rows[1])).resize((w // 8, 1), Image.BOX)
        cols = list(band.getdata())
        order = sorted(range(len(cols)), key=lambda i: -cols[i])[:max(1, len(cols) // 4)]
        centre = (sum(order) / len(order) + .5) / len(cols)
        info += f"; eye-box luma {e:.0f} vs flank {f:.0f}, min stddev {sd:.0f}, bright-band centre u={centre:.2f}"
        if e < 1.25 * f + 6:
            problems.append("eye boxes are not brighter than the band flanks: face not at u 0.40/0.60, v 0.64 (try --shift or regenerate)")
        if sd < 10:
            problems.append("eye boxes are flat (no eye detail there)")
        if abs(centre - .5) > .08:
            problems.append(f"face centre u={centre:.2f}, want 0.50 +-0.08 (try --shift)")
        if overlay:
            canvas = rgb.copy()
            d = ImageDraw.Draw(canvas)
            for b in eye_boxes(canvas.size):
                d.rectangle(b, outline=(0, 255, 0), width=3)
                cx, cy = (b[0] + b[2]) / 2, (b[1] + b[3]) / 2
                d.line((cx - 20, cy, cx + 20, cy), fill=(0, 255, 0), width=2)
                d.line((cx, cy - 20, cx, cy + 20), fill=(0, 255, 0), width=2)
            d.line((w / 2, 0, w / 2, h), fill=(255, 255, 0), width=1)
            Path(overlay).mkdir(parents=True, exist_ok=True)
            canvas.resize((w // 2, h // 2)).save(Path(overlay) / f"eyes_{spec_id}.png")
    if s.get("alpha") and im.mode == "RGBA":
        a = im.split()[3]
        cover = sum(1 for v in a.getdata() if v > 10) / (im.size[0] * im.size[1])
        info += f"; alpha coverage {cover:.2f}"
        if not (.03 < cover < .6):
            problems.append("alpha coverage should be 3-60 % (marks on transparent, not a filled square)")
    return info, problems


def make(spec_id, src, shift=0):
    s = SPECS[spec_id]
    im = Image.open(src).convert("RGB")
    gw, gh = s["gen"]
    if abs(im.size[0] / im.size[1] - gw / gh) > .03:
        sys.exit(f"{src}: {im.size} is not {gw}:{gh} aspect. Regenerate at {gw}x{gh}, do not stretch.")
    if im.size != (gw, gh):
        im = im.resize((gw, gh), Image.LANCZOS)
    cw, ch = s["crop"]
    x0 = max(0, min(gw - cw, (gw - cw) // 2 + shift))
    y0 = (gh - ch) // 2
    im = im.crop((x0, y0, x0 + cw, y0 + ch)).resize(s["final"], Image.LANCZOS)
    if s.get("alpha"):
        a = im.convert("L").point(lambda v: 0 if v < 12 else min(255, int((v - 12) * 255 / 200)))
        im = im.convert("RGBA")
        im.putalpha(a)
    path = s["dest"]
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.parent == PORTRAITS and not Path(str(PORTRAITS) + ".meta").exists():
        cc.write_folder_meta(PORTRAITS)
    im.save(path, optimize=True)
    cc.write_texture_meta(path, s["meta"])
    print("wrote", path.relative_to(ROOT), f"(crop x0={x0})")


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    sub.add_parser("list")
    m = sub.add_parser("make"); m.add_argument("id", choices=SPECS); m.add_argument("src"); m.add_argument("--shift", type=int, default=0)
    c = sub.add_parser("check"); c.add_argument("ids", nargs="*"); c.add_argument("--overlay")
    a = ap.parse_args()
    if a.cmd == "list":
        for k, s in SPECS.items():
            print(f"{k:18} gen {s['gen'][0]}x{s['gen'][1]}  crop {s['crop'][0]}x{s['crop'][1]}  final {s['final'][0]}x{s['final'][1]}  {s['dest'].relative_to(ROOT)}")
    elif a.cmd == "make":
        make(a.id, a.src, a.shift)
        info, problems = check_one(a.id)
        print(info, "| PROBLEMS: " + "; ".join(problems) if problems else "| OK")
        sys.exit(1 if problems else 0)
    else:
        bad = 0
        for k in (a.ids or [k for k, s in SPECS.items() if not s.get("optional")]):
            info, problems = check_one(k, a.overlay)
            print(f"{k:18}", info or "-", "| " + ("PROBLEMS: " + "; ".join(problems) if problems else "OK"))
            bad += bool(problems)
        sys.exit(1 if bad else 0)


if __name__ == "__main__":
    main()
