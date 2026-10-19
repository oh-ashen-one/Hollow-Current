#!/usr/bin/env python3
"""Wave FF (hc/ff-art): three procedural textures (numpy + Pillow, no network, own work, CC BY 4.0 with the repo's media):

    python3 tools/ff_hall_glass.py [--preview DIR]

  Castle/Textures/Castle_NightSkyCard.png   1024x512  the moonlit night sky behind the great hall's north wall (rose window, the four lancets),
                                            seen only from inside: deep blue-black at the top, a cold moon haze toward the sill line, soft
                                            cloud banks, a few stars. Unlit (HollowCurrent/CastleWindow), so the openings read as lit sky.
  Castle/Textures/Castle_MoonGlass.png      512x768   leaded glass for the blind gallery lancets: diamond quarries 0.13 x 0.20 m (the chapel's
                                            module) in pale moon-greys and blues, lead cames, a few dark (painted) and broken quarries, cold
                                            light stronger toward the head, as if the moon stood behind it.
  Castle/Textures/Castle_ClawScoresProcedural.png  1024x1024 RGBA  the latent claw-scores the grave-ash lantern shows (World.CastleTrace):
                                            four parallel gouges slanting across, a shorter crossing set of three, and a faint curved wing-drag
                                            smear, pale blue-white ash on black; alpha = brightness. Loaded only when Codex's generated
                                            Castle/ClawScores.png is absent (r14 critic: "a white *human* handprint labelled claw-scores").
Each gets a .meta from castle_common.write_texture_meta (path-derived guid).
"""
import sys, math
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter
sys.path.insert(0, str(Path(__file__).resolve().parent))
import castle_common as cc

rng = np.random.default_rng(20261019)

def fbm(h, w, octaves=5, base=4, seed=0):
    r = np.random.default_rng(seed); out = np.zeros((h, w)); amp = 1.0; tot = 0
    for o in range(octaves):
        n = base * 2 ** o
        g = r.random((n + 1, n * 2 + 1))
        im = Image.fromarray((g * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC)
        out += amp * np.asarray(im, np.float64) / 255; tot += amp; amp *= .5
    return out / tot

def sky_card():
    H, W = 512, 1024
    y = np.linspace(0, 1, H)[:, None]; x = np.linspace(0, 1, W)[None, :]
    top = np.array([12, 16, 34.]); low = np.array([58, 70, 104.])
    t = np.clip(y, 0, 1) ** 1.6
    img = np.repeat(top[None, None] * (1 - t[..., None]) + low[None, None] * t[..., None], W, 1)
    # moon haze: two soft glows behind the lancets (x ~ .27 and .73 of the card), stronger low
    for cx, col in ((.27, np.array([60, 66, 82.])), (.73, np.array([62, 40, 48.]))):
        d2 = ((x - cx) / .22) ** 2 + ((y - .62) / .45) ** 2
        img += col[None, None] * np.exp(-d2)[..., None] * .55
    clouds = fbm(H, W, 6, 3, 7)
    band = np.exp(-((y - .55) / .25) ** 2)
    cl = np.clip((clouds - .48) * 3.0, 0, 1) * band
    img = img * (1 - .35 * cl[..., None]) + np.array([70, 78, 98.])[None, None] * (.30 * cl[..., None])
    # stars in the upper half
    st = rng.random((H, W)) > .9988
    st &= (y < .55).repeat(W, 1)
    img[st] = np.maximum(img[st], np.array([150, 160, 190.]))
    im = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(.6))
    return im

def moon_glass():
    H, W = 768, 512                         # 1.5 x 2.25 m light at 341 px / m
    ppm = W / 1.5
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float64)
    hx, hz = .065 * ppm, .10 * ppm          # diamond half-widths (the chapel quarry: 0.13 x 0.20 m)
    u = xx / hx; v = yy / hz
    a, b = (u + v) / 2, (u - v) / 2         # diamond lattice coordinates
    ia, ib = np.floor(a), np.floor(b)
    fa, fb = a - ia, b - ib
    edge = np.minimum(np.minimum(fa, 1 - fa), np.minimum(fb, 1 - fb))
    lead = edge < .055                      # cames
    key = (ia * 73856093 + ib * 19349663) % 1000
    r = (key * 0.6180339) % 1.0
    pale = np.array([150, 166, 196.]); blue = np.array([92, 118, 168.]); grey = np.array([118, 124, 138.]); ink = np.array([22, 24, 32.])
    col = np.where((r < .45)[..., None], pale[None, None], np.where((r < .72)[..., None], blue[None, None], grey[None, None]))
    col = np.where((r > .955)[..., None], ink[None, None], col)                        # painted-over quarries
    broken = (r > .93) & (r <= .955)
    col = np.where(broken[..., None], np.array([30, 34, 50.])[None, None], col)        # broken out: dark night behind
    # moonlight from behind: stronger toward the head and the middle, a little mottle per quarry and inside each pane
    yn = yy / H; xn = xx / W
    glow = .55 + .45 * (1 - yn) ** .8
    glow *= .82 + .18 * np.exp(-((xn - .5) / .35) ** 2)
    mott = fbm(H, W, 4, 6, 11)
    col = col * (glow * (.86 + .28 * mott))[..., None] * (.9 + .2 * (key % 7 / 6.0))[..., None]
    col = np.where(lead[..., None], np.array([10, 10, 13.])[None, None], col)
    return Image.fromarray(np.clip(col, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(.5))

def claws():
    S = 1024
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float64) / S
    val = np.zeros((S, S))
    def gouge(p0, p1, width, depth, jag_seed):
        r = np.random.default_rng(jag_seed)
        p0, p1 = np.array(p0), np.array(p1); d = p1 - p0; L = np.linalg.norm(d); d /= L; n = np.array([-d[1], d[0]])
        rel = np.stack([xx - p0[0], yy - p0[1]], -1)
        t = rel @ d / L; s = rel @ n
        jag = np.interp(t, np.linspace(0, 1, 40), r.normal(0, .0035, 40))
        prof = np.clip(1 - np.abs(s - jag) / (width * (.35 + .65 * np.sin(np.clip(t, 0, 1) * math.pi) ** .6)), 0, 1)
        prof *= (t > 0) & (t < 1)
        taper = np.clip(t * 6, 0, 1) * np.clip((1 - t) * 3, 0, 1)
        core = prof ** 1.6 * taper * depth
        halo = np.clip(1 - np.abs(s - jag) / (width * 4), 0, 1) ** 2 * taper * (t > -.02) * (t < 1.02) * .22 * depth
        return np.maximum(core, halo)
    for k in range(4):                                        # four parallel claw gouges, slanting down to the right
        off = k * .085
        val = np.maximum(val, gouge((.16 + off, .14 + off * .25), (.58 + off, .86 + off * .1), .016 - k * .0015, 1.0 - k * .08, 31 + k))
    for k in range(3):                                        # a shorter crossing set
        off = k * .07
        val = np.maximum(val, gouge((.52 + off * .3, .30 + off), (.86 + off * .2, .18 + off * .9), .011, .7, 51 + k))
    # wing-drag smear: a faint curved band
    cx, cy, R = .30, 1.15, .78
    rr = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2)
    ang = np.arctan2(yy - cy, xx - cx)
    smear = np.exp(-((rr - R) / .03) ** 2) * (ang > -2.2) * (ang < -1.35) * (.35 + .65 * fbm(S, S, 4, 8, 77))
    val = np.maximum(val, smear * .35)
    val = np.clip(val + fbm(S, S, 3, 16, 5) * .03 * (val > .02), 0, 1)
    rgb = np.stack([val * 200 + 20 * val, val * 225 + 20 * val, val * 255], -1)
    a = np.clip(val * 1.1, 0, 1) * 255
    out = np.concatenate([np.clip(rgb, 0, 255), a[..., None]], -1).astype(np.uint8)
    return Image.fromarray(out, "RGBA").filter(ImageFilter.GaussianBlur(1.2))

def main():
    prev = Path(sys.argv[sys.argv.index("--preview") + 1]) if "--preview" in sys.argv else None
    jobs = [("Castle_NightSkyCard.png", sky_card, "sky"), ("Castle_MoonGlass.png", moon_glass, "emission"),
            ("Castle_ClawScoresProcedural.png", claws, "decal")]
    for name, fn, kind in jobs:
        im = fn(); p = cc.TEX / name
        im.save(p); cc.write_texture_meta(p, kind)
        if prev: prev.mkdir(parents=True, exist_ok=True); im.convert("RGB").save(prev / name)
        a = np.asarray(im.convert("RGB"), np.float64) @ [.299, .587, .114]
        print("HC_FF_GLASS", name, im.size, im.mode, "luma mean %.1f p5 %.1f p95 %.1f" % (a.mean(), np.percentile(a, 5), np.percentile(a, 95)))

if __name__ == "__main__":
    main()
