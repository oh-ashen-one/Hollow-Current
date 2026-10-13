"""Shared constants and Unity-metadata writers for the Haunted Castle art scripts.

Pure Python (no bpy, no numpy) so it imports from system python3 and from Blender.
Unity guids are derived from the project-relative path, so re-running a script never
changes a guid and existing .mat -> texture references stay valid.
"""
import hashlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CASTLE = ROOT / "Assets/HollowCurrent/Resources/Castle"
TEX = CASTLE / "Textures"
MATS = CASTLE / "Materials"
SKY = CASTLE / "Sky"

# name -> (base colour rgb linear-ish 0..1, smoothness, metallic, textured?)
# Mirrors Unity Standard-shader values and the Blender Principled values.
MATERIALS = {
    # Round 2 (hc/look): albedo lifted so mid-tones exist under moonlight (stone reads grey-blue, not black).
    "Castle_Stone":      ((0.50, 0.52, 0.57), 0.20, 0.0, True),   # weathered grey-blue ashlar
    "Castle_BlackStone": ((0.20, 0.20, 0.25), 0.62, 0.0, True),   # altar / crypt: polished, veined
    # Round 5 stone variants (world-space or UV; 2 m tile)
    "Castle_StoneWeathered": ((0.46, 0.50, 0.58), 0.22, 0.0, True),  # grey-blue, heavy chipped edges, bigger blocks
    "Castle_StoneDark":  ((0.17, 0.18, 0.22), 0.20, 0.0, True),   # dark rough-dressed stone: undercroft, crypt, lower walls
    "Castle_StoneMossy": ((0.42, 0.45, 0.48), 0.28, 0.0, True),   # stone base, moss only in the joints/crevices (mask-driven)
    "Castle_OakPlank":   ((0.40, 0.28, 0.17), 0.30, 0.0, True),   # wide weathered boards with nails: drawbridge leaf, scaffolds
    # Count Claude (flayed demon) gore set, round 6
    "Castle_GoreSkin":   ((0.60, 0.56, 0.55), 0.40, 0.0, True),   # pallid grey-white skin, faint veins
    "Castle_GoreFlesh":  ((0.42, 0.06, 0.06), 0.88, 0.0, True),   # exposed striated muscle, wet
    "Castle_GoreMembrane": ((0.62, 0.50, 0.50), 0.30, 0.0, True), # translucent wing membrane with veins
    "Castle_FlagStone":  ((0.30, 0.32, 0.38), 0.58, 0.0, True),   # wet floor slabs (rune-reflecting)
    "Castle_Oak":        ((0.30, 0.19, 0.11), 0.28, 0.0, True),   # dark oak
    "Castle_Brass":      ((0.62, 0.48, 0.22), 0.50, 0.85, True),  # scratched, tarnished brass
    "Castle_Oxblood":    ((0.42, 0.07, 0.09), 0.10, 0.0, True),   # woven cloth
    "Castle_Silver":     ((0.70, 0.71, 0.75), 0.45, 0.90, True),
    "Castle_Iron":       ((0.22, 0.22, 0.25), 0.35, 0.85, True),
    "Castle_Bone":       ((0.72, 0.67, 0.56), 0.30, 0.0, True),
    "Castle_Paper":      ((0.70, 0.62, 0.45), 0.08, 0.0, True),
    "Castle_Canvas":     ((1.00, 1.00, 1.00), 0.08, 0.0, False),  # portrait painting (Portrait.png) + canvas weave normal
    "Castle_Blood":      ((0.22, 0.012, 0.02), 0.95, 0.0, True),  # liquid: moat + crypt channel (scrolling normal in CastleArt/shader)
    "Castle_SunWindow":  ((1.00, 1.00, 1.00), 0.55, 0.0, False),  # painted-over sun window above the chapel altar
    "Castle_Flame":      ((1.00, 0.62, 0.22), 0.00, 0.0, False),  # emissive
}

def guid(rel):
    return hashlib.md5(("hollow-current-castle/" + str(rel)).encode()).hexdigest()

def rel_to_project(path):
    return Path(path).resolve().relative_to(ROOT).as_posix()

_TEX_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: {mips}
    sRGBTexture: {srgb}
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
    filterMode: {filter}
    aniso: {aniso}
    mipBias: 0
    wrapU: {wrap}
    wrapV: {wrap}
    wrapW: {wrap}
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: {atr}
  spriteTessellationDetail: -1
  textureType: {ttype}
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
    textureCompression: 1
    compressionQuality: 50
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

def write_texture_meta(png, kind="color", filter_mode=1, aniso=None):
    """kind: color (sRGB, repeat), normal (linear normal map, repeat), sky (sRGB, clamp, no mips)."""
    png = Path(png)
    g = guid(rel_to_project(png))
    cfg = dict(color=dict(srgb=1, ttype=0, wrap=0, mips=1, aniso=4),
               normal=dict(srgb=0, ttype=1, wrap=0, mips=1, aniso=4),
               mask=dict(srgb=0, ttype=0, wrap=0, mips=1, aniso=4),
               emission=dict(srgb=1, ttype=0, wrap=1, mips=1, aniso=4),
               decal=dict(srgb=1, ttype=0, wrap=1, mips=1, aniso=4, atr=1),
               sky=dict(srgb=1, ttype=0, wrap=1, mips=0, aniso=0))[kind]
    cfg.setdefault("atr", 0)
    cfg["filter"] = filter_mode
    if aniso is not None: cfg["aniso"] = aniso
    Path(str(png) + ".meta").write_text(_TEX_META.format(guid=g, **cfg))
    return g

def write_folder_meta(folder):
    folder = Path(folder)
    g = guid(rel_to_project(folder))
    Path(str(folder) + ".meta").write_text(
        "fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % g)

def _tex_env(name, tex_guid):
    if tex_guid:
        return "    - %s:\n        m_Texture: {fileID: 2800000, guid: %s, type: 3}\n        m_Scale: {x: 1, y: 1}\n        m_Offset: {x: 0, y: 0}\n" % (name, tex_guid)
    return "    - %s:\n        m_Texture: {fileID: 0}\n        m_Scale: {x: 1, y: 1}\n        m_Offset: {x: 0, y: 0}\n" % name

def write_standard_mat(path, name, color, smooth, metal, albedo_guid=None, normal_guid=None, emission=None,
                       mask_guid=None, emission_guid=None, bump_scale=1.0, occlusion=0.8):
    path = Path(path)
    keywords = []
    if normal_guid:
        keywords.append("_NORMALMAP")
    if mask_guid:
        keywords.append("_METALLICGLOSSMAP")
    if emission:
        keywords.append("_EMISSION")
    envs = ""
    for slot in ["_BumpMap", "_DetailAlbedoMap", "_DetailMask", "_DetailNormalMap", "_EmissionMap", "_MainTex",
                 "_MetallicGlossMap", "_OcclusionMap", "_ParallaxMap"]:
        g = {"_MainTex": albedo_guid, "_BumpMap": normal_guid, "_MetallicGlossMap": mask_guid,
             "_OcclusionMap": mask_guid, "_EmissionMap": emission_guid}.get(slot)
        envs += _tex_env(slot, g)
    em = emission or (0, 0, 0)
    text = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
  m_Shader: {{fileID: 46, guid: 0000000000000000f000000000000000, type: 0}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords: [{", ".join(keywords)}]
  m_InvalidKeywords: []
  m_LightmapFlags: {1 if emission else 4}
  m_EnableInstancingVariants: 1
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
    - _BumpScale: {bump_scale}
    - _Cutoff: 0.5
    - _DetailNormalMapScale: 1
    - _DstBlend: 0
    - _GlossMapScale: 1
    - _Glossiness: {smooth}
    - _GlossyReflections: 1
    - _Metallic: {metal}
    - _Mode: 0
    - _OcclusionStrength: {occlusion if mask_guid else 1}
    - _Parallax: 0.02
    - _SmoothnessTextureChannel: 0
    - _SpecularHighlights: 1
    - _SrcBlend: 1
    - _UVSec: 0
    - _ZWrite: 1
    m_Colors:
    - _Color: {{r: {1.0 if albedo_guid else color[0]}, g: {1.0 if albedo_guid else color[1]}, b: {1.0 if albedo_guid else color[2]}, a: 1}}
    - _EmissionColor: {{r: {em[0]}, g: {em[1]}, b: {em[2]}, a: 1}}
  m_BuildTextureStacks: []
  m_AllowLocking: 1
"""
    path.write_text(text)
    Path(str(path) + ".meta").write_text(
        "fileFormatVersion: 2\nguid: %s\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
        % guid(rel_to_project(path)))


# Nominal sizes reserved by piece 1 (hc/layout CastleLayout.ArtSizes), Unity axes (width X, height Y, depth Z).
# Origin = base centre, front = +Z at yaw 0 (the art contract in CastleLayout.cs).
NOMINAL = {
    "GateArch": (3.8, 5.6, 1.0), "Drawbridge": (3.2, .25, 6.0), "Winch": (1.0, 1.6, .8), "LanternCart": (1.8, 1.3, 1.0),
    "Buttress": (.9, 6.4, 1.2), "Battlement": (4.0, 1.0, .8), "DoorFrame": (1.9, 2.6, .8), "GreatDoorFrame": (3.6, 5.2, .9),
    "ServiceDoorFrame": (1.2, 2.3, .8), "DoorLeaf": (1.5, 2.0, .09), "GreatDoorLeaf": (1.5, 4.5, .12),
    "ServiceDoorLeaf": (.9, 2.0, .08), "PortraitDoorLeaf": (.9, 2.0, .1), "LancetWindow": (1.2, 1.6, .35),
    "TallLancetWindow": (1.4, 3.5, .35), "ClerestoryWindow": (1.2, 1.8, .35), "ObservatoryWindow": (1.6, 2.7, .35),
    "RoseWindow": (3.0, 3.0, .5), "ColumnGreat": (.9, 8.8, .9), "Column": (.7, 3.0, .7), "CryptRib": (8.4, .6, .6),
    "RafterBeam": (11.4, .5, .5), "Balustrade": (2.0, 1.1, .2), "Fountain": (2.6, 1.8, 2.6), "DeadTree": (2.4, 4.5, 2.4),
    "Candelabra": (.5, 1.7, .5), "Pew": (2.4, 1.0, .7), "Altar": (2.4, 1.1, 1.1), "ChapelBell": (1.2, 2.0, .9),
    "LongTable": (6.6, .95, 1.4), "DiningTable": (5.6, .9, 1.6), "HighBackChair": (.6, 1.3, .6), "Throne": (1.0, 1.9, .9),
    "Fireplace": (2.6, 2.6, .9), "Sideboard": (2.6, 1.1, .6), "KitchenHearth": (2.8, 2.4, 1.0), "ButcherBlock": (1.5, .9, 1.0),
    "Cauldron": (1.0, .9, 1.0), "Bookshelf": (2.0, 3.0, .5), "ReadingTable": (2.0, .85, 1.0), "Lectern": (.7, 1.2, .6),
    "Portrait": (.9, 1.3, .1), "StringCourse": (4.0, .24, .2), "Corbel": (.4, .58, .45), "GargoyleSpout": (.5, .62, 1.15), "TapestryPanel": (1.52, 2.7, .07), "CanopyBed": (2.4, 2.8, 2.6), "Orrery": (3.0, 2.8, 3.0), "Telescope": (1.0, 2.2, 2.4),
    "Sarcophagus": (2.2, 1.2, 1.0), "CryptBridge": (1.4, .3, 1.2), "ReliquaryBox": (1.0, 1.0, .7), "Pedestal": (.6, 1.0, .6),
}
