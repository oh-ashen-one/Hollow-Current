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
    "Castle_Stone":      ((0.34, 0.35, 0.37), 0.14, 0.0, True),   # weathered grey stone
    "Castle_BlackStone": ((0.075, 0.075, 0.09), 0.55, 0.0, True), # altar / crypt
    "Castle_Oak":        ((0.17, 0.105, 0.06), 0.22, 0.0, True),  # dark oak
    "Castle_Brass":      ((0.40, 0.31, 0.14), 0.42, 0.80, True),  # tarnished brass
    "Castle_Oxblood":    ((0.30, 0.045, 0.055), 0.08, 0.0, True), # cloth
    "Castle_Silver":     ((0.56, 0.57, 0.60), 0.38, 0.85, True),  # dull silver
    "Castle_Iron":       ((0.13, 0.13, 0.15), 0.30, 0.85, True),
    "Castle_Bone":       ((0.62, 0.58, 0.48), 0.20, 0.0, True),
    "Castle_Paper":      ((0.62, 0.55, 0.40), 0.05, 0.0, False),
    "Castle_Canvas":     ((0.20, 0.17, 0.14), 0.05, 0.0, False),
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
    filterMode: 1
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
  alphaIsTransparency: 0
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

def write_texture_meta(png, kind="color"):
    """kind: color (sRGB, repeat), normal (linear normal map, repeat), sky (sRGB, clamp, no mips)."""
    png = Path(png)
    g = guid(rel_to_project(png))
    cfg = dict(color=dict(srgb=1, ttype=0, wrap=0, mips=1, aniso=4),
               normal=dict(srgb=0, ttype=1, wrap=0, mips=1, aniso=4),
               sky=dict(srgb=1, ttype=0, wrap=1, mips=0, aniso=0))[kind]
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

def write_standard_mat(path, name, color, smooth, metal, albedo_guid=None, normal_guid=None, emission=None):
    path = Path(path)
    keywords = []
    if normal_guid:
        keywords.append("_NORMALMAP")
    if emission:
        keywords.append("_EMISSION")
    envs = ""
    for slot in ["_BumpMap", "_DetailAlbedoMap", "_DetailMask", "_DetailNormalMap", "_EmissionMap", "_MainTex",
                 "_MetallicGlossMap", "_OcclusionMap", "_ParallaxMap"]:
        g = albedo_guid if slot == "_MainTex" else (normal_guid if slot == "_BumpMap" else None)
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
    - _BumpScale: 1
    - _Cutoff: 0.5
    - _DetailNormalMapScale: 1
    - _DstBlend: 0
    - _GlossMapScale: 1
    - _Glossiness: {smooth}
    - _GlossyReflections: 1
    - _Metallic: {metal}
    - _Mode: 0
    - _OcclusionStrength: 1
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
