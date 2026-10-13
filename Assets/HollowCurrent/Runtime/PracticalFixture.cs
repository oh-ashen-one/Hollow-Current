using UnityEngine;
namespace HollowCurrent
{
    // Opal diffusers transmit light and extinguish with their actual lamp.
    public sealed class PracticalFixture : MonoBehaviour
    {
        public Light Source;
        Renderer[] surfaces;
        MaterialPropertyBlock block;
        void Awake(){surfaces=GetComponentsInChildren<Renderer>();block=new MaterialPropertyBlock();}
        void LateUpdate()
        {
            if(!Source)return;
            foreach(var r in surfaces)
            {
                if(r.name!="Shade")continue;
                block.SetColor("_EmissionColor",Source.enabled?Source.color*Mathf.Clamp01(Source.intensity)*.7f:Color.black);r.SetPropertyBlock(block);
            }
        }
    }
}
