using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HollowCurrent
{
    // Cold-moon vs warm-candle lighting, applied to the lights CastleWorld already built (CastleWorld.cs is untouched), plus a
    // few extra fill lights that are NOT in World.Lamps (so the lamp budget, breaker, strobe and scare flicker never touch them).
    // Lights are found by the names CastleWorld gives them:
    //   "Candle light"        point, realtime   -> warm orange, longer range (pools of light on floors and columns)
    //   "Window moonlight"    spot, baked       -> cold blue, intensity raised per window class
    //   "Castle moonlight"    directional       -> cold blue, dimmer than the candles
    //   "Castle second moon"  directional       -> dim crimson
    // Wave FB (owner: "it's too well-lit", castle should be dark and lantern-led like Bellwether): every default below was lowered, the old
    // value is kept in the comment. Targets (tools/exposure_report.py --lantern off|on): interior near-black (luma < 8) 35-55 % with the
    // lantern off, 12-25 % with it on. The Investigator's grave-ash lantern is the main light: TorchIntensityScale / TorchRange below.
    // Round 4: tuning now runs in Awake. Game.Awake adds this component and THEN records every lamp's intensity in `lampLevels`,
    // which Game.Update writes back every frame; tuning in Start was silently overwritten with the original intensity.
    // Idempotent per session (tuned instance ids are remembered; names are left alone because World.cs finds "Castle second moon"
    // by name). Baked lights (window spots, both moons) only change the picture after a castle re-bake: call
    // CastleLightTuning.Tune(root) in HouseLighting.Bake before baking. Everything else is realtime.
    public sealed class CastleLightTuning : MonoBehaviour
    {
        public Transform Root;

        [Header("Candles (realtime point lights)")]
        public Color CandleColor = new Color(1.0f, 0.77f, 0.55f);   // less saturated than r4 (S1 was 15% warm, ref 1.6%)
        public float CandleIntensityScale = 0.26f;   // owner 2026-10-19 ("way too light, we need more darkness") .40 -> .26; integ-fb: .53 left the hall 25 % near-black (target 35-55); was .82 before wave FB
        public float CandleRangeScale = 1.3f;
        public float CandleMinRange = 6.5f;
        public float CandleMaxRange = 10f;
        // wave FF (r14 critic: "the banquet table is a small dark shape at the far end, legible only with the lantern on"; test: >= 15k table px
        // lantern off): the two light-only candles on the table (CastleWorld names them "Table candle light", 0.55 m over the top) get a short range
        // and ForcePixel, so they light the table, its candlesticks and chairs instead of losing the object's forward pixel slots to the floor
        // stands (r14: table median luma 8.7 lantern off, 22 lantern on). Range 3.5 keeps the pool on the table, its chairs and the dais.
        // ff-integ (one tuning pass, -hc-hall-table sweep): .7 -> 1.2 (S1 table px at luma >= 14: 7.6k -> 10.8k; 1.6 / 2.2 gave 12.3k / 12.8k, so the
        // 15k bar is out of reach by light alone: the far end of the table is dark wood seen edge-on).
        public float TableCandleIntensity = .8f;     // owner 2026-10-19: 1.2 -> .8
        public float TableCandleRange = 3.5f;

        [Header("Moonlight (cold blue; must stay dimmer than the candles)")]
        public Color MoonColor = new Color(.36f, .51f, .75f);
        public float WindowMoonlightCrypt = .34f;    // was .43 (r6; .17 originally) (FloorY < 0): the crypt only loses 20 %, its fog profile stays
        public float WindowMoonlightFloor = .36f;    // owner 2026-10-19 .48 -> .36; was .80 (-40 %)
        public float WindowMoonlightRose = .38f;     // owner 2026-10-19 .50 -> .38; integ-fb .66 -> .50; was 1.10 (the shadowed rose window)
        // fc-light: non-rose window spots that sit in the great hall (clerestories, the north lancets; castle-local |x| <= 6.8,
        // z 13.5-29, y 0-9.5). Default = WindowMoonlightFloor, i.e. no change this round; it is the integrator's hall-only fallback knob
        // (BAKED: re-bake the castle after editing it). Chapel, courtyard and crypt windows keep WindowMoonlightFloor / WindowMoonlightCrypt.
        public float HallWindowMoonlight = .22f;    // owner 2026-10-19 .30 -> .22; integ-fc fallback (fc-light table, S1 off 30.3 % < 31): .48 -> .30
        // fe-exterior (r13 open item 5, hall lantern-off from_doors 32.6 / skeleton_head 31.8 / side_east 30.4 / S1 30.4 against 35-55; the
        // HallWindowMoonlight knob above measured no change in integ-fc): the other row of fc-light's fallback table, the castle bake ambient.
        // Linear multiplier on HouseLighting's castle trilight (x0.68 of hc/look3), applied in Tune(root) below, the castle-only bake hook, so
        // Bellwether's bake receipt is untouched. BAKED: re-bake the castle after editing it. 1 = wave FD.
        // fe r1 measured x0.5 against x1: every hall, chapel and crypt view moved 0.0-0.2 points (lantern off and on). The castle interior is
        // enclosed, so the bake's sky ambient barely reaches it; the knob is back at 1 (wave FD lightmaps) and stays only as the documented row.
        public float BakeAmbientScale = 1f;
        public float MoonDirectional = .10f;         // was .16
        public Color SecondMoonColor = new Color(.62f, .24f, .34f);
        public float SecondMoonDirectional = .035f;  // was .05

        [Header("Chapel (round 6: S2 was 35% near-black)")]
        public Color ChapelMoonColor = new Color(.42f, .56f, .90f);
        public float ChapelMoonIntensity = .95f;      // was 1.1: the cold light pouring through the sun window above the altar stays the chapel's landmark
        public Vector3 ChapelMoonPosition = new Vector3(13.1f, 3.3f, 6.5f);
        public float ChapelCandleIntensity = .50f;    // owner 2026-10-19 .75 -> .50; integ-fb .6 -> .75 (offsets the lower CandleIntensityScale; chapel was in band); was 1.0: two warm pools at the altar, one over the nave
        public Vector3 ChapelCandleA = new Vector3(12.5f, 1.5f, 4.8f);
        public Vector3 ChapelCandleB = new Vector3(12.5f, 1.5f, 8.2f);
        public Vector3 ChapelNaveFill = new Vector3(9.0f, 2.6f, 6.5f);

        [Header("The grave-ash lantern (Investigator torch) is the main light source of the castle")]
        // Investigator.Torch is a 52 degree spot, range 20, intensity .85 x Brightness (Bellwether keeps exactly that). In the castle the player
        // reads the room 6-8 m ahead with it and barely without, so it is scaled up modestly here; the scale is stored on the Investigator
        // (TorchScale) so Bellwether is untouched. TorchRange 0 keeps the shared range.
        // fc-light2 (director on fc2: "lantern on floods everything", S1 7.4 / S2 9.4 / S3 7.6 % near-black against 12-25, the hall lit flat to
        // the far wall). fc-light made the lantern reach every surface (ForcePixel), so the FB compensation (scale 2.4, wide 100 deg cone, 18 m)
        // now floods the room. A lantern is a POOL: lower, narrower, shorter, softer edge, warmer.
        public float TorchIntensityScale = 1.15f;     // owner 2026-10-19 ("the lantern and surrounding lights are way too light") 1.8 -> 1.15; fc-light2 2.4 -> 1.8 (r2 1.7 / 75 deg / 15 m: hall on 17-23 % in band, chapel 32-43 too dark; r1 1.2 with 65 deg / 13 m: S1/S2/S3 on 28.9/38.7/31.3 % near-black, almost the lantern-off frame); integ-fb 1.35 -> 2.4 (2.04 absolute): at 1.35 the lantern moved S1-S3 near-black by under 3 points
        public float TorchRange = 11f;               // owner 2026-10-19 15 -> 11; fc-light2 18 -> 15 (r1 13) (the far half of the 14 m hall falls back to dark); integ-fc 24 -> 18; integ-fb 0 (keep 20) -> 24
        public float TorchSpotAngle = 72f;           // owner 2026-10-19 88 -> 72; fc-light2 100 -> 88 (r1 65, r2 75: the chapel pews and side walls fell outside the cone); fc-light 80 -> 100; integ-fb: 80 (Bellwether keeps 52); 0 keeps the shared angle
        public float TorchInnerFraction = .35f;      // fc-light2: inner/outer angle (was .6) for a soft pool edge (used where the pipeline reads innerSpotAngle)
        public Color TorchColor = new Color(1f, .80f, .58f);   // fc-light2: a little warmer than the shared torch (1, .89, .72); castle only
        // fc-light: the torch is an Auto light, so in forward rendering it only reaches objects where it ranks among the Ultra tier's 4 pixel
        // lights. Diffing the fb2 lantern-on/off captures showed it lit the hall vault, candelabras and far wall but NOT the hall floor, near
        // walls, skeleton, chapel floor, pews or altar (those objects already carry the ForcePixel fills plus nearer candles). ForcePixel makes
        // the lantern light every surface in its cone (castle only; Bellwether's torch keeps Auto).
        public bool TorchForcePixel = true;
        // owner 2026-10-19: the warm glow around the lantern in her hand lit the whole room (Investigator default 1.6 / 5.5 m). The Count's hide
        // presentation still raises it to CountClaudeProfile.HideLanternGlow for its frames.
        public float LanternGlowIntensity = .75f;
        public float LanternGlowRange = 3.2f;

        [Header("Extra fill lights (castle-local positions; realtime, no shadows)")]
        public bool AddFillLights = true;
        public Color VaultFillColor = new Color(.42f, .55f, .90f);
        public float VaultFillIntensity = .03f;       // owner 2026-10-19 .05 -> .03; integ-fb .10 -> .05; was .32: faint cold light under the great-hall vault so the ribs and ceiling just read
        public Vector3 VaultFillPosition = new Vector3(0f, 7.6f, 21f);
        public float VaultFillRange = 12f;
        public Color CryptCandleColor = new Color(1.0f, .66f, .38f);
        public float CryptCandleIntensity = .6f;      // was .85; ONE warm pool on the west curb (r5: four pools at 1.6 = 4.2% near-black and 16% warm; r6: four at 0.65 = 41.9%)
        public float CryptCandleRange = 5f;
        public Color CryptColdColor = new Color(.34f, .44f, .80f);
        public float CryptColdIntensity = .22f;       // owner 2026-10-19 .32 -> .22; integ-fb .42 -> .32 (S3 32 % near-black); was .55: cold base so the room reads blue with one warm accent
        public Vector3 CryptColdPosition = new Vector3(0f, -1.3f, 22.5f);
        public Color CryptRuneColor = new Color(.60f, .32f, .90f);
        public float CryptRuneIntensity = .9f;       // violet rune glow over the sarcophagus
        public Color CryptBloodColor = new Color(.95f, .12f, .10f);
        public float CryptBloodIntensity = 1.2f;      // red glow just above the channel so the liquid blushes and spec highlights read

        [Header("Warm fills for the dark shots (round 5)")]
        public Color WarmFillColor = new Color(1.0f, .70f, .45f);
        public float CourtyardFillIntensity = .7f;    // owner 2026-10-19 1.3 -> .7; was 2.6: two candle pools by the courtyard/gatehouse where S6 stands the Count
        public float CourtyardFillRange = 9f;
        public Vector3 CourtyardFillA = new Vector3(-4.6f, 2.0f, 9.5f);
        public Vector3 CourtyardFillB = new Vector3(-1.5f, 2.0f, 12.5f);
        public float BalconyFillIntensity = .4f;       // owner 2026-10-19 .7 -> .4; was 1.4: warm rim on the perch above the hall for S5 and the hunt's first frames
        public float BalconyFillRange = 8f;
        public Vector3 BalconyFillPosition = new Vector3(-3.5f, 4.9f, 19.5f);
        public Vector3 BalconyFillPositionB = new Vector3(-5.2f, 4.8f, 22.5f);   // second candle pool by the rose-window ledge

        [Header("Exterior facade fill (on only while the camera is outside the gate)")]
        // fe-exterior (S4 frame time mean 16.7 / p99 23.1 ms against p99 <= 16.7): the facade, keep and bridge fills are ForcePixel lights with
        // ranges 8-50 m. From outside (S4 on the far landing, battlement_out) their volumes cover the whole castle, so every interior renderer in
        // the view frustum (walls, props and candles behind the solid curtain wall: hidden, but still drawn) took one forward-add pass per fill.
        // fe r1 -hc-exterior-profile at the S4 eye: 10,616 batches; with these three fills culled 7,900, render thread 11.6 -> 8.7 ms, every
        // frame 11.11 ms (p99 23.0 -> 11.1). Now the DEEP interior's render-only renderers sit on InteriorLayer, which the outside fills do not
        // light. Kept on their layer (so lit exactly as in wave FD): everything outside the shell, renderers within OuterFaceBand of its outer
        // faces, the gatehouse and courtyard block seen through the gate (|x| <= VisibleHalfX, z <= VisibleMaxZ), the front half's roofs and
        // wall walks seen from the battlements (bottom above RoofWalkY - .5 and z <= VisibleMaxZ), characters, and any renderer with a collider
        // (physics untouched). fe r2 kept only the outer-face band and darkened the inner parapets in battlement_in; r3 adds the two zones.
        // Every camera and every other light still renders and lights InteriorLayer, except the deep-interior rule below.
        public const int InteriorLayer = 27;
        public float OuterFaceBand = 1.0f;
        public float VisibleHalfX = 6.5f, VisibleMaxZ = 14.5f, RoofWalkY = 5f;
        // fe r3: with the fills masked, S4 still drew 9,123 batches (render thread 10.2 ms, p99 22.2): the interior's own realtime lights (candles,
        // hall/chapel/crypt/balcony fills) lit the deep interior behind the curtain wall (profile off_lights_interior: 3,557 batches). While the
        // camera is outside the gate AND the gate chain is shut (no Gate leaf ajar, or no Gatehouse great door ajar), nothing behind the
        // gatehouse can be seen, so those lights skip InteriorLayer too; the moment both are open, or the camera steps inside, the masks return.
        public bool MaskDeepInteriorOutside = true;
        [NonSerialized] public bool DeepMaskSuspended;   // CaptureMode's -hc-exterior-profile lifts the mask for one variant
        public Color FacadeColor = new Color(.40f, .55f, .88f);
        public float FacadeIntensity = 1.05f;
        public float FacadeRange = 34f;
        public float FacadeAngle = 80f;
        public Vector3 FacadePosition = new Vector3(0f, 9f, -18f);
        public Vector3 FacadeTarget = new Vector3(0f, 5f, 0f);
        public Color KeepColor = new Color(.40f, .55f, .90f);
        public float KeepIntensity = .35f;            // cold moon fill higher up, aimed at the keep and spires
        public float KeepRange = 50f;
        public float KeepAngle = 60f;
        public Vector3 KeepPosition = new Vector3(0f, 18f, -24f);
        public Vector3 KeepTarget = new Vector3(0f, 10f, 4f);
        public float BridgeFillIntensity = .3f;       // cold pool over the lowered drawbridge planks
        public Vector3 BridgeFillPosition = new Vector3(0f, 2.5f, -4.5f);
        public float FacadeMaxCameraZ = .5f;          // castle-local z: the gate wall is at 0

        static readonly HashSet<int> Done = new HashSet<int>();
        readonly List<Light> outsideLights = new List<Light>();   // facade, keep and bridge fills: on only while the camera is outside the gate
        Camera view;
        bool fillsBuilt;

        void Awake()
        {
            if (!Root)
            {
                var layout = FindFirstObjectByType<WorldLayout>();
                Root = layout ? layout.transform : null;
            }
            Apply();
            BuildFills();
        }

        void Start()
        {
            StartCoroutine(Later());
        }

        // CastleRun / floaters may add candle lights a moment after Awake. (Late lights are not in Game.lampLevels, so Game leaves
        // their intensity alone; they are tuned here for colour and range as well.)
        IEnumerator Later()
        {
            yield return new WaitForSeconds(1.5f);
            Apply();
            yield return new WaitForSeconds(6f);
            Apply();
            if (fillsBuilt && outsideLights.Count > 0) TagInterior("later");   // props CastleRun placed after Awake
        }

        void Update()
        {
            if (outsideLights.Count == 0) return;
            if (!view || !view.isActiveAndEnabled)
            {
                view = null;
                foreach (var c in Camera.allCameras) if (c.enabled && c.clearFlags != CameraClearFlags.Depth && c.cameraType == CameraType.Game) { view = c; break; }
            }
            bool outside = false;
            if (view)
            {
                var local = Root ? Root.InverseTransformPoint(view.transform.position) : view.transform.position;
                outside = local.z < FacadeMaxCameraZ;
            }
            foreach (var l in outsideLights) if (l && l.enabled != outside) l.enabled = outside;
            // fe-next: the castle's "Gate" is the drawbridge (no leaves; fe r3 logged gate=0 and the mask never switched on). From outside the
            // gate arch shows the gatehouse (kept lit, VisibleHalfX/VisibleMaxZ); the deep interior shows only through an open gatehouse exit.
            bool deep = MaskDeepInteriorOutside && !DeepMaskSuspended && outside && gatehouseDoors.Count > 0 && !AnyAjar(gatehouseDoors);
            if (deep != deepMasked) ApplyDeepMask(deep);
        }

        // ---- fe r3: deep-interior mask for the interior lights while the camera is outside and the gate chain is shut ------------------
        readonly List<Door> gateDoors = new List<Door>(), gatehouseDoors = new List<Door>();
        static readonly string[] GatehouseExits = { "Gatehouse great doors", "Gatehouse kitchen passage", "Gatehouse guard door" };   // CastleLayout.Doors
        readonly Dictionary<Light, int> interiorMasks = new Dictionary<Light, int>();
        bool deepMasked; int deepLogs;
        static bool AnyAjar(List<Door> doors)
        {
            foreach (var d in doors) if (d && (d.Open || Quaternion.Angle(d.transform.localRotation, d.Closed) > 3f)) return true;
            return false;
        }
        void FindGateDoors()
        {
            gateDoors.Clear(); gatehouseDoors.Clear();
            if (!Root) return;
            foreach (var it in Root.GetComponentsInChildren<Interactable>(true))
            {
                if (it.Kind != InteractKind.Door || !it.Door) continue;
                if (it.Label == "Gate") { if (!gateDoors.Contains(it.Door)) gateDoors.Add(it.Door); }
                foreach (var exit in GatehouseExits)
                    if ((it.Label == exit || it.Door.name.Contains(exit)) && !gatehouseDoors.Contains(it.Door)) gatehouseDoors.Add(it.Door);
            }
            Debug.Log("HC_DEEP_INTERIOR_DOORS gate=" + gateDoors.Count + " gatehouse_exits=" + gatehouseDoors.Count + (gatehouseDoors.Count == 0 ? " (no gatehouse exit found: the deep-interior mask stays off)" : ""));
        }
        void ApplyDeepMask(bool on)
        {
            deepMasked = on; int n = 0;
            if (Root)
                foreach (var l in Root.GetComponentsInChildren<Light>(true))
                {
                    if (!l || l.type == LightType.Directional || outsideLights.Contains(l) || l.name == "Brazier light" || l.name == "Window moonlight") continue;
                    if (on) { if (!interiorMasks.ContainsKey(l)) interiorMasks[l] = l.cullingMask; l.cullingMask = interiorMasks[l] & ~(1 << InteriorLayer); n++; }
                    else if (interiorMasks.TryGetValue(l, out var m)) { l.cullingMask = m; n++; }
                }
            if (deepLogs++ < 8) Debug.Log("HC_DEEP_INTERIOR_MASK on=" + on + " lights=" + n + " (interior lights skip InteriorLayer while outside with the gate chain shut)");
        }

        public void Apply()
        {
            try
            {
                Light[] lights = Root ? Root.GetComponentsInChildren<Light>(true) : FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Tune(lights, this);
                ApplyTorch();
            }
            catch (Exception e) { Debug.LogWarning("[CastleLightTuning] skipped: " + e.Message); }
        }

        // The lantern: scale (and optionally lengthen) the Investigator's torch for the castle. Safe to repeat; the Investigator may not exist yet
        // in the first Apply (the later passes catch it).
        public void ApplyTorch()
        {
            var player = FindFirstObjectByType<Investigator>();
            if (!player) return;
            player.TorchScale = TorchIntensityScale;
            if (TorchRange > 0 && player.Torch) player.Torch.range = TorchRange;
            if (TorchSpotAngle > 0 && player.Torch) { player.Torch.spotAngle = TorchSpotAngle; player.Torch.innerSpotAngle = TorchSpotAngle * TorchInnerFraction; }
            if (player.Torch && TorchColor.maxColorComponent > 0) player.Torch.color = TorchColor;
            if (TorchForcePixel && player.Torch) player.Torch.renderMode = LightRenderMode.ForcePixel;
            player.LanternGlowIntensity = LanternGlowIntensity; player.LanternGlowRange = LanternGlowRange;
        }

        // Editor/bake entry point: tune every light under the castle root with the default values.
        public static int Tune(Transform root)
        {
            var defaults = new GameObject("CastleLightTuning defaults") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var t = defaults.AddComponent<CastleLightTuning>();
                int n = Tune(root ? root.GetComponentsInChildren<Light>(true) : new Light[0], t);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    // fe-exterior: the castle bake ambient knob and the castle's occlusion data (CastleExteriorCost.BakeOcclusion) ride on this
                    // castle-only hook; HouseLighting has already set the castle trilight and saves the scene after the bake.
                    var k = Mathf.Max(0f, t.BakeAmbientScale);
                    Color Scale(Color c) { var l = c.linear * k; return new Color(l.r, l.g, l.b, c.a).gamma; }
                    RenderSettings.ambientSkyColor = Scale(RenderSettings.ambientSkyColor);
                    RenderSettings.ambientEquatorColor = Scale(RenderSettings.ambientEquatorColor);
                    RenderSettings.ambientGroundColor = Scale(RenderSettings.ambientGroundColor);
                    Debug.Log("HC_CASTLE_BAKE_AMBIENT scale=" + k.ToString("F2") + " sky=" + RenderSettings.ambientSkyColor + " equator=" + RenderSettings.ambientEquatorColor + " ground=" + RenderSettings.ambientGroundColor);
                    // fe r1 baked Unity occlusion data here: S4 drew 10,707 batches without it and 10,616 with it (no effect: the door leaves and
                    // the gate are portals and the window openings leak), so the castle bakes none (CastleExteriorCost.BakeOcclusion is kept for a
                    // later attempt with Occlusion Areas; the prepare's new scene carries no occlusion data).
                    Debug.Log("HC_OCCLUSION baked=False (fe r2: no measured effect in r1)");
                }
#endif
                return n;
            }
            finally { if (Application.isPlaying) Destroy(defaults); else DestroyImmediate(defaults); }
        }

        static int Tune(Light[] lights, CastleLightTuning v)
        {
            int n = 0;
            foreach (var l in lights)
            {
                if (!l || Done.Contains(l.GetInstanceID())) continue;
                string baseName = l.name;
                if (baseName == "Table candle light" && l.type == LightType.Point)
                {
                    l.color = v.CandleColor; l.intensity = v.TableCandleIntensity; l.range = v.TableCandleRange;
                    l.renderMode = LightRenderMode.ForcePixel; l.shadows = LightShadows.None;
                }
                else if (baseName == "Candle light" && l.type == LightType.Point)
                {
                    l.color = v.CandleColor;
                    l.intensity *= v.CandleIntensityScale;
                    l.range = Mathf.Clamp(l.range * v.CandleRangeScale, v.CandleMinRange, v.CandleMaxRange);
                }
                else if (baseName == "Window moonlight")
                {
                    l.color = v.MoonColor;
                    float old = l.intensity;
                    l.intensity = old >= .45f ? v.WindowMoonlightRose : old >= .25f ? (InHall(l.transform.localPosition) ? v.HallWindowMoonlight : v.WindowMoonlightFloor) : v.WindowMoonlightCrypt;
                }
                else if (baseName == "Castle moonlight" && l.type == LightType.Directional)
                {
                    l.color = v.MoonColor; l.intensity = v.MoonDirectional;
                }
                else if (baseName == "Castle second moon" && l.type == LightType.Directional)
                {
                    l.color = v.SecondMoonColor; l.intensity = v.SecondMoonDirectional;
                }
                else continue;
                Done.Add(l.GetInstanceID());
                n++;
            }
            if (n > 0) Debug.Log("[CastleLightTuning] tuned " + n + " lights");
            return n;
        }

        // Window spots are children of the castle root, so localPosition is castle-local. The hall is x -6..6, z 14.3..28; window spots
        // sit 0.6 m outside their wall plane (|x| 6.6 for the clerestories, z 28.6 for the north wall).
        static bool InHall(Vector3 p) => Mathf.Abs(p.x) <= 6.8f && p.z >= 13.5f && p.z <= 29f && p.y >= 0f && p.y <= 9.5f;

        // ---- extra fill lights -------------------------------------------------------------------------------------------
        void BuildFills()
        {
            if (!AddFillLights || fillsBuilt) return;
            fillsBuilt = true;
            var parent = Root ? Root : transform;
            if (parent.Find("Castle fill lights")) return;
            var group = new GameObject("Castle fill lights").transform;
            group.SetParent(parent, false);
            Fill(group, "Hall vault fill", VaultFillPosition, VaultFillColor, VaultFillIntensity, VaultFillRange);
            const float cy = -3.2f + .9f;
            Fill(group, "Crypt candle pool", new Vector3(-3.45f, cy, 19.9f), CryptCandleColor, CryptCandleIntensity, CryptCandleRange);
            Fill(group, "Crypt cold fill", CryptColdPosition, CryptColdColor, CryptColdIntensity, 8f);
            Fill(group, "Crypt rune glow", new Vector3(0f, -3.2f + 1.7f, 22.5f), CryptRuneColor, CryptRuneIntensity, 5f);
            Fill(group, "Crypt blood glow", new Vector3(0f, -3.2f + .5f, 20.1f), CryptBloodColor, CryptBloodIntensity, 4f);
            Fill(group, "Courtyard candle fill", CourtyardFillA, WarmFillColor, CourtyardFillIntensity, CourtyardFillRange);
            Fill(group, "Courtyard candle fill", CourtyardFillB, WarmFillColor, CourtyardFillIntensity, CourtyardFillRange);
            Fill(group, "Balcony warm fill", BalconyFillPosition, WarmFillColor, BalconyFillIntensity, BalconyFillRange);
            Fill(group, "Balcony warm fill", BalconyFillPositionB, WarmFillColor, BalconyFillIntensity * .7f, BalconyFillRange - 1f);
            // chapel: cold moon through the sun window + warm candle pools
            var moon = Fill(group, "Chapel sun-window moonlight", ChapelMoonPosition, ChapelMoonColor, ChapelMoonIntensity, 10f);
            moon.type = LightType.Spot; moon.spotAngle = 110f; moon.transform.localRotation = Quaternion.LookRotation(new Vector3(-1f, -.35f, 0f));
            Fill(group, "Chapel altar candle fill", ChapelCandleA, WarmFillColor, ChapelCandleIntensity, 5f);
            Fill(group, "Chapel altar candle fill", ChapelCandleB, WarmFillColor, ChapelCandleIntensity, 5f);
            Fill(group, "Chapel nave candle fill", ChapelNaveFill, WarmFillColor, ChapelCandleIntensity * .7f, 7f);
            // exterior (only while the camera is outside the gate)
            var facade = Spot(group, "Facade moon fill", FacadePosition, FacadeTarget, FacadeColor, FacadeIntensity, FacadeRange, FacadeAngle);
            var keep = Spot(group, "Keep moon fill", KeepPosition, KeepTarget, KeepColor, KeepIntensity, KeepRange, KeepAngle);
            var bridge = Fill(group, "Bridge moon fill", BridgeFillPosition, FacadeColor, BridgeFillIntensity, 8f);
            foreach (var l in new[] { facade, keep, bridge }) { l.enabled = false; l.cullingMask &= ~(1 << InteriorLayer); outsideLights.Add(l); }
            TagInterior("build");
            FindGateDoors();
        }

        // Interior render-only renderers -> InteriorLayer (see the facade header). Idempotent: only Default-layer renderers are moved.
        int TagInterior(string stage)
        {
            if (!Root) return 0;
            int moved = 0, face = 0, collider = 0;
            var min = CastleExteriorCost.ShellMin; var max = CastleExteriorCost.ShellMax; float band = OuterFaceBand;
            foreach (var r in Root.GetComponentsInChildren<Renderer>(true))
            {
                if (!r || r.gameObject.layer != 0 || r is ParticleSystemRenderer || r is SkinnedMeshRenderer || r.GetComponentInParent<Animator>()) continue;   // characters keep their own layer logic (Presence)
                if (CastleExteriorCost.Group(r, Root) != "interior") continue;
                if (r.GetComponent<Collider>()) { collider++; continue; }
                var b = r.bounds;
                Vector3 lo = Root.InverseTransformPoint(b.min), hi = Root.InverseTransformPoint(b.max);
                var bmin = Vector3.Min(lo, hi); var bmax = Vector3.Max(lo, hi);
                if (bmin.x < min.x + band || bmax.x > max.x - band || bmin.z < min.z + band || bmax.z > max.z - band) { face++; continue; }
                if (bmin.z <= VisibleMaxZ && ((bmax.x >= -VisibleHalfX && bmin.x <= VisibleHalfX) || bmin.y >= RoofWalkY - .5f)) { face++; continue; }   // gatehouse, courtyard, front roofs and walks
                r.gameObject.layer = InteriorLayer; moved++;
            }
            Debug.Log("HC_EXTERIOR_FILL_MASK stage=" + stage + " interior_layer=" + InteriorLayer + " moved=" + moved + " outer_face_kept=" + face + " collider_kept=" + collider + " (outside fills do not light InteriorLayer)");
            return moved;
        }

        static Light Spot(Transform parent, string name, Vector3 pos, Vector3 target, Color color, float intensity, float range, float angle)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.LookRotation((target - pos).normalized);
            var l = go.AddComponent<Light>();
            l.type = LightType.Spot; l.color = color; l.intensity = intensity; l.range = range; l.spotAngle = angle;
            l.shadows = LightShadows.None; l.renderMode = LightRenderMode.ForcePixel;
            return l;
        }

        static Light Fill(Transform parent, string name, Vector3 localPos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = color; l.intensity = intensity; l.range = range; l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.ForcePixel;   // forward rendering gives each object only 4 pixel lights; these must not lose to the candle lamps
            return l;
        }
    }
}
