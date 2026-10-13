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
    // Round 4: tuning now runs in Awake. Game.Awake adds this component and THEN records every lamp's intensity in `lampLevels`,
    // which Game.Update writes back every frame; tuning in Start was silently overwritten with the original intensity.
    // Idempotent per session (tuned instance ids are remembered; names are left alone because World.cs finds "Castle second moon"
    // by name). Baked lights (window spots, both moons) only change the picture after a castle re-bake: call
    // CastleLightTuning.Tune(root) in HouseLighting.Bake before baking. Everything else is realtime.
    public sealed class CastleLightTuning : MonoBehaviour
    {
        public Transform Root;

        [Header("Candles (realtime point lights)")]
        public Color CandleColor = new Color(1.0f, 0.74f, 0.50f);   // less saturated than r4 (S1 was 15% warm, ref 1.6%)
        public float CandleIntensityScale = 0.9f;
        public float CandleRangeScale = 1.3f;
        public float CandleMinRange = 6.5f;
        public float CandleMaxRange = 10f;

        [Header("Moonlight (cold blue; must stay dimmer than the candles)")]
        public Color MoonColor = new Color(.36f, .51f, .75f);
        public float WindowMoonlightCrypt = .43f;    // was .17 (FloorY < 0)
        public float WindowMoonlightFloor = .80f;    // was .36
        public float WindowMoonlightRose = 1.10f;    // was .50 (the shadowed rose window)
        public float MoonDirectional = .16f;         // was .22
        public Color SecondMoonColor = new Color(.62f, .24f, .34f);
        public float SecondMoonDirectional = .05f;   // was .06

        [Header("Chapel (round 6: S2 was 35% near-black)")]
        public Color ChapelMoonColor = new Color(.42f, .56f, .90f);
        public float ChapelMoonIntensity = 1.1f;      // cold light pouring through the sun window above the altar
        public Vector3 ChapelMoonPosition = new Vector3(13.1f, 3.3f, 6.5f);
        public float ChapelCandleIntensity = 1.0f;    // two warm pools at the altar, one over the nave
        public Vector3 ChapelCandleA = new Vector3(12.5f, 1.5f, 4.8f);
        public Vector3 ChapelCandleB = new Vector3(12.5f, 1.5f, 8.2f);
        public Vector3 ChapelNaveFill = new Vector3(9.0f, 2.6f, 6.5f);

        [Header("Extra fill lights (castle-local positions; realtime, no shadows)")]
        public bool AddFillLights = true;
        public Color VaultFillColor = new Color(.42f, .55f, .90f);
        public float VaultFillIntensity = .42f;       // faint cold light under the great-hall vault so the ribs and ceiling read
        public Vector3 VaultFillPosition = new Vector3(0f, 7.6f, 21f);
        public float VaultFillRange = 12f;
        public Color CryptCandleColor = new Color(1.0f, .66f, .38f);
        public float CryptCandleIntensity = 1.1f;     // ONE warm pool on the west curb (r5: four pools at 1.6 = 4.2% near-black and 16% warm; r6: four at 0.65 = 41.9%)
        public float CryptCandleRange = 5f;
        public Color CryptColdColor = new Color(.34f, .44f, .80f);
        public float CryptColdIntensity = .55f;       // cold base so the room reads blue with one warm accent
        public Vector3 CryptColdPosition = new Vector3(0f, -1.3f, 22.5f);
        public Color CryptRuneColor = new Color(.60f, .32f, .90f);
        public float CryptRuneIntensity = .9f;       // violet rune glow over the sarcophagus
        public Color CryptBloodColor = new Color(.95f, .12f, .10f);
        public float CryptBloodIntensity = 1.2f;      // red glow just above the channel so the liquid blushes and spec highlights read

        [Header("Warm fills for the dark shots (round 5)")]
        public Color WarmFillColor = new Color(1.0f, .70f, .45f);
        public float CourtyardFillIntensity = 2.2f;   // two candle pools by the courtyard/gatehouse where S6 stands the Count
        public float CourtyardFillRange = 9f;
        public Vector3 CourtyardFillA = new Vector3(-4.6f, 2.0f, 9.5f);
        public Vector3 CourtyardFillB = new Vector3(-1.5f, 2.0f, 12.5f);
        public float BalconyFillIntensity = 1.4f;      // warm rim on the perch above the hall for S5 and the hunt's first frames
        public float BalconyFillRange = 8f;
        public Vector3 BalconyFillPosition = new Vector3(-3.5f, 4.9f, 19.5f);
        public Vector3 BalconyFillPositionB = new Vector3(-5.2f, 4.8f, 22.5f);   // second candle pool by the rose-window ledge

        [Header("Exterior facade fill (on only while the camera is outside the gate)")]
        public Color FacadeColor = new Color(.40f, .55f, .88f);
        public float FacadeIntensity = .8f;
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
        }

        public void Apply()
        {
            try
            {
                Light[] lights = Root ? Root.GetComponentsInChildren<Light>(true) : FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Tune(lights, this);
            }
            catch (Exception e) { Debug.LogWarning("[CastleLightTuning] skipped: " + e.Message); }
        }

        // Editor/bake entry point: tune every light under the castle root with the default values.
        public static int Tune(Transform root)
        {
            var defaults = new GameObject("CastleLightTuning defaults") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var t = defaults.AddComponent<CastleLightTuning>();
                return Tune(root ? root.GetComponentsInChildren<Light>(true) : new Light[0], t);
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
                if (baseName == "Candle light" && l.type == LightType.Point)
                {
                    l.color = v.CandleColor;
                    l.intensity *= v.CandleIntensityScale;
                    l.range = Mathf.Clamp(l.range * v.CandleRangeScale, v.CandleMinRange, v.CandleMaxRange);
                }
                else if (baseName == "Window moonlight")
                {
                    l.color = v.MoonColor;
                    float old = l.intensity;
                    l.intensity = old >= .45f ? v.WindowMoonlightRose : old >= .25f ? v.WindowMoonlightFloor : v.WindowMoonlightCrypt;
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
            foreach (var l in new[] { facade, keep, bridge }) { l.enabled = false; outsideLights.Add(l); }
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
