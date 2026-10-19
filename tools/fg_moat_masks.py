#!/usr/bin/env python3
"""Wave FG (hc/fg-polish): corrects the exterior moat masks of tools/check_art_views.py for the real occluders the BVH proxy lacks.

    python3 tools/fg_moat_masks.py FF_ROOMS_OFF_DIR FF_EXT_DIR [--write] [--preview OUT_DIR]

tools/castle_fd_raycast.py --ff builds the moat masks from a proxy of the approach. The proxy has no CastleExterior shore rocks (seeded
procedural meshes, CastleExterior.Approach) and only a rough bank edge, so on the FF build (Assembly-CSharp 2026-10-19 14:03:44):
  * S4_exterior_sky: 95 % of the "left of the bridge" sd patch was the two lamp-lit foreground boulders (brown, textured), not water;
    the r14/FF "S4 glitter left" sd 16.3 measured their texture (water alone: 14.5);
  * art_exterior_battlement_out: the mask ran 5-30 px above the real water edge onto the dark bank rocks and the pier base, so the
    "seam" 32.9 at (884, 890) was the lamp-lit water next to a dark rock inside the mask (water alone: 9.7);
  * art_exterior_bridge_left: 122 px over the bank (no effect).
The cameras of these views are fixed (S4: the search result eye (0, 1.64, -22) pitch 28.5 fov 90; the art views: CaptureMode's art set) and
the occluders are static, so their silhouettes are taken once from the FF frames and cut out of the proxy masks:
  * battlement_out / bridge_left: proxy mask AND R > 1.15 B (the moat is blood red in every build since FD; rock, stone and bank are blue-grey),
    opened by 1 px, specks < 200 px dropped;
  * S4: proxy mask minus the boulders (R/G < 2.5 and B < 0.9 G, left of x 760, below y 900: brown lamp-lit rock; the pale moon column is
    blue-white, B >= 0.9 G, and stays), closed and hole-filled, grown 1 px.
No water pixel the proxy had is added, so a future seam or glitter still counts. Without --write it only prints the before / after numbers.
"""
import sys
from pathlib import Path
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

ROOT = Path(__file__).resolve().parents[1]
MASKS = ROOT / "tools" / "ff_art_masks"
sys.path.insert(0, str(ROOT / "tools"))

def load(p): return np.asarray(Image.open(p).convert("RGB")).astype(np.float64)
def mask(name): return np.asarray(Image.open(MASKS / name).convert("L")) > 127
def drop_small(m, n=200):
    lab, k = ndi.label(m)
    if not k: return m
    sz = ndi.sum(m, lab, range(1, k + 1)); return np.isin(lab, 1 + np.flatnonzero(sz >= n))

def corrected(rooms_off, ext):
    out = {}
    for v in ("battlement_out", "bridge_left"):
        a = load(Path(ext) / ("art_exterior_%s.png" % v)); M = mask("art_exterior_%s_moat.png" % v)
        N = drop_small(ndi.binary_opening(M & (a[..., 0] > 1.15 * a[..., 2]), iterations=1))
        out["art_exterior_%s_moat.png" % v] = (M, N, a)
    a = load(Path(rooms_off) / "S4_exterior_sky.png"); R, G, B = a[..., 0], a[..., 1], a[..., 2]
    H, W = R.shape; xs = np.arange(W)[None].repeat(H, 0); ys = np.arange(H)[:, None].repeat(W, 1)
    rock = (R / np.maximum(G, 1) < 2.5) & (B < .9 * G) & (xs < 760) & (ys > 900)
    rock = drop_small(rock, 2000)
    rock = ndi.binary_dilation(ndi.binary_fill_holes(ndi.binary_closing(rock, iterations=3)), iterations=1)
    M = mask("S4_exterior_sky_moat.png"); out["S4_exterior_sky_moat.png"] = (M, drop_small(M & ~rock), a)
    return out

def main():
    args = [x for x in sys.argv[1:] if not x.startswith("--")]
    if len(args) < 2: print(__doc__); return 2
    import check_art_views as c
    res = corrected(args[0], args[1])
    for name, (M, N, a) in res.items():
        L = a @ c.REC601
        if name.startswith("S4"):
            xs = np.arange(L.shape[1])[None].repeat(L.shape[0], 0)
            for lab, m in (("old", M), ("new", N)):
                r = m & (xs >= 1230); l = m & (xs <= 720) & (np.abs(xs - 748) > 100)
                print("%s %s px %d  sd right %.1f (%d px)  sd left %.1f (%d px)" % (name, lab, m.sum(), L[r].std(), r.sum(), L[l].std(), l.sum()))
        else:
            print("%s old px %d step %.1f at %s | new px %d step %.1f at %s" % (name, M.sum(), *c.row_steps(L, M)[:1], c.row_steps(L, M)[1:],
                                                                         N.sum(), *c.row_steps(L, N)[:1], c.row_steps(L, N)[1:]))
        if "--preview" in sys.argv:
            od = Path(sys.argv[sys.argv.index("--preview") + 1]); od.mkdir(parents=True, exist_ok=True)
            o = a.copy(); o[M & ~N] = o[M & ~N] * .4 + np.array([255, 0, 255]) * .6; o[N] = o[N] * .6 + np.array([0, 255, 0]) * .4
            Image.fromarray(o.astype(np.uint8)).save(od / name.replace("_moat.png", "_mask_fix.png"))
        if "--write" in sys.argv:
            Image.fromarray((N * 255).astype(np.uint8)).save(MASKS / name)
    if "--write" in sys.argv: print("HC_FG_MOAT_MASKS written", MASKS)
    return 0

if __name__ == "__main__":
    sys.exit(main())
