"""Window atlas for the exterior kit (round 12 polish, hc/art-exterior2). Pure python + numpy (no bpy), so both the Blender kit script and
the system-python proof can import it.

One 1024x1024 sRGB atlas, v up (row 0 of the written PNG is v = 1):
    slits    x   0..511, y 512..1023   four 128x512 cells (variants 0..3): lit arrow slit, a dark cross bar, a soft radial amber falloff
    lancet   x 512..1023, y 512..1023   one 512x512 cell drawn for a 1.3 x 6.2 m window: leaded diamonds, mullion, transoms, falloff
    gate     x   0..1023, y   0..511    the gate tympanum: a dim lit gatehouse seen through the hood (vault ribs, coursing, two torches,
                                        a deep amber glow low in the middle)
Colours: dark leading #1c0e04-ish, glass core #d9933a (217,147,58), glass edge #5a2e0a (90,46,10); no pure yellow. CastleWindow.shader multiplies
the texture by _Intensity (1.5, so the hottest channel stays under 2.0).
"""
import zlib, struct
from pathlib import Path
import numpy as np

SIZE = 1024
CORE = np.array([217, 147, 58], np.float32) / 255.0
EDGE = np.array([90, 46, 10], np.float32) / 255.0
LEAD = np.array([26, 12, 4], np.float32) / 255.0
# name -> (x0, y0, x1, y1) pixel rect with y measured from the BOTTOM (v up), physical size (w, h) metres the drawing assumes
CELLS = {
    "slit0": ((0, 512, 128, 1024), (.30, 1.7)), "slit1": ((128, 512, 256, 1024), (.30, 1.7)),
    "slit2": ((256, 512, 384, 1024), (.30, 1.7)), "slit3": ((384, 512, 512, 1024), (.30, 1.7)),
    "lancet": ((512, 512, 1024, 1024), (1.3, 6.2)),
    "gate": ((0, 0, 1024, 512), (3.7, 2.1)),
}
SLIT_VARIANTS = 4

def uv_rect(name):
    (x0, y0, x1, y1), _ = CELLS[name]
    return x0 / SIZE, y0 / SIZE, x1 / SIZE, y1 / SIZE

def _noise(w, h, seed, scale):
    rng = np.random.default_rng(seed)
    gw, gh = max(2, int(w / scale)), max(2, int(h / scale))
    g = rng.random((gh + 1, gw + 1)).astype(np.float32)
    ys = np.linspace(0, gh, h, endpoint=False); xs = np.linspace(0, gw, w, endpoint=False)
    y0 = ys.astype(int); x0 = xs.astype(int); fy = (ys - y0)[:, None]; fx = (xs - x0)[None, :]
    fy = fy * fy * (3 - 2 * fy); fx = fx * fx * (3 - 2 * fx)
    a = g[y0][:, x0]; b = g[y0][:, x0 + 1]; c = g[y0 + 1][:, x0]; d = g[y0 + 1][:, x0 + 1]
    return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy

def _cell(name, fn, seed):
    (x0, y0, x1, y1), (pw, ph) = CELLS[name]
    w, h = x1 - x0, y1 - y0
    # physical coordinates of every texel centre; y up, 0 at the bottom of the cell
    X = ((np.arange(w, dtype=np.float32) + .5) / w * pw)[None, :].repeat(h, 0)
    Y = ((np.arange(h, dtype=np.float32) + .5) / h * ph)[:, None].repeat(w, 1)
    return fn(X, Y, pw, ph, w, h, seed)

def _glow(I, mid=.0):
    I = np.clip(I, 0, 1)[..., None]
    return EDGE + (CORE - EDGE) * I

def _stripe(d, width, soft=.012):
    """1 inside a stripe of half width `width` around distance d = 0, soft edge."""
    return np.clip((width - np.abs(d)) / soft, 0, 1)

def slit(X, Y, pw, ph, w, h, seed):
    v = seed % SLIT_VARIANTS
    bright = (.74, 1.0, .86, .93)[v]
    cx, cy = pw * .5, ph * (.34, .5, .62, .44)[v]
    I = np.exp(-(((X - cx) / (pw * .75)) ** 2 + ((Y - cy) / (ph * .30)) ** 2)) * bright
    I = I * (.9 + .2 * _noise(w, h, seed + 5, 18))
    col = _glow(.08 + .92 * I)
    bar_y = (.38, .66, .5, .3)[v] * ph
    bar = _stripe(Y - bar_y, .028) + (_stripe(Y - bar_y - .5, .018) if v in (0, 2) else 0)
    edge = _stripe(X - pw * .5, pw * .5 + .001) - _stripe(X - pw * .5, pw * .5 - .028)           # thin dark jamb on each side
    dark = np.clip(bar + edge, 0, 1)[..., None]
    return col * (1 - dark) + LEAD * dark

def lancet(X, Y, pw, ph, w, h, seed):
    cx, cy = pw * .5, ph * .36
    I = np.exp(-(((X - cx) / (pw * .60)) ** 2 + ((Y - cy) / (ph * .30)) ** 2))
    I = I * (.86 + .28 * _noise(w, h, 7, 40))
    col = _glow(.05 + .95 * I)
    s = .42                                                    # diamond leading, 45 degrees
    d1 = ((X + Y) / 1.4142 + 1000 * s) % s - s / 2
    d2 = ((X - Y) / 1.4142 + 1000 * s) % s - s / 2
    lead = np.maximum(_stripe(d1, .02), _stripe(d2, .02))
    mull = _stripe(X - cx, .042)
    trans = np.maximum(_stripe(Y - ph * .30, .035), _stripe(Y - ph * .60, .035))
    frame = np.maximum(_stripe(X - pw * .5, pw * .5 + .001) - _stripe(X - pw * .5, pw * .5 - .08), 0)
    dark = np.clip(np.maximum.reduce([lead * .9, mull, trans, frame]), 0, 1)[..., None]
    return col * (1 - dark) + LEAD * dark

def gate(X, Y, pw, ph, w, h, seed):
    cx = pw * .5
    base = np.array([34, 17, 6], np.float32) / 255.0
    # central lit passage (deep amber, falls off hard toward the top and sides) and two torches low on the flanks
    I = np.exp(-(((X - cx) / 1.05) ** 2 + ((Y - .18) / .85) ** 2)) * .85
    for tx in (cx - 1.38, cx + 1.38):
        I = I + np.exp(-(((X - tx) / .42) ** 2 + ((Y - .55) / .62) ** 2)) * .9
    I = np.clip(I * (.9 + .2 * _noise(w, h, 3, 40)), 0, 1)[..., None]
    col = base + (CORE - base) * I
    # stone vault behind: coursing and two pointed ribs, dark against the glow
    cours = _stripe(((Y + 1000) % .36) - .18, .012) * .35
    rib = np.zeros_like(X)
    for r in (.62, .84):
        rr = np.sqrt(((X - cx) / (1.75 * r)) ** 2 + (Y / (2.0 * r)) ** 2)
        rib = np.maximum(rib, _stripe((rr - 1) * .9, .05))
    rib = np.maximum(rib, _stripe(X - cx, .035) * (Y > .6))
    torch_dark = np.zeros_like(X)
    for tx in (cx - 1.38, cx + 1.38):                            # a black bracket and flame stem under each torch
        torch_dark = np.maximum(torch_dark, _stripe(X - tx, .05) * ((Y > .05) & (Y < .42)))
    dark = np.clip(np.maximum.reduce([cours, rib * .9, torch_dark]), 0, 1)[..., None]
    return col * (1 - dark) + LEAD * dark

def build():
    """Return the atlas as float RGB (v up: row 0 = bottom). Unused texels are dark amber so a bilinear fetch near a cell edge stays dark."""
    img = np.zeros((SIZE, SIZE, 3), np.float32) + EDGE * .4
    for v in range(SLIT_VARIANTS):
        name = "slit%d" % v
        (x0, y0, x1, y1), _ = CELLS[name]
        img[y0:y1, x0:x1] = _cell(name, slit, v)
    for name, fn in (("lancet", lancet), ("gate", gate)):
        (x0, y0, x1, y1), _ = CELLS[name]
        img[y0:y1, x0:x1] = _cell(name, fn, 1)
    return img

def write_png(path, rgb_vup):
    arr = np.clip(rgb_vup[::-1] * 255 + .5, 0, 255).astype(np.uint8)           # PNG row 0 is the top (v = 1)
    h, w, _ = arr.shape
    raw = b"".join(b"\x00" + arr[y].tobytes() for y in range(h))
    def chunk(tag, data):
        c = struct.pack(">I", len(data)) + tag + data
        return c + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)
    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
    Path(path).write_bytes(png)
    return arr
