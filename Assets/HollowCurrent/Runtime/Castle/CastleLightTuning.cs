using System;
using System.Collections;
using UnityEngine;

namespace HollowCurrent
{
    // Round 3 (hc/look3): cold-moon vs warm-candle lighting, applied to the lights CastleWorld already built, so CastleWorld.cs
    // is untouched. Lights are found by the names CastleWorld gives them:
    //   "Candle light"        point, realtime   -> warm orange, brighter, longer range (pools of light on floors and columns)
    //   "Window moonlight"    spot, baked       -> cold blue, intensity raised per window class (see WindowMoonlight*)
    //   "Castle moonlight"    directional       -> cold blue, dimmer than the candles
    //   "Castle second moon"  directional       -> dim crimson
    // Idempotent per session (tuned light instance ids are remembered; names are left alone because World.cs finds "Castle second moon"
    // by name). Runtime caveat: lights with lightmapBakeType Baked (the window spots
    // and both moons) only change the picture after a castle re-bake; call CastleLightTuning.Tune(root) before baking
    // (HouseLighting.Bake / Prepare for MapId.Castle) so the new values are what gets baked. The realtime candle lights change
    // immediately, in play, with no re-bake.
    public sealed class CastleLightTuning : MonoBehaviour
    {
        public Transform Root;

        [Header("Candles (realtime point lights)")]
        public Color CandleColor = new Color(1.0f, 0.62f, 0.30f);
        public float CandleIntensityScale = 2.4f;    // existing 0.6-1.1 becomes 1.4-2.6
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

        static readonly System.Collections.Generic.HashSet<int> Done = new System.Collections.Generic.HashSet<int>();
        int tuned;

        void Start()
        {
            if (!Root)
            {
                var layout = FindFirstObjectByType<WorldLayout>();
                Root = layout ? layout.transform : null;
            }
            Apply();
            StartCoroutine(Later());
        }

        // CastleRun / floaters may add candle lights a moment after Start.
        IEnumerator Later()
        {
            yield return new WaitForSeconds(1.5f);
            Apply();
            yield return new WaitForSeconds(6f);
            Apply();
        }

        public void Apply()
        {
            try
            {
                Light[] lights = Root ? Root.GetComponentsInChildren<Light>(true) : FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                tuned += Tune(lights, this);
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
    }
}
