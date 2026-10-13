using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HollowCurrent
{
    // Castle look pass (round 2, hc/look). A plain MonoBehaviour with no dependency on Game/Investigation: add it once to any
    // GameObject in the castle scene (the Game object or the WorldLayout root). On Start it
    //   1. sets exponential-squared blue-grey fog and a cold-sky / dark-ground trilight ambient (never black),
    //   2. sets the global ambient-fill colours read by the CastleTriplanar / CastleLiquid / CastleLit shaders,
    //   3. swaps CastleWorld's flat "HC castle / x" cube materials for the textured world-space surfaces in CastleArt.Surface
    //      (blood becomes the scrolling-normal liquid) and the FBX props for the UV-mapped lit surfaces,
    //   4. builds the painted-over sun window above the chapel altar (emissive blue rim, no pure-black slab),
    //   5. stacks alpha-blended noise layers at 0-1.1 m over the great-hall and crypt floors (ground fog; built-in pipeline, no volumes),
    //   6. draws a subtle vignette in Camera.onPostRender. Round 3: the black lift is gone; blacks are real again.
    // Every step is wrapped: a missing shader/texture/material falls back to the old look and logs once, never throws.
    public sealed class CastleAtmosphere : MonoBehaviour
    {
        [Header("Fog (exponential squared)")]
        public bool EnableFog = true;
        // f = exp(-(d * density)^2): 12 m (S1 camera to far wall) keeps 70%, 25 m keeps 21%, 28 m 12% of the surface colour.
        // The fog colour is lighter than the surroundings so distant geometry visibly dissolves into mist.
        public Color FogColor = new Color(.30f, .37f, .52f);
        public float FogDensity = .05f;

        [Header("Ambient (trilight; gamma-space colours as in the Lighting window)")]
        public Color AmbientSky = new Color(.16f, .20f, .30f);
        public Color AmbientEquator = new Color(.095f, .12f, .185f);
        public Color AmbientGround = new Color(.04f, .04f, .06f);
        public float AmbientIntensity = 1f;

        [Header("Ambient fill for the castle surface shaders (linear). Kept low so shadows can reach real black.")]
        public Color FillSky = new Color(.016f, .022f, .036f);
        public Color FillGround = new Color(.004f, .004f, .008f);

        [Header("Materials")]
        public bool UpgradeMaterials = true;
        public bool UpgradeProps = true;

        [Header("Vignette (camera post-render)")]
        public bool Vignette = true;
        [Range(0f, 1f)] public float VignetteStrength = .22f;

        [Header("Ground fog (layered noise planes, castle-local)")]
        public bool GroundFog = true;
        public Color GroundFogColor = new Color(.34f, .42f, .58f);
        public int FogLayers = 6;
        public float FogLayerAlpha = .16f;
        public Vector3[] FogAreaCentres = { new Vector3(0f, 0f, 21f), new Vector3(0f, -3.2f, 21f) };   // floor centre of the great hall, crypt
        public Vector2[] FogAreaSizes = { new Vector2(12f, 14f), new Vector2(12f, 14f) };

        [Header("Painted-over sun window above the chapel altar")]
        public bool BuildSunWindow = true;
        public Transform Root;                                            // castle root; defaults to the WorldLayout transform, else this
        public Vector3 SunWindowBase = new Vector3(13.55f, 1.85f, 6.5f);  // sill centre on the chapel east wall face (castle local)
        public float SunWindowYaw = 90f;                                  // +90: pane faces -x (into the chapel)

        static readonly Dictionary<string, (string material, Color tint, bool liquid)> World = new Dictionary<string, (string, Color, bool)>
        {
            { "stone", ("Castle_Stone", new Color(.78f, .78f, .82f), false) },
            { "wall", ("Castle_Stone", new Color(.64f, .64f, .70f), false) },
            { "darkstone", ("Castle_FlagStone", new Color(.85f, .85f, .85f), false) },
            { "cobble", ("Castle_FlagStone", new Color(.70f, .70f, .70f), false) },
            { "wood", ("Castle_Oak", Color.white, false) },
            { "darkwood", ("Castle_Oak", new Color(.55f, .55f, .55f), false) },
            { "carpet", ("Castle_Oxblood", new Color(.9f, .9f, .9f), false) },
            { "cloth", ("Castle_Oxblood", Color.white, false) },
            { "brass", ("Castle_Brass", Color.white, false) },
            { "paper", ("Castle_Paper", Color.white, false) },
            { "metal", ("Castle_Iron", Color.white, false) },
            { "black", ("Castle_BlackStone", Color.white, false) },
            { "earth", ("Castle_FlagStone", new Color(.40f, .44f, .40f), false) },
            { "blood", ("Castle_Blood", Color.white, true) },
        };
        static readonly HashSet<string> PropMaterials = new HashSet<string>
        {
            "Castle_Stone", "Castle_BlackStone", "Castle_FlagStone", "Castle_Oak", "Castle_Brass", "Castle_Oxblood",
            "Castle_Silver", "Castle_Iron", "Castle_Bone", "Castle_Paper", "Castle_Blood",
        };

        const string WorldPrefix = "HC castle / ";
        Material glBlend;
        bool warnedGl;
        readonly HashSet<Renderer> done = new HashSet<Renderer>();

        void OnEnable() { Camera.onPostRender += PostRender; }
        void OnDisable() { Camera.onPostRender -= PostRender; }

        void Awake() { ApplyEnvironment(); }

        void Start()
        {
            if (!Root)
            {
                var layout = FindFirstObjectByType<WorldLayout>();
                Root = layout ? layout.transform : transform;
            }
            ApplyEnvironment();
            if (BuildSunWindow) Safe("sun window", MakeSunWindow);
            if (GroundFog) Safe("ground fog", MakeGroundFog);
            StartCoroutine(Passes());
        }

        // Other components (Game.Awake, CastleRun.Start spawning floaters) run around us: apply again after they have finished.
        IEnumerator Passes()
        {
            yield return null;
            ApplyEnvironment(); Safe("materials", Upgrade);
            yield return new WaitForSeconds(1.5f);
            ApplyEnvironment(); Safe("materials", Upgrade);
            yield return new WaitForSeconds(6f);
            Safe("materials", Upgrade);
        }

        static void Safe(string what, Action a)
        {
            try { a(); }
            catch (Exception e) { Debug.LogWarning("[CastleAtmosphere] " + what + " skipped: " + e.Message); }
        }

        // ---- environment ------------------------------------------------------------------------------------------
        public void ApplyEnvironment()
        {
            RenderSettings.fog = EnableFog;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogDensity = FogDensity;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = AmbientSky;
            RenderSettings.ambientEquatorColor = AmbientEquator;
            RenderSettings.ambientGroundColor = AmbientGround;
            RenderSettings.ambientIntensity = AmbientIntensity;
            Shader.SetGlobalColor("_CastleFillSky", FillSky);
            Shader.SetGlobalColor("_CastleFillGround", FillGround);
        }

        // ---- materials ----------------------------------------------------------------------------------------------
        public void Upgrade()
        {
            if (!UpgradeMaterials) return;
            int world = 0, props = 0;
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!r || done.Contains(r) || r is ParticleSystemRenderer || r is SkinnedMeshRenderer || r is SpriteRenderer || r is TrailRenderer || r is LineRenderer) continue;
                var shared = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < shared.Length; i++)
                {
                    var m = shared[i]; if (!m) continue;
                    string n = m.name; int cut = n.IndexOf(" (Instance)", StringComparison.Ordinal); if (cut >= 0) n = n.Substring(0, cut);
                    Material replacement = null;
                    if (n.StartsWith(WorldPrefix, StringComparison.Ordinal) && World.TryGetValue(n.Substring(WorldPrefix.Length), out var spec))
                    {
                        string mat = spec.material;
                        // floors and ceilings are cube "Slab / x" objects: lay flagstone on them instead of the wall coursing
                        if (mat == "Castle_Stone" && r.name.StartsWith("Slab /", StringComparison.Ordinal)) mat = "Castle_FlagStone";
                        replacement = CastleArt.Surface(mat, spec.liquid ? CastleArt.SurfaceKind.Liquid : CastleArt.SurfaceKind.World, spec.tint);
                        if (replacement) world++;
                    }
                    else if (UpgradeProps && PropMaterials.Contains(n))
                    {
                        replacement = CastleArt.Surface(n, CastleArt.SurfaceKind.Prop, Color.white);
                        if (replacement) props++;
                    }
                    if (replacement && replacement != m) { shared[i] = replacement; changed = true; }
                }
                if (changed) r.sharedMaterials = shared;
                done.Add(r);
            }
            if (world + props > 0) Debug.Log("[CastleAtmosphere] upgraded " + world + " world and " + props + " prop material slots");
        }

        // ---- the painted-over sun window --------------------------------------------------------------------------------
        void MakeSunWindow()
        {
            var parent = Root ? Root : transform;
            var existing = parent.Find("Painted sun window");
            if (existing) return;
            var group = new GameObject("Painted sun window");
            group.transform.SetParent(parent, false);
            group.transform.localPosition = SunWindowBase;
            group.transform.localRotation = Quaternion.Euler(0f, SunWindowYaw, 0f);   // local -Z faces the chapel

            // stone surround / reredos: 1.36 x 2.1 m, 0.15 m proud of the wall; it also covers whatever sat on this wall before
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "Sun window surround";
            var col = slab.GetComponent<Collider>(); if (col) Destroy(col);
            slab.transform.SetParent(group.transform, false);
            slab.transform.localPosition = new Vector3(0f, .85f - .02f, -.075f);
            slab.transform.localScale = new Vector3(1.36f, 2.1f, .15f);
            var stone = CastleArt.Surface("Castle_Stone", CastleArt.SurfaceKind.World, new Color(.8f, .82f, .9f));
            if (stone) slab.GetComponent<Renderer>().sharedMaterial = stone;

            // pointed-arch pane 1.0 x 1.7 m
            var pane = new GameObject("Sun window pane");
            pane.transform.SetParent(group.transform, false);
            pane.transform.localPosition = new Vector3(0f, 0f, -.156f);
            pane.AddComponent<MeshFilter>().sharedMesh = ArchMesh(1f, 1.7f);
            var mr = pane.AddComponent<MeshRenderer>();
            var mat = Resources.Load<Material>("Castle/Materials/Castle_SunWindow");
            if (mat) mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;

            // cold light spilling round the frame
            var l = new GameObject("Sun window leak").AddComponent<Light>();
            l.transform.SetParent(group.transform, false);
            l.transform.localPosition = new Vector3(0f, 1.25f, -.55f);
            l.type = LightType.Point; l.color = new Color(.35f, .55f, 1f); l.range = 3.2f; l.intensity = .55f; l.shadows = LightShadows.None;
        }

        // Equilateral pointed arch (two arcs of radius = width), origin at the sill centre, facing -Z, UV = (x / w + .5, y / h).
        static Mesh ArchMesh(float w, float h)
        {
            float rise = w * .8660254f, spring = h - rise;
            var outline = new List<Vector2> { new Vector2(-w / 2, 0), new Vector2(w / 2, 0), new Vector2(w / 2, spring) };
            for (int i = 1; i <= 12; i++) { float a = Mathf.Lerp(0f, 60f, i / 12f) * Mathf.Deg2Rad; outline.Add(new Vector2(-w / 2 + w * Mathf.Cos(a), spring + w * Mathf.Sin(a))); }
            for (int i = 1; i <= 12; i++) { float a = Mathf.Lerp(120f, 180f, i / 12f) * Mathf.Deg2Rad; outline.Add(new Vector2(w / 2 + w * Mathf.Cos(a), spring + w * Mathf.Sin(a))); }
            outline.Add(new Vector2(-w / 2, spring));
            var centre = new Vector2(0f, spring * .6f);
            var verts = new List<Vector3> { new Vector3(centre.x, centre.y, 0) };
            var uvs = new List<Vector2> { new Vector2(centre.x / w + .5f, centre.y / h) };
            foreach (var p in outline) { verts.Add(new Vector3(p.x, p.y, 0)); uvs.Add(new Vector2(p.x / w + .5f, p.y / h)); }
            var tris = new List<int>();
            for (int i = 0; i < outline.Count; i++) { tris.Add(0); tris.Add(1 + (i + 1) % outline.Count); tris.Add(1 + i); }   // clockwise seen from -Z
            var mesh = new Mesh { name = "Sun window arch" };
            mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            var normals = new Vector3[verts.Count]; for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.back;
            mesh.normals = normals;
            mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return mesh;
        }

        // ---- ground fog -----------------------------------------------------------------------------------------------
        // Built-in pipeline: no volumetrics, so stack FogLayers alpha-blended noise planes between 0.08 and 1.1 m over each floor.
        // Seen from eye height they build a veil that thickens towards the floor; each layer scrolls at its own speed.
        void MakeGroundFog()
        {
            var parent = Root ? Root : transform;
            if (parent.Find("Ground fog")) return;
            var shader = Resources.Load<Shader>("Castle/Shaders/CastleFogLayer");
            var noise = Resources.Load<Texture2D>("Castle/Textures/Castle_FogNoise");
            if (shader == null || !shader.isSupported || noise == null) { Debug.LogWarning("[CastleAtmosphere] CastleFogLayer unavailable; ground fog skipped (exponential fog still active)"); return; }
            var group = new GameObject("Ground fog").transform;
            group.SetParent(parent, false);
            int layers = Mathf.Clamp(FogLayers, 1, 12);
            var quad = BuildQuad();
            for (int a = 0; a < FogAreaCentres.Length && a < FogAreaSizes.Length; a++)
            {
                for (int i = 0; i < layers; i++)
                {
                    float t = i / (float)Mathf.Max(1, layers - 1);
                    var m = new Material(shader) { name = "Castle ground fog " + a + "/" + i };
                    m.SetTexture("_MainTex", noise);
                    m.SetColor("_Color", GroundFogColor);
                    m.SetFloat("_Alpha", FogLayerAlpha * Mathf.Lerp(1.25f, .6f, t));            // denser near the floor
                    m.SetFloat("_Tile", .18f + .05f * i);
                    m.SetVector("_Scroll", new Vector4(.008f + .003f * i, (i % 2 == 0 ? .005f : -.006f) - .001f * i, 0f, 0f));
                    m.SetVector("_Fade", new Vector4(.9f, 2.8f, 0f, 0f));
                    var go = new GameObject("Fog layer " + a + "." + i);
                    go.transform.SetParent(group, false);
                    go.transform.localPosition = FogAreaCentres[a] + new Vector3(0f, Mathf.Lerp(.08f, 1.1f, t), 0f);
                    go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    go.transform.localScale = new Vector3(FogAreaSizes[a].x, FogAreaSizes[a].y, 1f);
                    go.AddComponent<MeshFilter>().sharedMesh = quad;
                    var r = go.AddComponent<MeshRenderer>();
                    r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
                    r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
                }
            }
        }

        // Unit quad in XY facing -Z (rotated 90 deg about X it lies flat facing up), uv 0..1 for the edge falloff.
        static Mesh BuildQuad()
        {
            var mesh = new Mesh { name = "Fog quad" };
            mesh.SetVertices(new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0) });
            mesh.SetUVs(0, new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // ---- vignette (built-in pipeline, no post stack) ----------------------------------------------------------------------
        void EnsureGl()
        {
            if (glBlend != null) return;
            var shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
            {
                if (!warnedGl) { warnedGl = true; Debug.LogWarning("[CastleAtmosphere] Hidden/Internal-Colored missing: vignette disabled"); }
                Vignette = false; return;
            }
            glBlend = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };      // alpha
            glBlend.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); glBlend.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            glBlend.SetInt("_Cull", (int)CullMode.Off); glBlend.SetInt("_ZWrite", 0); glBlend.SetInt("_ZTest", (int)CompareFunction.Always);
        }

        void PostRender(Camera cam)
        {
            // skip the depth-only first-person equipment overlay camera so the vignette is applied exactly once
            if (!cam || cam.cameraType != CameraType.Game || cam.clearFlags == CameraClearFlags.Depth || !Vignette || VignetteStrength <= 0f) return;
            try
            {
                EnsureGl();
                if (glBlend == null) return;
                GL.PushMatrix();
                GL.LoadOrtho();
                glBlend.SetPass(0);
                GL.Begin(GL.TRIANGLES);
                const int seg = 48, rings = 5;
                for (int k = 0; k < rings; k++)
                {
                    float r0 = Mathf.Lerp(.35f, 1.15f, k / (float)rings), r1 = Mathf.Lerp(.35f, 1.15f, (k + 1) / (float)rings);
                    float a0 = VignetteStrength * Mathf.Pow(k / (float)rings, 2.2f), a1 = VignetteStrength * Mathf.Pow((k + 1) / (float)rings, 2.2f);
                    for (int i = 0; i < seg; i++)
                    {
                        float t0 = i / (float)seg * Mathf.PI * 2f, t1 = (i + 1) / (float)seg * Mathf.PI * 2f;
                        Vector3 p00 = Ring(r0, t0), p01 = Ring(r0, t1), p10 = Ring(r1, t0), p11 = Ring(r1, t1);
                        Color c0 = new Color(.004f, .006f, .012f, a0), c1 = new Color(.004f, .006f, .012f, a1);
                        GL.Color(c0); GL.Vertex(p00); GL.Color(c1); GL.Vertex(p10); GL.Color(c1); GL.Vertex(p11);
                        GL.Color(c0); GL.Vertex(p00); GL.Color(c1); GL.Vertex(p11); GL.Color(c0); GL.Vertex(p01);
                    }
                }
                GL.End();
                GL.PopMatrix();
            }
            catch (Exception e)
            {
                if (!warnedGl) { warnedGl = true; Debug.LogWarning("[CastleAtmosphere] vignette disabled: " + e.Message); }
                Vignette = false;
            }
        }

        // Point on an ellipse in 0..1 screen space (radius 1 reaches the screen corners' circumscribed ellipse).
        static Vector3 Ring(float r, float t) => new Vector3(.5f + Mathf.Cos(t) * r * .5f * 1.4142f, .5f + Mathf.Sin(t) * r * .5f * 1.4142f, 0f);
    }
}
