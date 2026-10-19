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
        // Wave FB (owner: "too well-lit ... feels like exploring, not being hunted"): the castle is dark and lantern-led, like Bellwether
        // (fog (.025,.04,.055), ambient sky (.105,.13,.16)). f = exp(-(d * density)^2): at density .03, 12 m (S1 camera to far wall)
        // keeps 88 %, 25 m keeps 57 %. The fog colour is now near-black blue instead of the old lighter mist (.30,.37,.52 at .05), so
        // distant geometry dissolves into darkness instead of into haze. The exterior (camera outside the gate) keeps the old mist so
        // the bridge, moat and facade still read against the sky; the crypt keeps its own profile below.
        public Color FogColor = new Color(.04f, .05f, .08f);
        public float FogDensity = .03f;
        public bool ExteriorFogProfile = true;
        // Wave FD (r13 critic item 3: the exterior views were "a fog plane"): at .05 the castle front from the bridge (14-22 m) was 40-70 % mist and
        // the far bank vanished. .034 keeps 57 % at 22 m and the colour is a darker, less milky night blue, so the keep, the rock and the far shore
        // read as silhouettes against the sky like refs/exterior_sky.png. The gate passage counts as outside from z 1.6 (the gate_out_S6 eye at
        // z 1.4 looks out through the arch and used to get the near-black hall fog over the whole view).
        public Color ExteriorFogColor = new Color(.20f, .24f, .34f);
        public float ExteriorFogDensity = .034f;
        public float ExteriorBelowZ = 1.6f;   // castle-local z: the gate wall is at 0, the gate passage inner face at 0.45

        [Header("Ambient (trilight; gamma-space colours as in the Lighting window)")]
        // was sky (.16,.20,.30), equator (.095,.12,.185), ground (.04,.04,.06), intensity 1; Bellwether: (.105,.13,.16) / (.055,.06,.07) / (.025,.028,.03)
        public Color AmbientSky = new Color(.10f, .125f, .19f);
        public Color AmbientEquator = new Color(.055f, .07f, .11f);
        public Color AmbientGround = new Color(.025f, .027f, .04f);
        public float AmbientIntensity = .65f;   // integ-fb .75 -> .65 (hall too bright with the lantern off)

        [Header("Ambient fill for the castle surface shaders (linear). Kept low so shadows can reach real black.")]
        public Color FillSky = new Color(.005f, .007f, .012f);       // was (.010,.014,.024)
        public Color FillGround = new Color(.004f, .005f, .009f);    // was (.008,.010,.018); downward faces (vault undersides) pick this up

        [Header("Materials")]
        public bool UpgradeMaterials = true;
        public bool UpgradeProps = true;

        [Header("Vignette (camera post-render)")]
        public bool Vignette = true;
        [Range(0f, 1f)] public float VignetteStrength = .30f;   // was .22

        [Header("Ground fog (layered noise planes, castle-local)")]
        public bool GroundFog = true;
        // Per area (0 = great hall, 1 = crypt). The crypt fog is deliberately low, thin and neutral warm-grey: a blue haze there turned
        // the blood channel mauve (R/B 2.6 -> 1.2 in round 4).
        public Vector3[] FogAreaCentres = { new Vector3(0f, 0f, 21f), new Vector3(0f, -3.2f, 21f) };   // floor centre of the great hall, crypt
        public Vector2[] FogAreaSizes = { new Vector2(12f, 14f), new Vector2(12f, 14f) };
        public Color[] FogAreaColors = { new Color(.12f, .15f, .21f), new Color(.30f, .27f, .30f) };   // hall was (.34,.42,.58): a bright floor mist; wave FB darkened it
        public int[] FogAreaLayers = { 8, 3 };
        // wave FD (r13 critic: "no mist"): hall alpha .12 -> .16 with HallMistBrightness .5 -> .375, i.e. the same mean lift (colour x alpha .06) and
        // the exposure band unchanged, but the wisps have more contrast against the clear gaps so the mist reads.
        public float[] FogAreaAlpha = { .16f, .09f };            // per-layer alpha at the floor (fades x0.35 towards the top layer); fc-light: hall .15 -> .12
        // fc-light (2026-10-18; docs/haunted-castle/handoffs/fc-light.md): the hall floor mist is unlit and alpha-blended, so it lifts every floor
        // pixel under it to luma 15-25 whatever the lights do; in the fb2 captures it was most of the hall's lantern-off brightness (floor bands
        // 0-6 % near-black while the walls were black). Linear multiplier on FogAreaColors[0] (the hall layers only; the crypt mist is untouched).
        // 1 = wave FB mist, .5 = fc-light. Integrator knob: lower it if the hall is still too bright with the lantern off (S1 < 35 % near-black),
        // raise it if hall views go past 55 %.
        // wave FF: .375 -> .30, to pay for the moonlit north sky card, the blind gallery lancets and the table pool it adds to S1 (the r14 S1
        // frame re-projected onto the FF layout: near-black 30.4 % at .375 with the new glass, 32.5 % at .30; tools/ff_hall_reproject.py --mist).
        public float HallMistBrightness = .30f;
        public float[] FogAreaHeight = { 1.1f, .40f };           // top layer height above the floor, metres

        [Header("Room fog profile (exponential fog while the camera is in the crypt, castle-local y below CryptBelowY)")]
        public bool CryptFogProfile = true;
        public float CryptBelowY = -1.5f;
        public Color CryptFogColor = new Color(.20f, .18f, .22f);
        public float CryptFogDensity = .030f;

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
            "Castle_Stone", "Castle_OakPlank", "Castle_StoneWeathered", "Castle_StoneDark", "Castle_StoneMossy", "Castle_BlackStone", "Castle_FlagStone", "Castle_Oak", "Castle_Brass", "Castle_Oxblood",
            "Castle_Silver", "Castle_Iron", "Castle_Bone", "Castle_Paper", "Castle_Blood", "Castle_DressedStone", "Castle_Wax",
        };

        const string WorldPrefix = "HC castle / ";
        Material glBlend;
        bool warnedGl;
        readonly HashSet<Renderer> done = new HashSet<Renderer>();

        Camera view;
        float cryptBlend;     // 0 = hall fog, 1 = crypt fog
        float exteriorBlend;  // 0 = hall fog, 1 = exterior mist (camera outside the gate)

        // Smoothly swap the exponential fog between the room profiles as the camera goes down into the crypt or out through the gate.
        void Update()
        {
            if (!EnableFog || (!CryptFogProfile && !ExteriorFogProfile)) return;
            if (!view || !view.isActiveAndEnabled)
            {
                view = null;
                foreach (var c in Camera.allCameras) if (c.enabled && c.clearFlags != CameraClearFlags.Depth && c.cameraType == CameraType.Game) { view = c; break; }
            }
            if (!view) return;
            var root = Root ? Root : transform;
            var local = root.InverseTransformPoint(view.transform.position);
            cryptBlend = Mathf.MoveTowards(cryptBlend, CryptFogProfile && local.y < CryptBelowY ? 1f : 0f, Time.unscaledDeltaTime * 2f);
            exteriorBlend = Mathf.MoveTowards(exteriorBlend, ExteriorFogProfile && local.z < ExteriorBelowZ && local.y > CryptBelowY ? 1f : 0f, Time.unscaledDeltaTime * 2f);
            var hallColor = Color.Lerp(FogColor, ExteriorFogColor, exteriorBlend); float hallDensity = Mathf.Lerp(FogDensity, ExteriorFogDensity, exteriorBlend);
            RenderSettings.fogColor = Color.Lerp(hallColor, CryptFogColor, cryptBlend);
            RenderSettings.fogDensity = Mathf.Lerp(hallDensity, CryptFogDensity, cryptBlend);
            SetFill();
        }

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

        // The crypt keeps only CryptFillScale of the shader fill so it has real shadow (round 5: 4% near-black, evenly lit and orange).
        public float CryptFillScale = .62f;
        void SetFill()
        {
            float k = Mathf.Lerp(1f, CryptFillScale, cryptBlend);
            Shader.SetGlobalColor("_CastleFillSky", FillSky * k);
            Shader.SetGlobalColor("_CastleFillGround", FillGround * k);
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
            SetFill();
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
                    if (n == "Castle_Flame" && !CastleArt.FlameShaderOk) { replacement = CastleArt.FlameFallback(); if (replacement && replacement != m) { shared[i] = replacement; changed = true; } continue; }
                    if (n.StartsWith("Castle surface / Castle_Blood", StringComparison.Ordinal)) RefreshLiquid(m);
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

        // CastleWorld bakes the liquid material into the scene at Prepare time; push the current look onto it at runtime.
        static void RefreshLiquid(Material m)
        {
            if (!m.HasProperty("_EmissionColor")) return;
            m.SetColor("_EmissionColor", CastleArt.LiquidEmission);
        }

        // ---- the painted-over sun window --------------------------------------------------------------------------------
        void MakeSunWindow()
        {
            var parent = Root ? Root : transform;
            var existing = parent.Find("Painted sun window");
            if (existing) return;
            // round 13: the chapel has a real broken sunburst window now (CastleLayout.Windows, art ChapelSunburst): no painted quad over it
            if (parent.Find("Castle / " + CastleArt.Names.ChapelSunburst)) return;
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
            var quad = BuildQuad();
            for (int a = 0; a < FogAreaCentres.Length && a < FogAreaSizes.Length; a++)
            {
                int layers = Mathf.Clamp(a < FogAreaLayers.Length ? FogAreaLayers[a] : 6, 1, 12);
                float alpha = a < FogAreaAlpha.Length ? FogAreaAlpha[a] : .2f;
                float height = a < FogAreaHeight.Length ? FogAreaHeight[a] : 1f;
                Color colour = a < FogAreaColors.Length ? FogAreaColors[a] : new Color(.34f, .42f, .58f);
                if (a == 0) { var lin = colour.linear * Mathf.Max(0f, HallMistBrightness); colour = new Color(lin.r, lin.g, lin.b, colour.a).gamma; }
                for (int i = 0; i < layers; i++)
                {
                    float t = i / (float)Mathf.Max(1, layers - 1);
                    var m = new Material(shader) { name = "Castle ground fog " + a + "/" + i };
                    m.SetTexture("_MainTex", noise);
                    m.SetColor("_Color", colour);
                    m.SetFloat("_Alpha", alpha * Mathf.Lerp(1.4f, .5f, t));                      // denser near the floor
                    m.SetFloat("_Tile", .18f + .05f * i);
                    m.SetVector("_Scroll", new Vector4(.008f + .003f * i, (i % 2 == 0 ? .005f : -.006f) - .001f * i, 0f, 0f));
                    m.SetVector("_Fade", new Vector4(.9f, 2.8f, 0f, 0f));
                    var go = new GameObject("Fog layer " + a + "." + i);
                    go.transform.SetParent(group, false);
                    go.transform.localPosition = FogAreaCentres[a] + new Vector3(0f, Mathf.Lerp(.08f, height, t), 0f);
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
