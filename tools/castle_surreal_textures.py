#!/usr/bin/env python3
"""Bake the castle's surreal sprite textures to PNG under Assets/HollowCurrent/Resources/Castle/Textures.

Why: CastleWorld runs in the editor and its output is saved into the prepared scene. Procedural Texture2D objects created
with `new Texture2D` (and made non-readable) did not survive that serialization: the player showed them as blank white, so every
flame, halo, moon and sarcophagus rune was an opaque white card. PNG assets are saved by reference and always arrive intact.
SurrealProps loads these through Resources and only falls back to the procedural versions when a file is missing.

Idempotent. Run: python3 tools/castle_surreal_textures.py
Outputs: Surreal_Flame.png (64x128), Surreal_FlameGlow.png (64x64), Surreal_Runes.png (256x192, 4x3 cells, cell 8 solid),
Surreal_MoonPale.png, Surreal_MoonRed.png (128x128). Also writes the .meta files (uncompressed, alpha is transparency, clamped).
"""
import hashlib, math, pathlib
import numpy as np
from PIL import Image

OUT = pathlib.Path(__file__).resolve().parent.parent / "Assets/HollowCurrent/Resources/Castle/Textures"

META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 100
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 100
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def grid(w, h):
    """u, v in 0..1 with v = 0 at the BOTTOM row (Unity texture space); returns arrays of shape (h, w) with row 0 = bottom."""
    u = (np.arange(w) + .5) / w
    v = (np.arange(h) + .5) / h
    return np.meshgrid(u, v)


def smooth(e0, e1, x):
    k = np.clip((x - e0) / (e1 - e0), 0, 1)
    return k * k * (3 - 2 * k)


def save(name, rgba):
    """rgba: float array (h, w, 4), row 0 = bottom (Unity). PNG rows run top to bottom, so flip."""
    img = (np.clip(rgba, 0, 1) * 255 + .5).astype(np.uint8)[::-1]
    OUT.mkdir(parents=True, exist_ok=True)
    path = OUT / (name + ".png")
    Image.fromarray(img, "RGBA").save(path)
    meta = OUT / (name + ".png.meta")
    guid = hashlib.md5(("hc-surreal-" + name).encode()).hexdigest()   # stable, so re-runs never churn the meta
    if not meta.exists():
        meta.write_text(META.format(guid=guid))
    print("wrote", path, img.shape[1], "x", img.shape[0])


def flame():
    u, v = grid(64, 128)
    width = .5 * np.power(np.abs(np.sin(np.pi * np.power(v, .6))), .8) * (1 - .25 * v)
    d = np.abs(u - .5)
    edge = np.where(width < .002, 0, np.clip((width - d) / .09, 0, 1))
    core = np.clip(1 - d / np.maximum(width, .001), 0, 1) * (1 - v * .6)
    t = np.power(core, 1.5)[..., None]
    rgb = np.array([1, .42, .08]) * (1 - t) + np.array([1, .95, .7]) * t
    a = edge * (.55 + .45 * core)
    save("Surreal_Flame", np.dstack([rgb, a]))


def flame_halo():
    """Round 9: core and halo in ONE sprite (128 x 128). The quad spans x -0.9..0.9 and y -0.4..1.4 flame heights (y = 0 the wick), so
    the teardrop fills the middle and the soft halo is centred at y = 0.45. The core runs orange to amber (B < 150 at full alpha)."""
    u, v = grid(128, 128)
    xf = (u - .5) * 1.8
    yf = v * 1.8 - .4
    yc = np.clip(yf, 0, 1)
    halfw = np.where((yf > 0) & (yf < 1), .25 * np.power(np.abs(np.sin(np.pi * np.power(yc, .6))), .8) * (1 - .25 * yc), 0)
    d = np.abs(xf)
    edge = np.where(halfw < .001, 0, np.clip((halfw - d) / .045, 0, 1))
    core = np.clip(1 - d / np.maximum(halfw, .001), 0, 1) * (1 - yc * .6) * (halfw > .001)
    t = np.power(core, 1.5)[..., None]
    core_rgb = np.array([1, .40, .06]) * (1 - t) + np.array([1, .82, .42]) * t
    core_a = edge * (.55 + .45 * core)
    r = np.sqrt(xf ** 2 + (yf - .45) ** 2) / .9
    halo_a = np.power(np.clip(1 - r, 0, 1), 2.2) * .5
    halo_rgb = np.ones(r.shape + (3,)) * np.array([1, .55, .2])
    pre = core_rgb * core_a[..., None] + halo_rgb * halo_a[..., None]
    a = np.clip(core_a + halo_a, 0, 1)
    rgb = np.where(a[..., None] > 1e-4, pre / np.maximum(a[..., None], 1e-4), 0)
    save("Surreal_FlameHalo", np.dstack([np.clip(rgb, 0, 1), a]))


def glow():
    u, v = grid(64, 64)
    r = np.sqrt((u - .5) ** 2 + (v - .5) ** 2) * 2
    a = np.power(np.clip(1 - r, 0, 1), 2.2) * .55
    rgb = np.ones(r.shape + (3,)) * np.array([1, .55, .2])
    save("Surreal_FlameGlow", np.dstack([rgb, a]))


def seg_dist(x, y, s):
    ax, ay, bx, by = s
    abx, aby = bx - ax, by - ay
    t = np.clip(((x - ax) * abx + (y - ay) * aby) / max(1e-6, abx * abx + aby * aby), 0, 1)
    return np.hypot(x - (ax + abx * t), y - (ay + aby * t))


def runes():
    import random
    cols, rows, cell = 4, 3, 64
    img = np.zeros((rows * cell, cols * cell, 4))
    cu, cv = grid(cell, cell)
    for g in range(7):
        rnd = random.Random(g * 7919 + 13)
        strokes = [(.5, .1, .5, .9)]
        if g % 3 == 2:
            strokes.append((.28, .1, .28, .7))
        for _ in range(2 + rnd.randrange(2)):
            y = .25 + .25 * rnd.randrange(3)
            side = -1 if rnd.randrange(2) == 0 else 1
            strokes.append((.5, y, .5 + side * (.18 + .22 * rnd.random()), min(.92, max(.08, y + (rnd.randrange(3) - 1) * .22))))
        best = np.full(cu.shape, 9.0)
        for s in strokes:
            best = np.minimum(best, seg_dist(cu, cv, s) * cell)
        a = np.clip(2.4 - best, 0, 1)
        r, c = g // cols, g % cols     # Unity row (from the bottom) and column
        block = np.dstack([np.ones(cu.shape), np.ones(cu.shape), np.ones(cu.shape), a])
        img[r * cell:(r + 1) * cell, c * cell:(c + 1) * cell] = block
    img[2 * cell:3 * cell, 0:cell] = 1.0   # solid cell: row 2, column 0 (cell index 8)
    save("Surreal_Runes", img)


def noise(w, h, freq, seed):
    rng = np.random.RandomState(seed)
    gx, gy = int(freq) + 2, int(freq) + 2
    lat = rng.rand(gy, gx)
    u, v = grid(w, h)
    x, y = u * freq, v * freq
    x0, y0 = x.astype(int), y.astype(int)
    fx, fy = x - x0, y - y0
    fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    a, b = lat[y0, x0], lat[y0, x0 + 1]
    c, d = lat[y0 + 1, x0], lat[y0 + 1, x0 + 1]
    return a * (1 - fx) * (1 - fy) + b * fx * (1 - fy) + c * (1 - fx) * fy + d * fx * fy


def moon(name, body, halo, seed):
    n = 128
    u, v = grid(n, n)
    r = np.sqrt((u - .5) ** 2 + (v - .5) ** 2) * 2
    crater = noise(n, n, 7, seed) * .35 + noise(n, n, 19, seed + 1) * .15
    body, halo = np.array(body), np.array(halo)
    t = np.clip(1 - crater * 1.4, 0, 1)[..., None]
    disc = body * .78 * (1 - t) + body * t
    inside = smooth(.78, .74, r)[..., None]
    glow_a = smooth(1, .76, r) * .5
    rgb = halo * (1 - inside) + disc * inside
    a = np.maximum(inside[..., 0], glow_a * .55)
    save(name, np.dstack([rgb, a]))


if __name__ == "__main__":
    flame(); flame_halo(); glow(); runes()
    moon("Surreal_MoonPale", (.9, .94, 1), (.45, .6, .95), 3)
    moon("Surreal_MoonRed", (.95, .3, .3), (.7, .18, .3), 11)
