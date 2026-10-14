using System.Collections.Generic;
using UnityEngine;

namespace HollowCurrent
{
    // Lights the carved rune inside a relic socket (RelicSocket*.fbx, its "Rune" part) once that relic has been laid on the altar:
    // found, and the player standing at the altar (Forge.AtAltar), or the sword already forged. It stays lit until the relics are
    // reset for a new run. The rune renderer is hidden until then. Castle only; it reads CastleRun.Forge and changes nothing in the rules.
    public sealed class SocketRune : MonoBehaviour
    {
        public Relic relic;
        Renderer[] runes;
        CastleRun run;
        float nextLookup;
        bool lit;
        MaterialPropertyBlock block;
        static readonly int ColorId=Shader.PropertyToID("_Color");
        void Awake()
        {
            var list=new List<Renderer>();
            foreach(var r in GetComponentsInChildren<Renderer>(true))if(r.name.StartsWith("Rune"))list.Add(r);
            runes=list.ToArray();
            foreach(var r in runes)r.enabled=false;
        }
        void Update()
        {
            if(runes==null||runes.Length==0)return;
            if(!run)
            {
                if(Time.unscaledTime<nextLookup)return;
                nextLookup=Time.unscaledTime+.5f;
                run=FindFirstObjectByType<CastleRun>();
                if(!run)return;
            }
            var forge=run.Forge;
            bool has=forge.HasRelic(relic);
            if(!has)lit=false;
            else if(forge.AtAltar||forge.HasSword)lit=true;
            if(runes[0].enabled!=lit)foreach(var r in runes)r.enabled=lit;
            if(!lit)return;
            // a slow breath on the emissive colour (the shared Castle_RuneGlow material is never written)
            if(block==null)block=new MaterialPropertyBlock();
            float k=.85f+.25f*Mathf.Sin(Time.time*1.7f+(int)relic*2.1f);
            block.SetColor(ColorId,new Color(.25f*k,.60f*k,1.6f*k,1));
            foreach(var r in runes)r.SetPropertyBlock(block);
        }
    }
}
