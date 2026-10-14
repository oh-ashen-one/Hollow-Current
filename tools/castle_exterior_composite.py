#!/usr/bin/env python3
"""System-python half of the exterior proofs: composites the Blender Workbench layers (tools/castle_exterior_proof.py) over the
real SkyCastle equirect by camera direction and writes docs/haunted-castle/rounds/r12/art/exterior_proof_<view>.png.

    python3 tools/castle_exterior_composite.py [DIR]      (default docs/haunted-castle/rounds/r12/art)
"""
import json, math, sys
from pathlib import Path
import numpy as np, cv2
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
out = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "docs/haunted-castle/rounds/r12/art"
sky = np.asarray(Image.open(ROOT / "docs/haunted-castle/art/sky_preview.jpg").convert("RGB")).astype(np.float32) / 255.0
def load(p): return np.asarray(Image.open(p).convert("RGBA")).astype(np.float32) / 255.0
for v in json.loads((out / "_raw/views.json").read_text()):
    W, H = v["size"]; f = np.array(v["dir"]); fov = v["fov"]
    r = np.cross([0, 1, 0], f); r /= np.linalg.norm(r); u = np.cross(f, r)
    ys, xs = np.mgrid[0:H, 0:W].astype(np.float32)
    th = math.tan(math.radians(fov) / 2); asp = W / H
    nx = ((xs + .5) / W * 2 - 1) * asp * th; ny = (1 - (ys + .5) / H * 2) * th
    d = f[None, None, :] + nx[..., None] * r[None, None, :] + ny[..., None] * u[None, None, :]
    d /= np.linalg.norm(d, axis=-1, keepdims=True)
    az = np.arctan2(d[..., 0], d[..., 2]); el = np.arcsin(np.clip(d[..., 1], -1, 1))
    su = ((az + math.pi) / (2 * math.pi) * (sky.shape[1] - 1)).astype(np.float32)
    sv = ((math.pi / 2 - el) / math.pi * (sky.shape[0] - 1)).astype(np.float32)
    img = cv2.remap(sky, su, sv, cv2.INTER_LINEAR, borderMode=cv2.BORDER_WRAP)
    for tag in ("back", "scene"):
        L = load(out / "_raw" / ("%s_%s.png" % (v["name"], tag))); a = L[..., 3:4]
        img = L[..., :3] * a + img * (1 - a)
    im = Image.fromarray((np.clip(img, 0, 1) * 255 + .5).astype(np.uint8))
    if W <= 1920: im = im.resize((1280, int(round(1280 * H / W))), Image.LANCZOS)    # the S4 frame stays at capture size (2416x1292)
    im.save(out / ("exterior_proof_%s.png" % v["name"]), optimize=True)
    print("wrote", "exterior_proof_%s.png" % v["name"])
