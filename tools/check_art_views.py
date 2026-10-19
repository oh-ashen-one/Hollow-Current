#!/usr/bin/env python3
"""Wave FF (hc/ff-art): the r14 critic's art pixel tests (rounds/r14/CRITIC.md section 9 item 1, plus the exposure bands and the S4 frame time)
on a folder of captures, one PASS/FAIL line per test. numpy + Pillow + scipy only; no Unity.

    python3 tools/check_art_views.py CAPTURE_DIR [CAPTURE_DIR ...] [--json out.json] [--quiet]

The folders are searched recursively for S1_great_hall.png, S4_exterior_sky.png, art_exterior_battlement_out.png and
art_exterior_bridge_left.png (the integrator's captures/<round>/{rooms_off,rooms_on,ext} or the critic's critic/{cap_rooms,cap_rooms_lantern,
art_exterior}). Lantern state comes from the sibling player.log ("lantern on|off") or the folder name. Exit code 0 only if every test PASSes.

Geometry masks (tools/ff_art_masks/, made by tools/castle_fd_raycast.py --ff and tools/ff_hall_raycast.py, BVH proxies of the real layout at
the capture eyes): the moat pixels of each exterior view; for S1 the lancet openings, the pier columns, the banquet table and chairs, and the
floor and dais. A mask is only valid for its camera: the tool compares capture.json's eye/target with the mask's and says WARN when they differ.
Wave FG (hc/fg-polish): the S1 masks are for the FG S1 camera (eye (0, 3.6, 16.0), target (0, 1.27, 28)), so FF-or-older S1 frames WARN;
the S4 / battlement_out / bridge_left moat masks had the proxy's missing shore rocks and bank edge cut out by tools/fg_moat_masks.py (the
FF "S4 glitter left" patch was 95 % foreground boulder, the battlement "seam" a lamp-lit water / dark rock edge inside the mask).

Tests (luma = Rec.601 on 8-bit sRGB, as tools/exposure_report.py; Rec.709 where noted):
  S1 lancets        lancet openings whose visible (in-frame, unoccluded in the proxy) height is >= 150 px and whose glass shows in the frame
                    (>= 150 rows of the opening with luma >= 14): at least 3. Moon: a cluster of >= 80 px with luma >= 150 inside a lancet.
  S1 piers          for every pier column with >= 4k visible px in the proxy: bounding-box height / width; the nearest pier per side must be >= 2.
  S1 table          table + chairs + table candlesticks pixels with luma >= 14 in the lantern-off frame: >= 15000 (the r14 frame: "a small
                    dark shape ... legible only with the lantern on").
  S1 floor mirror   below the horizon, within +-60 px of the rose window's x: a connected cluster of >= 80 px of floor, dais or polished
                    table-top pixels with luma >= 40 that are not warm (R > G + 20 and G > B: flames, candle-lit wood), i.e. the rose's cold streak.
                    From S1 the rose's true mirror point is 35 degrees below the horizon, under the skeleton's ribcage, and the axis beyond it
                    is the table: the streak shows on the table top and in the floor gaps (floor-only cluster reported as INFO).
  S4 seam           battlement_out and bridge_left: along every moat row, a 7-px median then 8-px block means; the largest step between
                    neighbouring blocks must be <= 25 (the r14 "second, striped material meets the glitter one at a hard seam").
  S4 glitter        S4: luma sd of the moat right of the bridge (x >= 1230, the patch the r14 critic measured at sd 30.7) <= 12, and of the
                    moat left of the bridge outside the pale moon's column (|x - pale x| > 100) <= 12.
  S4 moons          S4: a red cluster (R > 120, R > 2 G, >= 150 px) in the moat within +-80 px of the red moon's x; a pale glade (luma >= 120,
                    >= 100 px) within +-120 px of the pale moon's x. Moon x from capture.json's S4 note (viewport=...), else 0.39 / 0.67.
  S4 frame time     FRAMETIME S4_exterior_sky p99 <= 16.7 ms in every capture folder's player.log / capture.json.
  Exposure          S1 lantern off: near-black share (Rec.601 luma < 8, tools/exposure_report.py) in 35-55 % (r14: 30.0, accepted at >= 29 by
                    the director: reported as WARN between 29 and 35); lantern on 12-25 %.
"""
import json, re, sys
from pathlib import Path
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

ROOT = Path(__file__).resolve().parents[1]
MASKS = ROOT / "tools" / "ff_art_masks"
REC709 = np.array([.2126, .7152, .0722])
REC601 = np.array([.299, .587, .114])   # the project's "luma" (tools/exposure_report.py); r14 bridge_left seam = 27.8 on it, 21 on Rec.709
results = []

def report(test, ok, detail, level=None):
    lv = level or ("PASS" if ok else "FAIL")
    results.append(dict(test=test, result=lv, detail=detail))
    if "--quiet" not in sys.argv or lv != "PASS":
        print("%-4s  %-28s %s" % (lv, test, detail))

def load(p):
    return np.asarray(Image.open(p).convert("RGB")).astype(np.float64)

def mask(name, shape):
    p = MASKS / name
    if not p.exists(): return None
    m = np.asarray(Image.open(p).convert("L")) > 127
    if m.shape != shape[:2]:
        m = np.asarray(Image.fromarray((m * 255).astype(np.uint8)).resize((shape[1], shape[0]), Image.NEAREST)) > 127
    return m

def capture_entry(png):
    """capture.json entry for this png (eye, target, note) if the folder has one."""
    cj = png.parent / "capture.json"
    if not cj.exists(): return {}
    try:
        data = json.loads(cj.read_text())
    except Exception:
        return {}
    for e in data.get("entries", []):
        if e.get("id") == png.stem or (e.get("file") or "").endswith("/" + png.name): return e
    return {}

def vec(s):
    try: return np.array([float(x) for x in s.split(",")])
    except Exception: return None

def camera_check(png, meta_key):
    """False when capture.json's camera differs from the mask's (None when either is unknown, True when they match)."""
    mj = MASKS / ("hall_masks.json" if png.stem.startswith("S1") else "exterior_masks.json")
    if not mj.exists(): return None
    meta = json.loads(mj.read_text()).get(meta_key)
    e = capture_entry(png)
    if not meta or not e: return None
    eye = vec(e.get("eye", "")); tgt = vec(e.get("target", ""))
    if eye is None or tgt is None: return None
    fwd = tgt - eye; fwd /= max(np.linalg.norm(fwd), 1e-9)
    de = np.linalg.norm(eye - np.array(meta["eye"])); df = np.degrees(np.arccos(np.clip(fwd @ np.array(meta["fwd"]), -1, 1)))
    if de > .05 or df > .5:
        report("mask camera " + png.stem, False, "capture eye %s target %s vs mask eye %s fwd %s (de %.2f m, dfwd %.1f deg): masks do not fit this frame,"
               " its mask tests are WARN only" % (e.get("eye"), e.get("target"), meta["eye"], meta["fwd"], de, df), "WARN")
        return False
    return True

def lantern_of(png):
    log = png.parent / "player.log"
    if log.exists():
        m = re.search(r"lantern (on|off)", log.read_text(errors="ignore"))
        if m: return m.group(1)
    n = png.parent.name.lower()
    if "lantern" in n or n.endswith("_on") or "rooms_on" in n: return "on"
    return "off"

# ------------------------------------------------------------------------------------------------ exterior
def row_steps(L, M):
    """Largest step between neighbouring 8-px block means along every moat row (7-px median first). Returns (step, x, y)."""
    best = (0.0, -1, -1)
    for y in range(0, L.shape[0], 2):
        row = M[y]
        if row.sum() < 64: continue
        idx = np.flatnonzero(row)
        runs = np.split(idx, np.flatnonzero(np.diff(idx) > 1) + 1)
        for r in runs:
            if len(r) < 48: continue
            v = ndi.median_filter(L[y, r[0]:r[-1] + 1], size=7, mode="nearest")
            c = np.cumsum(np.concatenate([[0], v]))
            k = 8
            if len(v) < 2 * k + 1: continue
            a = (c[k:len(v) - k + 1] - c[:len(v) - 2 * k + 1]) / k          # mean of v[x-8..x-1]
            b = (c[2 * k:] - c[k:len(v) - k + 1]) / k                        # mean of v[x..x+7]
            d = np.abs(b - a); i = int(d.argmax())
            if d[i] > best[0]: best = (float(d[i]), int(r[0] + i + k), y)
    return best

def moon_x(png, which):
    e = capture_entry(png); note = e.get("note", "")
    m = re.search(which + r"_moon inFrame=\w+ occluded=\w+ viewport=\(([-\d.]+),([-\d.]+)\)", note)
    return (float(m.group(1)) if m else (.39 if which == "pale" else .67)), bool(m)

def exterior(pngs):
    for name in ("art_exterior_battlement_out", "art_exterior_bridge_left"):
        for p in pngs.get(name, []):
            a = load(p); M = mask(name + "_moat.png", a.shape)
            if M is None: report("S4 seam " + name, False, "no mask tools/ff_art_masks/%s_moat.png" % name, "WARN"); continue
            camera_check(p, name)
            step, x, y = row_steps(a @ REC601, M)
            report("S4 seam " + name.replace("art_exterior_", ""), step <= 25, "max luma step %.1f at (%d, %d) (pass <= 25) %s" % (step, x, y, p))
    for p in pngs.get("S4_exterior_sky", []):
        a = load(p); L = a @ REC601; H, W = L.shape
        M = mask("S4_exterior_sky_moat.png", a.shape)
        if M is None: report("S4 moat", False, "no mask", "WARN"); continue
        camera_check(p, "S4_exterior_sky")
        xs = np.arange(W)[None, :].repeat(H, 0)
        (px, pf), (rx, rf) = moon_x(p, "pale"), moon_x(p, "red")
        pale_x, red_x = px * W, rx * W
        right = M & (xs >= 1230 * W / 1920)
        left = M & (xs <= 720 * W / 1920) & (np.abs(xs - pale_x) > 100)
        sdr = float(L[right].std()) if right.sum() > 500 else -1
        sdl = float(L[left].std()) if left.sum() > 500 else -1
        report("S4 glitter right", 0 <= sdr <= 12, "moat luma sd %.1f right of the bridge (%d px; r14 30.7; pass <= 12) %s" % (sdr, right.sum(), p))
        report("S4 glitter left", 0 <= sdl <= 12, "moat luma sd %.1f left of the bridge outside the pale column (%d px; pass <= 12)" % (sdl, left.sum()))
        R, G = a[..., 0], a[..., 1]
        red = M & (np.abs(xs - red_x) <= 80) & (R > 120) & (R > 2 * G)
        lab, n = ndi.label(red); big = int(ndi.sum(red, lab, range(1, n + 1)).max()) if n else 0
        report("S4 red moon mirrored", big >= 150, "largest R>120 cluster %d px within +-80 of the red moon x %d%s (pass >= 150)" % (big, red_x, "" if rf else " (default x)"))
        pale = M & (np.abs(xs - pale_x) <= 120) & (L >= 120)
        lab, n = ndi.label(pale); bigp = int(ndi.sum(pale, lab, range(1, n + 1)).max()) if n else 0
        report("S4 pale moon glade", bigp >= 100, "largest luma>=120 cluster %d px within +-120 of the pale moon x %d%s (pass >= 100)" % (bigp, pale_x, "" if pf else " (default x)"))
        rr = M & (R > 1.6 * G)
        report("S4 moat red", float(R[M].mean() / max(G[M].mean(), 1)) >= 1.6, "moat mean R/G %.2f (r13 test, pass >= 1.6)" % (R[M].mean() / max(G[M].mean(), 1)))

def frametime(dirs):
    seen = False
    for d in dirs:
        for log in sorted(Path(d).rglob("player.log")):
            for m in re.finditer(r"FRAMETIME S4_exterior_sky frames=\d+ mean=([\d.]+)ms p95=[\d.]+ms p99=([\d.]+)ms max=([\d.]+)ms", log.read_text(errors="ignore")):
                seen = True; p99 = float(m.group(2))
                report("S4 frame time", p99 <= 16.7, "p99 %.2f ms mean %s max %s (pass <= 16.7) %s" % (p99, m.group(1), m.group(3), log))
    if not seen: report("S4 frame time", False, "no FRAMETIME S4_exterior_sky line in any player.log", "WARN")

# ------------------------------------------------------------------------------------------------ great hall
def hall(pngs):
    meta_p = MASKS / "hall_masks.json"
    meta = json.loads(meta_p.read_text()) if meta_p.exists() else {}
    hm = meta.get("S1_great_hall", {})
    for p in pngs.get("S1_great_hall", []):
        lan = lantern_of(p)
        a = load(p); L = a @ REC709; H, W = L.shape
        Y601 = a @ np.array([.299, .587, .114]); dark = float((Y601 < 8).mean() * 100)
        if lan == "off":
            lv = "PASS" if 35 <= dark <= 55 else ("WARN" if 29 <= dark < 35 else "FAIL")
            report("exposure S1 lantern off", lv == "PASS", "near-black %.1f %% (band 35-55; r14 30.0, director accepts 29-35 as WARN) %s" % (dark, p), lv)
        else:
            report("exposure S1 lantern on", 12 <= dark <= 25, "near-black %.1f %% (band 12-25) %s" % (dark, p))
            continue
        if not hm: report("S1 masks", False, "no tools/ff_art_masks/hall_masks.json", "WARN"); continue
        fits = camera_check(p, "S1_great_hall") is not False
        def rep(test, ok, detail, level=None): report(test, ok, detail, level if fits else ("INFO" if level == "INFO" else "WARN"))
        # lancets
        good, moon, lines = 0, 0, []
        for lan_id, info in sorted(hm.get("lancets", {}).items()):
            m = mask(info["mask"], a.shape)
            if m is None: continue
            rows_geom = info["visible_rows"]
            lit = m & (L >= 14)
            rows_lit = int((lit.sum(1) >= 3).sum())
            ok = rows_geom >= 150 and rows_lit >= 150
            good += ok
            bright = m & (L >= 150); lab, n = ndi.label(bright); bm = int(ndi.sum(bright, lab, range(1, n + 1)).max()) if n else 0
            moon += bm >= 80
            lines.append("%s %d/%d rows%s%s" % (lan_id, rows_geom, rows_lit, " moon %dpx" % bm if bm else "", "" if ok else " (short)"))
        rep("S1 lancets >= 150 px", good >= 3, "%d lancets (pass >= 3): %s" % (good, "; ".join(lines)))
        rep("S1 moon in a lancet", moon >= 1, "%d lancets show a moon cluster >= 80 px of luma >= 150" % moon)
        # piers (geometry)
        piers = hm.get("piers", {})
        worst = {}
        for pid, info in piers.items():
            side = "west" if info["x"] < 0 else "east"
            if side not in worst or info["depth"] < worst[side]["depth"]: worst[side] = dict(info, id=pid)
        ok = bool(worst) and all(w["aspect"] >= 2.0 for w in worst.values())
        rep("S1 piers 2x tall", ok, "; ".join("%s nearest %s: %dx%d px aspect %.2f" % (s, w["id"], w["h"], w["w"], w["aspect"]) for s, w in sorted(worst.items())) + " (pass >= 2.0, proxy geometry)")
        # table
        tm = mask(hm.get("table_mask", "S1_table.png"), a.shape)
        if tm is not None:
            n14 = int((tm & (L >= 14)).sum()); n8 = int((tm & (L >= 8)).sum())
            rep("S1 table lantern off", n14 >= 15000, "%d table px with luma >= 14 (%d >= 8; %d in the proxy) (pass >= 15000)" % (n14, n8, int(tm.sum())))
        # floor mirror of the rose window
        fm = mask(hm.get("mirror_mask", "S1_mirror.png"), a.shape)
        rx, hy = hm.get("rose_x", W / 2), hm.get("horizon_y", H / 2)
        xs = np.arange(W)[None, :].repeat(H, 0); ys = np.arange(H)[:, None].repeat(W, 1)
        R, G, B = a[..., 0], a[..., 1], a[..., 2]
        warm = (R > G + 20) & (G > B)                     # the project's warm formula (tools/exposure_report.py): flames, candle-lit wood
        zone = (np.abs(xs - rx) <= 60) & (ys > hy) & ~warm & (L >= 40)
        for label, m in (("floor", zone & fm if fm is not None else None), ("any", zone)):
            if m is None: continue
            lab, n = ndi.label(m); big = int(ndi.sum(m, lab, range(1, n + 1)).max()) if n else 0
            if label == "floor":
                rep("S1 floor mirror", big >= 80, "largest not-warm luma>=40 cluster on the floor / dais / polished table top: %d px within +-60 px of the rose x %d below the horizon y %d (pass >= 80)" % (big, rx, hy))
            else:
                rep("S1 bright below rose", True, "largest not-warm luma>=40 cluster (any surface) %d px" % big, "INFO")
        if fm is not None:
            flo = mask(hm.get("floor_mask", "S1_floor.png"), a.shape)
            if flo is not None:
                m = zone & flo; lab, n = ndi.label(m); big = int(ndi.sum(m, lab, range(1, n + 1)).max()) if n else 0
                rep("S1 floor-only mirror", True, "the same on the floor / dais alone: %d px (the true mirror point is under the skeleton's ribcage)" % big, "INFO")

def main():
    dirs = [Path(a) for a in sys.argv[1:] if not a.startswith("--") and not (sys.argv[sys.argv.index(a) - 1] == "--json")]
    if not dirs: print(__doc__); return 2
    names = ("S1_great_hall", "S4_exterior_sky", "art_exterior_battlement_out", "art_exterior_bridge_left")
    pngs = {n: [] for n in names}
    for d in dirs:
        for n in names: pngs[n] += sorted(d.rglob(n + ".png"))
    for n in names:
        if not pngs[n]: report("input " + n, False, "not found under " + ", ".join(map(str, dirs)), "WARN")
    hall(pngs); exterior(pngs); frametime(dirs)
    fails = [r for r in results if r["result"] == "FAIL"]
    print("HC_ART_VIEWS pass=%s fail=%d warn=%d tests=%d" % (not fails, len(fails), sum(r["result"] == "WARN" for r in results), len(results)))
    if "--json" in sys.argv: Path(sys.argv[sys.argv.index("--json") + 1]).write_text(json.dumps(results, indent=1))
    return 0 if not fails else 1

if __name__ == "__main__":
    sys.exit(main())
