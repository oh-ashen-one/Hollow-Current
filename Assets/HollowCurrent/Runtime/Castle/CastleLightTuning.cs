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
        public Color CandleColor = new Color(1.0f, 0.70f, 0.42f);
        public float CandleIntensityScale = 1.0f;
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

        [Header("Extra fill lights (castle-local positions; realtime, no shadows)")]
        public bool AddFillLights = true;
        public Color VaultFillColor = new Color(.42f, .55f, .90f);
        public float VaultFillIntensity = .35f;       // faint cold light under the great-hall vault so the ribs and ceiling read
        public Vector3 VaultFillPosition = new Vector3(0f, 7.0f, 21f);
        public float VaultFillRange = 10f;
        public Color CryptCandleColor = new Color(1.0f, .66f, .38f);
        public float CryptCandleIntensity = 1.3f;     // four warm candle pools on the channel curbs
        public float CryptCandleRange = 5f;
        public Color CryptRuneColor = new Color(.60f, .32f, .90f);
        public float CryptRuneIntensity = .9f;       // violet rune glow over the sarcophagus
        public Color CryptBloodColor = new Color(.95f, .12f, .10f);
        public float CryptBloodIntensity = .9f;      // red glow just above the channel so the liquid blushes and spec highlights read

        [Header("Exterior facade fill (on only while the camera is outside the gate)")]
        public Color FacadeColor = new Color(.40f, .55f, .88f);
        public float FacadeIntensity = 1.2f;
        public float FacadeRange = 34f;
        public float FacadeAngle = 80f;
        public Vector3 FacadePosition = new Vector3(0f, 9f, -18f);
        public Vector3 FacadeTarget = new Vector3(0f, 5f, 0f);
        public float FacadeMaxCameraZ = .5f;          // castle-local z: the gate wall is at 0

        static readonly HashSet<int> Done = new HashSet<int>();
        Light facade;
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
            if (!facade) return;
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
            if (facade.enabled != outside) facade.enabled = outside;
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
            foreach (var x in new[] { -3.45f, 3.45f })
                foreach (var z in new[] { 19.9f, 25.1f })
                    Fill(group, "Crypt candle pool", new Vector3(x, cy, z), CryptCandleColor, CryptCandleIntensity, CryptCandleRange);
            Fill(group, "Crypt rune glow", new Vector3(0f, -3.2f + 1.7f, 22.5f), CryptRuneColor, CryptRuneIntensity, 5f);
            Fill(group, "Crypt blood glow", new Vector3(0f, -3.2f + .5f, 20.1f), CryptBloodColor, CryptBloodIntensity, 4f);
            var go = new GameObject("Facade moon fill");
            go.transform.SetParent(group, false);
            go.transform.localPosition = FacadePosition;
            go.transform.localRotation = Quaternion.LookRotation((FacadeTarget - FacadePosition).normalized);
            facade = go.AddComponent<Light>();
            facade.type = LightType.Spot; facade.color = FacadeColor; facade.intensity = FacadeIntensity; facade.range = FacadeRange;
            facade.spotAngle = FacadeAngle; facade.shadows = LightShadows.None; facade.enabled = false;
        }

        static Light Fill(Transform parent, string name, Vector3 localPos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = color; l.intensity = intensity; l.range = range; l.shadows = LightShadows.None;
            return l;
        }
    }
}
