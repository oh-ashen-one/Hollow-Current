#!/usr/bin/env python3
"""Procedural two-moon night skybox (6-sided) for the Haunted Castle. No downloads.

    python3 tools/castle_sky.py [face_size=2048]

Writes Assets/HollowCurrent/Resources/Castle/Sky/Sky_{Front,Back,Left,Right,Up,Down}.png (+ .meta, clamp, no mips),
Resources/Castle/SkyCastle.mat (Skybox/6 Sided) and docs/haunted-castle/art/sky_preview.jpg (equirect preview).

World layout (Unity axes, azimuth measured from +Z towards +X; the player enters looking down +Z):
  large pale moon  az -34 deg, el +55 deg, angular radius 11 deg   (upper left)
  small blood moon az +42 deg, el +42 deg, angular radius 4.2 deg   (right)
  faint green aurora band between them, el ~30 deg
  point-like stars (round 12 polish: disc radius 0.03-0.08 deg with a soft 1 px edge, about 20 at 0.12 deg with a faint cross glint)
Face convention (Unity Skybox/6 Sided): Front=+Z, Back=-Z, Left=+X, Right=-X, Up=+Y, Down=-Y.
"""
import sys, math
from pathlib import Path
import numpy as np
from PIL import Image
sys.path.insert(0, str(Path(__file__).resolve().parent))
import castle_common as cc

N = int(sys.argv[1]) if len(sys.argv) > 1 else 2048
rng = np.random.default_rng(20261013)

FACES = {  # name: (forward, right, up)
    "Front": ((0, 0, 1), (1, 0, 0), (0, 1, 0)),
    "Back": ((0, 0, -1), (-1, 0, 0), (0, 1, 0)),
    "Left": ((1, 0, 0), (0, 0, -1), (0, 1, 0)),
    "Right": ((-1, 0, 0), (0, 0, 1), (0, 1, 0)),
    "Up": ((0, 1, 0), (1, 0, 0), (0, 0, -1)),
    "Down": ((0, -1, 0), (1, 0, 0), (0, 0, 1)),
}

def face_dirs(face):
    f, r, u = [np.array(v, np.float32) for v in FACES[face]]
    g = (np.arange(N, dtype=np.float32) + .5) / N * 2 - 1
    uu = g[None, :]; vv = -g[:, None]
    D = f[None, None, :] + uu[..., None] * r[None, None, :] + vv[..., None] * u[None, None, :]
    return D / np.linalg.norm(D, axis=-1, keepdims=True)

def dir_from(az, el):
    az, el = math.radians(az), math.radians(el)
    return np.array([math.sin(az) * math.cos(el), math.sin(el), math.cos(az) * math.cos(el)], np.float32)

def hash3(ix, iy, iz, seed=0):
    h = (ix.astype(np.int64) * 73856093) ^ (iy.astype(np.int64) * 19349663) ^ (iz.astype(np.int64) * 83492791) ^ (seed * 668265263)
    h = (h ^ (h >> 13)) * 1274126177 & 0xFFFFFFFF
    h = (h ^ (h >> 16)) & 0xFFFF
    return h.astype(np.float32) / 65535.0

def vnoise(p, seed=0):
    i = np.floor(p); f = p - i; f = f * f * (3 - 2 * f)
    ix, iy, iz = i[..., 0], i[..., 1], i[..., 2]
    fx, fy, fz = f[..., 0], f[..., 1], f[..., 2]
    def c(dx, dy, dz): return hash3(ix + dx, iy + dy, iz + dz, seed)
    x00 = c(0, 0, 0) * (1 - fx) + c(1, 0, 0) * fx; x10 = c(0, 1, 0) * (1 - fx) + c(1, 1, 0) * fx
    x01 = c(0, 0, 1) * (1 - fx) + c(1, 0, 1) * fx; x11 = c(0, 1, 1) * (1 - fx) + c(1, 1, 1) * fx
    y0 = x00 * (1 - fy) + x10 * fy; y1 = x01 * (1 - fy) + x11 * fy
    return y0 * (1 - fz) + y1 * fz

def fbm(p, octs=5, seed=0):
    a = 0.5; tot = 0; s = 0
    for o in range(octs):
        tot = tot + a * vnoise(p * (2 ** o), seed + o); s += a; a *= .5
    return tot / s

def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t)

PALE = dict(c=dir_from(-34, 55), R=math.radians(11.0), col=np.array([.86, .91, 1.0], np.float32))
RED = dict(c=dir_from(42, 42), R=math.radians(4.2), col=np.array([.78, .13, .09], np.float32))

def basis(c):
    up = np.array([0, 1, 0], np.float32)
    e1 = np.cross(up, c); e1 /= np.linalg.norm(e1); e2 = np.cross(c, e1)
    return e1, e2

def moon_layer(D, m, craters, seed):
    """Wave L (hc/exterior-l): textured discs, not clip art. Multi-scale albedo (broad soft maria, mid-scale mottling, fine grain), craters as
    soft shaded dimples WITHOUT bright rim rings, a soft terminator from a fixed light direction with earthshine on the dark side, limb
    darkening, the existing glow, and a thin atmospheric halo just outside the limb."""
    c = m["c"]; R = m["R"]
    cosang = np.clip(D @ c, -1, 1); ang = np.arccos(cosang)
    e1, e2 = basis(c)
    glow = np.zeros(D.shape, np.float32)
    col = m["col"]
    if m is PALE:
        glow += (np.array([.30, .38, .58]) * np.exp(-ang / .10)[..., None] * .55)
        glow += (np.array([.06, .09, .17]) * np.exp(-ang / .55)[..., None] * .9)
        halo_a = .07
    else:
        glow += (np.array([.55, .05, .04]) * np.exp(-ang / .045)[..., None] * .5)
        glow += (np.array([.10, .015, .02]) * np.exp(-ang / .25)[..., None] * .8)
        halo_a = .05
    # thin atmospheric halo: a faint soft ring hugging the limb (no hard edge), tinted like the moon
    halo = np.exp(-(((ang - R * 1.07) / (R * .075)) ** 2)) * halo_a + np.exp(-(((ang - R * 1.22) / (R * .16)) ** 2)) * halo_a * .35
    glow += halo[..., None] * (col * np.array([.8, .9, 1.0], np.float32))[None, None, :]
    px = (D @ e1) / math.sin(R); py = (D @ e2) / math.sin(R)
    r2 = px * px + py * py
    alpha = np.clip((R - ang) / (math.pi / 2 / N * 1.2) + .5, 0, 1)
    inside = r2 < 1.02
    pz = np.sqrt(np.clip(1 - r2, 0, 1))
    pn = np.stack([px, py, pz], -1)
    surf = np.zeros(D.shape[:2], np.float32) + .75
    idx = np.where(inside & (alpha > 0))
    if len(idx[0]):
        P = pn[idx]
        broad = fbm(P * 1.5 + 5.0, 4, seed)                                       # continents and maria, soft edged
        mare = smoothstep(.44, .66, broad)
        mid = fbm(P * 5.5 + 2.0, 5, seed + 3)                                     # mottled highlands
        fine = fbm(P * 26.0, 3, seed + 9)                                         # grain
        alb = 1.02 - .27 * mare + .16 * (mid - .5) * (1 - .5 * mare) + .09 * (fine - .5)
        for (cn, rc, depth) in craters:                                           # soft shaded dimples, bright ejecta haze, no rim ring
            d = np.linalg.norm(P - cn[None, :], axis=-1) / rc
            alb += -.055 * depth * np.exp(-(d / .8) ** 2) + .035 * np.exp(-(((d - 1.5) / .7) ** 2)) * (1 - mare)
        alb = np.clip(alb, .22, 1.1)
        # light from the upper left and slightly behind the viewer: soft terminator toward the lower right, earthshine on the dark limb
        L = np.array([-.58, .48, .66], np.float32); L /= np.linalg.norm(L)
        ndl = P @ L
        lit = smoothstep(-.60, .50, ndl)                                          # wide soft terminator
        earth = .24
        limb = np.clip(pz[idx], 0, 1) ** .16
        surf[idx] = alb * (earth + (1 - earth) * lit) * limb
    disk = surf[..., None] * col[None, None, :] * alpha[..., None]
    if m is RED:
        disk = disk * np.array([1.0, .95, .9], np.float32)
    return glow, disk, alpha

def make_craters(count, seed):
    r = np.random.default_rng(seed)
    out = []
    for _ in range(count):
        v = r.normal(size=3); v[2] = abs(v[2]) + .2; v /= np.linalg.norm(v)
        out.append((v.astype(np.float32), float(r.uniform(.03, .13)), float(r.uniform(.5, 1))))
    return out

def sky_base(D):
    e = D[..., 1]
    zen = np.array([.004, .008, .026], np.float32); mid = np.array([.022, .038, .09], np.float32); hor = np.array([.075, .10, .17], np.float32)
    below = np.array([.012, .016, .030], np.float32)
    t1 = smoothstep(-.02, .12, e)[..., None]; t2 = smoothstep(.0, .7, e)[..., None]
    col = below * (1 - t1) + (hor * (1 - t2) + zen * t2) * t1
    col = col + (mid * (smoothstep(.05, .55, e) * (1 - smoothstep(.55, 1.0, e)))[..., None]) * .8
    haze = np.exp(-(np.clip(e, -.2, 1) / .10) ** 2)[..., None] * np.array([.035, .045, .075], np.float32)
    return col + haze

def clouds(D, glow_hint):
    p = D * np.array([2.2, 5.5, 2.2], np.float32) + 11.0
    n = fbm(p, 5, 31)
    e = D[..., 1]
    mask = smoothstep(.46, .72, n) * np.clip(e + .16, 0, 1) ** .6 * (1 - smoothstep(.75, 1.0, e) * .6)
    return mask

def aurora(D):
    az = np.arctan2(D[..., 0], D[..., 2]); el = np.arcsin(np.clip(D[..., 1], -1, 1))
    center = math.radians(31) + math.radians(9) * np.sin(az * 2.3 + .7) + math.radians(2.5) * np.sin(az * 7.0)
    width = math.radians(8.0) + math.radians(2.5) * np.sin(az * 3 + 2)
    dz = (el - center) / width
    ribbon = np.exp(-np.where(dz < 0, dz * dz * 2.2, dz * dz * .55))
    window = smoothstep(math.radians(-18), math.radians(10), az) * (1 - smoothstep(math.radians(62), math.radians(100), az))
    rays = fbm(np.stack([az * 18.0, el * 3.0, np.zeros_like(az)], -1), 4, 55)
    rays = .45 + .55 * smoothstep(.2, .75, rays)
    amp = ribbon * window * rays * (.5 + .5 * np.sin(az * 5 + 1.3))
    col = np.array([.10, .95, .52], np.float32)[None, None, :] * (1 - np.clip(dz, 0, 1)[..., None] * .5) \
        + np.array([.25, .35, .9], np.float32)[None, None, :] * np.clip(dz, 0, 2)[..., None] * .22
    return col * (amp * .85)[..., None]

def make_stars(count):
    r = np.random.default_rng(404)                                    # same seed and draw order as before: star positions are unchanged
    v = r.normal(size=(count, 3)); v /= np.linalg.norm(v, axis=1, keepdims=True)
    mag = r.random(count) ** 3.2
    flare = (mag > .93)                                               # about 20 stars: the brightest, with a faint cross glint
    rad = np.radians(.03 + .05 * (1 - mag) ** 2.0 * (r.random(count) * .5 + .5))
    rad = np.where(flare, np.radians(.12), rad)                      # (one r.random(count) consumed above, as before: tints unchanged)
    tint = r.choice(3, count, p=[.7, .2, .1])
    cols = np.array([[1, 1, 1], [.75, .85, 1.0], [1.0, .85, .65]], np.float32)[tint]
    bright = np.where(flare, 1.0, .55 + .45 * (1 - mag))              # point stars are a little brighter than the old blobs: they cover far fewer pixels
    return v.astype(np.float32), rad.astype(np.float32), cols, bright.astype(np.float32), flare

def paint_stars(img, face, stars):
    v, rad, cols, bright, flare = stars
    f, r, u = [np.array(x, np.float32) for x in FACES[face]]
    for i in range(len(v)):
        d = v[i]; fd = d @ f
        if fd < .35 or d[1] < .03: continue
        if np.arccos(np.clip(d @ PALE["c"], -1, 1)) < PALE["R"] * 1.7: continue
        if np.arccos(np.clip(d @ RED["c"], -1, 1)) < RED["R"] * 2.4: continue
        x = (d @ r) / fd; y = (d @ u) / fd
        cx = (x + 1) / 2 * N; cy = (1 - y) / 2 * N
        rp_true = rad[i] * N / 2 * (1 + x * x + y * y)                 # radius in texels on this face
        rp = max(rp_true, .55)                                         # never thinner than half a texel; dim by the lost area instead
        dim = float(np.clip((rp_true / rp) ** 2, .6, 1.0))
        reach = (9 if flare[i] else 3) + int(rp)
        x0, x1 = max(int(cx) - reach, 0), min(int(cx) + reach + 1, N)
        y0, y1 = max(int(cy) - reach, 0), min(int(cy) + reach + 1, N)
        if x0 >= x1 or y0 >= y1: continue
        gx = np.arange(x0, x1, dtype=np.float32)[None, :] + .5 - cx
        gy = np.arange(y0, y1, dtype=np.float32)[:, None] + .5 - cy
        dist = np.sqrt(gx * gx + gy * gy)
        val = np.clip((rp - dist) / 1.0 + .5, 0, 1)                    # hard-ish disc, soft 1 px edge, no halo
        if flare[i]:
            arm = 8.0 + 4 * rp
            cross = np.clip(1 - np.abs(gx) / arm, 0, 1) ** 2 * np.clip(1 - np.abs(gy) / .9, 0, 1) \
                + np.clip(1 - np.abs(gy) / arm, 0, 1) ** 2 * np.clip(1 - np.abs(gx) / .9, 0, 1)
            val = val + cross * .22
        img[y0:y1, x0:x1, :] += (val * bright[i] * dim)[..., None] * cols[i][None, None, :]

def build_face(face, stars, craters):
    D = face_dirs(face)
    img = sky_base(D)
    mask = clouds(D, None)
    pg, pd, pa = moon_layer(D, PALE, craters[0], 1)
    rg, rd, ra = moon_layer(D, RED, craters[1], 2)
    lit = (pg * .9 + rg * .8)
    cloud_col = np.array([.018, .028, .055], np.float32)[None, None, :] + lit * 1.4
    img = img * (1 - mask[..., None] * .55) + cloud_col * mask[..., None] * .75
    img = img + aurora(D) * (1 - mask[..., None] * .5)
    paint_stars(img, face, stars)
    img = img + pg + rg
    img = img * (1 - pa[..., None]) + pd
    img = img * (1 - ra[..., None]) + rd
    img = img + (rng.random(img.shape, dtype=np.float32) - .5) / 255.0 * 1.6   # dither dark gradients
    return np.clip(img, 0, 1)

def equirect_preview(faces, W=2048):
    H = W // 2
    az = (np.arange(W, dtype=np.float32) + .5) / W * 2 * np.pi - np.pi; el = (.5 - (np.arange(H, dtype=np.float32) + .5) / H) * np.pi
    A, E = np.meshgrid(az, el)
    D = np.stack([np.sin(A) * np.cos(E), np.sin(E), np.cos(A) * np.cos(E)], -1)
    out = np.zeros((H, W, 3), np.float32)
    ad = np.abs(D); major = np.argmax(ad, -1)
    for face, (f, r, u) in FACES.items():
        f, r, u = [np.array(x, np.float32) for x in (f, r, u)]
        fd = D @ f
        sel = (fd > 0) & (fd >= ad.max(-1) - 1e-6)
        x = ((D @ r) / np.where(fd == 0, 1, fd)); y = ((D @ u) / np.where(fd == 0, 1, fd))
        px = np.clip(((x + 1) / 2 * N).astype(int), 0, N - 1); py = np.clip(((1 - y) / 2 * N).astype(int), 0, N - 1)
        out[sel] = faces[face][py[sel], px[sel]]
    return out

def main():
    cc.SKY.mkdir(parents=True, exist_ok=True)
    stars = make_stars(900)
    craters = (make_craters(70, 5), make_craters(40, 6))
    faces = {}
    guids = {}
    for face in FACES:
        img = build_face(face, stars, craters)
        faces[face] = img
        p = cc.SKY / ("Sky_%s.png" % face)
        Image.fromarray((img * 255 + .5).astype(np.uint8), "RGB").save(p, optimize=True)
        guids[face] = cc.write_texture_meta(p, "sky")
        print("sky face", face, p.stat().st_size // 1024, "KB", flush=True)
    prev = equirect_preview(faces)
    pdir = cc.ROOT / "docs/haunted-castle/art"; pdir.mkdir(parents=True, exist_ok=True)
    Image.fromarray((np.clip(prev, 0, 1) * 255 + .5).astype(np.uint8), "RGB").resize((2048, 1024)).save(pdir / "sky_preview.jpg", quality=88)
    envs = "".join("    - %s:\n        m_Texture: {fileID: 2800000, guid: %s, type: 3}\n        m_Scale: {x: 1, y: 1}\n        m_Offset: {x: 0, y: 0}\n" % (slot, guids[face])
                   for slot, face in (("_BackTex", "Back"), ("_DownTex", "Down"), ("_FrontTex", "Front"), ("_LeftTex", "Left"), ("_RightTex", "Right"), ("_UpTex", "Up")))
    mat = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: SkyCastle
  m_Shader: {{fileID: 103, guid: 0000000000000000f000000000000000, type: 0}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords: []
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap: {{}}
  disabledShaderPasses: []
  m_LockedProperties: 
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
{envs}    m_Ints: []
    m_Floats:
    - _Exposure: 1
    - _Rotation: 0
    m_Colors:
    - _Tint: {{r: 0.5, g: 0.5, b: 0.5, a: 0.5}}
  m_BuildTextureStacks: []
  m_AllowLocking: 1
"""
    p = cc.CASTLE / "SkyCastle.mat"
    p.write_text(mat)
    Path(str(p) + ".meta").write_text("fileFormatVersion: 2\nguid: %s\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % cc.guid(cc.rel_to_project(p)))
    print("CASTLE_SKY_PASS")

if __name__ == "__main__":
    main()
